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
    /// WRITER service for the Outdoor Patient (OPD Bill) form - indent line edits.
    /// New indents may be added while the file is not yet billed (BILLGEN = 0/null);
    /// billed files (BILLGEN = 1) are locked and reject every edit at the SQL level
    /// (WHERE clause guard), not just in the UI.
    ///
    /// Business rules
    ///   * NEVER hard-deletes: a delete marks OBIL_DET.Retired = 1 and the line
    ///     disappears from totals and the grid.
    ///   * Every add/edit/delete writes one OpdBillAudit row (who, what, old, new).
    ///   * All writes run inside a transaction; the guard UPDATE also flips
    ///     OBIL_MST.STAT = 'E' on first touch if it was something else.
    ///   * Amounts are recomputed from Qty * Rate; a negative qty is rejected.
    ///   * Each add/edit/delete also refreshes the stored totals: OBIL_MST.NTOTAL (the
    ///     slip total, NTOTAL == SUM(NAMT) as in the legacy data) and REGISTER.TBILLAMT
    ///     (the file's total bill amount == signed sum of its slip totals).
    ///
    /// Developer: Tajwer Jalal | Date: 20-Oct-2026 | Time: 15:15
    /// </summary>
    public class OpdBillLineService
    {
        private readonly ApplicationDbContext _db;

        public OpdBillLineService(ApplicationDbContext db) => _db = db;

        /// <summary>True when the file may be edited: not yet billed (BILLGEN = 0 or null).</summary>
        public async Task<bool> IsEditableAsync(string regNo)
        {
            var billGen = await _db.LegacyRegisters
                .Where(r => r.Type == "O" && r.RegNo == regNo)
                .Select(r => r.BillGen)
                .FirstOrDefaultAsync();
            return billGen == true;
        }

        /// <summary>
        /// Adds a new indent line. A new slip (OBIL_MST) is created when the
        /// (dept, slipNo) pair does not exist yet for this file.
        /// </summary>
        /// <param name="regNo">Registration number.</param>
        /// <param name="dept">2-char department code.</param>
        /// <param name="slipNo">Slip number (per department sequence).</param>
        /// <param name="slipDate">Slip date.</param>
        /// <param name="itemCode">Legacy 6-char item code.</param>
        /// <param name="description">Item description.</param>
        /// <param name="qty">Quantity (must be > 0).</param>
        /// <param name="rate">Rate per unit.</param>
        /// <param name="remarks">Free text remark.</param>
        /// <param name="user">Web user name for the audit row.</param>
        /// <param name="userId">Web user id for the audit row.</param>
        /// <returns>The new OBIL_DET row id.</returns>
        /// <exception cref="InvalidOperationException">File is locked or qty invalid.</exception>
        public async Task<long> AddLineAsync(string regNo, string dept, string slipNo, DateTime slipDate,
            string itemCode, string description, decimal qty, decimal rate, string remarks,
            string user, long? userId)
        {
            if (qty <= 0) throw new InvalidOperationException("Quantity must be greater than zero.");

            await using var tx = await _db.Database.BeginTransactionAsync();
            await GuardEditableAsync(regNo);

            var slip = await _db.ObilMsts
                .FirstOrDefaultAsync(m => m.HosCode == regNo && m.Dept == dept && m.SlipNo == slipNo);
            if (slip == null)
            {
                slip = new ObilMst
                {
                    HosCode = regNo, Dept = dept, SlipNo = slipNo, SlipDt = slipDate,
                    Type = "N", Bill = 0, Stat = "E"
                };
                _db.ObilMsts.Add(slip);
                await _db.SaveChangesAsync();
            }

            var line = new ObilDet
            {
                Dept = dept, SlipNo = slipNo, Code = itemCode, Desc = description,
                Qty = qty, Rate = rate, NAmt = Math.Round(qty * rate, 2), Rem = remarks,
                Retired = false
            };
            _db.ObilDets.Add(line);
            await _db.SaveChangesAsync();

            // Keep the slip total (OBIL_MST.NTOTAL) and the file total (REGISTER.TBILLAMT) in step.
            await RecalculateTotalsAsync(regNo, dept, slipNo);

            await AuditAsync(regNo, "LINE_ADD", "OBIL_DET", line.Id.ToString(), null,
                $"{qty} x {rate} = {line.NAmt}", "New indent line", user, userId);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return line.Id;
        }

        /// <summary>
        /// Edits an existing line (qty / rate / description / remarks).
        /// </summary>
        public async Task EditLineAsync(long lineId, decimal qty, decimal rate, string description,
            string remarks, string regNo, string user, long? userId)
        {
            if (qty <= 0) throw new InvalidOperationException("Quantity must be greater than zero.");

            await using var tx = await _db.Database.BeginTransactionAsync();
            await GuardEditableAsync(regNo);

            var line = await _db.ObilDets.FirstOrDefaultAsync(d => d.Id == lineId);
            if (line == null) throw new InvalidOperationException("Line not found.");

            var old = $"{line.Qty} x {line.Rate} = {line.NAmt} | {line.Rem}";
            line.Qty = qty;
            line.Rate = rate;
            line.NAmt = Math.Round(qty * rate, 2);
            line.Desc = description;
            line.Rem = remarks;
            await _db.SaveChangesAsync();

            // Keep the slip total (OBIL_MST.NTOTAL) and the file total (REGISTER.TBILLAMT) in step.
            await RecalculateTotalsAsync(regNo, line.Dept, line.SlipNo);

            await AuditAsync(regNo, "LINE_EDIT", "OBIL_DET", lineId.ToString(),
                old, $"{qty} x {rate} = {line.NAmt} | {remarks}", "Indent line edited", user, userId);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        /// <summary>
        /// Soft-deletes a line (Retired = 1). The row is never removed from the table.
        /// </summary>
        public async Task RetireLineAsync(long lineId, string regNo, string user, long? userId)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            await GuardEditableAsync(regNo);

            var line = await _db.ObilDets.FirstOrDefaultAsync(d => d.Id == lineId);
            if (line == null) throw new InvalidOperationException("Line not found.");

            var old = $"{line.Qty} x {line.Rate} = {line.NAmt}";
            line.Retired = true;
            await _db.SaveChangesAsync();

            // Keep the slip total (OBIL_MST.NTOTAL) and the file total (REGISTER.TBILLAMT) in step.
            await RecalculateTotalsAsync(regNo, line.Dept, line.SlipNo);

            await AuditAsync(regNo, "LINE_DEL", "OBIL_DET", lineId.ToString(),
                old, "retired", "Indent line retired (soft delete)", user, userId);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        /// <summary>
        /// Refuses the call when the file is locked (BILLGEN = 1).
        /// Runs inside the caller's transaction.
        /// </summary>
        private async Task GuardEditableAsync(string regNo)
        {
            var updated = await _db.Database.ExecuteSqlRawAsync(
                @"UPDATE dbo.REGISTER
                     SET BILLGEN = CASE WHEN BILLGEN IS NULL OR BILLGEN = 0 THEN 1 ELSE BILLGEN END
                   WHERE TYPE = 'O' AND REGNO = {0}
                     AND (BILLGEN IS NULL OR BILLGEN = 0)", regNo);
            if (updated == 0)
                throw new InvalidOperationException(
                    "This file is locked (bill generated). Only an Authorized Officer can reverse it.");
        }

        /// <summary>
        /// Recomputes a slip's stored total so that OBIL_MST.NTOTAL always equals
        /// the sum of its non-retired OBIL_DET.NAMT rows.
        /// </summary>
        private async Task RecalculateSlipTotalAsync(string regNo, string dept, string slipNo)
        {
            if (string.IsNullOrWhiteSpace(regNo) || string.IsNullOrWhiteSpace(dept)
                || string.IsNullOrWhiteSpace(slipNo))
                return;

            var lineSum = await _db.ObilDets
                .Where(d => d.Dept == dept && d.SlipNo == slipNo
                            && (d.Retired == null || d.Retired == false))
                .SumAsync(d => (decimal?)d.NAmt) ?? 0m;

            var slip = await _db.ObilMsts
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
        private async Task RecalculateRegisterBillAmountAsync(string regNo)
        {
            if (string.IsNullOrWhiteSpace(regNo)) return;
            await _db.Database.ExecuteSqlRawAsync(
                @"UPDATE dbo.REGISTER
                     SET TBILLAMT = ISNULL((SELECT SUM(CASE WHEN m.TYPE = 'R' THEN -ISNULL(m.NTOTAL, 0)
                                                        ELSE ISNULL(m.NTOTAL, 0) END)
                                              FROM dbo.OBIL_MST m
                                             WHERE m.HOS_CODE = {0}), 0)
                   WHERE TYPE = 'O' AND REGNO = {0}", regNo);
        }

        /// <summary>
        /// Recomputes every stored total touched by an indent change: the slip total
        /// (OBIL_MST.NTOTAL) and the file total (REGISTER.TBILLAMT).
        /// </summary>
        private async Task RecalculateTotalsAsync(string regNo, string dept, string slipNo)
        {
            await RecalculateSlipTotalAsync(regNo, dept, slipNo);
            await RecalculateRegisterBillAmountAsync(regNo);
        }

        private async Task AuditAsync(string regNo, string action, string entity, string entityKey,
            string oldValue, string newValue, string field, string user, long? userId)
        {
            _db.OpdBillAudits.Add(new OpdBillAudit
            {
                RegNo = regNo, Action = action, Entity = entity, EntityKey = entityKey,
                Field = field, OldValue = oldValue, NewValue = newValue,
                UserName = user, WebUserId = userId, CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
    }
}