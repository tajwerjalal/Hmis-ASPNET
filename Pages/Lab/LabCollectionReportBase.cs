using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HmisWeb.Data;

namespace HmisWeb.Pages.Lab
{
    /// <summary>
    /// Base for the five laboratory collection reports (periodic / company / patient /
    /// group / test wise) migrated from VFP Labsys <c>lb02401</c> - <c>lb02405</c>.
    /// </summary>
    public abstract class LabCollectionReportBase : PageModel
    {
        protected readonly ApplicationDbContext Db;
        protected LabCollectionReportBase(ApplicationDbContext db) { Db = db; }

        public abstract string ReportTitle { get; }
        public abstract string GroupHeader { get; }
        public abstract string CsvFileName { get; }

        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; } = DateTime.Today.AddDays(-30);
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; } = DateTime.Today;

        public List<LabCollectionQuery.Row> Rows { get; set; } = new();
        public double TotalNet => Rows.Sum(r => r.Net);
        public double TotalGross => Rows.Sum(r => r.Gross);
        public int TotalCount => Rows.Sum(r => r.Count);

        protected abstract Task<List<LabCollectionQuery.Row>> BuildRowsAsync(DateTime from, DateTime to);

        protected async Task LoadAsync()
        {
            var from = (FromDate ?? DateTime.Today.AddDays(-30)).Date;
            var to = (ToDate ?? DateTime.Today).Date;
            Rows = await BuildRowsAsync(from, to);
        }

        public async Task OnGetAsync() => await LoadAsync();

        public async Task<IActionResult> OnGetExportAsync()
        {
            await LoadAsync();
            var csv = LabCollectionQuery.Csv(Rows, GroupHeader);
            return File(new UTF8Encoding(false).GetBytes(csv), "text/csv", CsvFileName);
        }
    }
}