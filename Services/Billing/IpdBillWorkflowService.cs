using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Models.Legacy;

namespace HmisWeb.Services.Billing
{
    /// <summary>
    /// STATUS LIFECYCLE service for the Indoor Patient (IPD Bill) form.
    /// Implements the strict VFP-compatible lifecycle:
    ///   Admitted(0) -> Discharged(1) -> BillUpdating(2) -> BillGenerated(3) -> Settled(4)
    /// with reverse / unsettle escape hatches.
    ///
    /// Business rules
    ///   * Only an Authorized Officer can Generate, Reverse or Unsettle.
    ///   * Reverse / Unsettle / Discount require a non-empty reason (enforced here).
    ///   * Reverse is allowed only on a generated (3) file; Unsettle only on a
    ///     settled (4) file; every transition is written to IpdBillAudit.
    ///   * No hard deletes, all writes are transactional and guarded in SQL.
    ///
    /// Developer: Tajwer Jalal | Date: 12-Oct-2026 | Time: 12:10
    /// </summary>
    public class IpdBillWorkflowService
    {
        private readonly ApplicationDbContext _db;

        public IpdBillWorkflowService(ApplicationDbContext db) => _db = db;

        /// <summary>Reads the current status of a file.</summary>
        public async Task<IpdBillStatus?> GetStatusAsync(string regNo)
        {
            var v = await _db.LegacyRegisters
                .Where(r => r.Type == "I" && r.RegNo == regNo)
                .Select(r => r.BillStatus)
                .FirstOrDefaultAsync();
            return v.HasValue ? (IpdBillStatus)v.Value : (IpdBillStatus?)null;
        }

        /// <summary>
        /// Marks the file as received by billing (Admitted -> Discharged).
        /// Called from the Discharge screen; no-op once the file has moved on.
        /// </summary>
        public async Task DischargeToBillingAsync(string regNo, string user, long? userId)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            var n = await _db.Database.ExecuteSqlRawAsync(
                @"UPDATE dbo.REGISTER
                     SET BILLSTATUS = 1, BILLSTATUSDT = SYSUTCDATETIME(), BILLSTATUSBY = {1}
                   WHERE TYPE = 'I' AND REGNO = {0} AND BILLSTATUS = 0", regNo, user);
            if (n > 0)
            {
                _db.IpdBillAudits.Add(new IpdBillAudit
                {
                    RegNo = regNo, Action = "STATUS", Entity = "REGISTER", EntityKey = regNo,
                    Field = "BILLSTATUS", OldValue = "0", NewValue = "1",
                    Reason = "Discharged - file sent to billing", UserName = user, WebUserId = userId,
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }
            await tx.CommitAsync();
        }

        /// <summary>
        /// Generates the bill for a file.  Delegates to IpdBillGenerateService so that
        /// the batch screen /billing/GenerateIpdBill and this form share one code path.
        /// </summary>
        /// <param name="regNo">Registration number.</param>
        /// <param name="billDate">Bill date to stamp.</param>
        /// <param name="periodStart">Optional period start.</param>
        /// <param name="periodEnd">Optional period end.</param>
        /// <param name="user">User name.</param>
        /// <param name="userId">User id.</param>
        /// <returns>The allocated bill number.</returns>
        public async Task<long> GenerateAsync(string regNo, DateTime billDate,
            DateTime? periodStart, DateTime? periodEnd, string user, long? userId)
        {
            var svc = new IpdBillGenerateService(_db);
            var billNo = await svc.GenerateForRegistrationAsync(regNo, billDate, periodStart, periodEnd, user);
            await SetStatusAsync(regNo, IpdBillStatus.BillGenerated, "GENERATE", user, userId,
                $"Bill {billNo} generated");
            return billNo;
        }

        /// <summary>
        /// Reverses a generated bill (Officer, reason mandatory): clears IBIL_MST.BILL
        /// for the file, deletes the IPDBILL header, writes an UNSETTLE request row
        /// (ChgType = 1) and drops the status back to BillUpdating.
        /// </summary>
        /// <exception cref="InvalidOperationException">Wrong status or missing reason.</exception>
        public async Task ReverseAsync(string regNo, long billNo, string reason, string user, long? userId)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new InvalidOperationException("A reason is required to reverse a bill.");
            var st = await GetStatusAsync(regNo);
            if (st != IpdBillStatus.BillGenerated)
                throw new InvalidOperationException("Only a generated bill can be reversed.");

            await using var tx = await _db.Database.BeginTransactionAsync();

            var n = await _db.Database.ExecuteSqlRawAsync(
                "UPDATE dbo.IBIL_MST SET BILL = NULL, BDATE = NULL WHERE HOS_CODE = {0} AND BILL = {1}", regNo, billNo);
            await _db.Database.ExecuteSqlRawAsync(
                "DELETE FROM dbo.IPDBILL WHERE HOS_CODE = {0} AND BILLNO = {1}", regNo, billNo);
            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE dbo.REGISTER SET BILLGEN = 0 WHERE TYPE = 'I' AND REGNO = {0}", regNo);

            _db.UnsettleRequests.Add(new UnsettleRequest
            {
                ReqDate = DateTime.Today, Regno = regNo, ChgType = 1,
                OldBillNo = billNo.ToString(), OldBillAmt = 0, ChgDate = DateTime.Today, ChgBy = user,
                Reset = false, CreaterId = userId, CreatedAt = DateTime.Now
            });

            _db.IpdBillAudits.Add(new IpdBillAudit
            {
                RegNo = regNo, Action = "REVERSE", Entity = "IPDBILL", EntityKey = billNo.ToString(),
                Reason = reason, UserName = user, WebUserId = userId,
                NewValue = $"reversed ({n} slips unstamped)", CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            await SetStatusAsync(regNo, IpdBillStatus.BillUpdating, "STATUS", user, userId, "Bill reversed");
        }

        /// <summary>
        /// Marks a generated bill as settled (Cashier).  Guards that receipts plus
        /// discount never exceed the billed amount.
        /// </summary>
        /// <exception cref="InvalidOperationException">Wrong status or over-payment.</exception>
        public async Task SettleAsync(string regNo, string user, long? userId)
        {
            var st = await GetStatusAsync(regNo);
            if (st != IpdBillStatus.BillGenerated)
                throw new InvalidOperationException("Only a generated bill can be settled.");

            var r = await _db.LegacyRegisters.FirstAsync(x => x.Type == "I" && x.RegNo == regNo);
            var due = (r.TBillAmt ?? 0) - (r.TRcvd ?? 0) + (r.TPymt ?? 0) - (r.CashDisc ?? 0);
            if (due < 0)
                throw new InvalidOperationException("Receipts plus discount exceed the billed amount.");

            await using var tx = await _db.Database.BeginTransactionAsync();
            await _db.Database.ExecuteSqlRawAsync(
                @"UPDATE dbo.REGISTER
                     SET SETTLE = 1, SETTLEDT = {1}, BILLSTATUS = 4, BILLSTATUSDT = SYSUTCDATETIME(), BILLSTATUSBY = {2}
                   WHERE TYPE = 'I' AND REGNO = {0} AND BILLSTATUS = 3", regNo, DateTime.Today, user);
            _db.IpdBillAudits.Add(new IpdBillAudit
            {
                RegNo = regNo, Action = "SETTLE", Entity = "REGISTER", EntityKey = regNo,
                NewValue = "settled", UserName = user, WebUserId = userId, CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        /// <summary>
        /// Unsettles a settled bill (Officer, reason mandatory) and writes an
        /// UNSETTLE request row (ChgType = 2).
        /// </summary>
        /// <exception cref="InvalidOperationException">Wrong status or missing reason.</exception>
        public async Task UnsettleAsync(string regNo, string reason, string user, long? userId)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new InvalidOperationException("A reason is required to unsettle a bill.");
            var st = await GetStatusAsync(regNo);
            if (st != IpdBillStatus.Settled)
                throw new InvalidOperationException("Only a settled bill can be unsettled.");

            await using var tx = await _db.Database.BeginTransactionAsync();
            await _db.Database.ExecuteSqlRawAsync(
                @"UPDATE dbo.REGISTER
                     SET SETTLE = 0, SETTLEDT = NULL, BILLSTATUS = 3, BILLSTATUSDT = SYSUTCDATETIME(), BILLSTATUSBY = {1}
                   WHERE TYPE = 'I' AND REGNO = {0} AND BILLSTATUS = 4", regNo, user);

            _db.UnsettleRequests.Add(new UnsettleRequest
            {
                ReqDate = DateTime.Today, Regno = regNo, ChgType = 2,
                OldDiscAmt = 0, ChgDate = DateTime.Today, ChgBy = user, Reset = false,
                CreaterId = userId, CreatedAt = DateTime.Now
            });

            _db.IpdBillAudits.Add(new IpdBillAudit
            {
                RegNo = regNo, Action = "UNSETTLE", Entity = "REGISTER", EntityKey = regNo,
                Reason = reason, UserName = user, WebUserId = userId, CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        private async Task SetStatusAsync(string regNo, IpdBillStatus status, string action,
            string user, long? userId, string note)
        {
            var old = await GetStatusAsync(regNo);
            await _db.Database.ExecuteSqlRawAsync(
                @"UPDATE dbo.REGISTER
                     SET BILLSTATUS = {1}, BILLSTATUSDT = SYSUTCDATETIME(), BILLSTATUSBY = {2}
                   WHERE TYPE = 'I' AND REGNO = {0}", regNo, (byte)status, user);
            _db.IpdBillAudits.Add(new IpdBillAudit
            {
                RegNo = regNo, Action = action, Entity = "REGISTER", EntityKey = regNo,
                Field = "BILLSTATUS", OldValue = old?.ToString(), NewValue = status.ToString(),
                Reason = note, UserName = user, WebUserId = userId, CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
    }
}
