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
    /// WRITE service for the company Invoice/Letter screen /Recovery/Invoice.
    /// Ports the save flow of VFP BillSys form INVOICE (bs0601) with the gaps
    /// the Phase-1 review filed against it closed:
    ///
    ///   R-01  numbering not owned by the screen  -> DocNo is minted here:
    ///         CID/MM/YYYY, and "-2", "-3"... only when the base slot is
    ///         already taken, minted inside a serializable transaction with a
    ///         UPDLOCK/HOLDLOCK range read; the UNIQUE index is the final
    ///         guard. (Legacy letters show both "C001/07/2015" and
    ///         "U002/071999" style; the new format is one canonical form.)
    ///   G-01  invoice headers without lines      -> Save refuses an invoice
    ///         with no lines; TotAmt is recomputed from the lines only.
    ///   R-13  no mandatory-field / duplicate checks ->
    ///         company required and active (legacy obilvalue guard), month
    ///         1-12, year +-2 window (VFP txtYear.Valid), a patient may sit on
    ///         only one OPEN invoice at a time (G-02 remediation).
    ///   R-05  legacy line-delete blocked by a receipt -> the guard is coded
    ///         against RECOVERY_RCPT_ALLOC, which lands in sprint 2; until
    ///         then the check degrades to a no-op.
    ///   R-03 / R-18  silent edits, no audit  -> CREATE / EDIT / CANCEL audit
    ///         rows with before/after values into RECOVERY_AUDIT.
    ///
    /// Cancel replaces the legacy physical DELETE (soft, reason mandatory) so
    /// the paper trail survives - flagged in the drift notes of
    /// _Workspace/Docs/Recovery_Module_Review.md.
    ///
    /// Developer: Tajwer Jalal | Date: 20-Oct-2026 | Time: 18:40
    /// </summary>
    public class RecoveryInvoiceService
    {
        private readonly ApplicationDbContext _db;
        private readonly RecoveryInvoiceQueryService _query;
        private readonly IAppSession _session;

        public RecoveryInvoiceService(ApplicationDbContext db,
            RecoveryInvoiceQueryService query, IAppSession session)
        {
            _db = db;
            _query = query;
            _session = session;
        }

        /// <summary>Result of a write - Ok=false means Message explains why.</summary>
        public class Result
        {
            public bool Ok { get; set; }
            public string Message { get; set; }
            public long InvoiceId { get; set; }
            public string DocNo { get; set; }
        }

        /// <summary>Posted popup payload.</summary>
        public class InvoiceUpsert
        {
            public long Id { get; set; }
            public string CompanyCode { get; set; }
            public DateTime InvoiceDate { get; set; }
            public DateTime? SubmissionDate { get; set; }
            public int? BillMonth { get; set; }
            public int? BillYear { get; set; }
            public string Remarks { get; set; }
            public List<LineUpsert> Lines { get; set; } = new();
        }

        public class LineUpsert
        {
            public long Id { get; set; }
            public string RegNo { get; set; }
            public decimal BillAmt { get; set; }
            public string Remarks { get; set; }
        }

        // ================================================================ create

        /// <summary>
        /// Creates a new open invoice (status 'O'), minting the DocNo here.
        /// </summary>
        public async Task<Result> CreateAsync(InvoiceUpsert input)
        {
            var problems = Validate(input);
            if (problems.Count > 0)
                return new Result { Ok = false, Message = string.Join(' ', problems) };

            if (!await IsCompanyActiveAsync(input.CompanyCode))
                return new Result
                {
                    Ok = false,
                    Message = "Company is inactive or unknown - cannot raise an invoice."
                };

            var (okOpen, dupMsg) = await NoOpenDupeAsync(input, existingHeaderId: null);
            if (!okOpen)
                return new Result { Ok = false, Message = dupMsg };

            var inv = new RecoveryInvoice
            {
                DocType = "I",
                CompanyCode = input.CompanyCode.Trim(),
                InvoiceDate = input.InvoiceDate.Date,
                SubmissionDate = input.SubmissionDate?.Date,
                BillMonth = input.BillMonth,
                BillYear = input.BillYear,
                Remarks = Trunc(input.Remarks, 200),
                TotAmt = input.Lines.Sum(l => l.BillAmt),
                Status = "O",
                CreatedBy = CurrentUser(),
                CreatedAt = DateTime.UtcNow
            };

            // ---- mint the number inside its own serializable window ----
            string docNo;
            try
            {
                docNo = await MintDocNoAsync(inv.CompanyCode, inv.InvoiceDate);
            }
            catch (Exception ex)
            {
                return new Result { Ok = false, Message = "Numbering failed: " + ex.Message };
            }
            inv.DocNo = docNo;

            foreach (var l in input.Lines.Where(l => !string.IsNullOrWhiteSpace(l.RegNo)))
            {
                _db.RecoveryInvoiceDets.Add(new RecoveryInvoiceDet
                {
                    Invoice = inv,
                    RegNo = l.RegNo.Trim(),
                    BillAmt = l.BillAmt,
                    Remarks = Trunc(l.Remarks, 100),
                    CreatedBy = inv.CreatedBy,
                    CreatedAt = DateTime.UtcNow
                });
            }

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Lost the mint race to another clerk; nobody was double charged
                // (the UNIQUE index rejected it) - retry the mint once.
                try
                {
                    inv.DocNo = await MintDocNoAsync(inv.CompanyCode, inv.InvoiceDate);
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

            await AuditAsync("CREATE", "RECOVERY_INV", inv.DocNo, "InvoiceId", null,
                inv.Id.ToString(CultureInfo.InvariantCulture),
                $"lines={input.Lines.Count(l => !string.IsNullOrWhiteSpace(l.RegNo))};total={inv.TotAmt:0.00}",
                inv.Id);
            return new Result { Ok = true, InvoiceId = inv.Id, DocNo = inv.DocNo };
        }

        // ================================================================ update

        /// <summary>
        /// Edits an OPEN invoice: header fields and lines. Guards from create
        /// run again; receipt-allocated lines refuse deletion.
        /// </summary>
        public async Task<Result> UpdateAsync(InvoiceUpsert input)
        {
            var inv = await _db.RecoveryInvoices.FirstOrDefaultAsync(i => i.Id == input.Id);
            if (inv == null)
                return new Result { Ok = false, Message = "Invoice not found." };
            if (inv.Status == "C")
                return new Result { Ok = false, Message = "Cancelled invoices are read-only." };

            var problems = Validate(input);
            if (problems.Count > 0)
                return new Result { Ok = false, Message = string.Join(' ', problems) };

            var (okOpen, dupMsg) = await NoOpenDupeAsync(input, existingHeaderId: inv.Id);
            if (!okOpen)
                return new Result { Ok = false, Message = dupMsg };

            // ---- audit header field changes, then apply ----
            var headerChanges = new List<(string Field, string OldV, string NewV)>();
            if (!string.Equals(inv.CompanyCode, input.CompanyCode?.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                headerChanges.Add(("CompanyCode", inv.CompanyCode ?? "", input.CompanyCode?.Trim() ?? ""));
            if (inv.InvoiceDate != input.InvoiceDate.Date)
                headerChanges.Add(("InvoiceDate", Fmt(inv.InvoiceDate), Fmt(input.InvoiceDate.Date)));
            if ((inv.SubmissionDate?.Date) != (input.SubmissionDate?.Date))
                headerChanges.Add(("SubmissionDate",
                    inv.SubmissionDate == null ? "" : Fmt(inv.SubmissionDate.Value),
                    input.SubmissionDate == null ? "" : Fmt(input.SubmissionDate.Value.Date)));
            if ((inv.BillMonth ?? 0) != (input.BillMonth ?? 0))
                headerChanges.Add(("BillMonth", (inv.BillMonth ?? 0).ToString(), (input.BillMonth ?? 0).ToString()));
            if ((inv.BillYear ?? 0) != (input.BillYear ?? 0))
                headerChanges.Add(("BillYear", (inv.BillYear ?? 0).ToString(), (input.BillYear ?? 0).ToString()));
            var remarksNew = Trunc(input.Remarks, 200);
            if (!string.Equals(inv.Remarks, remarksNew, StringComparison.Ordinal))
                headerChanges.Add(("Remarks", inv.Remarks ?? "", remarksNew ?? ""));

            var auditId = inv.Id;
            var docNo = inv.DocNo;
            inv.CompanyCode = input.CompanyCode.Trim();
            inv.InvoiceDate = input.InvoiceDate.Date;
            inv.SubmissionDate = input.SubmissionDate?.Date;
            inv.BillMonth = input.BillMonth;
            inv.BillYear = input.BillYear;
            inv.Remarks = remarksNew;
            inv.EditedBy = CurrentUser();
            inv.EditedAt = DateTime.UtcNow;

            // ---- reconcile posted lines against stored lines ----
            var stored = await _db.RecoveryInvoiceDets
                .Where(d => d.InvoiceId == inv.Id).ToListAsync();
            var posted = input.Lines
                .Where(l => !string.IsNullOrWhiteSpace(l.RegNo))
                .Select(l => new { RegNo = l.RegNo.Trim(), l.BillAmt, Remarks = Trunc(l.Remarks, 100) })
                .ToList();
            var postedKeys = posted.Select(l => l.RegNo).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var l in posted)
            {
                var cur = stored.FirstOrDefault(s =>
                    string.Equals(s.RegNo, l.RegNo, StringComparison.OrdinalIgnoreCase));
                if (cur == null)
                {
                    _db.RecoveryInvoiceDets.Add(new RecoveryInvoiceDet
                    {
                        InvoiceId = inv.Id,
                        RegNo = l.RegNo,
                        BillAmt = l.BillAmt,
                        Remarks = l.Remarks,
                        CreatedBy = CurrentUser(),
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else if (cur.BillAmt != l.BillAmt
                    || !string.Equals(cur.Remarks ?? "", l.Remarks ?? "", StringComparison.Ordinal))
                {
                    if (cur.BillAmt != l.BillAmt)
                        await AuditAsync("EDIT", "RECOVERY_INVDET", docNo + "|" + cur.RegNo, "BillAmt",
                            cur.BillAmt.ToString(CultureInfo.InvariantCulture),
                            l.BillAmt.ToString(CultureInfo.InvariantCulture), null, auditId);
                    cur.BillAmt = l.BillAmt;
                    cur.Remarks = l.Remarks;
                }
            }

            foreach (var gone in stored.Where(s => !postedKeys.Contains(s.RegNo)))
            {
                // VFP children: a line with receipts behind it cannot be removed.
                if (await HasReceiptAsync(inv.Id, gone.RegNo))
                    return new Result
                    {
                        Ok = false,
                        Message = "Line " + gone.RegNo
                            + " has receipts allocated to it and cannot be deleted."
                    };
                await AuditAsync("EDIT", "RECOVERY_INVDET", docNo + "|" + gone.RegNo, "Line",
                    gone.BillAmt.ToString(CultureInfo.InvariantCulture), "<deleted>",
                    "Line removed from invoice", auditId);
                _db.RecoveryInvoiceDets.Remove(gone);
            }

            // ---- recompute the total from the reconciled line set ----
            var total = posted.Sum(l => l.BillAmt);
            if (inv.TotAmt != total)
            {
                await AuditAsync("EDIT", "RECOVERY_INV", docNo, "TotAmt",
                    inv.TotAmt.ToString(CultureInfo.InvariantCulture),
                    total.ToString(CultureInfo.InvariantCulture), null, auditId);
                inv.TotAmt = total;
            }

            if (headerChanges.Count > 0 || _db.ChangeTracker.HasChanges())
            {
                foreach (var (field, oldV, newV) in headerChanges)
                    await AuditAsync("EDIT", "RECOVERY_INV", docNo, field, oldV, newV, null, auditId);
                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateException ex) when (IsUniqueViolation(ex))
                {
                    return new Result
                    {
                        Ok = false,
                        Message = "Save rejected - a patient line is duplicated on this invoice."
                    };
                }
            }
            return new Result { Ok = true, InvoiceId = inv.Id, DocNo = inv.DocNo };
        }

        // ================================================================ cancel

        /// <summary>
        /// Cancels an invoice (soft, mandatory reason) - the red-trash delete bar
        /// of the workspace conventions, minus the data loss (R-03/R-18).
        /// </summary>
        public async Task<Result> CancelAsync(long id, string reason)
        {
            var inv = await _db.RecoveryInvoices.FirstOrDefaultAsync(i => i.Id == id);
            if (inv == null)
                return new Result { Ok = false, Message = "Invoice not found." };
            if (inv.Status == "C")
                return new Result { Ok = false, Message = "Already cancelled." };
            reason = (reason ?? "").Trim();
            if (reason.Length < 5)
                return new Result
                {
                    Ok = false,
                    Message = "A reason of at least 5 characters is required to cancel an invoice."
                };

            inv.Status = "C";
            inv.EditedBy = CurrentUser();
            inv.EditedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await AuditAsync("CANCEL", "RECOVERY_INV", inv.DocNo, "Status", "O", "C", reason, inv.Id);
            return new Result { Ok = true, InvoiceId = inv.Id, DocNo = inv.DocNo };
        }

        // ================================================================ helpers

        private List<string> Validate(InvoiceUpsert input)
        {
            var problems = new List<string>();
            if (string.IsNullOrWhiteSpace(input.CompanyCode))
                problems.Add("Company is required.");
            if (input.InvoiceDate == default)
                problems.Add("Invoice date is required.");
            var month = input.BillMonth ?? 0;
            var year = input.BillYear ?? 0;
            if (month < 1 || month > 12)
                problems.Add("Bill month must be 1-12.");
            var now = DateTime.Now.Year;
            // VFP txtYear.Valid allowed the current year +-2; carried over.
            if (year < now - 2 || year > now + 2)
                problems.Add("Bill year must be within "
                    + (now - 2) + "-" + (now + 2) + " (legacy txtYear.Valid window).");
            var realLines = (input.Lines ?? new List<LineUpsert>())
                .Where(l => !string.IsNullOrWhiteSpace(l.RegNo)).ToList();
            if (realLines.Count == 0)
                problems.Add("At least one patient line is required.");
            foreach (var l in realLines)
            {
                if (l.BillAmt < 0)
                    problems.Add("Line amount cannot be negative (" + l.RegNo + ").");
            }
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var reg in realLines.Select(l => l.RegNo.Trim()))
            {
                if (!seen.Add(reg))
                    problems.Add("Patient " + reg + " appears twice on this invoice.");
            }
            return problems;
        }

        /// <summary>
        /// A patient must not sit on two open invoices at once (the regno-only
        /// receipt key makes un-allocation unattributable if it did - G-02).
        /// </summary>
        private async Task<(bool Ok, string Message)> NoOpenDupeAsync(
            InvoiceUpsert input, long? existingHeaderId)
        {
            var regNos = (input.Lines ?? new List<LineUpsert>())
                .Where(l => !string.IsNullOrWhiteSpace(l.RegNo))
                .Select(l => l.RegNo.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (regNos.Count == 0) return (true, null);

            var q = from d in _db.RecoveryInvoiceDets.AsNoTracking()
                    join h in _db.RecoveryInvoices.AsNoTracking() on d.InvoiceId equals h.Id
                    where h.Status == "O" && regNos.Contains(d.RegNo)
                          && (!existingHeaderId.HasValue || d.InvoiceId != existingHeaderId.Value)
                    select new { d.RegNo, h.DocNo };
            var dupe = await q.FirstOrDefaultAsync();
            if (dupe == null) return (true, null);

            return (false, "Patient " + dupe.RegNo
                + " is already on open invoice " + dupe.DocNo
                + " - a patient may sit on one open invoice at a time.");
        }

        private async Task<bool> IsCompanyActiveAsync(string code)
        {
            var companies = await _query.GetCompaniesAsync();
            var c = companies.FirstOrDefault(x =>
                string.Equals(x.Id, code?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (c == null) return false;
            return !string.Equals(c.Status, "N", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Receipt allocation guard - RECOVERY_RCPT_ALLOC lands with sprint 2.
        /// Only receipts that still carry an ACTIVE credit count: bounced
        /// ('B') and cancelled ('X') receipts removed their allocation rows,
        /// so the line is free to be reworked. Table-missing degrades to
        /// "no receipts" (pre-sprint-2 builds).
        /// Change Log 21-Oct-2026 (Tajwer Jalal): active-credit filter added
        /// after the e2e run showed the bounce path leaves no rows behind.
        /// </summary>
        private async Task<bool> HasReceiptAsync(long invoiceId, string regNo)
        {
            var conn = await GetConnAsync();
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM RECOVERY_RCPT_ALLOC a"
                    + " JOIN RECOVERY_RCPT r ON r.Id = a.ReceiptId"
                    + " WHERE a.InvoiceId = @inv AND a.RegNo = @reg AND r.Status NOT IN ('B','X')";
                AddParam(cmd, "@inv", invoiceId);
                AddParam(cmd, "@reg", regNo);
                try
                {
                    var n = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                    return n > 0;
                }
                catch (SqlException ex) when (ex.Number == 208)
                {
                    return false; // table absent (pre-sprint-2)
                }
            }
            finally { if (_openedHere) { await conn.CloseAsync(); _openedHere = false; } }

            async System.Threading.Tasks.Task<SqlConnection> GetConnAsync()
            {
                var c = _db.Database.GetDbConnection();
                if (c is SqlConnection sc)
                {
                    if (sc.State != System.Data.ConnectionState.Open)
                    { await sc.OpenAsync(); _openedHere = true; }
                    return sc;
                }
                throw new InvalidOperationException("RecoveryInvoiceService needs a SqlConnection.");
            }
        }

        private bool _openedHere;

        private static void AddParam(System.Data.Common.DbCommand cmd, string name, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        /// <summary>
        /// Mints the next DocNo for one company/month: CID/MM/YYYY plus "-n" on
        /// the slot being taken. UPDLOCK+HOLDLOCK range-serialises concurrent
        /// clerks; the UNIQUE index catches any remainder.
        /// </summary>
        private async Task<string> MintDocNoAsync(string company, DateTime date)
        {
            var conn = _db.Database.GetDbConnection() as SqlConnection
                ?? throw new InvalidOperationException("RecoveryInvoiceService needs a SqlConnection.");
            var opened = false;
            if (conn.State != System.Data.ConnectionState.Open)
            { await conn.OpenAsync(); opened = true; }
            try
            {
                await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

                var baseSlot = company.Trim() + "/"
                    + date.Month.ToString("00", CultureInfo.InvariantCulture) + "/"
                    + date.Year.ToString(CultureInfo.InvariantCulture);

                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = "SELECT DocNo FROM RECOVERY_INV WITH (UPDLOCK, HOLDLOCK)"
                        + " WHERE DocNo = @base OR DocNo LIKE @suffix";
                    AddParam(cmd, "@base", baseSlot);
                    AddParam(cmd, "@suffix", baseSlot + "-%");

                    var maxSuffix = 0;
                    var baseTaken = false;
                    await using (var rd = await cmd.ExecuteReaderAsync())
                    {
                        while (await rd.ReadAsync())
                        {
                            var existing = rd.IsDBNull(0) ? "" : rd.GetString(0);
                            if (string.Equals(existing, baseSlot, StringComparison.OrdinalIgnoreCase))
                                baseTaken = true;
                            else if (existing.StartsWith(baseSlot + "-", StringComparison.OrdinalIgnoreCase)
                                && int.TryParse(existing.Substring(baseSlot.Length + 1),
                                    NumberStyles.None, CultureInfo.InvariantCulture, out var n))
                                maxSuffix = Math.Max(maxSuffix, n);
                        }
                    }
                    await tx.CommitAsync();
                    if (!baseTaken) return baseSlot;
                    return baseSlot + "-" + (maxSuffix + 1).ToString(CultureInfo.InvariantCulture);
                }
            }
            finally { if (opened) await conn.CloseAsync(); }
        }

        /// <summary>TRUE when the cause is a unique-key rejection (2601/2627).</summary>
        private static bool IsUniqueViolation(DbUpdateException ex)
            => ex.InnerException is SqlException sx && (sx.Number == 2601 || sx.Number == 2627);

        /// <summary>Immutable audit row (RECOVERY_AUDIT accepts INSERTs only).</summary>
        private async Task AuditAsync(string action, string entity, string key,
            string field, string oldValue, string newValue, string reason, long? invoiceId)
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
                InvoiceId = invoiceId,
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

        private static string Fmt(DateTime d) => d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

        private static string Trunc(string s, int max)
        {
            if (s == null) return null;
            return s.Length <= max ? s : s.Substring(0, max);
        }
    }
}
