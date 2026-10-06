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

namespace HmisWeb.Pages.Us.Reports
{
    /// <summary>
    /// Migrated from the VFP "Company wise Summary Report" (us0205). VFP grouped TRANS by
    /// company and summed OPD/IPD test counts and amounts over a date range:
    ///   cid, company name, OTests, OAmount, ITests, IAmount, ntest, NAMOUNT
    /// </summary>
    public class CompanySummaryModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public CompanySummaryModel(ApplicationDbContext db) { _db = db; }

        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; }
        [BindProperty(SupportsGet = true)] public long? CompanyId { get; set; }

        public List<SummaryRow> Rows { get; set; } = new();
        public bool Searched { get; set; }

        public string PeriodText =>
            (!FromDate.HasValue && !ToDate.HasValue) ? "All dates"
            : "From: " + (FromDate?.ToString("dd MMM yyyy") ?? "start")
            + " to " + (ToDate?.ToString("dd MMM yyyy") ?? "today");

        public double GrandTotal => Rows.Sum(r => r.TotalAmount);
        public int GrandTests => Rows.Sum(r => r.TotalTests);

        public async Task OnGetAsync()
        {
            var q = _db.Bills
                .Include(b => b.PatientEncounter)
                .Include(b => b.BillItems).ThenInclude(bi => bi.Item)
                .Where(b => !b.Retired && !b.Cancelled);

            if (FromDate.HasValue) q = q.Where(b => (b.BillDate ?? b.InvoiceDate) >= FromDate.Value.Date);
            if (ToDate.HasValue) q = q.Where(b => (b.BillDate ?? b.InvoiceDate) <= ToDate.Value.Date.AddDays(1).AddSeconds(-1));
            if (CompanyId.HasValue) q = q.Where(b => b.CreditCompanyId == CompanyId.Value);

            var bills = await q.ToListAsync();
            Searched = true;

            var companyIds = bills.Where(b => b.CreditCompanyId.HasValue).Select(b => b.CreditCompanyId.Value).Distinct().ToList();
            var companyNames = companyIds.Count == 0
                ? new Dictionary<long, string>()
                : (await _db.Institutions.Where(i => companyIds.Contains(i.Id)).ToListAsync())
                    .GroupBy(i => i.Id).ToDictionary(g => g.Key, g => g.First().Name);

            // key 0 = private patients (no company), matching the VFP left-join on company
            var buckets = new Dictionary<string, SummaryRow>();

            foreach (var b in bills)
            {
                var charges = b.BillItems
                    .Where(bi => !bi.Retired && bi.Item?.Code != null
                                 && bi.Item.Code.StartsWith("INV-US-", StringComparison.OrdinalIgnoreCase))
                    .Sum(bi => bi.NetValue);

                if (charges <= 0) continue;

                var isIpd = b.PatientEncounter != null
                            && b.PatientEncounter.EncounterType == HmisWeb.Data.Enums.EncounterType.Inward;

                var name = b.CreditCompanyId.HasValue
                    && companyNames.TryGetValue(b.CreditCompanyId.Value, out var cn) ? cn : "Private";

                if (!buckets.TryGetValue(name, out var row))
                {
                    row = new SummaryRow { Company = name, CompanyId = b.CreditCompanyId };
                    buckets[name] = row;
                }

                if (isIpd) { row.IpdTests++; row.IpdAmount += charges; }
                else { row.OpdTests++; row.OpdAmount += charges; }
                row.TotalTests++;
                row.TotalAmount += charges;
            }

            Rows = buckets.Values.OrderBy(r => r.Company).ToList();
        }

        public async Task<IActionResult> OnGetExportAsync()
        {
            await OnGetAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Company,OPD Tests,OPD Amount,IPD Tests,IPD Amount,Total Tests,Total Amount");
            foreach (var r in Rows)
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    Csv(r.Company),
                    r.OpdTests.ToString(CultureInfo.InvariantCulture), r.OpdAmount.ToString("0.00", CultureInfo.InvariantCulture),
                    r.IpdTests.ToString(CultureInfo.InvariantCulture), r.IpdAmount.ToString("0.00", CultureInfo.InvariantCulture),
                    r.TotalTests.ToString(CultureInfo.InvariantCulture), r.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture)
                }));
            }
            return File(new UTF8Encoding(false).GetBytes(sb.ToString()), "text/csv", "CompanySummary.csv");
        }

        private static string Csv(string v)
        {
            if (string.IsNullOrEmpty(v)) return "";
            return v.Contains(',') || v.Contains('"') ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
        }

        public class SummaryRow
        {
            public long? CompanyId { get; set; }
            public string Company { get; set; } = "";
            public int OpdTests { get; set; }
            public double OpdAmount { get; set; }
            public int IpdTests { get; set; }
            public double IpdAmount { get; set; }
            public int TotalTests { get; set; }
            public double TotalAmount { get; set; }
        }
    }
}