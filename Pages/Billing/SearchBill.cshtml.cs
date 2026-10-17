using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HmisWeb.Pages.Billing
{
    /// <summary>
    /// Search OPD / IPD Bill No. - find a bill and view the patient and its charges.
    /// Migrated from the VFP Billsys forms <c>osrchbil</c> (OPD) and <c>isrchbil</c> (IPD).
    /// </summary>
    public class SearchBillModel : Shared.PagedPageModel
    {
        private readonly ApplicationDbContext _db;
        public SearchBillModel(ApplicationDbContext db) { _db = db; }

        [BindProperty(SupportsGet = true)] public string Type { get; set; } = "opd";
        [BindProperty(SupportsGet = true)] public string BillNo { get; set; }
        [BindProperty(SupportsGet = true)] public string PatientSearch { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; }

        public List<Bill> Bills { get; set; } = new();
        public Bill Selected { get; set; }
        public List<BillItem> SelectedItems { get; set; } = new();

        public bool IsInward => string.Equals(Type, "inward", StringComparison.OrdinalIgnoreCase);
        public string TypeLabel => IsInward ? "IPD" : "OPD";

        private static readonly BillTypeAtomic[] OpdTypes =
        {
            BillTypeAtomic.OpdBill, BillTypeAtomic.OpdBilledBill, BillTypeAtomic.OpdPreBill, BillTypeAtomic.OpdReturnBill
        };
        private static readonly BillTypeAtomic[] InwardTypes =
        {
            BillTypeAtomic.InwardBill, BillTypeAtomic.InwardFinalBill, BillTypeAtomic.InwardPreBill, BillTypeAtomic.InwardReturnBill
        };

        public async Task OnGetAsync()
        {
            await LoadAsync();
            if (TryGetId("id", out var id) && id > 0)
            {
                Selected = await _db.Bills
                    .Include(b => b.Patient).ThenInclude(p => p.Person)
                    .Include(b => b.PatientEncounter)
                    .Include(b => b.Institution)
                    .FirstOrDefaultAsync(b => b.Id == id);
                if (Selected != null)
                {
                    SelectedItems = await _db.BillItems
                        .Include(i => i.Item)
                        .Where(i => i.BillId == id && !i.Retired)
                        .OrderBy(i => i.SearialNo)
                        .ToListAsync();
                }
            }
        }

        public async Task<IActionResult> OnGetExportAsync()
        {
            await LoadAsync(export: true);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("BillNo,Date,Patient,Total,Paid,Balance,Type");
            foreach (var b in Bills)
                sb.AppendLine(string.Join(",",
                    b.Id,
                    b.CreatedAt?.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture),
                    Csv(b.Patient?.Person?.Name ?? ""),
                    b.Total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    b.PaidAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    b.Balance.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    b.BillTypeAtomic?.ToString()));
            return File(new System.Text.UTF8Encoding(false).GetBytes(sb.ToString()), "text/csv", $"Search{TypeLabel}Bill.csv");
        }

        private static string Csv(string v) => string.IsNullOrEmpty(v) ? "" : (v.Contains(',') || v.Contains('"') ? "\"" + v.Replace("\"", "\"\"") + "\"" : v);

        private async Task LoadAsync(bool export = false)
        {
            var types = IsInward ? InwardTypes : OpdTypes;
            var q = _db.Bills
                .Include(b => b.Patient).ThenInclude(p => p.Person)
                .Where(b => !b.Retired && b.BillTypeAtomic != null && types.Contains(b.BillTypeAtomic.Value));

            if (FromDate.HasValue) q = q.Where(b => b.CreatedAt >= FromDate.Value.Date);
            if (ToDate.HasValue) q = q.Where(b => b.CreatedAt < ToDate.Value.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(BillNo))
            {
                var bn = BillNo.Trim();
                q = q.Where(b => b.Id.ToString() == bn || b.ReferenceNumber == bn);
            }
            if (!string.IsNullOrWhiteSpace(PatientSearch))
            {
                var t = PatientSearch.Trim().ToLower();
                q = q.Where(b => (b.Patient.Person.Name != null && b.Patient.Person.Name.ToLower().Contains(t))
                              || (b.Patient.Phn != null && b.Patient.Phn.ToLower().Contains(t)));
            }

            var ordered = q.OrderByDescending(b => b.Id);
            TotalCount = await ordered.CountAsync();
            Bills = export
                ? await ordered.ToListAsync()
                : await ordered.Skip(Skip).Take(Take).ToListAsync();
        }

        private bool TryGetId(string key, out long id)
        {
            id = 0;
            return Request.Query.TryGetValue(key, out var raw) && long.TryParse(raw, out id);
        }
    }
}