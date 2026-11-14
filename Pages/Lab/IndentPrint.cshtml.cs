// Developer : Tajwer Jalal
// Date/Time : 29-Oct-2026 11:45 AM
// Change    : Lab indent payment-slip print page (original for UNPAID, duplicate for PAID).

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Lab
{
    /// <summary>
    /// Server-rendered payment-slip (indent) printer. GET /lab/indent/{Id:int}/print.
    ///   * duplicate = false -> ORIGINAL slip for an UNPAID indent (patient takes it to cashier).
    ///   * duplicate = true  -> DUPLICATE COPY for a PAID indent (cashier re-print).
    /// Every duplicate print is audit-logged in INDENT_PRINT_LOG and the cumulative
    /// print count is returned on the slip.
    /// </summary>
    public class IndentPrintModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;

        public IndentPrintModel(ApplicationDbContext db, IAppSession appSession)
        {
            _db = db;
            _appSession = appSession;
        }

        public long Id { get; set; }
        public bool Duplicate { get; set; }
        public Bill Bill { get; set; }
        public IndentPrintLog PrintLog { get; set; }
        public int PrintCount { get; set; }
        public string UserName { get; set; }
        public string PaymentStatus { get; set; } = "UNPAID";
        public string Mode { get; set; } = "OPD";
        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(long Id, bool duplicate = false)
        {
            this.Id = Id;
            this.Duplicate = duplicate;

            if (Id <= 0)
            {
                ErrorMessage = "Invalid indent number.";
                return Page();
            }

            var bill = await _db.Bills.AsNoTracking()
                .Include(b => b.Patient).ThenInclude(p => p.Person)
                .Include(b => b.Department)
                .Include(b => b.BillItems).ThenInclude(bi => bi.Item)
                .FirstOrDefaultAsync(b => b.Id == Id && !b.Retired);

            if (bill == null)
            {
                ErrorMessage = $"No indent found for number {Id}.";
                return Page();
            }

            Bill = bill;

            // Duplicate copies are only permitted for PAID indents.
            if (Duplicate && !bill.Paid)
            {
                ErrorMessage = "Only PAID indents can be printed as duplicate copies.";
                return Page();
            }

            PaymentStatus = bill.Paid ? "PAID" : "UNPAID";
            Mode = bill.BillType == BillType.InwardServiceBill ? "IPD" : "OPD";

            PrintLog = await _db.IndentPrintLogs.AsNoTracking()
                .FirstOrDefaultAsync(l => l.BillId == bill.Id && l.Duplicate == duplicate);
            PrintCount = await GetOrLogPrintCountAsync(bill.Id, duplicate);

            UserName = await _db.WebUsers.AsNoTracking()
                .Where(w => w.Id == _appSession.WebUserId)
                .Select(w => (string?)w.UserName ?? w.RealUserName)
                .FirstOrDefaultAsync() ?? "Staff";

            return Page();
        }

        /// <summary>
        /// Gets the cumulative print count for (bill, duplicate) and creates the audit
        /// row on first print. Every duplicate print writes/updates INDENT_PRINT_LOG.
        /// </summary>
        private async Task<int> GetOrLogPrintCountAsync(long billId, bool duplicate)
        {
            try
            {
                var log = await _db.IndentPrintLogs.AsNoTracking()
                    .FirstOrDefaultAsync(l => l.BillId == billId && l.Duplicate == duplicate);

                if (log == null)
                {
                    var entry = new IndentPrintLog
                    {
                        BillId = billId,
                        Duplicate = duplicate,
                        PrintedByUserId = _appSession.WebUserId ?? 0,
                        PrintedAt = DateTime.Now,
                        PrintCount = 1,
                        CreatedAt = DateTime.Now
                    };
                    _db.IndentPrintLogs.Add(entry);
                    await _db.SaveChangesAsync();
                    return 1;
                }

                log.PrintCount += 1;
                log.PrintedAt = DateTime.Now;
                log.PrintedByUserId = _appSession.WebUserId ?? 0;
                _db.IndentPrintLogs.Update(log);
                await _db.SaveChangesAsync();
                return log.PrintCount;
            }
            catch (Exception)
            {
                // Audit table not provisioned yet - fail open with a zero print count.
                return 0;
            }
        }
    }
}
