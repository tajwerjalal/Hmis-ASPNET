using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;

namespace HmisWeb.Pages.Lab
{
    /// <summary>
    /// Test wise laboratory collection report - migrated from VFP Labsys <c>lb02405</c>.
    /// </summary>
    public class TestCollectionReportModel : LabCollectionReportBase
    {
        public TestCollectionReportModel(ApplicationDbContext db) : base(db) { }

        public override string ReportTitle => "Test wise Collection Report";
        public override string GroupHeader => "Test";
        public override string CsvFileName => "TestCollectionReport.csv";

        protected override async Task<List<LabCollectionQuery.Row>> BuildRowsAsync(DateTime from, DateTime to)
        {
            var items = await LabCollectionQuery.LabBillItems(Db, from, to)
                .Select(b => new { Test = b.Item.Name, b.GrossValue, b.Discount, b.NetValue })
                .ToListAsync();

            return items
                .GroupBy(b => string.IsNullOrEmpty(b.Test) ? "(unknown)" : b.Test)
                .Select(g => new LabCollectionQuery.Row
                {
                    Group = g.Key,
                    Count = g.Count(),
                    Gross = g.Sum(x => x.GrossValue),
                    Discount = g.Sum(x => x.Discount),
                    Net = g.Sum(x => x.NetValue)
                })
                .OrderByDescending(r => r.Net)
                .ToList();
        }
    }
}