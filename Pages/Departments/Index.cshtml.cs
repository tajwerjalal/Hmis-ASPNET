using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;

namespace HmisWeb.Pages.Departments
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<Department> Departments { get; set; } = new();

        public async Task OnGetAsync()
        {
            Departments = await _db.Departments
                .Include(d => d.Institution)
                .Where(d => !d.Retired)
                .OrderBy(d => d.Name)
                .ToListAsync();
        }
    }
}