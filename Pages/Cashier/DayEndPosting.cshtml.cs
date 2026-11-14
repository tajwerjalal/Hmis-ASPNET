using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Cashier
{
    /// <summary>
    /// Day End Posting (migrated from VFP cashpost). Closes the operator's counter
    /// and moves all un-posted receipts/payments of the counter's company+line into
    /// the posted history (CHIST/CHISTDET), stamping a day-end voucher number.
    /// </summary>
    public class DayEndPostingModel : CashierPageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public DayEndPostingModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        public string ErrorMessage { get; set; } = "";
        public string Success { get; set; } = "";
        public bool AlreadyClosed { get; set; }

        public async Task OnGetAsync()
        {
            var dept = _session.LoggedDepartment;
            if (dept == null) { ErrorMessage = "No department selected."; return; }
            var session = await CashierPageModel.FindOpenCounterAsync(_db, HttpContext, _session.LoggedUser?.UserName, dept.Id);
            if (session == null) ErrorMessage = "No active counter for this department.";
        }

        public async Task<IActionResult> OnPostPostAsync()
        {
            ErrorMessage = ""; Success = "";
            var dept = _session.LoggedDepartment;
            if (dept == null) { ErrorMessage = "No department selected."; return Page(); }
            var userName = _session.LoggedUser?.UserName ?? "";
            var ccode = dept.Code ?? "";
            var lcode = (int)dept.Id;

            // The open-counter guard has already run; resolve the same counter row.
            var counter = await CashierPageModel.FindOpenCounterAsync(_db, HttpContext, userName, dept.Id);
            if (counter == null) { ErrorMessage = "No open counter found for this user."; return Page(); }
            if (counter.Status == "C")
            {
                ErrorMessage = "Your default counter has already been closed.";
                AlreadyClosed = true;
                return Page();
            }

            // Close the counter first (business rule from VFP).
            counter.Status = "C";
            counter.CloseDt = DateTime.Today;
            counter.CloseTm = DateTime.Now.ToString("HH:mm");
            counter.CloseBy = _session.LoggedUser?.UserName ?? "";

            // Allocate voucher number from CCONTROL (single row).
            var ctrl = await _db.CashControls.FirstOrDefaultAsync();
            if (ctrl == null) { ctrl = new CashControl { Id = 1 }; _db.CashControls.Add(ctrl); }
            ctrl.VouNo += 1;
            var vouNo = ctrl.VouNo.ToString("D9");
            var vouDate = DateTime.Today;
            var vouTime = DateTime.Now.ToString("HH:mm");

            counter.Vno = vouNo;
            await _db.SaveChangesAsync();

            // Move receipts -> CHIST
            var receipts = await _db.CashReceipts
                .Include(r => r.Details)
                .Where(r => r.CCode == ccode && r.LCode == lcode)
                .ToListAsync();

            foreach (var r in receipts)
            {
                var hist = new CashHistory
                {
                    VouNo = vouNo,
                    VouDate = vouDate,
                    VouTime = vouTime,
                    TrType = "R",
                    TrNo = r.TrNo,
                    TrDate = r.TrDate,
                    TrTime = r.TrTime,
                    Type = r.Type,
                    RegNo = r.RegNo,
                    Settle = r.Settle,
                    Reference = r.Reference,
                    Mode = r.Mode,
                    CardType = r.CardType,
                    CardName = r.CardName,
                    CardNo = r.CardNo,
                    CardValid = r.CardValid,
                    CardApprNo = r.CardApprNo,
                    CDisc = r.CDisc,
                    DiscBy = r.DiscBy,
                    TotalAmt = r.TotalAmt,
                    UserId = r.UserId,
                    UDate = r.UDate,
                    UTime = r.UTime
                };
                foreach (var d in r.Details)
                {
                    hist.Details.Add(new CashHistoryDetail
                    {
                        TrType = "R",
                        TrNo = r.TrNo,
                        AccNo = d.AccNo,
                        Dept = d.Dept,
                        IndentNo = d.IndentNo,
                        Remarks = d.Remarks,
                        Amount = d.Amount,
                        UserId = d.UserId,
                        UDate = d.UDate,
                        UTime = d.UTime
                    });
                }
                _db.CashHistories.Add(hist);
                _db.CashReceipts.Remove(r);
            }

            // Move payments -> CHIST
            var payments = await _db.CashPayments
                .Include(p => p.Details)
                .Where(p => p.CCode == ccode && p.LCode == lcode)
                .ToListAsync();

            foreach (var p in payments)
            {
                var hist = new CashHistory
                {
                    VouNo = vouNo,
                    VouDate = vouDate,
                    VouTime = vouTime,
                    TrType = "P",
                    TrNo = p.TrNo,
                    TrDate = p.TrDate,
                    TrTime = p.TrTime,
                    Type = p.Type,
                    RegNo = p.RegNo,
                    ECode = p.ECode,
                    Settle = p.Settle,
                    Reference = p.Reference,
                    TotalAmt = p.TotalAmt,
                    CDisc = p.CDisc,
                    DiscBy = p.DiscBy,
                    UserId = p.UserId,
                    UDate = p.UDate,
                    UTime = p.UTime
                };
                foreach (var d in p.Details)
                {
                    hist.Details.Add(new CashHistoryDetail
                    {
                        TrType = "P",
                        TrNo = p.TrNo,
                        AccNo = d.AccNo,
                        Dept = d.Dept,
                        IndentNo = d.IndentNo,
                        Remarks = d.Remarks,
                        Amount = d.Amount,
                        UserId = d.UserId,
                        UDate = d.UDate,
                        UTime = d.UTime
                    });
                }
                _db.CashHistories.Add(hist);
                _db.CashPayments.Remove(p);
            }

            await _db.SaveChangesAsync();
            HttpContext.Session.Remove(CashierPageModel.CounterSessionKey);

            Success = "Day-end posted. Voucher No: " + vouNo + ". Receipts: " + receipts.Count +
                      ", Payments: " + payments.Count + ".";
            return Page();
        }
    }
}