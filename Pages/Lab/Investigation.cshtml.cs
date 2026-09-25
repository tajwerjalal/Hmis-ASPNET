using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Lab
{
    public class InvestigationModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public InvestigationModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<Item> Items { get; set; } = new();

        public async Task OnGetAsync()
        {
            Items = await _db.Items
                .Include(i => i.Category)
                .Where(i => !i.Retired && i.ItemType == ItemType.Investigation)
                .OrderBy(i => i.Name)
                .ToListAsync();
        }
    }
}
