using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Us
{
    /// <summary>
    /// Test Report Setup - the web equivalent of the VFP US0320 screen.
    ///
    /// Maintains the predefined rich-text layout for each ultrasound investigation. A template
    /// holds {{Placeholder}} tokens which Result Entry resolves against the real patient at
    /// load time, so editing a template here never touches an already-issued report.
    /// </summary>
    public class ReportSetupModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;
        private readonly IUsReportTemplateService _templates;

        public ReportSetupModel(ApplicationDbContext db, IAppSession appSession, IUsReportTemplateService templates)
        {
            _db = db;
            _appSession = appSession;
            _templates = templates;
        }

        private static readonly Regex TokenPattern =
            new(@"\{\{\s*(?<key>[A-Za-z0-9_]+)\s*\}\}", RegexOptions.Compiled);

        [BindProperty] public string Filter { get; set; }
        [BindProperty] public long? EditingId { get; set; }
        [BindProperty] public long? TemplateUsServiceId { get; set; }
        [BindProperty] public string TemplateName { get; set; }
        [BindProperty] public string TemplateDescription { get; set; }
        [BindProperty] public string TemplateHtml { get; set; }
        [BindProperty] public bool TemplateIsDefault { get; set; }

        [BindProperty] public string NewMedicalTerm { get; set; }
        [BindProperty] public string NewMedicalCategory { get; set; }
        [BindProperty] public string NewMedicalDefinition { get; set; }
        [BindProperty] public string NewMedicalSynonyms { get; set; }

        public List<UsReportTemplate> Templates { get; set; } = new();
        public List<UsService> Services { get; set; } = new();
        public List<SelectListItem> ServiceOptions { get; set; } = new();
        public List<MedicalTerm> MedicalTerms { get; set; } = new();
        public UsReportTemplate Editing { get; set; }
        public List<string> Sections { get; set; } = new();
        public List<PlaceholderDoc> Placeholders { get; set; } = new();
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string Success { get; set; }

        public async Task OnGetAsync(long? edit)
        {
            if (edit.HasValue) EditingId = edit.Value;
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostSaveTemplateAsync()
        {
            if (string.IsNullOrWhiteSpace(TemplateName))
            {
                ErrorMessage = "Template name is required.";
                await LoadAsync();
                return Page();
            }

            var html = _templates.StripDangerousHtml(TemplateHtml ?? "");

            var template = EditingId.HasValue
                ? await _db.UsReportTemplates.FirstOrDefaultAsync(t => t.Id == EditingId.Value)
                : null;

            if (template == null)
            {
                template = new UsReportTemplate
                {
                    CreatedAt = DateTime.Now,
                    CreaterId = _appSession.WebUserId
                };
                _db.UsReportTemplates.Add(template);
            }

            template.UsServiceId = TemplateUsServiceId;
            template.Name = TemplateName.Trim();
            template.Description = TemplateDescription?.Trim() ?? "";
            template.TemplateHtml = html;
            template.IsDefault = TemplateIsDefault;
            template.SectionsJson = JsonSerializer.Serialize(_templates.GetStandardSections());
            template.EditedAt = DateTime.Now;
            template.EditerId = _appSession.WebUserId;

            if (template.IsDefault && template.UsServiceId == null)
            {
                // Only one shared default may exist.
                var others = await _db.UsReportTemplates
                    .Where(t => t.IsDefault && t.UsServiceId == null && t.Id != template.Id)
                    .ToListAsync();
                foreach (var o in others) o.IsDefault = false;
            }

            await _db.SaveChangesAsync();

            EditingId = null;
            Success = $"Template '{template.Name}' saved.";
            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteTemplateAsync(long id)
        {
            var template = await _db.UsReportTemplates.FirstOrDefaultAsync(t => t.Id == id);
            if (template == null)
            {
                ErrorMessage = "Template not found.";
                await LoadAsync();
                return Page();
            }

            template.Retired = true;
            template.RetiredAt = DateTime.Now;
            template.IsDefault = false;
            await _db.SaveChangesAsync();

            Success = $"Template '{template.Name}' removed.";
            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAddMedicalTermAsync()
        {
            if (string.IsNullOrWhiteSpace(NewMedicalTerm) || string.IsNullOrWhiteSpace(NewMedicalDefinition))
            {
                ErrorMessage = "Medical term and definition are both required.";
                await LoadAsync();
                return Page();
            }

            var term = NewMedicalTerm.Trim();
            var exists = await _db.MedicalTerms
                .FirstOrDefaultAsync(t => t.Term == term && !t.Retired);
            if (exists != null)
            {
                exists.Definition = NewMedicalDefinition.Trim();
                exists.Category = NewMedicalCategory?.Trim() ?? "";
                exists.Synonyms = NewMedicalSynonyms?.Trim() ?? "";
            }
            else
            {
                _db.MedicalTerms.Add(new MedicalTerm
                {
                    Term = term,
                    Definition = NewMedicalDefinition.Trim(),
                    Category = NewMedicalCategory?.Trim() ?? "",
                    Synonyms = NewMedicalSynonyms?.Trim() ?? "",
                    CreatedAt = DateTime.Now
                });
            }

            await _db.SaveChangesAsync();

            Success = $"Medical term '{term}' saved.";
            NewMedicalTerm = "";
            NewMedicalDefinition = "";
            NewMedicalSynonyms = "";
            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteMedicalTermAsync(long id)
        {
            var term = await _db.MedicalTerms.FirstOrDefaultAsync(t => t.Id == id);
            if (term != null)
            {
                term.Retired = true;
                await _db.SaveChangesAsync();
                Success = $"Medical term '{term.Term}' removed.";
            }
            await LoadAsync();
            return Page();
        }

        private async Task LoadAsync()
        {
            Sections = _templates.GetStandardSections();
            Placeholders = BuildPlaceholderDocs();
            Services = await _db.UsServices
                .Where(s => !s.Retired && s.Active)
                .OrderBy(s => s.Code)
                .ToListAsync();

            ServiceOptions = Services
                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Code + " - " + s.Name })
                .ToList();
            ServiceOptions.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "-- Shared template (all investigations) --"
            });

            var templateQuery = _db.UsReportTemplates
                .Include(t => t.UsService)
                .Where(t => !t.Retired);

            Templates = await templateQuery
                .OrderBy(t => t.UsService == null ? 0 : 1)
                .ThenBy(t => t.Name)
                .ToListAsync();

            if (EditingId.HasValue)
            {
                Editing = await _db.UsReportTemplates
                    .Include(t => t.UsService)
                    .FirstOrDefaultAsync(t => t.Id == EditingId.Value && !t.Retired);

                if (Editing != null)
                {
                    TemplateUsServiceId = Editing.UsServiceId;
                    TemplateName = Editing.Name;
                    TemplateDescription = Editing.Description;
                    TemplateHtml = Editing.TemplateHtml;
                    TemplateIsDefault = Editing.IsDefault;
                }
            }

            if (string.IsNullOrEmpty(TemplateHtml))
                TemplateHtml = _templates.GetDefaultTemplateHtml();

            var medQuery = _db.MedicalTerms.Where(t => !t.Retired);
            if (!string.IsNullOrWhiteSpace(Filter))
                medQuery = medQuery.Where(t => t.Term.Contains(Filter) || t.Category.Contains(Filter));

            MedicalTerms = await medQuery
                .OrderBy(t => t.Term)
                .Take(120)
                .ToListAsync();
        }

        private static List<PlaceholderDoc> BuildPlaceholderDocs()
        {
            return new List<PlaceholderDoc>
            {
                new PlaceholderDoc("{{PatientName}}", "Patient name from PERSON"),
                new PlaceholderDoc("{{PatientAge}}", "Age written out, e.g. 2 years 3 months"),
                new PlaceholderDoc("{{PatientAgeYears}}", "Years only"),
                new PlaceholderDoc("{{Gender}}", "Gender from PERSON"),
                new PlaceholderDoc("{{PatientId}}", "PATIENT.PatientId"),
                new PlaceholderDoc("{{MrNo}}", "Medical record number"),
                new PlaceholderDoc("{{Phn}}", "Hospital number"),
                new PlaceholderDoc("{{Nic}}", "NIC"),
                new PlaceholderDoc("{{Mobile}}", "Contact number"),
                new PlaceholderDoc("{{UsIndentNo}}", "US indent number (same as bill number)"),
                new PlaceholderDoc("{{BillNo}}", "Bill number"),
                new PlaceholderDoc("{{VisitNo}}", "Encounter / visit number"),
                new PlaceholderDoc("{{Barcode}}", "Specimen barcode"),
                new PlaceholderDoc("{{TestName}}", "Investigation name"),
                new PlaceholderDoc("{{TestCode}}", "Investigation code"),
                new PlaceholderDoc("{{SampleType}}", "Sample / body part examined"),
                new PlaceholderDoc("{{ReferringDoctor}}", "Referring consultant or staff"),
                new PlaceholderDoc("{{InvestigatedBy}}", "Radiologist who reported"),
                new PlaceholderDoc("{{Institution}}", "Institution"),
                new PlaceholderDoc("{{Department}}", "Department"),
                new PlaceholderDoc("{{InvestigationDate}}", "Date the scan was performed"),
                new PlaceholderDoc("{{ReportDate}}", "Report date"),
                new PlaceholderDoc("{{ReportTime}}", "Report time"),
                new PlaceholderDoc("{{CurrentDate}}", "Today's date"),
                new PlaceholderDoc("{{CurrentDateTime}}", "Today's date and time"),
                new PlaceholderDoc("{{WeekDays}}", "Day of the week")
            };
        }

        public class PlaceholderDoc
        {
            public PlaceholderDoc() { }
            public PlaceholderDoc(string token, string description)
            {
                Token = token;
                Description = description;
            }
            public string Token { get; set; }
            public string Description { get; set; }
        }
    }
}