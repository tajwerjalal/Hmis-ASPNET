using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;

namespace HmisWeb.Pages.Institutions
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<Institution> Institutions { get; set; } = new();

        public async Task OnGetAsync()
        {
            Institutions = await _db.Institutions
                .Where(i => !i.Retired)
                .OrderBy(i => i.Name)
                .ToListAsync();
        }
    }
}