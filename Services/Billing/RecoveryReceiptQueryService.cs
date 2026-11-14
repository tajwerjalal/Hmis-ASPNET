using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models.Recovery;

namespace HmisWeb.Services.Billing
{
    /// <summary>
    /// READ service for the Receipts / Cheques (IPD) screen /Recovery/Receipt.
    /// Ports the reads of VFP BillSys form RECPT (bs0603) onto RECOVERY_RCPT /
    /// RECOVERY_RCPT_ALLOC plus RECOVERY_INV(+DET) for the allocation targets.
    ///
    /// Business rules carried over from the VFP receipt grid (rcptf)
    ///   * receipt lines are per-patient (one line per regno per voucher);
    ///   * the amount CREDITED against a bill is RcptAmt + ItaxDed + OthDed
    ///     (legacy gross-up semantics; rcptamt/95*5 tax default); the money
    ///     reaching the bank is RcptAmt alone (legacy TOT_AMT semantic);
    ///   * cash reaches the counter cleared ('C'); a cheque stays 'P'ending
    ///     until the Clear action - the legacy system had no cheque status.
    ///
    /// Remaining = invoice (line) amount - sum of credited amounts of receipts
    /// still active ('P'/'C'); a bounced or cancelled receipt refunds its
    /// credit automatically (bounce deletes its allocation rows).
    ///
    /// No data is written here - side-effect free.
    ///
    /// Developer: Tajwer Jalal | Date: 21-Oct-2026 | Time: 10:55
    /// </summary>
    public class RecoveryReceiptQueryService
    {
        private readonly ApplicationDbContext _db;

        public RecoveryReceiptQueryService(ApplicationDbContext db) => _db = db;

        /// <summary>Rows per page of the list tab.</summary>
        public const int PageSize = 50;

        /// <summary>Paged receipt list with company names + line counts.</summary>
        public async Task<SearchResult> SearchPageAsync(string company, string method,
            string status, string term, int pageIndex, int pageSize = 0)
        {
            if (pageSize <= 0) pageSize = PageSize;
            if (pageIndex < 1) pageIndex = 1;

            var q = _db.RecoveryRcpts.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(company))
                q = q.Where(r => r.CompanyCode == company);
            if (!string.IsNullOrWhiteSpace(method))
                q = q.Where(r => r.Method == method);
            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(r => r.Status == status);
            if (!string.IsNullOrWhiteSpace(term))
            {
                var t = term.Trim();
                q = q.Where(r => r.ReceiptNo.Contains(t)
                    || (r.CompanyCode != null && r.CompanyCode.Contains(t))
                    || (r.ChequeNo != null && r.ChequeNo.Contains(t))
                    || (r.Remarks != null && r.Remarks.Contains(t)));
            }

            var total = await q.CountAsync();
            var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
            if (pageIndex > totalPages) pageIndex = totalPages;

            var page = await q
                .OrderByDescending(r => r.RcptDate).ThenByDescending(r => r.Id)
                .Skip((pageIndex - 1) * pageSize).Take(pageSize)
                .Select(r => new
                {
                    r.Id, r.ReceiptNo, r.Method, r.RcptDate, r.CompanyCode,
                    r.ChequeNo, r.Bank, r.ChequeDate, r.RcvdBy, r.TotAmt, r.Status, r.Remarks,
                    Lines = _db.RecoveryRcptAllocs.Count(a => a.ReceiptId == r.Id)
                })
                .ToListAsync();

            var nameMap = await CompanyNamesAsync(
                page.Select(x => x.CompanyCode).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList());

            return new SearchResult
            {
                Rows = page.Select(x => new ListRow
                {
                    Id = x.Id,
                    ReceiptNo = x.ReceiptNo,
                    Method = x.Method,
                    RcptDate = x.RcptDate,
                    CompanyCode = x.CompanyCode,
                    CompanyName = nameMap.TryGetValue(x.CompanyCode ?? "", out var n) ? n : "-",
                    ChequeNo = x.ChequeNo,
                    Bank = x.Bank,
                    ChequeDate = x.ChequeDate,
                    RcvdBy = x.RcvdBy,
                    LineCount = x.Lines,
                    TotAmt = x.TotAmt,
                    Status = x.Status,
                    Remarks = x.Remarks
                }).ToList(),
                TotalRows = total,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        /// <summary>Full popup payload: header + lines + audit trail.</summary>
        public async Task<ReceiptDetail> GetDetailAsync(long id)
        {
            var h = await _db.RecoveryRcpts.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (h == null) return null;

            var invoiceIds = await _db.RecoveryRcptAllocs.AsNoTracking()
                .Where(a => a.ReceiptId == id && a.InvoiceId != null)
                .Select(a => a.InvoiceId.Value).Distinct().ToListAsync();

            var invMap = await _db.RecoveryInvoices.AsNoTracking()
                .Where(i => invoiceIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i.DocNo);

            var lines = await _db.RecoveryRcptAllocs.AsNoTracking()
                .Where(a => a.ReceiptId == id)
                .OrderBy(a => a.Id)
                .Select(a => new { a.Id, a.InvoiceId, a.RegNo, a.RcptAmt, a.ItaxDed, a.OthDed, a.Remarks })
                .ToListAsync();

            var patients = await PatientNamesAsync(lines.Select(l => l.RegNo).Distinct().ToList());

            // both the receipt's own rows and its allocation rows (entity key
            // "ReceiptNo|RegNo") belong to one trail
            var audit = await _db.RecoveryAudits.AsNoTracking()
                .Where(a => a.EntityKey == h.ReceiptNo
                            || a.EntityKey.StartsWith(h.ReceiptNo + "|"))
                .OrderByDescending(a => a.CreatedAt).Take(50)
                .Select(a => new AuditRow
                {
                    Id = a.Id, Action = a.Action, Field = a.Field,
                    OldValue = a.OldValue, NewValue = a.NewValue, Reason = a.Reason,
                    UserName = a.UserName, CreatedAt = a.CreatedAt
                }).ToListAsync();

            return new ReceiptDetail
            {
                Header = h,
                Lines = lines.Select(l => new LineRow
                {
                    Id = l.Id,
                    InvoiceId = l.InvoiceId,
                    DocNo = l.InvoiceId.HasValue && invMap.TryGetValue(l.InvoiceId.Value, out var d) ? d : null,
                    RegNo = l.RegNo,
                    PatientName = patients.TryGetValue(l.RegNo, out var p) ? p : "-",
                    RcptAmt = l.RcptAmt,
                    ItaxDed = l.ItaxDed,
                    OthDed = l.OthDed,
                    Remarks = l.Remarks
                }).ToList(),
                Audit = audit
            };
        }

        /// <summary>
        /// OPEN invoices of one company with money still outstanding after the
        /// credited amounts of ACTIVE ('P'/'C') receipts - the allocation list
        /// of the popup. Per line: BillAmt - SUM(credited) for that regno.
        /// Credited money is computed with raw SQL (GROUP BY) - the amounts
        /// credited = RcptAmt + ItaxDed + OthDed per active receipt line.
        /// </summary>
        public async Task<List<InvoiceCandidate>> OpenInvoicesAsync(string company)
        {
            var invoices = await _db.RecoveryInvoices.AsNoTracking()
                .Where(i => i.DocType == "I" && i.Status == "O"
                            && i.CompanyCode == company && i.TotAmt > 0)
                .OrderBy(i => i.InvoiceDate).ThenBy(i => i.Id)
                .ToListAsync();
            if (invoices.Count == 0) return new List<InvoiceCandidate>();

            var ids = invoices.Select(i => i.Id).ToList();
            var lines = await _db.RecoveryInvoiceDets.AsNoTracking()
                .Where(d => ids.Contains(d.InvoiceId))
                .Select(d => new { d.InvoiceId, d.RegNo, d.BillAmt })
                .ToListAsync();

            var idList = string.Join(",", ids);
            var invCred = new Dictionary<long, decimal>();
            var lineCred = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var conn = _db.Database.GetDbConnection();
            var opened = false;
            if (conn.State != System.Data.ConnectionState.Open) { await conn.OpenAsync(); opened = true; }
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText =
                    "SELECT a.InvoiceId, a.RegNo,"
                    + " SUM(a.RcptAmt + a.ItaxDed + a.OthDed) AS Applied"
                    + " FROM RECOVERY_RCPT_ALLOC a"
                    + " JOIN RECOVERY_RCPT r ON r.Id = a.ReceiptId"
                    + " WHERE a.InvoiceId IN (" + idList + ")"
                    + " AND r.Status IN ('P','C')"
                    + " GROUP BY a.InvoiceId, a.RegNo";
                using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    var invId = rd.GetInt64(0);
                    var reg = rd.IsDBNull(1) ? "" : rd.GetString(1).Trim();
                    var applied = rd.IsDBNull(2) ? 0m : rd.GetDecimal(2);
                    invCred[invId] = (invCred.TryGetValue(invId, out var c0) ? c0 : 0m) + applied;
                    var key = invId + "|" + reg;
                    lineCred[key] = (lineCred.TryGetValue(key, out var c1) ? c1 : 0m) + applied;
                }
            }
            finally { if (opened) await conn.CloseAsync(); }

            var result = new List<InvoiceCandidate>();
            foreach (var inv in invoices)
            {
                invCred.TryGetValue(inv.Id, out var invApplied);
                var remaining = inv.TotAmt - invApplied;
                if (remaining <= 0) continue;

                var c = new InvoiceCandidate
                {
                    InvoiceId = inv.Id,
                    DocNo = inv.DocNo,
                    InvoiceDate = inv.InvoiceDate,
                    TotAmt = inv.TotAmt,
                    Remaining = remaining
                };
                foreach (var l in lines.Where(l => l.InvoiceId == inv.Id))
                {
                    lineCred.TryGetValue(inv.Id + "|" + l.RegNo.Trim(), out var applied);
                    var lineRemaining = l.BillAmt - applied;
                    if (lineRemaining <= 0) continue;
                    c.Lines.Add(new InvoiceCandidateLine
                    {
                        LineId = 0,
                        RegNo = l.RegNo,
                        BillAmt = l.BillAmt,
                        Applied = applied,
                        Remaining = lineRemaining
                    });
                }
                result.Add(c);
            }
            return result;
        }

        /// <summary>Company names for a set of COMPANY.Id codes (raw SQL - the COMPANY mirror has no EF entity).</summary>
        public async Task<Dictionary<string, string>> CompanyNamesAsync(List<string> codes)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            codes ??= new List<string>();
            codes = codes.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
            if (codes.Count == 0) return map;

            var conn = _db.Database.GetDbConnection();
            var opened = false;
            if (conn.State != System.Data.ConnectionState.Open) { await conn.OpenAsync(); opened = true; }
            try
            {
                using var cmd = conn.CreateCommand();
                var names = new List<string>();
                for (var i = 0; i < codes.Count; i++)
                {
                    names.Add("@c" + i);
                    var p = cmd.CreateParameter();
                    p.ParameterName = "@c" + i;
                    p.Value = codes[i];
                    cmd.Parameters.Add(p);
                }
                cmd.CommandText = "SELECT ID, NAME FROM COMPANY WHERE ID IN ("
                    + string.Join(",", names) + ")";
                using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    var id = rd.IsDBNull(0) ? "" : rd.GetString(0).Trim();
                    map[id] = rd.IsDBNull(1) ? "-" : rd.GetString(1).Trim();
                }
            }
            finally { if (opened) await conn.CloseAsync(); }
            return map;
        }

        /// <summary>Patient names for a set of regnos (dictionary by regno).</summary>
        public async Task<Dictionary<string, string>> PatientNamesAsync(List<string> regNos)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (regNos == null || regNos.Count == 0) return map;

            var rows = await (
                from r in _db.LegacyRegisters.AsNoTracking()
                where regNos.Contains(r.RegNo)
                join p in _db.Patients.AsNoTracking() on r.PCode equals p.Code into pj
                from p in pj.DefaultIfEmpty()
                select new { r.RegNo, Name = p != null && p.Person != null ? p.Person.Name : null })
                .ToListAsync();

            foreach (var x in rows)
                map[x.RegNo] = string.IsNullOrWhiteSpace(x.Name) ? "-" : x.Name!;
            foreach (var r in regNos)
                if (!map.ContainsKey(r)) map[r] = "-";
            return map;
        }

        // ------------------------------------------------------------ shapes

        /// <summary>List tab row.</summary>
        public class ListRow
        {
            public long Id { get; set; }
            public string ReceiptNo { get; set; }
            public string Method { get; set; }
            public DateTime RcptDate { get; set; }
            public string CompanyCode { get; set; }
            public string CompanyName { get; set; }
            public string ChequeNo { get; set; }
            public string Bank { get; set; }
            public DateTime? ChequeDate { get; set; }
            public string RcvdBy { get; set; }
            public int LineCount { get; set; }
            public decimal TotAmt { get; set; }
            public string Status { get; set; }
            public string Remarks { get; set; }
        }

        /// <summary>Search payload with paging metadata.</summary>
        public class SearchResult
        {
            public List<ListRow> Rows { get; set; } = new();
            public int TotalRows { get; set; }
            public int PageIndex { get; set; }
            public int PageSize { get; set; }
            public int TotalPages => TotalRows == 0 ? 1 : (int)Math.Ceiling(TotalRows / (double)PageSize);
            public int FirstRowOnPage => TotalRows == 0 ? 0 : (PageIndex - 1) * PageSize + 1;
            public int LastRowOnPage => Math.Min(PageIndex * PageSize, TotalRows);
        }

        /// <summary>Allocation line of the popup.</summary>
        public class LineRow
        {
            public long Id { get; set; }
            public long? InvoiceId { get; set; }
            public string DocNo { get; set; }
            public string RegNo { get; set; }
            public string PatientName { get; set; }
            public decimal RcptAmt { get; set; }
            public decimal ItaxDed { get; set; }
            public decimal OthDed { get; set; }
            public string Remarks { get; set; }
        }

        /// <summary>Audit row of the popup.</summary>
        public class AuditRow
        {
            public long Id { get; set; }
            public string Action { get; set; }
            public string Field { get; set; }
            public string OldValue { get; set; }
            public string NewValue { get; set; }
            public string Reason { get; set; }
            public string UserName { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        /// <summary>Popup payload.</summary>
        public class ReceiptDetail
        {
            public RecoveryRcpt Header { get; set; }
            public List<LineRow> Lines { get; set; } = new();
            public List<AuditRow> Audit { get; set; } = new();
        }

        /// <summary>Open invoice candidate for the allocation dialog.</summary>
        public class InvoiceCandidate
        {
            public long InvoiceId { get; set; }
            public string DocNo { get; set; }
            public DateTime InvoiceDate { get; set; }
            public decimal TotAmt { get; set; }
            public decimal Remaining { get; set; }
            public List<InvoiceCandidateLine> Lines { get; set; } = new();
        }

        /// <summary>Patient line of an open-invoice candidate.</summary>
        public class InvoiceCandidateLine
        {
            public long LineId { get; set; }
            public string RegNo { get; set; }
            public decimal BillAmt { get; set; }
            public decimal Applied { get; set; }
            public decimal Remaining { get; set; }
        }
    }
}
