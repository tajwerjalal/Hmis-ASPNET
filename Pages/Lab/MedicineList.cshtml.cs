using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;

namespace HmisWeb.Pages.Lab
{
    /// <summary>Medicine List - the medicine master list. VFP Labsys report medlist.</summary>
    public class MedicineListModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public MedicineListModel(ApplicationDbContext db) { _db = db; }

        [BindProperty(SupportsGet = true)] public string SearchTerm { get; set; }
        public List<Item> Items { get; set; } = new();

        public async Task OnGetAsync() => await LoadAsync();

        public async Task<IActionResult> OnGetExportAsync()
        {
            await LoadAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Code,Name,Generic,Classification,Price");
            foreach (var m in Items)
                sb.AppendLine(string.Join(",", LabCollectionQuery.Q(m.Code), LabCollectionQuery.Q(m.Name), LabCollectionQuery.Q(m.Sname), LabCollectionQuery.Q(m.Category?.Name),
                    (m.Total ?? 0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
            return File(new UTF8Encoding(false).GetBytes(sb.ToString()), "text/csv", "MedicineList.csv");
        }

        private async Task LoadAsync()
        {
            var q = _db.Items.Include(i => i.Category).Where(i => i.ItemType == ItemType.Medicine && !i.Retired);
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var t = SearchTerm.Trim().ToLower();
                q = q.Where(i => (i.Name != null && i.Name.ToLower().Contains(t)) || (i.Code != null && i.Code.ToLower().Contains(t)) || (i.Sname != null && i.Sname.ToLower().Contains(t)));
            }
            Items = await q.OrderBy(i => i.Name).ToListAsync();
        }
    }
}