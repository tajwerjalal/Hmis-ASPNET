using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Xr
{
    /// <summary>
    /// Report Entry workspace page model.
    ///
    /// Flow mirrors the VFP US0110 Report Entry screen: pick a patient from the pending queue,
    /// choose one test in the left grid, and write the report in the Quill editor on the right.
    /// Rich HTML is stored in XRORDER.ResultHtml and mirrored to plain text in XRORDER.Result so
    /// the register report and search keep working unchanged.
    /// </summary>
    public class ResultEntryModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ICommonService _common;
        private readonly IAppSession _appSession;
        private readonly IXrReportTemplateService _templates;
        private readonly IXrReportDictionaryService _dictionary;

        public ResultEntryModel(
            ApplicationDbContext db,
            ICommonService common,
            IAppSession appSession,
            IXrReportTemplateService templates,
            IXrReportDictionaryService dictionary)
        {
            _db = db;
            _common = common;
            _appSession = appSession;
            _templates = templates;
            _dictionary = dictionary;
        }

        private const string PatientKey = "XrResultPatientId";
        private const string OrderKey = "XrResultOrderId";
        private const string TemplateKey = "XrResultTemplateId";
        private const string TemplateReloadKey = "XrResultTemplateReload";
        private const string IncludeCompletedKey = "XrResultIncludeCompleted";

        // ---- Bound from the page ----
        [BindProperty] public string PatientSearch { get; set; }
        [BindProperty] public long? SelectedPatientId { get; set; }
        [BindProperty] public long? SelectedOrderId { get; set; }
        [BindProperty] public long? SelectedTemplateId { get; set; }
        [BindProperty] public long? InvestigatedById { get; set; }
        [BindProperty] public bool IncludeCompleted { get; set; }

        [BindProperty] public string ReportHtml { get; set; }
        [BindProperty] public string ResultNotes { get; set; }
        [BindProperty] public int WordCount { get; set; }
        [BindProperty] public int CharCount { get; set; }

        // ---- Walk-in patient registration ----
        [BindProperty] public string NewName { get; set; }
        [BindProperty] public string NewNameWithInitials { get; set; }
        [BindProperty] public string NewNic { get; set; }
        [BindProperty] public string NewMobile { get; set; }
        [BindProperty] public string NewPhone { get; set; }
        [BindProperty] public Sex? NewSex { get; set; }

        // ---- Display state ----
        public Patient SelectedPatient { get; set; }
        public ReportPatientInfo PatientInfo { get; set; } = new();
        public List<Patient> PatientResults { get; set; } = new();
        public List<PendingReportRow> PendingReports { get; set; } = new();
        public List<XrTestRow> Tests { get; set; } = new();
        public XrTestRow ActiveTest { get; set; }
        public List<XrReportTemplate> Templates { get; set; } = new();
        public List<DoctorOption> Doctors { get; set; } = new();
        public List<SelectListItem> DoctorOptions { get; set; } = new();
        public List<string> StandardSections { get; set; } = new();
        public List<string> StandardObservations { get; set; } = new();
        public List<string> MedicalVocabulary { get; set; } = new();
        public string ReportStatusLabel { get; set; } = "Draft";
        public bool IsFinalized { get; set; }
        public DateTime? LastSavedAt { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string Success { get; set; }

        public async Task OnGetAsync()
        {
            await LoadStateAsync();
            if (TempData["XrResultSuccess"] != null)
                Success = TempData["XrResultSuccess"].ToString();
        }

        public async Task<IActionResult> OnPostSearchPatientsAsync()
        {
            PatientResults = await _common.SearchPatientsAsync(PatientSearch);
            await LoadPendingReportsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSelectPatientAsync()
        {
            if (SelectedPatientId == null)
                return RedirectToPage();

            var patient = await _db.Patients
                .Include(p => p.Person)
                .FirstOrDefaultAsync(p => p.Id == SelectedPatientId.Value && !p.Retired);

            if (patient == null)
                return RedirectToPage();

            HttpContext.Session.SetString(PatientKey, patient.Id.ToString());
            HttpContext.Session.Remove(OrderKey);
            HttpContext.Session.Remove(TemplateKey);
            return RedirectToPage("/Xr/ResultEntry");
        }

        /// <summary>
        /// Switches the active test in the left grid. Redirects so the page reloads with the
        /// chosen template; the editor itself is never reset while typing.
        /// </summary>
        public async Task<IActionResult> OnPostSelectOrderAsync()
        {
            if (SelectedOrderId == null)
                return RedirectToPage();

            var order = await _db.XrOrders
                .FirstOrDefaultAsync(o => o.Id == SelectedOrderId.Value && !o.Retired);

            if (order == null)
                return RedirectToPage();

            var patientId = GetSelectedPatientId();
            if (patientId != order.PatientId)
                return RedirectToPage();

            HttpContext.Session.SetString(OrderKey, order.Id.ToString());
            return RedirectToPage("/Xr/ResultEntry");
        }

        /// <summary>
        /// Loads a chosen template into the editor. This deliberately does not save: the
        /// previous body is replaced in the editor only, so a wrong pick can be discarded
        /// by switching back to the previous template before saving.
        /// </summary>
        public async Task<IActionResult> OnPostLoadTemplateAsync()
        {
            if (SelectedOrderId == null)
                return RedirectToPage();

            var order = await _db.XrOrders
                .FirstOrDefaultAsync(o => o.Id == SelectedOrderId.Value && !o.Retired);

            if (order == null || order.PatientId != GetSelectedPatientId())
                return RedirectToPage();

            if (order.ReportStatus == XrReportStatus.Finalized)
            {
                TempData["XrResultError"] = "Reopen the report before changing its template.";
                return RedirectToPage("/Xr/ResultEntry");
            }

            HttpContext.Session.SetString(OrderKey, order.Id.ToString());

            if (SelectedTemplateId.HasValue)
                HttpContext.Session.SetString(TemplateKey, SelectedTemplateId.Value.ToString());
            else
                HttpContext.Session.Remove(TemplateKey);

            // Flag the reload so the next GET rebuilds the editor body even when the order
            // already has saved text, which is what "Load Template" is expected to do.
            HttpContext.Session.SetString(TemplateReloadKey, order.Id.ToString());

            TempData["XrResultMessage"] = SelectedTemplateId.HasValue
                ? "Template loaded. Review the findings, then save the draft."
                : "Default template loaded.";

            return RedirectToPage("/Xr/ResultEntry");
        }

        /// <summary>
        /// Keeps finalized reports visible in the grid so a referring doctor can reopen one
        /// without losing their place. Stored in the session rather than the querystring so it
        /// survives the redirects used by every other action on this page.
        /// </summary>
        public IActionResult OnPostToggleIncludeCompletedAsync()
        {
            var current = GetIncludeCompleted();
            HttpContext.Session.SetString(IncludeCompletedKey, current ? "0" : "1");
            return RedirectToPage("/Xr/ResultEntry");
        }

        public IActionResult OnPostStartNewAsync()
        {
            HttpContext.Session.Remove(PatientKey);
            HttpContext.Session.Remove(OrderKey);
            HttpContext.Session.Remove(TemplateKey);
            return RedirectToPage("/Xr/ResultEntry");
        }

        public async Task<IActionResult> OnPostCreatePatientAsync()
        {
            if (string.IsNullOrWhiteSpace(NewName))
            {
                ErrorMessage = "Patient name is required.";
                await LoadPendingReportsAsync();
                return Page();
            }

            var person = new Person
            {
                Name = NewName.Trim(),
                NameWithInitials = string.IsNullOrWhiteSpace(NewNameWithInitials) ? NewName.Trim() : NewNameWithInitials.Trim(),
                Nic = NewNic?.Trim() ?? "",
                Mobile = NewMobile?.Trim() ?? "",
                Phone = NewPhone?.Trim() ?? "",
                Sex = NewSex,
                CreatedAt = DateTime.Now,
                CreaterId = _appSession.WebUserId
            };
            _db.Persons.Add(person);
            await _db.SaveChangesAsync();

            long? maxPatientId = await _db.Patients.MaxAsync(p => (long?)p.PatientId);
            var patient = new Patient
            {
                PersonId = person.Id,
                PatientId = (maxPatientId ?? 0) + 1,
                CreatedAt = DateTime.Now,
                CreaterId = _appSession.WebUserId,
                CreatedInstitutionId = _appSession.LoggedSite?.Id
            };
            _db.Patients.Add(patient);
            await _db.SaveChangesAsync();

            HttpContext.Session.SetString(PatientKey, patient.Id.ToString());
            HttpContext.Session.Remove(OrderKey);
            HttpContext.Session.Remove(TemplateKey);
            return RedirectToPage("/Xr/ResultEntry");
        }

        /// <summary>
        /// Saves the rich-text report. mode=Draft keeps the order open for later editing,
        /// mode=Finalize locks it and marks the scan complete, mode=Reopen unlocks it again.
        /// </summary>
        public async Task<IActionResult> OnPostSaveReportAsync(string mode)
        {
            // LoadStateAsync rebuilds the display state from the database, which would otherwise
            // overwrite the body the editor just posted. Capture what was submitted first.
            var postedHtml = ReportHtml;
            var postedNotes = ResultNotes;
            var postedWords = WordCount;
            var postedChars = CharCount;

            await LoadStateAsync();

            ReportHtml = postedHtml;
            ResultNotes = postedNotes;
            WordCount = postedWords;
            CharCount = postedChars;

            if (ActiveTest == null)
            {
                ErrorMessage = "Select a test from the list before saving.";
                await LoadPendingReportsAsync();
                return Page();
            }

            if (ActiveTest.IsFinalized && !string.Equals(mode, "Reopen", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "This report is finalized. Reopen it before making changes.";
                return Page();
            }

            var html = _templates.StripDangerousHtml(ReportHtml ?? "");
            var plain = _templates.ToPlainText(html);

            if (string.IsNullOrWhiteSpace(plain) && !string.Equals(mode, "Save", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "The report is empty. Write the findings before saving.";
                return Page();
            }

            var order = await _db.XrOrders
                .Include(o => o.XrService)
                .FirstOrDefaultAsync(o => o.Id == ActiveTest.OrderId && !o.Retired);

            if (order == null)
            {
                ErrorMessage = "The selected test no longer exists.";
                await LoadPendingReportsAsync();
                return Page();
            }

            var now = DateTime.Now;
            var userId = _appSession.WebUserId;

            if (InvestigatedById.HasValue)
                order.DoctorId = InvestigatedById.Value;

            order.ResultHtml = html;
            order.Result = plain;
            order.ResultNotes = ResultNotes ?? string.Empty;
            order.WordCount = WordCount > 0 ? WordCount : plain.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            order.CharCount = CharCount > 0 ? CharCount : plain.Length;
order.LastSavedAt = now;
            order.EditerId = userId;
            order.EditedAt = now;
            if (SelectedTemplateId.HasValue)
                order.ReportTemplateId = SelectedTemplateId.Value;

            switch ((mode ?? "Draft").ToLowerInvariant())
            {
                case "finalize":
                    order.ReportStatus = XrReportStatus.Finalized;
                    order.FinalizedAt = now;
                    order.FinalizedById = userId;
                    order.Completed = true;
                    order.CompletedAt = now;
                    break;
                case "reopen":
                    order.ReportStatus = XrReportStatus.Draft;
                    order.FinalizedAt = null;
                    order.FinalizedById = null;
                    order.Completed = false;
                    order.CompletedAt = null;
                    break;
                default:
                    // Draft keeps Completed false so the order stays in the pending queue.
                    order.ReportStatus = XrReportStatus.Saved;
                    break;
            }

            await _db.SaveChangesAsync();

            TempData["XrResultSuccess"] = mode?.ToLowerInvariant() switch
            {
                "finalize" => "Report finalized.",
                "reopen" => "Report reopened for editing.",
                _ => "Report saved as draft."
            };
            return RedirectToPage("/Xr/ResultEntry");
        }

        /// <summary>
        /// Auto-save endpoint used by the editor's idle timer. Saves silently as a draft and
        /// returns the counters so the status bar can update without a page reload.
        /// </summary>
        public async Task<IActionResult> OnPostAutoSaveAsync()
        {
            var orderId = GetSelectedOrderId();
            if (orderId == null) return new JsonResult(new { ok = false });

            var order = await _db.XrOrders.FirstOrDefaultAsync(o => o.Id == orderId.Value && !o.Retired);
            if (order == null || order.ReportStatus == XrReportStatus.Finalized)
                return new JsonResult(new { ok = false, reason = "finalized" });

            var html = _templates.StripDangerousHtml(ReportHtml ?? "");
            var plain = _templates.ToPlainText(html);

            order.ResultHtml = html;
            order.Result = plain;
            order.WordCount = WordCount > 0 ? WordCount : plain.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            order.CharCount = CharCount > 0 ? CharCount : plain.Length;
            order.LastSavedAt = DateTime.Now;
            order.EditerId = _appSession.WebUserId;
            order.EditedAt = DateTime.Now;
            if (order.ReportStatus == XrReportStatus.Draft)
                order.ReportStatus = XrReportStatus.Saved;

            await _db.SaveChangesAsync();

            return new JsonResult(new
            {
                ok = true,
                lastSaved = order.LastSavedAt.Value.ToString("hh:mm tt"),
                words = order.WordCount,
                chars = order.CharCount
            });
        }

        /// <summary>Serves a filtered template list to the editor's template picker.</summary>
        public async Task<IActionResult> OnGetTemplatesAsync()
        {
            var list = await _templates.GetAllTemplatesAsync();
            return new JsonResult(list.Select(t => new
            {
                id = t.Id,
                name = t.Name,
                service = t.XrService?.Name ?? "All investigations",
                isDefault = t.IsDefault
            }));
        }

        /// <summary>Plain-English dictionary lookup for the word under the cursor.</summary>
        public async Task<IActionResult> OnGetDictionaryAsync(string word)
        {
            var result = await _dictionary.LookupGeneralAsync(word);
            return new JsonResult(result);
        }

        /// <summary>Local medical terminology lookup - never leaves the server.</summary>
        public async Task<IActionResult> OnGetMedicalTermsAsync(string term)
        {
            var list = string.IsNullOrWhiteSpace(term)
                ? await _dictionary.LookupMedicalAsync(null, 30)
                : await _dictionary.LookupMedicalAsync(term);
            return new JsonResult(list);
        }

        private async Task LoadStateAsync()
        {
            SelectedPatient = await GetSelectedPatientAsync();

            // Template choice and the "show completed" filter live in the session: every other
            // action on this page redirects, so bound values alone would be lost.
            var sessionTemplate = GetSelectedTemplateId();
            if (SelectedTemplateId.HasValue || sessionTemplate.HasValue)
                SelectedTemplateId = sessionTemplate;
            IncludeCompleted = GetIncludeCompleted();

            // A "Load Template" reload only applies to the order that requested it.
            var reloadFor = HttpContext.Session.GetString(TemplateReloadKey);
            var reloadRequested = reloadFor != null
                && long.TryParse(reloadFor, out var reloadOrderId)
                && GetSelectedOrderId() == reloadOrderId;
            if (!reloadRequested)
                HttpContext.Session.Remove(TemplateReloadKey);

            Templates = await _templates.GetAllTemplatesAsync();
            StandardSections = _templates.GetStandardSections();
            StandardObservations = _templates.GetStandardObservations();

            if (SelectedPatient == null)
            {
                Tests = new();
                ActiveTest = null;
                PatientInfo = new();
                ReportHtml = "";
                ResultNotes = "";
                ReportStatusLabel = "Draft";
                IsFinalized = false;
            }
            else
            {
                Tests = await LoadTestsAsync(SelectedPatient.Id);
                ActiveTest = ResolveActiveTest(Tests);
                PatientInfo = await BuildPatientInfoAsync(ActiveTest);

                if (ActiveTest != null)
                {
                    ReportHtml = ActiveTest.ReportHtml;
                    ResultNotes = ActiveTest.ResultNotes;
                    WordCount = ActiveTest.WordCount;
                    CharCount = ActiveTest.CharCount;
                    LastSavedAt = ActiveTest.LastSavedAt;
                    IsFinalized = ActiveTest.IsFinalized;
                    ReportStatusLabel = ActiveTest.ReportStatus.ToString();

                    if (ActiveTest.ReportHtml.Length == 0 || reloadRequested)
                    {
                        ReportHtml = await BuildTemplatePreviewAsync(ActiveTest);
                        // The body was just replaced, so the stored counters no longer match
                        // what the user sees until they save.
                        WordCount = 0;
                        CharCount = 0;
                    }

                    var doctors = await _db.Doctors
                        .Include(d => d.Person)
                        .Where(d => !d.Retired)
                        .OrderBy(d => d.Code)
                        .ToListAsync();
                    Doctors = doctors
                        .Select(d => new DoctorOption
                        {
                            Id = d.Id,
                            Name = d.Person?.Name ?? d.Code,
                            Code = d.Code
                        })
                        .ToList();

                    // Built here rather than inline in the view: Razor tag helpers cannot
                    // evaluate a lambda with an interpolated string.
                    DoctorOptions = Doctors
                        .Select(d => new SelectListItem
                        {
                            Value = d.Id.ToString(),
                            Text = $"{d.Name} ({d.Code})",
                            Selected = d.Id == ActiveTest.DoctorId
                        })
                        .ToList();
                }
            }

            await LoadPendingReportsAsync();
            MedicalVocabulary = await _dictionary.GetMedicalTermsAsync();

            if (TempData["XrResultMessage"] != null)
                Message = TempData["XrResultMessage"].ToString();
            if (TempData["XrResultError"] != null)
                ErrorMessage = TempData["XrResultError"].ToString();
        }

        /// <summary>
        /// Builds the template-filled preview shown before the report has been saved. The stored
        /// template itself is never altered; only this copy gets the patient values.
        /// </summary>
        private async Task<string> BuildTemplatePreviewAsync(XrTestRow test)
        {
            var order = await _db.XrOrders
                .Include(o => o.XrService)
                .Include(o => o.Patient).ThenInclude(p => p.Person)
                .Include(o => o.PatientEncounter).ThenInclude(e => e.ReferringConsultant).ThenInclude(c => c.Person)
                .FirstOrDefaultAsync(o => o.Id == test.OrderId);

            if (order == null) return "";

            var template = SelectedTemplateId.HasValue
                ? await _db.XrReportTemplates.FirstOrDefaultAsync(t => t.Id == SelectedTemplateId.Value)
                : null;
            template ??= order.ReportTemplateId.HasValue
                ? await _db.XrReportTemplates.FirstOrDefaultAsync(t => t.Id == order.ReportTemplateId.Value)
                : null;
            template ??= await _templates.GetTemplateForServiceAsync(order.XrServiceId);

            var html = template?.TemplateHtml;
            if (string.IsNullOrWhiteSpace(html)) html = _templates.GetDefaultTemplateHtml();

            var values = await _templates.BuildPlaceholderValuesAsync(order);
            return _templates.ResolvePlaceholders(html, values);
        }

        private async Task<List<XrTestRow>> LoadTestsAsync(long patientId)
        {
            var query = _db.XrOrders.AsNoTracking()
                .Include(o => o.XrService)
                .Include(o => o.Bill)
                .Where(o => o.PatientId == patientId && !o.Retired && o.SampleCollected);

            if (!IncludeCompleted)
                query = query.Where(o => !o.Completed);

            var orders = await query
                .OrderBy(o => o.SampleCollectedAt)
                .ThenBy(o => o.OrderName)
                .ToListAsync();

            return orders.Select((o, i) => new XrTestRow
            {
                OrderId = o.Id,
                RowNo = i + 1,
                TestCode = o.XrService?.Code ?? "-",
                TestName = string.IsNullOrWhiteSpace(o.OrderName) ? o.XrService?.Name : o.OrderName,
                Barcode = o.Barcode,
                BillNo = o.BillId?.ToString(),
                CollectedAt = o.SampleCollectedAt,
                ReportStatus = o.ReportStatus.ToString(),
                IsFinalized = o.ReportStatus == XrReportStatus.Finalized,
                IsCompleted = o.Completed,
                ReportHtml = o.ResultHtml ?? "",
                ResultNotes = o.ResultNotes ?? "",
                WordCount = o.WordCount,
                CharCount = o.CharCount,
                LastSavedAt = o.LastSavedAt,
                FinalizedAt = o.FinalizedAt,
                DoctorId = o.DoctorId
            }).ToList();
        }

        private XrTestRow ResolveActiveTest(List<XrTestRow> tests)
        {
            if (!tests.Any()) return null;

            var wanted = GetSelectedOrderId();
            if (wanted.HasValue)
            {
                var match = tests.FirstOrDefault(t => t.OrderId == wanted.Value);
                if (match != null) return match;
            }

            // Open on the first report that still needs attention.
            return tests.FirstOrDefault(t => !t.IsFinalized) ?? tests.First();
        }

        private async Task<ReportPatientInfo> BuildPatientInfoAsync(XrTestRow test)
        {
            var person = SelectedPatient?.Person;
            var info = new ReportPatientInfo
            {
                PatientName = string.IsNullOrWhiteSpace(person?.Name) ? "Unnamed" : person.Name,
                PatientId = SelectedPatient?.PatientId?.ToString(),
                Phn = SelectedPatient?.Phn,
                Mobile = person?.Mobile,
                Sex = person?.Sex?.ToString(),
                Age = person?.Dob == null ? "" : person.GetAgeString(),
                XrIndentNo = test?.BillNo ?? SelectedPatient?.Id.ToString(),
                BillNo = test?.BillNo,
                Barcode = test?.Barcode,
                TestName = test?.TestName,
                TestCode = test?.TestCode,
                CollectedAt = test?.CollectedAt,
                ReportDate = DateTime.Now
            };

            if (test?.OrderId == null) return info;

            var order = await _db.XrOrders.AsNoTracking()
                .Include(o => o.PatientEncounter).ThenInclude(e => e.ReferringConsultant).ThenInclude(c => c.Person)
                .Include(o => o.PatientEncounter).ThenInclude(e => e.ReferringStaff).ThenInclude(s => s.Person)
                .FirstOrDefaultAsync(o => o.Id == test.OrderId);

            if (order == null) return info;

            info.MrNo = SelectedPatient?.PatientId?.ToString();
            info.VisitNo = order.VisitId?.ToString();
            info.BhtNo = order.PatientEncounter?.BhtNo;
            info.ReferringDoctor = order.PatientEncounter?.ReferringConsultant?.Person?.Name
                ?? order.PatientEncounter?.ReferringStaff?.Person?.Name;

            if (order.DoctorId.HasValue)
            {
                info.InvestigatedBy = await _db.Staffs
                    .Where(s => s.Id == order.DoctorId.Value)
                    .Select(s => s.Person != null ? s.Person.Name : s.Code)
                    .FirstOrDefaultAsync();
            }

            return info;
        }

        /// <summary>
        /// Scans already collected whose report is not finalized. This is the queue a user
        /// works from, since they only reach this page holding a report from the doctor.
        /// </summary>
        private async Task LoadPendingReportsAsync()
        {
            var rows = await (
                from o in _db.XrOrders.AsNoTracking()
                join p in _db.Patients.AsNoTracking() on o.PatientId equals p.Id
                join per in _db.Persons.AsNoTracking() on p.PersonId equals per.Id
                where !o.Retired && o.SampleCollected && !o.Completed && !p.Retired && !per.Retired
                select new { o, p, per }
            ).ToListAsync();

            PendingReports = rows
                .GroupBy(x => new { x.p.Id, PatientNumber = x.p.PatientId, x.p.Phn, x.per.Name, x.per.Sex, x.per.Dob })
                .Select(g => new PendingReportRow
                {
                    PatientDbId = g.Key.Id,
                    PatientId = g.Key.PatientNumber,
                    Phn = g.Key.Phn,
                    PatientName = g.Key.Name,
                    Sex = g.Key.Sex,
                    Dob = g.Key.Dob,
                    PendingCount = g.Count(),
                    Scans = string.Join(", ", g.Select(x => x.o.OrderName).Distinct().OrderBy(n => n)),
                    BillNo = g.Select(x => x.o.BillId).FirstOrDefault(b => b.HasValue)?.ToString(),
                    Barcode = g.Select(x => x.o.Barcode).FirstOrDefault(b => !string.IsNullOrEmpty(b)),
                    CollectedAt = g.Min(x => x.o.SampleCollectedAt ?? DateTime.MinValue)
                })
                // Oldest collection first so nothing gets stranded.
                .OrderBy(r => r.CollectedAt)
                .ToList();
        }

        private async Task<Patient> GetSelectedPatientAsync()
        {
            var id = GetSelectedPatientId();
            if (id == null) return null;
            return await _db.Patients
                .Include(p => p.Person)
                .FirstOrDefaultAsync(p => p.Id == id.Value && !p.Retired);
        }

        private long? GetSelectedPatientId()
        {
            var val = HttpContext.Session.GetString(PatientKey);
            return long.TryParse(val, out var id) ? id : null;
        }

        private long? GetSelectedOrderId()
        {
            var val = HttpContext.Session.GetString(OrderKey);
            return long.TryParse(val, out var id) ? id : null;
        }

        private long? GetSelectedTemplateId()
        {
            var val = HttpContext.Session.GetString(TemplateKey);
            return long.TryParse(val, out var id) ? id : null;
        }

        private bool GetIncludeCompleted()
        {
            return HttpContext.Session.GetString(IncludeCompletedKey) == "1";
        }

        // ---- View models ----

        public class ReportPatientInfo
        {
            public string PatientName { get; set; }
            public string PatientId { get; set; }
            public string Phn { get; set; }
            public string Mobile { get; set; }
            public string Age { get; set; }
            public string Sex { get; set; }
            public string MrNo { get; set; }
            public string XrIndentNo { get; set; }
            public string BillNo { get; set; }
            public string VisitNo { get; set; }
            public string BhtNo { get; set; }
            public string Barcode { get; set; }
            public string TestName { get; set; }
            public string TestCode { get; set; }
            public string ReferringDoctor { get; set; }
            public string InvestigatedBy { get; set; }
            public DateTime? CollectedAt { get; set; }
            public DateTime ReportDate { get; set; }
        }

        public class XrTestRow
        {
            public long OrderId { get; set; }
            public int RowNo { get; set; }
            public string TestCode { get; set; }
            public string TestName { get; set; }
            public string Barcode { get; set; }
            public string BillNo { get; set; }
            public DateTime? CollectedAt { get; set; }
            public string ReportStatus { get; set; }
            public bool IsFinalized { get; set; }
            public bool IsCompleted { get; set; }
            public string ReportHtml { get; set; }
            public string ResultNotes { get; set; }
            public int WordCount { get; set; }
            public int CharCount { get; set; }
            public DateTime? LastSavedAt { get; set; }
            public DateTime? FinalizedAt { get; set; }
            public long? DoctorId { get; set; }
        }

        public class DoctorOption
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public string Code { get; set; }
        }

        public class PendingReportRow
        {
            public long PatientDbId { get; set; }
            public long? PatientId { get; set; }
            public string Phn { get; set; }
            public string PatientName { get; set; }
            public Sex? Sex { get; set; }
            public DateTime? Dob { get; set; }
            public int PendingCount { get; set; }
            public string Scans { get; set; }
            public string BillNo { get; set; }
            public string Barcode { get; set; }
            public DateTime CollectedAt { get; set; }

            public string AgeString
            {
                get
                {
                    if (Dob == null) return "";
                    var age = DateTime.Now - Dob.Value;
                    var years = age.Days / 365;
                    var months = (age.Days % 365) / 30;
                    var days = (age.Days % 365) % 30;
                    if (years > 0) return $"{years}y {months}m {days}d";
                    if (months > 0) return $"{months}m {days}d";
                    return $"{days}d";
                }
            }
        }
    }
}