using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;

namespace HmisWeb.Pages.Lab
{
    /// <summary>
    /// Periodic (date wise) laboratory collection report - migrated from VFP Labsys
    /// <c>lb02401</c>.
    /// </summary>
    public class PeriodicCollectionReportModel : LabCollectionReportBase
    {
        public PeriodicCollectionReportModel(ApplicationDbContext db) : base(db) { }

        [BindProperty(SupportsGet = true)]
        public string Period { get; set; } = "day";

        public override string ReportTitle => "Periodic Collection Report";
        public override string GroupHeader => "Period";
        public override string CsvFileName => "PeriodicCollectionReport.csv";

        protected override async Task<List<LabCollectionQuery.Row>> BuildRowsAsync(DateTime from, DateTime to)
        {
            var items = await LabCollectionQuery.LabBillItems(Db, from, to)
                .Select(b => new { Date = b.Bill.CreatedAt.Value, b.GrossValue, b.Discount, b.NetValue })
                .ToListAsync();

            return items
                .GroupBy(b => Period == "month" ? b.Date.ToString("yyyy-MM") : b.Date.ToString("yyyy-MM-dd"))
                .Select(g => new LabCollectionQuery.Row
                {
                    Group = g.Key,
                    Count = g.Count(),
                    Gross = g.Sum(x => x.GrossValue),
                    Discount = g.Sum(x => x.Discount),
                    Net = g.Sum(x => x.NetValue)
                })
                .OrderBy(r => r.Group)
                .ToList();
        }
    }
}