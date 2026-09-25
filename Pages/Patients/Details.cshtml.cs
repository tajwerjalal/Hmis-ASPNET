using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;

namespace HmisWeb.Pages.Patients
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public DetailsModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public Patient Patient { get; set; }
        public List<Bill> RecentBills { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
                return RedirectToPage("/patients/Index");

            Patient = await _db.Patients
                .Include(p => p.Person)
                .FirstOrDefaultAsync(p => p.Id == id && !p.Retired);

            if (Patient == null)
                return RedirectToPage("/patients/Index");

            RecentBills = await _db.Bills
                .Include(b => b.BillItems)
                .Where(b => !b.Retired && b.PatientId == id)
                .OrderByDescending(b => b.CreatedAt)
                .Take(20)
                .ToListAsync();

            return Page();
        }
    }
}