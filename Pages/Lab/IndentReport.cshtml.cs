using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// <summary>
    /// Indent No. wise Report - laboratory investigation orders (indents). Migrated from VFP
    /// Labsys form/report <c>frmindwise</c>.
    /// </summary>
    public class IndentReportModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public IndentReportModel(ApplicationDbContext db) { _db = db; }

        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; } = DateTime.Today.AddDays(-30);
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; } = DateTime.Today;
        [BindProperty(SupportsGet = true)] public string SearchTerm { get; set; }

        public List<InvestigationOrder> Items { get; set; } = new();

        public async Task OnGetAsync() => await LoadAsync();

        public async Task<IActionResult> OnGetExportAsync()
        {
            await LoadAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Indent,Date,Patient,Test,Ordered,Sample Collected,Completed,Due Date");
            foreach (var o in Items)
                sb.AppendLine(string.Join(",",
                    o.Id.ToString(),
                    o.CreatedAt?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    LabCollectionQuery.Q(o.Patient?.Person?.Name),
                    LabCollectionQuery.Q(o.Item?.Name ?? o.InvestigationName),
                    o.Ordered ? "yes" : "no",
                    o.SampleCollected ? "yes" : "no",
                    o.Completed ? "yes" : "no",
                    o.DueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
            return File(new UTF8Encoding(false).GetBytes(sb.ToString()), "text/csv", "IndentReport.csv");
        }

        private async Task LoadAsync()
        {
            var toExclusive = (ToDate ?? DateTime.Today).Date.AddDays(1);
            var q = _db.InvestigationOrders
                .Include(o => o.Patient).ThenInclude(p => p.Person)
                .Include(o => o.Item)
                .Where(o => !o.Retired && o.CreatedAt != null
                    && o.CreatedAt >= (FromDate ?? DateTime.Today.AddDays(-30)).Date
                    && o.CreatedAt < toExclusive);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var t = SearchTerm.Trim().ToLower();
                q = q.Where(o => (o.Patient.Person.Name != null && o.Patient.Person.Name.ToLower().Contains(t))
                    || (o.InvestigationName != null && o.InvestigationName.ToLower().Contains(t)));
            }

            Items = await q.OrderByDescending(o => o.CreatedAt).Take(1000).ToListAsync();
        }
    }
}