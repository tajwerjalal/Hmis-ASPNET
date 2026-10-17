using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;

namespace HmisWeb.Pages.Lab
{
    /// <summary>Test Schedule Report - tests with turnaround/requirements. VFP Labsys report lb0201.</summary>
    public class TestScheduleReportModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public TestScheduleReportModel(ApplicationDbContext db) { _db = db; }

        [BindProperty(SupportsGet = true)] public long? CategoryId { get; set; }
        [BindProperty(SupportsGet = true)] public string SearchTerm { get; set; }

        public List<Item> Items { get; set; } = new();
        public List<SelectListItem> CategoryList { get; set; } = new();

        public async Task OnGetAsync() => await LoadAsync();

        public async Task<IActionResult> OnGetExportAsync()
        {
            await LoadAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Code,Name,Classification,Min Duration,Max Duration,Requirements,Price");
            foreach (var i in Items)
                sb.AppendLine(string.Join(",", LabCollectionQuery.Q(i.Code), LabCollectionQuery.Q(i.Name), LabCollectionQuery.Q(i.Category?.Name),
                    LabCollectionQuery.Q(i.MinDuration), LabCollectionQuery.Q(i.MaxDuration), LabCollectionQuery.Q(i.Requirements),
                    (i.Total ?? 0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
            return File(new UTF8Encoding(false).GetBytes(sb.ToString()), "text/csv", "TestScheduleReport.csv");
        }

        private async Task LoadAsync()
        {
            var q = _db.Items.Include(i => i.Category)
                .Where(i => !i.Retired && (i.ItemType == ItemType.Investigation || i.ItemType == ItemType.Service));
            if (CategoryId.HasValue && CategoryId.Value > 0) q = q.Where(i => i.CategoryId == CategoryId.Value);
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var t = SearchTerm.Trim().ToLower();
                q = q.Where(i => i.Name != null && i.Name.ToLower().Contains(t));
            }
            Items = await q.OrderBy(i => i.Category.Name).ThenBy(i => i.Name).ToListAsync();
            CategoryList = await _db.Categories.Where(c => !c.Retired).OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToListAsync();
        }
    }
}