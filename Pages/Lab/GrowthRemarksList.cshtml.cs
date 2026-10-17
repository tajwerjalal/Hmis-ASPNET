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
    /// <summary>Growth Remarks List - culture growth descriptions. VFP Labsys report grremark.</summary>
    public class GrowthRemarksListModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public GrowthRemarksListModel(ApplicationDbContext db) { _db = db; }

        public List<GrowthDescription> Items { get; set; } = new();

        public async Task OnGetAsync() => await LoadAsync();

        public async Task<IActionResult> OnGetExportAsync()
        {
            await LoadAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Code,Description");
            foreach (var g in Items) sb.AppendLine(string.Join(",", LabCollectionQuery.Q(g.Code), LabCollectionQuery.Q(g.Description)));
            return File(new UTF8Encoding(false).GetBytes(sb.ToString()), "text/csv", "GrowthRemarksList.csv");
        }

        private async Task LoadAsync()
        {
            Items = await _db.GrowthDescriptions.Where(g => !g.Retired).OrderBy(g => g.Code).ToListAsync();
        }
    }
}