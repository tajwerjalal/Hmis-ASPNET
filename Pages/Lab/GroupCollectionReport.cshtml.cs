using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;

namespace HmisWeb.Pages.Lab
{
    /// <summary>
    /// Group (test classification) wise laboratory collection report - migrated from VFP
    /// Labsys <c>lb02404</c>.
    /// </summary>
    public class GroupCollectionReportModel : LabCollectionReportBase
    {
        public GroupCollectionReportModel(ApplicationDbContext db) : base(db) { }

        public override string ReportTitle => "Group wise Collection Report";
        public override string GroupHeader => "Group";
        public override string CsvFileName => "GroupCollectionReport.csv";

        protected override async Task<List<LabCollectionQuery.Row>> BuildRowsAsync(DateTime from, DateTime to)
        {
            var items = await LabCollectionQuery.LabBillItems(Db, from, to)
                .Select(b => new { Group = b.Item.Category != null ? b.Item.Category.Name : "(Unclassified)", b.GrossValue, b.Discount, b.NetValue })
                .ToListAsync();

            return items
                .GroupBy(b => string.IsNullOrEmpty(b.Group) ? "(Unclassified)" : b.Group)
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