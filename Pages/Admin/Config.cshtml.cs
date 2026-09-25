using HmisWeb.Data;
using HmisWeb.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Admin
{
    public class ConfigModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public ConfigModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<ConfigOption> Configs { get; set; } = new();

        public async Task OnGetAsync()
        {
            Configs = await _db.ConfigOptions
                .Include(c => c.Institution)
                .Include(c => c.Department)
                .Where(c => !c.Retired)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
    }
}
