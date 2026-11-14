using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;

namespace HmisWeb.Pages.Lab
{
    /// <summary>Test Report II - tests summarised by classification. VFP Labsys report lb0220.</summary>
    public class TestReportIIModel : PageModel
    {
        /// <summary>Mirror prefix of the laboratory bank on the shared services master.</summary>

        private const string ItemPrefix = "INV-LB-";


        private readonly ApplicationDbContext _db;
        public TestReportIIModel(ApplicationDbContext db) { _db = db; }

        public class SummaryRow
        {
            public string Classification { get; set; } = "";
            public int Count { get; set; }
            public double TotalPrice { get; set; }
        }

        public List<SummaryRow> Rows { get; set; } = new();
        public int TotalCount => Rows.Sum(r => r.Count);
        public double TotalPrice => Rows.Sum(r => r.TotalPrice);

        public async Task OnGetAsync() => await LoadAsync();

        public async Task<IActionResult> OnGetExportAsync()
        {
            await LoadAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Classification,Tests,Total Price");
            foreach (var r in Rows)
                sb.AppendLine(string.Join(",", LabCollectionQuery.Q(r.Classification), r.Count.ToString(), r.TotalPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
            return File(new UTF8Encoding(false).GetBytes(sb.ToString()), "text/csv", "TestReportII.csv");
        }

        private async Task LoadAsync()
        {
            var items = await _db.Items.Include(i => i.Category)
                .Where(i => !i.Retired && (i.ItemType == ItemType.Investigation || i.ItemType == ItemType.Service)
                && i.Code.StartsWith(ItemPrefix))
                .Select(i => new { ClassName = i.Category != null ? i.Category.Name : "(Unclassified)", Price = i.Total ?? 0 })
                .ToListAsync();

            Rows = items
                .GroupBy(i => string.IsNullOrEmpty(i.ClassName) ? "(Unclassified)" : i.ClassName)
                .Select(g => new SummaryRow { Classification = g.Key, Count = g.Count(), TotalPrice = g.Sum(x => x.Price) })
                .OrderBy(r => r.Classification)
                .ToList();
        }
    }
}