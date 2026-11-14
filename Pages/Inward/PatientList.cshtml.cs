using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Inward
{
    /// <summary>
    /// Inpatient list - /inward/PatientList.
    ///
    /// Paged, searchable list of the active inpatient stays, showing the current
    /// ward / bed (from [PATIENTROOM] -> [BED_POS]) and quick links to billing,
    /// ward transfer, company transfer and discharge. Replaces the previous
    /// unpaged view so the screen stays usable once the inpatient volume grows.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 22-Oct-2026
    /// Time      : 18:30
    /// Change Log: 22-Oct-2026 - paged + searchable, current bed shown,
    ///             transfer links added.
    /// </summary>
    public class PatientListModel : Shared.PagedPageModel
    {
        private readonly ApplicationDbContext _db;

        public PatientListModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<PatientEncounterRow> Inpatients { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string Q { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Ward { get; set; }

        public List<SelectListItem> WardList { get; set; } = new();

        public async Task OnGetAsync()
        {
            WardList = await _db.Wards.AsNoTracking()
                .Where(w => !w.Retired)
                .OrderBy(w => w.Code)
                .Select(w => new SelectListItem { Value = w.Code, Text = $"{w.Code} - {w.Name}" })
                .ToListAsync();

            var query = _db.PatientEncounters
                .Include(e => e.Patient).ThenInclude(p => p.Person)
                .Include(e => e.Department)
                .Include(e => e.ReferringConsultant).ThenInclude(s => s.Person)
                .Include(e => e.OpdDoctor).ThenInclude(s => s.Person)
                .Where(e => e.EncounterType == EncounterType.Inward
                            && !e.Retired
                            && e.DateOfDischarge == null)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(Q))
            {
                var term = Q.Trim();
                query = query.Where(e =>
                    (e.BhtNo != null && e.BhtNo.Contains(term))
                    || (e.Patient != null && e.Patient.Person != null
                        && e.Patient.Person.Name != null
                        && e.Patient.Person.Name.Contains(term)));
            }

            TotalCount = await query.CountAsync();
            ClampPaging();

            var encounters = await ApplyPaging(query
                .OrderByDescending(e => e.DateOfAdmission))
                .ToListAsync();

            var encounterIds = encounters.Select(e => e.Id).ToList();
            var rooms = await _db.PatientRooms
                .Include(pr => pr.BedPosition)
                .Where(pr => encounterIds.Contains(pr.PatientEncounterId ?? 0)
                             && !pr.Retired
                             && pr.FromTime != null && pr.ToTime == null)
                .ToListAsync();

            Inpatients = encounters.Select(e => new PatientEncounterRow
            {
                Encounter = e,
                CurrentBed = rooms.FirstOrDefault(pr => pr.PatientEncounterId == e.Id)?.BedPosition
            }).ToList();
        }

        public class PatientEncounterRow
        {
            public PatientEncounter Encounter { get; set; }
            public BedPosition CurrentBed { get; set; }
        }
    }
}
