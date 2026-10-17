using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;

namespace HmisWeb.Pages.Lab
{
    /// <summary>
    /// Company wise laboratory collection report - migrated from VFP Labsys <c>lb02402</c>.
    /// </summary>
    public class CompanyCollectionReportModel : LabCollectionReportBase
    {
        public CompanyCollectionReportModel(ApplicationDbContext db) : base(db) { }

        public override string ReportTitle => "Company wise Collection Report";
        public override string GroupHeader => "Company";
        public override string CsvFileName => "CompanyCollectionReport.csv";

        protected override async Task<List<LabCollectionQuery.Row>> BuildRowsAsync(DateTime from, DateTime to)
        {
            var items = await LabCollectionQuery.LabBillItems(Db, from, to)
                .Select(b => new { Company = b.Bill.Institution != null ? b.Bill.Institution.Name : "(Cash)", b.GrossValue, b.Discount, b.NetValue })
                .ToListAsync();

            return items
                .GroupBy(b => string.IsNullOrEmpty(b.Company) ? "(Cash)" : b.Company)
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