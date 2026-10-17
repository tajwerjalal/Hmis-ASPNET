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
    /// <summary>Test Remarks Report - canned remarks by test. VFP Labsys report tbremark.</summary>
    public class TestRemarksReportModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public TestRemarksReportModel(ApplicationDbContext db) { _db = db; }

        [BindProperty(SupportsGet = true)] public string SearchTerm { get; set; }
        public List<TestRemark> Items { get; set; } = new();

        public async Task OnGetAsync() => await LoadAsync();

        public async Task<IActionResult> OnGetExportAsync()
        {
            await LoadAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Test,Remarks");
            foreach (var r in Items)
                sb.AppendLine(string.Join(",", LabCollectionQuery.Q(r.Item != null ? r.Item.Name : "(any)"), LabCollectionQuery.Q(r.Remarks)));
            return File(new UTF8Encoding(false).GetBytes(sb.ToString()), "text/csv", "TestRemarksReport.csv");
        }

        private async Task LoadAsync()
        {
            var q = _db.TestRemarks.Include(r => r.Item).Where(r => !r.Retired);
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var t = SearchTerm.Trim().ToLower();
                q = q.Where(r => r.Remarks != null && r.Remarks.ToLower().Contains(t));
            }
            Items = await q.OrderBy(r => r.Item.Name).ThenBy(r => r.Id).ToListAsync();
        }
    }
}