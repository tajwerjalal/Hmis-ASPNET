using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;

namespace HmisWeb.Pages.Lab
{
    /// <summary>
    /// Patient wise laboratory collection report - migrated from VFP Labsys <c>lb02403</c>.
    /// </summary>
    public class PatientCollectionReportModel : LabCollectionReportBase
    {
        public PatientCollectionReportModel(ApplicationDbContext db) : base(db) { }

        public override string ReportTitle => "Patient wise Collection Report";
        public override string GroupHeader => "Patient";
        public override string CsvFileName => "PatientCollectionReport.csv";

        protected override async Task<List<LabCollectionQuery.Row>> BuildRowsAsync(DateTime from, DateTime to)
        {
            var items = await LabCollectionQuery.LabBillItems(Db, from, to)
                .Select(b => new
                {
                    Patient = b.Bill.Patient != null && b.Bill.Patient.Person != null
                        ? (b.Bill.Patient.Person.Name ?? b.Bill.Patient.Person.FullName)
                        : "(unknown)",
                    b.GrossValue, b.Discount, b.NetValue
                })
                .ToListAsync();

            return items
                .GroupBy(b => string.IsNullOrEmpty(b.Patient) ? "(unknown)" : b.Patient)
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