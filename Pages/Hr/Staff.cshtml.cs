using HmisWeb.Data;
using HmisWeb.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Hr
{
    public class StaffModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public StaffModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<Staff> StaffList { get; set; } = new();

        public async Task OnGetAsync()
        {
            StaffList = await _db.Staffs
                .Include(s => s.Person)
                .Include(s => s.Designation)
                .Include(s => s.Department)
                .Include(s => s.Institution)
                .Where(s => !s.Retired)
                .OrderBy(s => s.Person != null ? s.Person.Name : "")
                .ToListAsync();
        }
    }
}
