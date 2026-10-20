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
    /// STATUS LIFECYCLE service for the Outdoor Patient (OPD Bill) form.
    /// Implements the VFP-compatible OPD lifecycle:
    ///   Open (no bill) -> Generated (BILLGEN=1 + OPDBILL) -> Reversed (STAT='X') / Settled (SETTLE=1)
    /// with reverse escape hatch.
    ///
    /// Business rules
    ///   * Only an Authorized Officer can Generate or Reverse.
    ///   * Reverse requires a non-empty reason (enforced here).
    ///   * Reverse is allowed only on a generated (BILLGEN=1) file; clears OBIL_MST.BILL
    ///     and marks OPDBILL.STAT='X'; writes an UNSETTLE request row (ChgType = 1).
    ///   * No hard deletes, all writes are transactional and guarded in SQL.
    ///
    /// Developer: Tajwer Jalal | Date: 20-Oct-2026 | Time: 15:30
    /// </summary>
    public class OpdBillWorkflowService
    {
        private readonly ApplicationDbContext _db;

        public OpdBillWorkflowService(ApplicationDbContext db) => _db = db;

        /// <summary>
        /// Generates the bill for a file. Delegates to OpdBillGenerateService so that
        /// the batch screen /billing/GenerateOpdBill and this form share one code path.
        /// </summary>
        /// <param name="regNo">Registration number.</param>
        /// <param name="billDate">Bill date to stamp.</param>
        /// <param name="periodStart">Optional period start (SDATE).</param>
        /// <param name="periodEnd">Optional period end (EDATE).</param>
        /// <param name="user">User name.</param>
        /// <param name="userId">User id.</param>
        /// <returns>The allocated bill number.</returns>
        public async Task<long> GenerateAsync(string regNo, DateTime billDate,
            DateTime? periodStart, DateTime? periodEnd, string user, long? userId)
        {
            var svc = new OpdBillGenerateService(_db);
            var billNo = await svc.GenerateForRegistrationAsync(regNo, billDate, periodStart, periodEnd, user);
            
            // Update REGISTER.BILLGEN is done by GenerateForRegistrationAsync
            await AuditAsync(regNo, "GENERATE", "OPDBILL", billNo.ToString(), null,
                $"Bill {billNo} generated", null, user, userId);
            return billNo;
        }

        /// <summary>
        /// Reverses a generated bill (Officer, reason mandatory): clears OBIL_MST.BILL
        /// for the file and bill number, marks OPDBILL.STAT = 'X', writes an UNSETTLE
        /// request row (ChgType = 1).
        /// </summary>
        /// <exception cref="InvalidOperationException">Wrong status or missing reason.</exception>
        public async Task ReverseAsync(string regNo, long billNo, string reason, string user, long? userId)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new InvalidOperationException("A reason is required to reverse a bill.");

            // Verify file is billed
            var billGen = await _db.LegacyRegisters
                .Where(r => r.Type == "O" && r.RegNo == regNo)
                .Select(r => r.BillGen)
                .FirstOrDefaultAsync();
            if (billGen != true)
                throw new InvalidOperationException("Only a generated bill can be reversed.");

            // Verify the bill exists and is not already reversed
            var bill = await _db.OpdBillHeaders
                .FirstOrDefaultAsync(h => h.HosCode == regNo && h.BillNo == billNo && h.Stat != "X");
            if (bill == null)
                throw new InvalidOperationException("Bill not found or already reversed.");

            await using var tx = await _db.Database.BeginTransactionAsync();

            // Clear the bill stamp from slips
            var n = await _db.Database.ExecuteSqlRawAsync(
                "UPDATE dbo.OBIL_MST SET BILL = NULL, BDATE = NULL WHERE HOS_CODE = {0} AND BILL = {1}", regNo, billNo);

            // Mark the bill header as reversed
            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE dbo.OPDBILL SET STAT = 'X' WHERE HOS_CODE = {0} AND BILLNO = {1}", regNo, billNo);

            // Reset REGISTER.BILLGEN and TBILLAMT
            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE dbo.REGISTER SET BILLGEN = 0, TBILLAMT = 0 WHERE TYPE = 'O' AND REGNO = {0}", regNo);

            // Write UNSETTLE request (ChgType = 1 for reverse)
            _db.UnsettleRequests.Add(new UnsettleRequest
            {
                ReqDate = DateTime.Today, Regno = regNo, ChgType = 1,
                OldBillNo = billNo.ToString(), OldBillAmt = (double?)(bill.NTot ?? 0), ChgDate = DateTime.Today, ChgBy = user,
                Reset = false, CreaterId = userId, CreatedAt = DateTime.Now
            });

            await AuditAsync(regNo, "REVERSE", "OPDBILL", billNo.ToString(), null,
                $"reversed ({n} slips unstamped)", reason, user, userId);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        private async Task AuditAsync(string regNo, string action, string entity, string entityKey,
            string oldValue, string newValue, string reason, string user, long? userId)
        {
            _db.OpdBillAudits.Add(new OpdBillAudit
            {
                RegNo = regNo, Action = action, Entity = entity, EntityKey = entityKey,
                Field = null, OldValue = oldValue, NewValue = newValue,
                Reason = reason, UserName = user, WebUserId = userId, CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
    }
}