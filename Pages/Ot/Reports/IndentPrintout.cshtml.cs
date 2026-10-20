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

namespace HmisWeb.Pages.Ot.Reports
{
    /// <summary>
    /// Migrated from the VFP "Indent Printout" (us0250). VFP took an indent/lab number and
    /// printed the COLLECT header joined to its TRANS lines. In HMIS the indent is BILL and
    /// the lines are BILLITEM, so this reproduces the same single-indent printout.
    /// </summary>
    public class IndentPrintoutModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        /// <summary>Billing-item prefix that marks a line as belonging to ultrasound.</summary>
        private const string MirrorPrefix = "INV-US-";

        public IndentPrintoutModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public long? IndentNo { get; set; }

        public bool Found { get; set; }
        public IndentHeader Header { get; set; }
        public List<IndentLine> Lines { get; set; } = new();
        public string ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            if (!IndentNo.HasValue) return;

            var bill = await _db.Bills
                .Include(b => b.Patient).ThenInclude(p => p.Person)
                .Include(b => b.PatientEncounter)
                .FirstOrDefaultAsync(b => b.Id == IndentNo.Value);

            if (bill == null)
            {
                ErrorMessage = $"No indent found for number {IndentNo.Value}.";
                return;
            }

            var companyName = await _db.Institutions
                .Where(i => i.Id == bill.CreditCompanyId)
                .Select(i => i.Name)
                .FirstOrDefaultAsync();

            Header = new IndentHeader
            {
                IndentNo = bill.Id,
                Date = bill.BillDate ?? bill.InvoiceDate,
                InvoiceNo = bill.InvoiceNumber,
                PatientName = bill.Patient?.Person?.NameWithInitials ?? bill.Patient?.Person?.Name,
                PatientCode = bill.Patient?.PatientId?.ToString(CultureInfo.InvariantCulture),
                EncounterNo = bill.PatientEncounter?.BhtNo,
                EncounterType = bill.PatientEncounter?.EncounterType?.ToString(),
                Company = companyName,
                Total = bill.Total,
                Discount = bill.Discount,
                NetTotal = bill.NetTotal,
                Balance = bill.Balance,
                Posted = bill.BillClosed,
                Cancelled = bill.Cancelled,
                Retired = bill.Retired
            };

            Lines = await _db.BillItems
                .Where(bi => bi.BillId == bill.Id && !bi.Retired)
                .OrderBy(bi => bi.SearialNo)
                .Select(bi => new IndentLine
                {
                    Serial = bi.SearialNo,
                    Description = bi.Descreption,
                    Qty = bi.Qty ?? 0,
                    Rate = bi.Rate,
                    Discount = bi.Discount,
                    Amount = bi.NetValue,
                    IsUltrasound = true
                })
                .ToListAsync();

            Found = true;
        }

        public async Task<IActionResult> OnGetExportAsync()
        {
            await OnGetAsync();
            if (!Found) return Content("Indent not found.", "text/plain");

            var sb = new StringBuilder();
            sb.AppendLine("Indent No,Date,Patient,Company,Total,Net");
            sb.AppendLine(string.Join(",", new[]
            {
                Header.IndentNo.ToString(CultureInfo.InvariantCulture),
                (Header.Date ?? DateTime.MinValue).ToString("yyyy-MM-dd"),
                Csv(Header.PatientName), Csv(Header.Company),
                Header.Total.ToString("0.00", CultureInfo.InvariantCulture),
                Header.NetTotal.ToString("0.00", CultureInfo.InvariantCulture)
            }));
            sb.AppendLine();
            sb.AppendLine("#,Description,Qty,Rate,Discount,Amount");
            foreach (var l in Lines)
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    l.Serial.ToString(CultureInfo.InvariantCulture),
                    Csv(l.Description),
                    l.Qty.ToString("0.##", CultureInfo.InvariantCulture),
                    l.Rate.ToString("0.00", CultureInfo.InvariantCulture),
                    l.Discount.ToString("0.00", CultureInfo.InvariantCulture),
                    l.Amount.ToString("0.00", CultureInfo.InvariantCulture)
                }));
            }

            return File(new UTF8Encoding(false).GetBytes(sb.ToString()), "text/csv", $"Indent-{IndentNo}.csv");
        }

        private static string Csv(string v)
        {
            if (string.IsNullOrEmpty(v)) return "";
            return v.Contains(',') || v.Contains('"') ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
        }

        public class IndentHeader
        {
            public long IndentNo { get; set; }
            public DateTime? Date { get; set; }
            public string InvoiceNo { get; set; }
            public string PatientName { get; set; }
            public string PatientCode { get; set; }
            public string EncounterNo { get; set; }
            public string EncounterType { get; set; }
            public string Company { get; set; }
            public double Total { get; set; }
            public double Discount { get; set; }
            public double NetTotal { get; set; }
            public double Balance { get; set; }
            public bool Posted { get; set; }
            public bool Cancelled { get; set; }
            public bool Retired { get; set; }
        }

        public class IndentLine
        {
            public int Serial { get; set; }
            public string Description { get; set; }
            public double Qty { get; set; }
            public double Rate { get; set; }
            public double Discount { get; set; }
            public double Amount { get; set; }
            public bool IsUltrasound { get; set; }
        }
    }
}