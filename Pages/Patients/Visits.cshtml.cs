using HmisWeb.Data;
using HmisWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Patients
{
    public class VisitsModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public VisitsModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        public List<PatientEncounter> Encounters { get; set; } = new();

        public async Task OnGetAsync()
        {
            if (FromDate == null) FromDate = DateTime.Today.AddDays(-30);
            if (ToDate == null) ToDate = DateTime.Today;

            var q = _db.PatientEncounters
                .Include(e => e.Patient).ThenInclude(p => p.Person)
                .Include(e => e.Department)
                .Include(e => e.ReferringStaff).ThenInclude(s => s.Person)
                .Where(e => !e.Retired && e.EncounterDate != null && e.EncounterDate >= FromDate && e.EncounterDate <= ToDate.Value.Date.AddDays(1).AddMilliseconds(-1));

            Encounters = await q
                .OrderByDescending(e => e.EncounterDate)
                .ThenByDescending(e => e.EncounterTime)
                .ToListAsync();
        }
    }
}