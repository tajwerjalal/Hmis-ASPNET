// Developer : Tajwer Jalal
// Date/Time : 29-Oct-2026 12:05 PM
// Change    : Lab indent unpaid list page - /lab/indent/unpaid.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Lab
{
    /// <summary>
    /// Lists laboratory indents that are UNPAID (payment_status = 'UNPAID').
    /// Route: /lab/indent/unpaid?regNo=..&dateFrom=..&dateTo=..&indentNo=..
    /// </summary>
    public class IndentUnpaidModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;

        public IndentUnpaidModel(ApplicationDbContext db, IAppSession appSession)
        {
            _db = db;
            _appSession = appSession;
        }

        [BindProperty] public string RegNo { get; set; }
        [BindProperty] public string DateFrom { get; set; }
        [BindProperty] public string DateTo { get; set; }
        [BindProperty] public string IndentNo { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; } = 10;

        public List<Bill> Rows { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => (CurrentPage - 1) * PageSize < TotalCount;
        public Bill SelectedBill { get; set; }
        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadUnpaidAsync();
            return Page();
        }

        /// <summary>
        /// Loads UNPAID indents only (payment_status = 'UNPAID') for the filtered period/patient.
        /// Unpaid rows are never listed on the main /lab/indent page.
        /// </summary>
        private async Task LoadUnpaidAsync()
        {
            var query = _db.Bills.AsNoTracking()
                .Where(b => !b.Retired && !b.Cancelled && !b.Paid);

            long? patientAppId = null;
            if (!string.IsNullOrWhiteSpace(RegNo) && long.TryParse(RegNo, out var regNo))
            {
                patientAppId = await _db.Patients.AsNoTracking()
                    .Where(p => p.PatientId == regNo && !p.Retired)
                    .Select(p => (long?)p.Id)
                    .FirstOrDefaultAsync();
            }

            if (patientAppId.HasValue)
                query = query.Where(b => b.PatientId == patientAppId);

            DateTime? fromDate = null;
            DateTime? toDate = null;
            if (DateTime.TryParse(DateFrom, out var from))
                fromDate = from;
            if (DateTime.TryParse(DateTo, out var to))
                toDate = to;

            if (fromDate.HasValue)
                query = query.Where(b => b.BillDate != null && b.BillDate >= fromDate);
            if (toDate.HasValue)
                query = query.Where(b => b.BillDate != null && b.BillDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));
            if (!string.IsNullOrWhiteSpace(IndentNo) && long.TryParse(IndentNo, out var indentId))
                query = query.Where(b => b.Id == indentId);

            CurrentPage = Math.Max(1, Page);
            TotalCount = await query.CountAsync();

            var skip = (CurrentPage - 1) * PageSize;
            Rows = await query
                .Include(b => b.Patient).ThenInclude(p => p.Person)
                .Include(b => b.BillItems).ThenInclude(bi => bi.Item)
                .OrderByDescending(b => b.BillDate).ThenByDescending(b => b.Id)
                .Skip(skip)
                .Take(PageSize)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostViewBillAsync([FromForm] long BillId)
        {
            SelectedBill = await _db.Bills.AsNoTracking()
                .Include(b => b.Patient).ThenInclude(p => p.Person)
                .Include(b => b.BillItems).ThenInclude(bi => bi.Item)
                .FirstOrDefaultAsync(b => b.Id == BillId && !b.Retired);
            return Page();
        }
    }
}
