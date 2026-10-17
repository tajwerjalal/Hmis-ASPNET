using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;

namespace HmisWeb.Pages.Lab
{
    /// <summary>Organisms List - micro-organisms for culture reporting. VFP Labsys report organisms.</summary>
    public class OrganismsListModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public OrganismsListModel(ApplicationDbContext db) { _db = db; }

        public List<Organism> Items { get; set; } = new();

        public async Task OnGetAsync() => await LoadAsync();

        public async Task<IActionResult> OnGetExportAsync()
        {
            await LoadAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Code,Name");
            foreach (var o in Items) sb.AppendLine(string.Join(",", LabCollectionQuery.Q(o.Code), LabCollectionQuery.Q(o.Name)));
            return File(new UTF8Encoding(false).GetBytes(sb.ToString()), "text/csv", "OrganismsList.csv");
        }

        private async Task LoadAsync()
        {
            Items = await _db.Organisms.Where(o => !o.Retired).OrderBy(o => o.Name).ToListAsync();
        }
    }
}