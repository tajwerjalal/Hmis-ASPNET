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
using HmisWeb.Services;
using HmisWeb.Models;

namespace HmisWeb.Pages.Xr.Reports
{
    /// <summary>
    /// Migrated from the VFP "Audit Statement" (us0201). VFP selected from TRANS inner-joined
    /// to COLLECT, filtered by date/patient type/company type/company, ordered by
    /// Date, indentno, sid and printed with the COLLECT layout. Here the same rows are
    /// derived from BILL + BILLITEM for ultrasound items only.
    /// </summary>
    public class AuditStatementModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public AuditStatementModel(ApplicationDbContext db) { _db = db; }

        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; }
        [BindProperty(SupportsGet = true)] public bool WholePeriod { get; set; }
        [BindProperty(SupportsGet = true)] public string PatientType { get; set; } = "Both";
        [BindProperty(SupportsGet = true)] public string CompanyType { get; set; } = "Both";
        [BindProperty(SupportsGet = true)] public long? CompanyId { get; set; }
        [BindProperty(SupportsGet = true)] public string RegNo { get; set; }

        public List<Institution> Companies { get; set; } = new();
        public List<AuditRow> Rows { get; set; } = new();
        public bool Searched { get; set; }

        public string PeriodText
        {
            get
            {
                if (WholePeriod) return "Whole period";
                if (!FromDate.HasValue && !ToDate.HasValue) return "All dates";
                if (FromDate.HasValue && ToDate.HasValue && FromDate == ToDate)
                    return "Dated: " + FromDate.Value.ToString("dd MMM yyyy");
                return "From: " + (FromDate?.ToString("dd MMM yyyy") ?? "start")
                     + " to " + (ToDate?.ToString("dd MMM yyyy") ?? "today");
            }
        }

        public int TotalRows => Rows.Count;
        public double TotalCharges => Rows.Sum(r => r.Charges);
        public double TotalNet => Rows.Select(r => r.Net).Distinct().Sum();
        public double TotalBalance => Rows.Select(r => r.Balance).Distinct().Sum();

        public async Task OnGetAsync()
        {
            Companies = await _db.Institutions.Where(i => !i.Retired).OrderBy(i => i.Name).ToListAsync();

            var q = _db.Bills
                .Include(b => b.Patient).ThenInclude(p => p.Person)
                .Include(b => b.PatientEncounter)
                .Include(b => b.BillItems).ThenInclude(bi => bi.Item)
                .Where(b => !b.Retired && !b.Cancelled);

            if (!WholePeriod)
            {
                if (FromDate.HasValue) q = q.Where(b => (b.BillDate ?? b.InvoiceDate) >= FromDate.Value.Date);
                if (ToDate.HasValue) q = q.Where(b => (b.BillDate ?? b.InvoiceDate) <= ToDate.Value.Date.AddDays(1).AddSeconds(-1));
            }

            if (string.Equals(PatientType, "Opd", StringComparison.OrdinalIgnoreCase))
                q = q.Where(b => b.PatientEncounter == null || b.PatientEncounter.EncounterType == HmisWeb.Data.Enums.EncounterType.Opd);
            else if (string.Equals(PatientType, "Ipd", StringComparison.OrdinalIgnoreCase))
                q = q.Where(b => b.PatientEncounter != null && b.PatientEncounter.EncounterType == HmisWeb.Data.Enums.EncounterType.Inward);

            if (string.Equals(CompanyType, "Private", StringComparison.OrdinalIgnoreCase))
                q = q.Where(b => b.CreditCompanyId == null);
            else if (string.Equals(CompanyType, "Panel", StringComparison.OrdinalIgnoreCase))
                q = q.Where(b => b.CreditCompanyId != null);

            if (CompanyId.HasValue) q = q.Where(b => b.CreditCompanyId == CompanyId.Value);

            if (!string.IsNullOrWhiteSpace(RegNo))
            {
                var reg = RegNo.Trim();
                q = q.Where(b =>
                    (b.PatientEncounter != null && b.PatientEncounter.BhtNo != null && b.PatientEncounter.BhtNo.Contains(reg)) ||
                    (b.Patient != null && b.Patient.Person != null && b.Patient.Person.Name != null && b.Patient.Person.Name.Contains(reg)));
            }

            var bills = await q.ToListAsync();
            Searched = true;

            var services = await _db.XrServices().Include(s => s.Category).ToListAsync();
            var byCode = services.ToDictionary(s => s.Code ?? "", s => s, StringComparer.OrdinalIgnoreCase);

            var companyIds = bills.Where(b => b.CreditCompanyId.HasValue).Select(b => b.CreditCompanyId.Value).Distinct().ToList();
            var companyNames = companyIds.Count == 0
                ? new Dictionary<long, string>()
                : (await _db.Institutions.Where(i => companyIds.Contains(i.Id)).ToListAsync())
                    .GroupBy(i => i.Id).ToDictionary(g => g.Key, g => g.First().Name);

            var rows = new List<AuditRow>();
            foreach (var b in bills)
            {
                foreach (var bi in b.BillItems.Where(x => !x.Retired))
                {
                    var itemCode = bi.Item?.Code;
                    if (string.IsNullOrEmpty(itemCode) || !itemCode.StartsWith("INV-XR-", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var svcCode = itemCode.Substring("INV-XR-".Length);
                    byCode.TryGetValue(svcCode, out var svc);

                    rows.Add(new AuditRow
                    {
                        Date = b.BillDate ?? b.InvoiceDate,
                        IndentNo = b.Id,
                        RegNo = b.PatientEncounter?.BhtNo,
                        PatientName = b.Patient?.Person?.Name ?? "",
                        PatientType = b.PatientEncounter != null && b.PatientEncounter.EncounterType == HmisWeb.Data.Enums.EncounterType.Inward ? "IPD" : "OPD",
                        Group = svc?.Category?.Name ?? "",
                        Code = svcCode,
                        Service = svc?.Name ?? bi.Descreption ?? "",
                        Charges = bi.NetValue,
                        Company = b.CreditCompanyId.HasValue && companyNames.TryGetValue(b.CreditCompanyId.Value, out var cn) ? cn : "Private",
                        BillTotal = b.Total,
                        Concession = b.Discount,
                        Net = b.NetTotal,
                        Balance = b.Balance,
                        Posted = b.BillClosed
                    });
                }
            }

            // VFP: ORDER BY Date, collect.indentno, sid
            Rows = rows.OrderBy(r => r.Date).ThenBy(r => r.IndentNo).ThenBy(r => r.Code).ToList();
        }

        public async Task<IActionResult> OnGetExportAsync()
        {
            await OnGetAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Date,Indent,Reg No,Patient,Type,Group,Code,Service,Charges,Company,Bill Total,Concession,Net,Balance,Posted");
            foreach (var r in Rows)
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    (r.Date ?? DateTime.MinValue).ToString("yyyy-MM-dd"),
                    r.IndentNo.ToString(CultureInfo.InvariantCulture),
                    Csv(r.RegNo), Csv(r.PatientName), r.PatientType, Csv(r.Group), Csv(r.Code), Csv(r.Service),
                    r.Charges.ToString("0.00", CultureInfo.InvariantCulture), Csv(r.Company),
                    r.BillTotal.ToString("0.00", CultureInfo.InvariantCulture),
                    r.Concession.ToString("0.00", CultureInfo.InvariantCulture),
                    r.Net.ToString("0.00", CultureInfo.InvariantCulture),
                    r.Balance.ToString("0.00", CultureInfo.InvariantCulture),
                    r.Posted ? "Posted" : "Un-Posted"
                }));
            }
            return File(new UTF8Encoding(false).GetBytes(sb.ToString()), "text/csv", "AuditStatement.csv");
        }

        private static string Csv(string v)
        {
            if (string.IsNullOrEmpty(v)) return "";
            return v.Contains(',') || v.Contains('"') ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
        }

        public class AuditRow
        {
            public DateTime? Date { get; set; }
            public long IndentNo { get; set; }
            public string RegNo { get; set; } = "";
            public string PatientName { get; set; } = "";
            public string PatientType { get; set; } = "";
            public string Group { get; set; } = "";
            public string Code { get; set; } = "";
            public string Service { get; set; } = "";
            public double Charges { get; set; }
            public string Company { get; set; } = "";
            public double BillTotal { get; set; }
            public double Concession { get; set; }
            public double Net { get; set; }
            public double Balance { get; set; }
            public bool Posted { get; set; }
        }
    }
}