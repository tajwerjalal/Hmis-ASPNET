using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using HmisWeb.Data;
using HmisWeb.Models.Recovery;
using HmisWeb.Services;

namespace HmisWeb.Services.Billing
{
    /// <summary>
    /// WRITE service for the Receipts / Cheques (IPD) screen /Recovery/Receipt.
    /// Ports the save flow of VFP BillSys form RECPT (bs0603, rcpt/rcptf) with
    /// the gaps the Phase-1 review filed against it closed:
    ///
    ///   R-01  VNO zero-padded but NOT numbered by the form -> ReceiptNo is
    ///         minted here: RCP/MM/YYYY-nnnn (per-month serial, zero-padded 4),
    ///         inside a serializable UPDLOCK/HOLDLOCK window; the UNIQUE index
    ///         is the final guard.
    ///   G-02  receipts never linked to invoices -> each allocation line may
    ///         point at (InvoiceId, RegNo); InvoiceId NULL keeps the money on
    ///         the company ledger (the legacy "received but never invoiced"
    ///         advance book of 33,478 regnos survives as a first-class state).
    ///   G-04  885 over-allocated lines in legacy data -> over-allocation is
    ///         REFUSED, checked both per invoice line (RcptAmt+ItaxDed+OthDed
    ///         vs RECOVERY_INVDET.BillAmt) and per invoice (vs TotAmt).
    ///   R-03 / R-18  silent edits, no audit -> CREATE / CLEAR / BOUNCE /
    ///         CANCEL audit rows with before/after values into RECOVERY_AUDIT;
    ///         bounce and cancel delete the allocation rows *with an audit row
    ///         per line* so the re-opening of invoices stays traceable.
    ///   (new) cheque lifecycle 'P' pending -> 'C' cleared / 'B' bounced -
    ///         the legacy schema had no cheque status at all (review 1.3).
    ///
    /// Legacy conventions carried over as-is
    ///   * TotAmt = SUM(lines.RcptAmt) only - the bankable money (verified
    ///     13,856/13,859 rows in the Phase-1 review); deductions gross up the
    ///     credited amount instead.
    ///   * ItaxDed UI default rcptamt/95*5 (5% gross-up) when left empty.
    ///   * CHQNO blank means cash  -> explicit Method 'C'ash/'Q'ueque.
    ///   * Cancel replaces any physical delete (soft, mandatory reason).
    ///
    /// Developer: Tajwer Jalal | Date: 21-Oct-2026 | Time: 11:20
    /// </summary>
    public class RecoveryReceiptService
    {
        private readonly ApplicationDbContext _db;
        private readonly RecoveryInvoiceQueryService _invoiceQuery;
        private readonly IAppSession _session;

        public RecoveryReceiptService(ApplicationDbContext db,
            RecoveryInvoiceQueryService invoiceQuery, IAppSession session)
        {
            _db = db;
            _invoiceQuery = invoiceQuery;
            _session = session;
        }

        /// <summary>Result of a write - Ok=false means Message explains why.</summary>
        public class Result
        {
            public bool Ok { get; set; }
            public string Message { get; set; }
            public long ReceiptId { get; set; }
            public string ReceiptNo { get; set; }
        }

        /// <summary>Posted popup payload (JSON body of the Receipt screen).</summary>
        public class ReceiptUpsert
        {
            public string CompanyCode { get; set; }
            public DateTime RcptDate { get; set; }
            /// <summary>'C' cash or 'Q' cheque.</summary>
            public string Method { get; set; }
            public string ChequeNo { get; set; }
            public string Bank { get; set; }
            public DateTime? ChequeDate { get; set; }
            public string RcvdBy { get; set; }
            public string Remarks { get; set; }
            public List<LineUpsert> Lines { get; set; } = new();
        }

        /// <summary>One allocation line. InvoiceId NULL = advance money.</summary>
        public class LineUpsert
        {
            public long? InvoiceId { get; set; }
            public string RegNo { get; set; }
            public decimal RcptAmt { get; set; }
            public decimal? ItaxDed { get; set; }
            public decimal OthDed { get; set; }
            public string Remarks { get; set; }
        }

        // ================================================================ create

        /// <summary>
        /// Creates a receipt (header + allocation lines). Cash receipts are
        /// cleared the moment they are raised; cheques wait in 'P'ending.
        /// </summary>
        public async Task<Result> CreateAsync(ReceiptUpsert input)
        {
            var problems = Validate(input);
            if (problems.Count > 0)
                return new Result { Ok = false, Message = string.Join(' ', problems) };

            if (!await IsCompanyActiveAsync(input.CompanyCode))
                return new Result
                {
                    Ok = false,
                    Message = "Company is inactive or unknown - cannot raise a receipt."
                };

            var realLines = input.Lines
                .Where(l => !string.IsNullOrWhiteSpace(l.RegNo))
                .ToList();

            // ---- over-allocation validation against the invoice book (G-04) ----
            var overAllocProblems = await ValidateAllocationsAsync(
                input.CompanyCode, realLines, existingReceiptId: null);
            if (overAllocProblems.Count > 0)
                return new Result { Ok = false, Message = string.Join(' ', overAllocProblems) };

            var isCheque = input.Method == "Q";
            var rcpt = new RecoveryRcpt
            {
                CompanyCode = input.CompanyCode.Trim(),
                RcptDate = input.RcptDate.Date,
                Method = input.Method,
                ChequeNo = isCheque ? Trunc(input.ChequeNo, 32) : null,
                Bank = isCheque ? Trunc(input.Bank, 80) : null,
                ChequeDate = isCheque ? input.ChequeDate?.Date : null,
                RcvdBy = Trunc(input.RcvdBy, 40),
                TotAmt = realLines.Sum(l => l.RcptAmt),
                Status = isCheque ? "P" : "C", // cash horizontal: cleared on entry
                Remarks = Trunc(input.Remarks, 200),
                CreatedBy = CurrentUser(),
                CreatedAt = DateTime.UtcNow
            };

            var allocs = new List<RecoveryRcptAlloc>();

            foreach (var l in realLines)
            {
                var itax = l.ItaxDed ?? LegacyTaxDefault(l.RcptAmt);
                allocs.Add(new RecoveryRcptAlloc
                {
                    Receipt = rcpt,
                    InvoiceId = l.InvoiceId,
                    RegNo = l.RegNo.Trim(),
                    RcptAmt = l.RcptAmt,
                    ItaxDed = itax,
                    OthDed = l.OthDed,
                    Remarks = Trunc(l.Remarks, 50),
                    CreatedBy = CurrentUser(),
                    CreatedAt = DateTime.UtcNow
                });
            }

            // ---- mint the number inside its own serializable window ----
            string receiptNo;
            try
            {
                receiptNo = await MintReceiptNoAsync(rcpt.RcptDate);
            }
            catch (Exception ex)
            {
                return new Result { Ok = false, Message = "Numbering failed: " + ex.Message };
            }
            rcpt.ReceiptNo = receiptNo;

            _db.RecoveryRcpts.Add(rcpt);
            _db.RecoveryRcptAllocs.AddRange(allocs);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Lost the mint race to another clerk; the UNIQUE index
                // rejected it - retry the mint once and save again.
                try
                {
                    rcpt.ReceiptNo = await MintReceiptNoAsync(rcpt.RcptDate);
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return new Result
                    {
                        Ok = false,
                        Message = "Numbering race - nothing was saved, please retry."
                    };
                }
            }

            await AuditAsync("CREATE", "RECOVERY_RCPT", rcpt.ReceiptNo, "ReceiptId", null,
                rcpt.Id.ToString(CultureInfo.InvariantCulture),
                $"lines={realLines.Count};total={rcpt.TotAmt:0.00};method={rcpt.Method};status={rcpt.Status}");
            if (!isCheque)
            {
                // auditable parity with the legacy book: cash = received & clear
                await AuditAsync("CREATE", "RECOVERY_RCPT", rcpt.ReceiptNo, "Status",
                    "P", "C", "Cash receipt is cleared on entry.");
            }
            return new Result { Ok = true, ReceiptId = rcpt.Id, ReceiptNo = rcpt.ReceiptNo };
        }

        // ============================================================ transitions

        /// <summary>Cheque arrived good: 'P'ending -> 'C'leared (audit-rolled).</summary>
        public async Task<Result> ClearAsync(long id)
        {
            var rcpt = await _db.RecoveryRcpts.FirstOrDefaultAsync(r => r.Id == id);
            if (rcpt == null)
                return new Result { Ok = false, Message = "Receipt not found." };
            if (rcpt.Method != "Q")
                return new Result { Ok = false, Message = "Cash receipts are already cleared." };
            if (rcpt.Status != "P")
                return new Result { Ok = false, Message = "Only a pending cheque can be cleared." };

            rcpt.Status = "C";
            rcpt.EditedBy = CurrentUser();
            rcpt.EditedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await AuditAsync("CLEAR", "RECOVERY_RCPT", rcpt.ReceiptNo, "Status", "P", "C",
                "Cheque marked cleared (money confirmed at bank).");
            return new Result { Ok = true, ReceiptId = rcpt.Id, ReceiptNo = rcpt.ReceiptNo };
        }

        /// <summary>
        /// Cheque bounced: 'P' (or 'C') -> 'B' with a mandatory reason of at
        /// least 5 chars. The allocation rows are removed WITH an audit row per
        /// line, which automatically re-opens the credited invoice money.
        /// </summary>
        public async Task<Result> BounceAsync(long id, string reason)
        {
            var rcpt = await _db.RecoveryRcpts.FirstOrDefaultAsync(r => r.Id == id);
            if (rcpt == null)
                return new Result { Ok = false, Message = "Receipt not found." };
            if (rcpt.Method != "Q")
                return new Result { Ok = false, Message = "Only cheque receipts can bounce." };
            if (rcpt.Status == "B")
                return new Result { Ok = false, Message = "Already bounced." };
            if (rcpt.Status == "X")
                return new Result { Ok = false, Message = "Cancelled receipts cannot bounce." };

            reason = (reason ?? "").Trim();
            if (reason.Length < 5)
                return new Result
                {
                    Ok = false,
                    Message = "A reason of at least 5 characters is required to bounce a cheque."
                };

            var allocs = await _db.RecoveryRcptAllocs
                .Where(a => a.ReceiptId == rcpt.Id).ToListAsync();
            foreach (var a in allocs)
            {
                await AuditAsync("BOUNCE", "RECOVERY_RCPT_ALLOC", rcpt.ReceiptNo + "|" + a.RegNo,
                    "Credit", (a.RcptAmt + a.ItaxDed + a.OthDed)
                        .ToString(CultureInfo.InvariantCulture), "<removed>",
                    "Cheque bounced: " + reason);
                _db.RecoveryRcptAllocs.Remove(a);
            }

            var prevStatus = rcpt.Status;
            rcpt.Status = "B";
            rcpt.EditedBy = CurrentUser();
            rcpt.EditedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await AuditAsync("BOUNCE", "RECOVERY_RCPT", rcpt.ReceiptNo, "Status",
                prevStatus, "B", "Cheque bounced: " + reason);
            return new Result { Ok = true, ReceiptId = rcpt.Id, ReceiptNo = rcpt.ReceiptNo };
        }

        /// <summary>
        /// Soft-cancel (mandatory reason) - the red-trash delete bar of the
        /// workspace conventions, minus the data loss. Allocation rows are
        /// removed so the invoices re-open, and every removal is audited.
        /// </summary>
        public async Task<Result> CancelAsync(long id, string reason)
        {
            var rcpt = await _db.RecoveryRcpts.FirstOrDefaultAsync(r => r.Id == id);
            if (rcpt == null)
                return new Result { Ok = false, Message = "Receipt not found." };
            if (rcpt.Status == "X")
                return new Result { Ok = false, Message = "Already cancelled." };

            reason = (reason ?? "").Trim();
            if (reason.Length < 5)
                return new Result
                {
                    Ok = false,
                    Message = "A reason of at least 5 characters is required to cancel a receipt."
                };

            var allocs = await _db.RecoveryRcptAllocs
                .Where(a => a.ReceiptId == rcpt.Id).ToListAsync();
            foreach (var a in allocs)
            {
                await AuditAsync("CANCEL", "RECOVERY_RCPT_ALLOC", rcpt.ReceiptNo + "|" + a.RegNo,
                    "Credit", (a.RcptAmt + a.ItaxDed + a.OthDed)
                        .ToString(CultureInfo.InvariantCulture), "<removed>",
                    "Receipt cancelled: " + reason);
                _db.RecoveryRcptAllocs.Remove(a);
            }

            var prevStatus = rcpt.Status;
            rcpt.Status = "X";
            rcpt.EditedBy = CurrentUser();
            rcpt.EditedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await AuditAsync("CANCEL", "RECOVERY_RCPT", rcpt.ReceiptNo, "Status",
                prevStatus, "X", reason);
            return new Result { Ok = true, ReceiptId = rcpt.Id, ReceiptNo = rcpt.ReceiptNo };
        }

        // ================================================================ helpers

        private List<string> Validate(ReceiptUpsert input)
        {            var problems = new List<string>();
            if (string.IsNullOrWhiteSpace(input.CompanyCode))
                problems.Add("Company is required.");
            if (input.RcptDate == default)
                problems.Add("Receipt date is required.");
            var method = (input.Method ?? "").Trim().ToUpperInvariant();
            if (method != "C" && method != "Q")
                problems.Add("Payment method must be Cash or Cheque.");
            input.Method = method;
            if (method == "Q")
            {
                if (string.IsNullOrWhiteSpace(input.ChequeNo))
                    problems.Add("Cheque number is required for a cheque receipt.");
                if (string.IsNullOrWhiteSpace(input.Bank))
                    problems.Add("Bank is required for a cheque receipt.");
                if (input.ChequeDate == null)
                    problems.Add("Cheque date is required for a cheque receipt.");
            }
            var realLines = (input.Lines ?? new List<LineUpsert>())
                .Where(l => !string.IsNullOrWhiteSpace(l.RegNo)).ToList();
            if (realLines.Count == 0)
                problems.Add("At least one patient line is required (legacy rcptdet invariant).");
            foreach (var l in realLines)
            {
                if (l.RcptAmt <= 0)
                    problems.Add("Line amount must be greater than zero (" + l.RegNo + ").");
                if ((l.ItaxDed ?? 0) < 0 || l.OthDed < 0)
                    problems.Add("Deductions cannot be negative (" + l.RegNo + ").");
            }
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var reg in realLines.Select(l => l.RegNo.Trim()))
            {
                if (!seen.Add(reg))
                    problems.Add("Patient " + reg + " appears twice on this receipt.");
            }
            return problems;
        }

        /// <summary>
        /// Per-invoice and per-line over-allocation check. Credited = the
        /// RcptAmt + ItaxDed + OthDed of every ACTIVE ('P'/'C') receipt line
        /// already in the book, plus the candidate lines of THIS submission.
        /// </summary>
        private async Task<List<string>> ValidateAllocationsAsync(
            string company, List<LineUpsert> lines, long? existingReceiptId)
        {
            var problems = new List<string>();
            var withInv = lines.Where(l => l.InvoiceId.HasValue).ToList();
            if (withInv.Count == 0) return problems;

            var invIds = withInv.Select(l => l.InvoiceId.Value).Distinct().ToList();

            // candidate + current credited money per (invoice, regno)
            var candidateByLine = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var candidateByInvoice = new Dictionary<long, decimal>();
            foreach (var l in withInv)
            {
                var applied = l.RcptAmt + (l.ItaxDed ?? LegacyTaxDefault(l.RcptAmt)) + l.OthDed;
                var key = l.InvoiceId.Value + "|" + l.RegNo.Trim();
                candidateByLine[key] = (candidateByLine.TryGetValue(key, out var cur)
                    ? cur : 0m) + applied;
                candidateByInvoice[l.InvoiceId.Value] =
                    (candidateByInvoice.TryGetValue(l.InvoiceId.Value, out var cur2)
                        ? cur2 : 0m) + applied;
            }

            var conn = _db.Database.GetDbConnection();
            var opened = false;
            if (conn.State != System.Data.ConnectionState.Open)
            { await conn.OpenAsync(); opened = true; }
            try
            {
                // existing credited money for the same lines (this receipt's own
                // rows excluded only when a real receiptId is given; on create
                // there is none)
                var idList = string.Join(",", invIds);
                var exclude = existingReceiptId.HasValue
                    ? " AND a.ReceiptId <> " + existingReceiptId.Value.ToString(CultureInfo.InvariantCulture)
                    : "";
                using var cmd = conn.CreateCommand();
                cmd.CommandText =
                    "SELECT a.InvoiceId, a.RegNo, SUM(a.RcptAmt + a.ItaxDed + a.OthDed) AS Applied"
                    + " FROM RECOVERY_RCPT_ALLOC a"
                    + " JOIN RECOVERY_RCPT r ON r.Id = a.ReceiptId"
                    + " WHERE a.InvoiceId IN (" + idList + ")"
                    + " AND r.Status IN ('P','C')" + exclude
                    + " GROUP BY a.InvoiceId, a.RegNo";
                using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    var invId = rd.GetInt64(0);
                    var reg = rd.IsDBNull(1) ? "" : rd.GetString(1).Trim();
                    var applied = rd.IsDBNull(2) ? 0m : rd.GetDecimal(2);
                    var key = invId + "|" + reg;
                    candidateByLine[key] = (candidateByLine.TryGetValue(key, out var cur)
                        ? cur : 0m) + applied;
                    candidateByInvoice[invId] = (candidateByInvoice.TryGetValue(invId, out var cur2)
                        ? cur2 : 0m) + applied;
                }

                // invoice totals
                var invoices = await _db.RecoveryInvoices.AsNoTracking()
                    .Where(i => invIds.Contains(i.Id))
                    .Select(i => new { i.Id, i.DocNo, i.TotAmt, i.Status, i.CompanyCode })
                    .ToDictionaryAsync(i => i.Id);
                foreach (var l in withInv)
                {
                    if (!invoices.TryGetValue(l.InvoiceId.Value, out var inv))
                    {
                        problems.Add("Invoice not found for line " + l.RegNo + ".");
                        continue;
                    }
                    if (inv.Status != "O")
                        problems.Add("Invoice " + inv.DocNo + " is not open - it cannot take an allocation.");
                    else if (!string.Equals(inv.CompanyCode, company,
                            StringComparison.OrdinalIgnoreCase))
                        problems.Add("Invoice " + inv.DocNo + " belongs to another company.");
                }

                // line level guard (G-04): compare against RECOVERY_INVDET.BillAmt
                var lineAmts = await _db.RecoveryInvoiceDets.AsNoTracking()
                    .Where(d => invIds.Contains(d.InvoiceId))
                    .Select(d => new { d.InvoiceId, d.RegNo, d.BillAmt })
                    .ToListAsync();
                var lineMap = lineAmts.ToDictionary(
                    x => x.InvoiceId + "|" + x.RegNo.Trim(), x => x.BillAmt,
                    StringComparer.OrdinalIgnoreCase);
                foreach (var kv in candidateByLine)
                {
                    if (lineMap.TryGetValue(kv.Key, out var billAmt)
                        && kv.Value > billAmt)
                    {
                        // readable message: "Invoice N024/10/2026-1 patient 187006 ..."
                        long invPart;
                        var sep = kv.Key.IndexOf('|');
                        long.TryParse(kv.Key.Substring(0, sep), out invPart);
                        var regPart = kv.Key.Substring(sep + 1);
                        var label = invoices.TryGetValue(invPart, out var iv)
                            ? "Invoice " + iv.DocNo + " patient " + regPart
                            : "Patient line " + regPart;
                        problems.Add(label + " is over-allocated: applying "
                            + kv.Value.ToString("0.00", CultureInfo.InvariantCulture)
                            + " against a bill of "
                            + billAmt.ToString("0.00", CultureInfo.InvariantCulture) + ".");
                    }
                }

                // invoice level guard: compare against RECOVERY_INV.TotAmt
                foreach (var kv in candidateByInvoice)
                {
                    if (invoices.TryGetValue(kv.Key, out var inv) && kv.Value > inv.TotAmt)
                        problems.Add("Invoice " + inv.DocNo + " is over-allocated: "
                            + kv.Value.ToString("0.00", CultureInfo.InvariantCulture)
                            + " against a total of "
                            + inv.TotAmt.ToString("0.00", CultureInfo.InvariantCulture) + ".");
                }
            }
            finally { if (opened) await conn.CloseAsync(); }

            return problems;
        }

        /// <summary>Legacy ITAXDED UI default: rcptamt/95*5 (5% gross-up).</summary>
        private static decimal LegacyTaxDefault(decimal rcptAmt)
            => Math.Round(rcptAmt / 95m * 5m, 2);

        private async Task<bool> IsCompanyActiveAsync(string code)
        {
            var companies = await _invoiceQuery.GetCompaniesAsync();
            var c = companies.FirstOrDefault(x =>
                string.Equals(x.Id, code?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (c == null) return false;
            return !string.Equals(c.Status, "N", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Mints the next ReceiptNo for one month: RCP/MM/YYYY-nnnn (per-month
        /// serial, monotonic, zero-padded 4). UPDLOCK+HOLDLOCK serialises
        /// concurrent clerks; the UNIQUE index catches any remainder.
        /// </summary>
        private async Task<string> MintReceiptNoAsync(DateTime date)
        {
            var conn = _db.Database.GetDbConnection() as SqlConnection
                ?? throw new InvalidOperationException("RecoveryReceiptService needs a SqlConnection.");
            var opened = false;
            if (conn.State != System.Data.ConnectionState.Open)
            { await conn.OpenAsync(); opened = true; }
            try
            {
                await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

                var baseSlot = "RCP/"
                    + date.Month.ToString("00", CultureInfo.InvariantCulture) + "/"
                    + date.Year.ToString(CultureInfo.InvariantCulture);

                var maxSuffix = 0;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = "SELECT ReceiptNo FROM RECOVERY_RCPT"
                        + " WITH (UPDLOCK, HOLDLOCK) WHERE ReceiptNo LIKE @base";
                    var p = cmd.CreateParameter();
                    p.ParameterName = "@base";
                    p.Value = baseSlot + "-%";
                    cmd.Parameters.Add(p);

                    await using (var rd = await cmd.ExecuteReaderAsync())
                    {
                        while (await rd.ReadAsync())
                        {
                            var existing = rd.IsDBNull(0) ? "" : rd.GetString(0);
                            if (existing.StartsWith(baseSlot + "-", StringComparison.OrdinalIgnoreCase)
                                && int.TryParse(existing.Substring(baseSlot.Length + 1),
                                    NumberStyles.None, CultureInfo.InvariantCulture, out var n))
                                maxSuffix = Math.Max(maxSuffix, n);
                        }
                    }
                    await tx.CommitAsync();
                }
                return baseSlot + "-"
                    + (maxSuffix + 1).ToString("0000", CultureInfo.InvariantCulture);
            }
            finally { if (opened) await conn.CloseAsync(); }
        }

        /// <summary>TRUE when the cause is a unique-key rejection (2601/2627).</summary>
        private static bool IsUniqueViolation(DbUpdateException ex)
            => ex.InnerException is SqlException sx && (sx.Number == 2601 || sx.Number == 2627);

        /// <summary>Immutable audit row (RECOVERY_AUDIT accepts INSERTs only).</summary>
        private async Task AuditAsync(string action, string entity, string key,
            string field, string oldValue, string newValue, string reason)
        {
            var u = _session.LoggedUser;
            _db.RecoveryAudits.Add(new RecoveryAudit
            {
                Action = action,
                Entity = entity,
                EntityKey = Trunc(key, 80),
                Field = Trunc(field, 40),
                OldValue = Trunc(oldValue, 200),
                NewValue = Trunc(newValue, 200),
                Reason = Trunc(reason, 300),
                InvoiceId = null,
                WebUserId = u?.Id,
                UserName = Trunc(u?.RealUserName ?? u?.UserName ?? "", 80) ?? "",
                RoleName = "",
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        private string CurrentUser()
            => Trunc(_session.LoggedUser?.RealUserName ?? _session.LoggedUser?.UserName ?? "unknown", 40)
               ?? "unknown";

        private static string Trunc(string s, int max)
        {
            if (s == null) return null;
            return s.Length <= max ? s : s.Substring(0, max);
        }
    }
}
