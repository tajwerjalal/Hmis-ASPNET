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

namespace HmisWeb.Pages.Us
{
    public class ReportsModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public ReportsModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        public List<ReportRow> Rows { get; set; } = new();
        public int TotalScans => Rows.Count;
        public int CompletedScans => Rows.Count(r => r.Status == "Reported");
        public int PendingScans => Rows.Count(r => r.Status != "Reported");
        public string ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        public async Task<IActionResult> OnGetExportAsync()
        {
            await LoadAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Date,PHN,Patient Name,Service,Code,Category,Barcode,Bill,Qty,Collected At,Reported At,Status");

            foreach (var r in Rows)
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    Csv(r.OrderDate),
                    Csv(r.PatientCode),
                    Csv(r.PatientName),
                    Csv(r.ServiceName),
                    Csv(r.Code),
                    Csv(r.Category),
                    Csv(r.Barcode),
                    Csv(r.BillNo),
                    r.Qty.ToString(CultureInfo.InvariantCulture),
                    Csv(r.CollectedAt),
                    Csv(r.ReportedAt),
                    Csv(r.Status)
                }));
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var name = $"us-scan-register-{DateTime.Now:yyyyMMdd-HHmm}.csv";
            return File(bytes, "text/csv", name);
        }

        private async Task LoadAsync()
        {
            FromDate ??= DateTime.Today;
            ToDate ??= DateTime.Today;

            if (ToDate < FromDate)
            {
                ErrorMessage = "The 'To' date cannot be earlier than the 'From' date.";
                ToDate = FromDate;
            }

            var from = FromDate.Value.Date;
            var to = ToDate.Value.Date.AddDays(1).AddTicks(-1);

            var query = _db.UsOrders
                .Include(o => o.Patient).ThenInclude(p => p.Person)
                .Include(o => o.UsService).ThenInclude(s => s.Category)
                .Include(o => o.Bill)
                .Where(o => !o.Retired && o.CreatedAt != null && o.CreatedAt >= from && o.CreatedAt <= to);

            if (string.Equals(Status, "Completed", StringComparison.OrdinalIgnoreCase))
                query = query.Where(o => o.Completed);
            else if (string.Equals(Status, "Pending", StringComparison.OrdinalIgnoreCase))
                query = query.Where(o => !o.Completed);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim();
                query = query.Where(o =>
                    (o.OrderName != null && o.OrderName.Contains(term)) ||
                    (o.Barcode != null && o.Barcode.Contains(term)) ||
                    (o.UsService != null && o.UsService.Code != null && o.UsService.Code.Contains(term)) ||
                    (o.Patient != null && o.Patient.Person != null && o.Patient.Person.Name != null && o.Patient.Person.Name.Contains(term))
                );
            }

            var data = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            Rows = data.Select(o => new ReportRow
            {
                OrderId = o.Id,
                OrderDate = o.CreatedAt,
                PatientCode = o.Patient?.PatientId?.ToString(),
                PatientName = o.Patient?.Person?.NameWithInitials ?? o.Patient?.Person?.Name,
                ServiceName = o.OrderName,
                Code = o.UsService?.Code,
                Category = o.UsService?.Category?.Name,
                Barcode = o.Barcode,
                BillNo = o.Bill?.InvoiceNumber ?? (o.BillId.HasValue ? "#" + o.BillId.Value.ToString(CultureInfo.InvariantCulture) : null),
                Qty = o.Qty,
                CollectedAt = o.SampleCollectedAt,
                ReportedAt = o.CompletedAt,
                Status = o.Completed ? "Reported" : (o.SampleCollected ? "Collected" : "Pending")
            }).ToList();
        }

        private static string Csv(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string Csv(DateTime? value)
        {
            return value.HasValue ? "\"" + value.Value.ToString("dd MMM yyyy HH:mm") + "\"" : string.Empty;
        }

        public class ReportRow
        {
            public long OrderId { get; set; }
            public DateTime? OrderDate { get; set; }
            public string PatientCode { get; set; }
            public string PatientName { get; set; }
            public string ServiceName { get; set; }
            public string Code { get; set; }
            public string Category { get; set; }
            public string Barcode { get; set; }
            public string BillNo { get; set; }
            public int Qty { get; set; }
            public DateTime? CollectedAt { get; set; }
            public DateTime? ReportedAt { get; set; }
            public string Status { get; set; }
        }
    }
}
