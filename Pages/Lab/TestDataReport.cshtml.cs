using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;

namespace HmisWeb.Pages.Lab
{
    /// <summary>
    /// Test Data Report - the parameters (with units/ranges) recorded against tests.
    /// Migrated from VFP Labsys report <c>tbdata</c>.
    /// </summary>
    public class TestDataReportModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public TestDataReportModel(ApplicationDbContext db) { _db = db; }

        [BindProperty(SupportsGet = true)] public string SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)] public long? CategoryId { get; set; }

        public List<TestData> Items { get; set; } = new();
        public List<SelectListItem> CategoryList { get; set; } = new();

        public async Task OnGetAsync() => await LoadAsync();

        public async Task<IActionResult> OnGetExportAsync()
        {
            await LoadAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Code,Name,Unit,Normal Range,Classification");
            foreach (var d in Items)
                sb.AppendLine(string.Join(",", LabCollectionQuery.Q(d.Code), LabCollectionQuery.Q(d.Name), LabCollectionQuery.Q(d.Unit), LabCollectionQuery.Q(d.Range), LabCollectionQuery.Q(d.Category?.Name)));
            return File(new UTF8Encoding(false).GetBytes(sb.ToString()), "text/csv", "TestDataReport.csv");
        }

        private async Task LoadAsync()
        {
            var q = _db.TestData.Include(d => d.Category).Where(d => !d.Retired);
            if (CategoryId.HasValue && CategoryId.Value > 0) q = q.Where(d => d.CategoryId == CategoryId.Value);
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var t = SearchTerm.Trim().ToLower();
                q = q.Where(d => (d.Name != null && d.Name.ToLower().Contains(t)) || (d.Code != null && d.Code.ToLower().Contains(t)));
            }
            Items = await q.OrderBy(d => d.Category.Name).ThenBy(d => d.OrderNo).ThenBy(d => d.Name).ToListAsync();
            CategoryList = await _db.Categories.Where(c => !c.Retired).OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToListAsync();
        }
    }
}