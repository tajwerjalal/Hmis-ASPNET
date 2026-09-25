using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Inward
{
    public class PatientListModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public PatientListModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<PatientEncounterRow> Inpatients { get; set; } = new();

        public async Task OnGetAsync()
        {
            var encounters = await _db.PatientEncounters
                .Include(e => e.Patient).ThenInclude(p => p.Person)
                .Include(e => e.Department)
                .Include(e => e.ReferringConsultant).ThenInclude(s => s.Person)
                .Include(e => e.OpdDoctor).ThenInclude(s => s.Person)
                .Where(e => e.EncounterType == HmisWeb.Data.Enums.EncounterType.Inward
                            && !e.Retired
                            && e.DateOfDischarge == null)
                .OrderByDescending(e => e.DateOfAdmission)
                .ToListAsync();

            var encounterIds = encounters.Select(e => e.Id).ToList();
            var rooms = await _db.PatientRooms
                .Include(pr => pr.Room)
                .Where(pr => encounterIds.Contains(pr.PatientEncounterId ?? 0)
                             && !pr.Retired
                             && pr.FromTime != null && pr.ToTime == null)
                .ToListAsync();

            Inpatients = encounters.Select(e => new PatientEncounterRow
            {
                Encounter = e,
                CurrentRoom = rooms.FirstOrDefault(pr => pr.PatientEncounterId == e.Id)?.Room
            }).ToList();
        }

        public class PatientEncounterRow
        {
            public PatientEncounter Encounter { get; set; }
            public Room CurrentRoom { get; set; }
        }
    }
}