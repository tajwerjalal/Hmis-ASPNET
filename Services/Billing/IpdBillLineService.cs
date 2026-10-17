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
    /// WRITER service for the Indoor Patient (IPD Bill) form - indent line edits.
    /// New indents may be added while the file is not yet billed and not settled
    /// (Admitted 0, Discharged 1 or BillUpdating 2); generated (3) and settled (4)
    /// files are locked and reject every edit at the SQL level (WHERE clause guard),
    /// not just in the UI.
    ///
    /// Business rules
    ///   * NEVER hard-deletes: a delete marks IBIL_DET.Retired = 1 and the line
    ///     disappears from totals and the grid.
    ///   * Every add/edit/delete writes one IpdBillAudit row (who, what, old, new).
    ///   * All writes run inside a transaction; the guard UPDATE also flips
    ///     status 1 -> 2 (Discharged -> BillUpdating) on first touch.
    ///   * Amounts are recomputed from Qty * Rate; a negative qty is rejected.
    ///   * Each add/edit/delete also refreshes the stored totals: IBIL_MST.NTOTAL (the
    ///     slip total, NTOTAL == SUM(NAMT) as in the legacy data) and REGISTER.TBILLAMT
    ///     (the file's total bill amount == signed sum of its slip totals).
    ///
    /// Developer: Tajwer Jalal | Date: 12-Oct-2026 | Time: 12:10
    /// </summary>
    public class IpdBillLineService
    {
        private readonly ApplicationDbContext _db;

        public IpdBillLineService(ApplicationDbContext db) => _db = db;

        /// <summary>True when the file may be edited: not yet billed and not settled (status 0, 1 or 2).</summary>
        public async Task<bool> IsEditableAsync(string regNo)
        {
            var st = await _db.LegacyRegisters
                .Where(r => r.Type == "I" && r.RegNo == regNo)
                .Select(r => r.BillStatus)
                .FirstOrDefaultAsync();
            return st == (byte)IpdBillStatus.Admitted
                || st == (byte)IpdBillStatus.Discharged
                || st == (byte)IpdBillStatus.BillUpdating;
        }

        /// <summary>
        /// Adds a new indent line.  A new slip (IBIL_MST) is created when the
        /// (dept, slipNo) pair does not exist yet for this file.
        /// </summary>
        /// <param name="regNo">Registration number.</param>
        /// <param name="dept">2-char department code.</param>
        /// <param name="slipNo">Slip number (per department sequence).</param>
        /// <param name="slipDate">Slip date.</param>
        /// <param name="itemCode">Legacy 6-char item code.</param>
        /// <param name="description">Item description.</param>
        /// <param name="qty">Quantity (must be &gt; 0).</param>
        /// <param name="rate">Rate per unit.</param>
        /// <param name="remarks">Free text remark.</param>
        /// <param name="user">Web user name for the audit row.</param>
        /// <param name="userId">Web user id for the audit row.</param>
        /// <returns>The new IBIL_DET row id.</returns>
        /// <exception cref="InvalidOperationException">File is locked or qty invalid.</exception>
        public async Task<long> AddLineAsync(string regNo, string dept, string slipNo, DateTime slipDate,
            string itemCode, string description, decimal qty, decimal rate, string remarks,
            string user, long? userId)
        {
            if (qty <= 0) throw new InvalidOperationException("Quantity must be greater than zero.");

            await using var tx = await _db.Database.BeginTransactionAsync();
            await GuardEditableAsync(regNo);

            var slip = await _db.IbilMsts
                .FirstOrDefaultAsync(m => m.HosCode == regNo && m.Dept == dept && m.SlipNo == slipNo);
            if (slip == null)
            {
                slip = new IbilMst
                {
                    HosCode = regNo, Dept = dept, SlipNo = slipNo, SlipDt = slipDate,
                    Type = "N", Bill = 0
                };
                _db.IbilMsts.Add(slip);
                await _db.SaveChangesAsync();
            }

            var line = new IbilDet
            {
                Dept = dept, SlipNo = slipNo, Code = itemCode, Desc = description,
                Qty = qty, Rate = rate, NAmt = Math.Round(qty * rate, 2), Rem = remarks,
                Retired = false
            };
            _db.IbilDets.Add(line);
            await _db.SaveChangesAsync();

            // Keep the slip total (IBIL_MST.NTOTAL) and the file total (REGISTER.TBILLAMT) in step.
            await RecalculateTotalsAsync(regNo, dept, slipNo);

            await AuditAsync(regNo, "LINE_ADD", "IBIL_DET", line.Id.ToString(), null,
                $"{qty} x {rate} = {line.NAmt}", "New indent line", user, userId);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return line.Id;
        }

        /// <summary>
        /// Edits an existing line (qty / rate / description / remarks).
        /// </summary>
        /// <param name="lineId">IBIL_DET.Id (surrogate key).</param>
        /// <param name="qty">New quantity (&gt; 0).</param>
        /// <param name="rate">New rate.</param>
        /// <param name="description">New description.</param>
        /// <param name="remarks">New remark.</param>
        /// <param name="regNo">Registration number (used for the lock guard).</param>
        /// <param name="user">Web user name for the audit row.</param>
        /// <param name="userId">Web user id for the audit row.</param>
        /// <returns>Task.</returns>
        /// <exception cref="InvalidOperationException">File is locked or line missing.</exception>
        public async Task EditLineAsync(long lineId, decimal qty, decimal rate, string description,
            string remarks, string regNo, string user, long? userId)
        {
            if (qty <= 0) throw new InvalidOperationException("Quantity must be greater than zero.");

            await using var tx = await _db.Database.BeginTransactionAsync();
            await GuardEditableAsync(regNo);

            var line = await _db.IbilDets.FirstOrDefaultAsync(d => d.Id == lineId);
            if (line == null) throw new InvalidOperationException("Line not found.");

            var old = $"{line.Qty} x {line.Rate} = {line.NAmt} | {line.Rem}";
            line.Qty = qty;
            line.Rate = rate;
            line.NAmt = Math.Round(qty * rate, 2);
            line.Desc = description;
            line.Rem = remarks;
            await _db.SaveChangesAsync();

            // Keep the slip total (IBIL_MST.NTOTAL) and the file total (REGISTER.TBILLAMT) in step.
            await RecalculateTotalsAsync(regNo, line.Dept, line.SlipNo);

            await AuditAsync(regNo, "LINE_EDIT", "IBIL_DET", lineId.ToString(),
                old, $"{qty} x {rate} = {line.NAmt} | {remarks}", "Indent line edited", user, userId);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        /// <summary>
        /// Soft-deletes a line (Retired = 1).  The row is never removed from the table.
        /// </summary>
        /// <param name="lineId">IBIL_DET.Id.</param>
        /// <param name="regNo">Registration number (lock guard).</param>
        /// <param name="user">Web user name.</param>
        /// <param name="userId">Web user id.</param>
        /// <returns>Task.</returns>
        public async Task RetireLineAsync(long lineId, string regNo, string user, long? userId)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            await GuardEditableAsync(regNo);

            var line = await _db.IbilDets.FirstOrDefaultAsync(d => d.Id == lineId);
            if (line == null) throw new InvalidOperationException("Line not found.");

            var old = $"{line.Qty} x {line.Rate} = {line.NAmt}";
            line.Retired = true;
            await _db.SaveChangesAsync();

            // Keep the slip total (IBIL_MST.NTOTAL) and the file total (REGISTER.TBILLAMT) in step.
            await RecalculateTotalsAsync(regNo, line.Dept, line.SlipNo);

            await AuditAsync(regNo, "LINE_DEL", "IBIL_DET", lineId.ToString(),
                old, "retired", "Indent line retired (soft delete)", user, userId);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        /// <summary>
        /// Refuses the call when the file is locked and flips status 1 -> 2 otherwise.
        /// A new indent may be added while the file is not yet billed and not settled
        /// (status 0 Admitted, 1 Discharged or 2 Bill Updating); generated (3) and
        /// settled (4) files are locked. Runs inside the caller's transaction.
        /// </summary>
        private async Task GuardEditableAsync(string regNo)
        {
            var updated = await _db.Database.ExecuteSqlRawAsync(
                @"UPDATE dbo.REGISTER
                     SET BILLSTATUS = CASE WHEN BILLSTATUS = 1 THEN 2 ELSE BILLSTATUS END,
                         BILLSTATUSDT = SYSUTCDATETIME()
                   WHERE TYPE = 'I' AND REGNO = {0}
                     AND BILLSTATUS IN (0, 1, 2)", regNo);
            if (updated == 0)
                throw new InvalidOperationException(
                    "This file is locked (bill generated or settled). Only an Authorized Officer can reverse it.");
        }

        /// <summary>
        /// Recomputes a slip's stored total so that <c>IBIL_MST.NTOTAL</c> always equals
        /// the sum of its non-retired <c>IBIL_DET.NAMT</c> rows.
        /// </summary>
        /// <param name="regNo">Registration number (IBIL_MST.HOS_CODE) that owns the slip.</param>
        /// <param name="dept">2-char department code.</param>
        /// <param name="slipNo">Slip (indent) number within the department.</param>
        /// <returns>Task.</returns>
        /// <remarks>
        /// The Department Summary grid, the Used Amount / Amt. Due figures and the legacy
        /// reports all read <c>IBIL_MST.NTOTAL</c>, and the legacy data keeps the invariant
        /// <c>NTOTAL == SUM(NAMT)</c> for every slip. Without this, a line added by this form
        /// left NTOTAL NULL and the summary showed 0.00. Called on every add / edit / retire,
        /// inside the caller's transaction. A slip whose lines are all retired gets 0.
        /// Developer: Tajwer Jalal | Date: 16-Oct-2026 | Time: 14:35
        /// </remarks>
        private async Task RecalculateSlipTotalAsync(string regNo, string dept, string slipNo)
        {
            if (string.IsNullOrWhiteSpace(regNo) || string.IsNullOrWhiteSpace(dept)
                || string.IsNullOrWhiteSpace(slipNo))
                return;

            var lineSum = await _db.IbilDets
                .Where(d => d.Dept == dept && d.SlipNo == slipNo
                            && (d.Retired == null || d.Retired == false))
                .SumAsync(d => (decimal?)d.NAmt) ?? 0m;

            var slip = await _db.IbilMsts
                .FirstOrDefaultAsync(m => m.HosCode == regNo && m.Dept == dept && m.SlipNo == slipNo);
            if (slip != null)
            {
                slip.NTotal = lineSum;
                await _db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Refreshes REGISTER.TBILLAMT for one file - the stored "total bill amount".
        /// </summary>
        /// <param name="regNo">Registration number (REGISTER.REGNO, TYPE='I').</param>
        /// <returns>Task.</returns>
        /// <remarks>
        /// REGISTER.TBILLAMT is the file's total bill amount; the List tab's Billed / Due
        /// columns and the Amt. Due figure read it. The legacy data keeps it equal to the
        /// signed sum of the file's IBIL_MST.NTOTAL rows (TYPE='R' subtracting) - the same
        /// number this form shows as "Used Amount". It used to be written only by bill
        /// generation, so a file that already had indents showed Billed = NULL / 0.00. Now
        /// every indent add / edit / retire refreshes it in the same transaction. Run as raw
        /// SQL so it reads the slip totals written moments earlier and never disturbs a
        /// tracked REGISTER entity.
        /// Developer: Tajwer Jalal | Date: 16-Oct-2026 | Time: 15:05
        /// </remarks>
        private async Task RecalculateRegisterBillAmountAsync(string regNo)
        {
            if (string.IsNullOrWhiteSpace(regNo)) return;
            await _db.Database.ExecuteSqlRawAsync(
                @"UPDATE dbo.REGISTER
                     SET TBILLAMT = ISNULL((SELECT SUM(CASE WHEN m.TYPE = 'R' THEN -ISNULL(m.NTOTAL, 0)
                                                            ELSE ISNULL(m.NTOTAL, 0) END)
                                              FROM dbo.IBIL_MST m
                                             WHERE m.HOS_CODE = {0}), 0)
                   WHERE TYPE = 'I' AND REGNO = {0}", regNo);
        }

        /// <summary>
        /// Recomputes every stored total touched by an indent change: the slip total
        /// (IBIL_MST.NTOTAL) and the file total (REGISTER.TBILLAMT).
        /// </summary>
        /// <param name="regNo">Registration number.</param>
        /// <param name="dept">2-char department code of the changed slip.</param>
        /// <param name="slipNo">Slip number of the changed slip.</param>
        /// <returns>Task.</returns>
        /// <remarks>Developer: Tajwer Jalal | Date: 16-Oct-2026 | Time: 15:05</remarks>
        private async Task RecalculateTotalsAsync(string regNo, string dept, string slipNo)
        {
            await RecalculateSlipTotalAsync(regNo, dept, slipNo);
            await RecalculateRegisterBillAmountAsync(regNo);
        }

        private async Task AuditAsync(string regNo, string action, string entity, string entityKey,
            string oldValue, string newValue, string field, string user, long? userId)
        {
            _db.IpdBillAudits.Add(new IpdBillAudit
            {
                RegNo = regNo, Action = action, Entity = entity, EntityKey = entityKey,
                Field = field, OldValue = oldValue, NewValue = newValue,
                UserName = user, WebUserId = userId, CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
    }
}
