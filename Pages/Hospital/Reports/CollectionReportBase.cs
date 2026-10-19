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

namespace HmisWeb.Pages.Hospital.Reports
{
    /// <summary>
    /// Shared implementation behind the four VFP collection reports
    /// (us022501 Periodic, us022510 Company Wise, us022520 Patient Wise,
    /// us022530 Test Wise). In VFP all four ran the same form and differed only in the
    /// final ORDER BY, so that is the only thing each derived screen changes here.
    ///
    /// VFP mapped as:  collect -&gt; BILL,  trans -&gt; BILLITEM,  services/sergrp -&gt;
    /// USSERVICE / USSERVICECATEGORY,  company -&gt; INSTITUTION (BILL.CreditCompanyId).
    /// </summary>
    public abstract class CollectionReportBase : PageModel
    {
        protected readonly ApplicationDbContext Db;

        protected CollectionReportBase(ApplicationDbContext db)
        {
            Db = db;
        }

        // ---- filters (all four VFP screens shared these) ----
        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; } = DateTime.Today.AddDays(-30);
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; } = DateTime.Today;
        [BindProperty(SupportsGet = true)] public bool WholePeriod { get; set; }
        [BindProperty(SupportsGet = true)] public string PatientType { get; set; } = "Both";   // Ipd | Opd | Both
        [BindProperty(SupportsGet = true)] public string CompanyType { get; set; } = "Both";   // Private | Panel | Both
        [BindProperty(SupportsGet = true)] public string PostedStatus { get; set; } = "Both";  // Posted | UnPosted | Both
        [BindProperty(SupportsGet = true)] public long? CompanyId { get; set; }
        [BindProperty(SupportsGet = true)] public long? CategoryId { get; set; }
        [BindProperty(SupportsGet = true)] public string ServiceCode { get; set; }
        [BindProperty(SupportsGet = true)] public string RegNo { get; set; }

        public List<Institution> Companies { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<CollectionRow> Rows { get; set; } = new();
        public bool Searched { get; set; }
        public string ErrorMessage { get; set; }

        public abstract string ReportTitle { get; }
        public abstract string ReportSubtitle { get; }

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

        public int TotalTests => Rows.Count;
        public double TotalCharges => Rows.Sum(r => r.Charges);

        protected virtual async Task BuildAsync()
        {
            Companies = await Db.Institutions.Where(i => !i.Retired).OrderBy(i => i.Name).ToListAsync();
            Categories = await Db.Categories.Where(c => !c.Retired).OrderBy(c => c.Name).ToListAsync();

            var billsQuery = Db.Bills
                .Include(b => b.Patient).ThenInclude(p => p.Person)
                .Include(b => b.PatientEncounter)
                .Include(b => b.BillItems).ThenInclude(bi => bi.Item)
                .Where(b => !b.Retired && !b.Cancelled);

            if (!WholePeriod)
            {
                if (FromDate.HasValue) billsQuery = billsQuery.Where(b => (b.BillDate ?? b.InvoiceDate) >= FromDate.Value.Date);
                if (ToDate.HasValue) billsQuery = billsQuery.Where(b => (b.BillDate ?? b.InvoiceDate) <= ToDate.Value.Date.AddDays(1).AddSeconds(-1));
            }

            // VFP: .and.Type = "O" / "I"
            if (string.Equals(PatientType, "Opd", StringComparison.OrdinalIgnoreCase))
                billsQuery = billsQuery.Where(b => b.PatientEncounter == null || b.PatientEncounter.EncounterType == HmisWeb.Data.Enums.EncounterType.Opd);
            else if (string.Equals(PatientType, "Ipd", StringComparison.OrdinalIgnoreCase))
                billsQuery = billsQuery.Where(b => b.PatientEncounter != null && b.PatientEncounter.EncounterType == HmisWeb.Data.Enums.EncounterType.Inward);

            // VFP: .and.Empty(cid) / .and.!empty(cid)
            if (string.Equals(CompanyType, "Private", StringComparison.OrdinalIgnoreCase))
                billsQuery = billsQuery.Where(b => b.CreditCompanyId == null);
            else if (string.Equals(CompanyType, "Panel", StringComparison.OrdinalIgnoreCase))
                billsQuery = billsQuery.Where(b => b.CreditCompanyId != null);

            if (CompanyId.HasValue) billsQuery = billsQuery.Where(b => b.CreditCompanyId == CompanyId.Value);

            // VFP: posted='P' / posted=' '
            if (string.Equals(PostedStatus, "Posted", StringComparison.OrdinalIgnoreCase))
                billsQuery = billsQuery.Where(b => b.BillClosed);
            else if (string.Equals(PostedStatus, "UnPosted", StringComparison.OrdinalIgnoreCase))
                billsQuery = billsQuery.Where(b => !b.BillClosed);

            if (!string.IsNullOrWhiteSpace(RegNo))
            {
                var reg = RegNo.Trim();
                billsQuery = billsQuery.Where(b =>
                    (b.PatientEncounter != null && b.PatientEncounter.BhtNo != null && b.PatientEncounter.BhtNo.Contains(reg)) ||
                    (b.Patient != null && b.Patient.PatientId != null && b.Patient.PatientId.ToString().Contains(reg)) ||
                    (b.Patient != null && b.Patient.Person != null && b.Patient.Person.Name != null && b.Patient.Person.Name.Contains(reg)));
            }

            var bills = await billsQuery.ToListAsync();
            Searched = true;

            // services are few (61 rows) so mapping the INV-US-{code} mirror in memory
            // avoids a cross-table join on a computed column.
            var services = await Db.Items.Include(s => s.Category).ToListAsync();
            var byCode = services.ToDictionary(s => s.Code ?? "", s => s, StringComparer.OrdinalIgnoreCase);

            var companyIds = bills.Where(b => b.CreditCompanyId.HasValue).Select(b => b.CreditCompanyId.Value).Distinct().ToList();
            var companyNames = companyIds.Count == 0
                ? new Dictionary<long, string>()
                : (await Db.Institutions.Where(i => companyIds.Contains(i.Id)).ToListAsync())
                    .GroupBy(i => i.Id).ToDictionary(g => g.Key, g => g.First().Name);

            var rows = new List<CollectionRow>();

            foreach (var b in bills)
            {
                foreach (var bi in b.BillItems.Where(x => !x.Retired))
                {
                    var itemCode = bi.Item?.Code;
                    if (string.IsNullOrEmpty(itemCode))
                        continue;

                    var svcCode = itemCode;
                    byCode.TryGetValue(svcCode, out var svc);

                    if (CategoryId.HasValue && (svc?.CategoryId != CategoryId.Value)) continue;
                    if (!string.IsNullOrWhiteSpace(ServiceCode) &&
                        !string.Equals(svcCode, ServiceCode.Trim(), StringComparison.OrdinalIgnoreCase)) continue;

                    rows.Add(new CollectionRow
                    {
                        Date = b.BillDate ?? b.InvoiceDate,
                        IndentNo = b.Id,
                        InvoiceNo = b.InvoiceNumber,
                        RegNo = b.PatientEncounter?.BhtNo,
                        PatientName = b.Patient?.Person?.NameWithInitials ?? b.Patient?.Person?.Name,
                        PatientType = b.PatientEncounter == null ? "OPD" : (b.PatientEncounter.EncounterType == HmisWeb.Data.Enums.EncounterType.Inward ? "IPD" : "OPD"),
                        CompanyId = b.CreditCompanyId,
                        Company = b.CreditCompanyId.HasValue && companyNames.TryGetValue(b.CreditCompanyId.Value, out var cn) ? cn : "Private",
                        Category = svc?.Category?.Name ?? "",
                        Code = svcCode,
                        Service = svc?.Name ?? bi.Descreption ?? "",
                        Charges = bi.NetValue,
                        BillTotal = b.Total,
                        Concession = b.Discount,
                        Net = b.NetTotal,
                        Balance = b.Balance,
                        Posted = b.BillClosed
                    });
                }
            }

            Rows = ApplyOrdering(rows).ToList();
        }

        /// <summary>The only real difference between the four VFP collection screens.</summary>
        protected abstract IEnumerable<CollectionRow> ApplyOrdering(IEnumerable<CollectionRow> rows);

        public async Task OnGetAsync() => await BuildAsync();

        public async Task<IActionResult> OnGetExportAsync()
        {
            await BuildAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Date,Indent No,Invoice,Reg No,Patient,Type,Company,Group,Code,Service,Charges,Bill Total,Concession,Net,Balance,Posted");
            foreach (var r in Rows)
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    (r.Date ?? DateTime.MinValue).ToString("yyyy-MM-dd"),
                    r.IndentNo.ToString(CultureInfo.InvariantCulture),
                    Csv(r.InvoiceNo), Csv(r.RegNo), Csv(r.PatientName), r.PatientType, Csv(r.Company),
                    Csv(r.Category), Csv(r.Code), Csv(r.Service),
                    r.Charges.ToString("0.00", CultureInfo.InvariantCulture),
                    r.BillTotal.ToString("0.00", CultureInfo.InvariantCulture),
                    r.Concession.ToString("0.00", CultureInfo.InvariantCulture),
                    r.Net.ToString("0.00", CultureInfo.InvariantCulture),
                    r.Balance.ToString("0.00", CultureInfo.InvariantCulture),
                    r.Posted ? "Posted" : "Un-Posted"
                }));
            }
            return File(new UTF8Encoding(false).GetBytes(sb.ToString()), "text/csv", ReportTitle.Replace(' ', '_') + ".csv");
        }

        protected static string Csv(string v)
        {
            if (string.IsNullOrEmpty(v)) return "";
            return v.Contains(',') || v.Contains('"') ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
        }

        public class CollectionRow
        {
            public DateTime? Date { get; set; }
            public long IndentNo { get; set; }
            public string InvoiceNo { get; set; } = "";
            public string RegNo { get; set; } = "";
            public string PatientName { get; set; } = "";
            public string PatientType { get; set; } = "";
            public long? CompanyId { get; set; }
            public string Company { get; set; } = "";
            public string Category { get; set; } = "";
            public string Code { get; set; } = "";
            public string Service { get; set; } = "";
            public double Charges { get; set; }
            public double BillTotal { get; set; }
            public double Concession { get; set; }
            public double Net { get; set; }
            public double Balance { get; set; }
            public bool Posted { get; set; }
        }
    }

    // us022501 - ORDER BY Date, indentno, sid
    public class CollectionPeriodicModel : CollectionReportBase
    {
        public CollectionPeriodicModel(ApplicationDbContext db) : base(db) { }
        public override string ReportTitle => "Periodic Collection Report";
        public override string ReportSubtitle => "VFP us022501 - ordered by date, indent number and test";
        protected override IEnumerable<CollectionRow> ApplyOrdering(IEnumerable<CollectionRow> rows)
            => rows.OrderBy(r => r.Date).ThenBy(r => r.IndentNo).ThenBy(r => r.Code);
    }

    // us022510 - ORDER BY cid, Date, indentno, sid
    public class CollectionCompanyModel : CollectionReportBase
    {
        public CollectionCompanyModel(ApplicationDbContext db) : base(db) { }
        public override string ReportTitle => "Company Wise Collection Report";
        public override string ReportSubtitle => "VFP us022510 - grouped by company first";
        protected override IEnumerable<CollectionRow> ApplyOrdering(IEnumerable<CollectionRow> rows)
            => rows.OrderBy(r => r.Company).ThenBy(r => r.Date).ThenBy(r => r.IndentNo).ThenBy(r => r.Code);
    }

    // us022520 - ORDER BY regno, Date, sid
    public class CollectionPatientModel : CollectionReportBase
    {
        public CollectionPatientModel(ApplicationDbContext db) : base(db) { }
        public override string ReportTitle => "Patient Wise Collection Report";
        public override string ReportSubtitle => "VFP us022520 - grouped by patient first";
        protected override IEnumerable<CollectionRow> ApplyOrdering(IEnumerable<CollectionRow> rows)
            => rows.OrderBy(r => r.RegNo).ThenBy(r => r.PatientName).ThenBy(r => r.Date).ThenBy(r => r.Code);
    }

    // us022530 - ORDER BY sid, Date, indentno
    public class CollectionServiceModel : CollectionReportBase
    {
        public CollectionServiceModel(ApplicationDbContext db) : base(db) { }
        public override string ReportTitle => "Test Wise Collection Report";
        public override string ReportSubtitle => "VFP us022530 - grouped by test first";
        protected override IEnumerable<CollectionRow> ApplyOrdering(IEnumerable<CollectionRow> rows)
            => rows.OrderBy(r => r.Code).ThenBy(r => r.Date).ThenBy(r => r.IndentNo);
    }
}