using HmisWeb.Data;
using HmisWeb.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Hr
{
    public class SalaryModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public SalaryModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<StaffSalary> Salaries { get; set; } = new();

        public async Task OnGetAsync()
        {
            Salaries = await _db.StaffSalaries
                .Include(ss => ss.Staff).ThenInclude(s => s.Person)
                .Where(ss => !ss.Retired)
                .OrderByDescending(ss => ss.SalaryDate)
                .ToListAsync();
        }
    }
}
