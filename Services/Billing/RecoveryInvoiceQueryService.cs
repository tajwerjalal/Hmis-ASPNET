using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Models.Legacy;
using HmisWeb.Models.Recovery;

namespace HmisWeb.Services.Billing
{
    /// <summary>
    /// READ service for the company Invoice/Letter screen /Recovery/Invoice.
    /// Ports the reads of VFP BillSys form INVOICE (bs0601) against the new
    /// RECOVERY_INV / RECOVERY_INVDET tables plus the legacy mirrors used by the
    /// line-patient lookup: REGISTER (TYPE='I', BILLGEN=1), PATIENT, COMPANY.
    ///
    /// Business rules carried over from the VFP child grid lookups
    ///   * only company-billed inpatients: REGISTER.TYPE='I' AND BILLGEN <> 0
    ///     AND REGISTER.CID = the invoice's company;
    ///   * line amount defaults from REGISTER.TBILLAMT (VFP txtCgrdRegno.Valid);
    ///   * a patient already on an OPEN invoice of any company is flagged so the
    ///     service can refuse the line (review finding G-02 remediation).
    ///
    /// No data is written by this service - it is side-effect free.
    ///
    /// Developer: Tajwer Jalal | Date: 20-Oct-2026 | Time: 18:35
    /// </summary>
    public class RecoveryInvoiceQueryService
    {
        private readonly ApplicationDbContext _db;

        public RecoveryInvoiceQueryService(ApplicationDbContext db) => _db = db;

        /// <summary>Rows per page of the list tab.</summary>
        public const int PageSize = 50;

        /// <summary>
        /// Paged list of invoices with the company name resolved.
        /// </summary>
        /// <param name="company">Optional COMPANY.ID exact filter.</param>
        /// <param name="month">Optional bill month 1-12.</param>
        /// <param name="year">Optional bill year.</param>
        /// <param name="term">Optional DocNo / company fragment search.</param>
        /// <param name="status">'O' pen / 'C' cancelled / null = all.</param>
        public async Task<SearchResult> SearchPageAsync(string company, int? month, int? year,
            string term, string status, int pageIndex, int pageSize = 0)
        {
            if (pageSize <= 0) pageSize = PageSize;
            if (pageIndex < 1) pageIndex = 1;

            var q = _db.RecoveryInvoices.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(company))
                q = q.Where(i => i.CompanyCode == company);
            if (month.HasValue)
                q = q.Where(i => i.BillMonth == month.Value);
            if (year.HasValue)
                q = q.Where(i => i.BillYear == year.Value);
            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(i => i.Status == status);
            if (!string.IsNullOrWhiteSpace(term))
            {
                var t = term.Trim();
                q = q.Where(i => i.DocNo.Contains(t)
                    || (i.CompanyCode != null && i.CompanyCode.Contains(t))
                    || (i.Remarks != null && i.Remarks.Contains(t)));
            }

            var total = await q.CountAsync();
            var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
            if (pageIndex > totalPages) pageIndex = totalPages;

            var page = await q
                .OrderByDescending(i => i.InvoiceDate).ThenByDescending(i => i.Id)
                .Skip((pageIndex - 1) * pageSize).Take(pageSize)
                .Select(i => new
                {
                    i.Id, i.DocNo, i.DocType, i.CompanyCode, i.InvoiceDate,
                    i.SubmissionDate, i.BillMonth, i.BillYear, i.TotAmt, i.Status, i.Remarks,
                    Lines = _db.RecoveryInvoiceDets.Count(d => d.InvoiceId == i.Id)
                })
                .ToListAsync();

            var nameMap = await CompanyNamesAsync(
                page.Select(x => x.CompanyCode).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList());

            return new SearchResult
            {
                Rows = page.Select(x => new ListRow
                {
                    Id = x.Id,
                    DocNo = x.DocNo,
                    DocType = x.DocType,
                    CompanyCode = x.CompanyCode,
                    CompanyName = nameMap.TryGetValue(x.CompanyCode ?? "", out var n) ? n : "-",
                    InvoiceDate = x.InvoiceDate,
                    SubmissionDate = x.SubmissionDate,
                    BillMonth = x.BillMonth,
                    BillYear = x.BillYear,
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

        /// <summary>
        /// Full popup payload: header + lines + audit trail of one invoice.
        /// </summary>
        public async Task<InvoiceDetail> GetDetailAsync(long id)
        {
            var h = await _db.RecoveryInvoices.AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id);
            if (h == null) return null;

            var regNos = await _db.RecoveryInvoiceDets.AsNoTracking()
                .Where(d => d.InvoiceId == id)
                .Select(d => d.RegNo).ToListAsync();

            var patients = await PatientNamesAsync(regNos);
            var lines = await _db.RecoveryInvoiceDets.AsNoTracking()
                .Where(d => d.InvoiceId == id)
                .OrderBy(d => d.Id)
                .Select(d => new { d.Id, d.RegNo, d.BillAmt, d.Remarks })
                .ToListAsync();

            var audit = await _db.RecoveryAudits.AsNoTracking()
                .Where(a => a.InvoiceId == id)
                .OrderByDescending(a => a.CreatedAt).Take(50)
                .Select(a => new AuditRow
                {
                    Id = a.Id, Action = a.Action, Entity = a.Entity, EntityKey = a.EntityKey,
                    Field = a.Field, OldValue = a.OldValue, NewValue = a.NewValue,
                    Reason = a.Reason, UserName = a.UserName, CreatedAt = a.CreatedAt
                }).ToListAsync();

            return new InvoiceDetail
            {
                Header = h,
                Lines = lines.Select(l => new LineRow
                {
                    Id = l.Id,
                    RegNo = l.RegNo,
                    PatientName = patients.TryGetValue(l.RegNo, out var n) ? n : "-",
                    BillAmt = l.BillAmt,
                    Remarks = l.Remarks
                }).ToList(),
                Audit = audit
            };
        }

        /// <summary>
        /// Line-patient lookup for the popup (VFP child grid regno lookup, set based).
        /// Returns billed inpatients of one company with the default amount.
        /// Change Log 20-Oct-2026 20:20 (Tajwer Jalal): first cut correlated the
        /// patient name / last-bill subqueries per row over the 100k-row
        /// REGISTER mirror and timed out. Two-passed instead: take the page of
        /// register rows first, then batch-resolve names and last bill nos.
        /// </summary>
        /// <param name="company">COMPANY.Id (required - the grid was company keyed).</param>
        /// <param name="term">Optional regno / patient-code fragment.</param>
        public async Task<List<PatientRow>> PatientSearchAsync(string company, string term,
            List<string> excludeOpen = null)
        {
            var q = _db.LegacyRegisters.AsNoTracking()
                .Where(r => r.Type == "I" && r.BillGen == true && r.Cid == company);

            if (!string.IsNullOrWhiteSpace(term))
            {
                var t = term.Trim();
                q = q.Where(r => r.RegNo.Contains(t) || r.PCode.Contains(t));
            }
            if (excludeOpen != null && excludeOpen.Count > 0)
                q = q.Where(r => !excludeOpen.Contains(r.RegNo));

            // pass 1 - the window of register rows only (one indexed scan).
            var baseRows = await q
                .OrderByDescending(r => r.RDate)
                .Take(60)
                .Select(r => new { r.RegNo, r.PCode, r.RDate, r.DDate, r.TBillAmt })
                .ToListAsync();
            if (baseRows.Count == 0) return new List<PatientRow>();

            var regNos = baseRows.Select(x => x.RegNo).ToList();
            var pCodes = baseRows.Select(x => x.PCode).Where(c => !string.IsNullOrWhiteSpace(c)).ToList();

            // pass 2a - patient names for exactly these rows.
            var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (pCodes.Count > 0)
            {
                var nm = await _db.Patients.AsNoTracking()
                    .Where(p => pCodes.Contains(p.Code))
                    .Select(p => new
                    {
                        p.Code,
                        Name = p.Person != null
                            ? (p.Person.Name ?? p.Person.NameWithInitials ?? p.Person.FullName)
                            : null
                    })
                    .ToListAsync();
                foreach (var x in nm)
                    if (!string.IsNullOrWhiteSpace(x.Name))
                        names[x.Code] = x.Name;
            }

            // pass 2b - the latest billed bill-no per regno of the window.
            var bills = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            var bl = await _db.IpdBillHeaders.AsNoTracking()
                .Where(h => regNos.Contains(h.HosCode))
                .GroupBy(h => h.HosCode)
                .Select(g => new { HosCode = g.Key, BillNo = g.Max(h => h.BillNo) })
                .ToListAsync();
            foreach (var x in bl)
                bills[x.HosCode] = x.BillNo;

            return baseRows.Select(x => new PatientRow
            {
                RegNo = x.RegNo,
                PCode = x.PCode,
                PatientName = names.TryGetValue(x.PCode ?? "", out var n) ? n : "-",
                AdmitDate = x.RDate,
                DischargeDate = x.DDate,
                BillNo = bills.TryGetValue(x.RegNo, out var bn) ? bn : 0,
                DefaultAmt = x.TBillAmt ?? 0m,
                AlreadyBilledAmt = x.TBillAmt ?? 0m
            }).ToList();
        }

        /// <summary>
        /// Patient names for a set of regnos (used by the detail popup and the
        /// covering-letter renderer). One query, dictionary keyed by regno.
        /// </summary>
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
            // regnos with no REGISTER row still need an entry for the UI.
            foreach (var r in regNos)
                if (!map.ContainsKey(r)) map[r] = "-";
            return map;
        }

        /// <summary>
        /// Companies for the filter/posting dropdowns (COMPANY legacy mirror, read
        /// through raw SQL exactly like BillingReportService does - there is no
        /// COMPANY entity in the context).Status rides along so the popup can
        /// refuse inactive companies (legacy obilvalue guard).
        /// </summary>
        public async Task<List<CompanyRow>> GetCompaniesAsync()
        {
            var rows = new List<CompanyRow>();
            var conn = _db.Database.GetDbConnection();
            var opened = false;
            if (conn.State != System.Data.ConnectionState.Open) { await conn.OpenAsync(); opened = true; }
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT ID, NAME, STATUS FROM COMPANY "
                    + "WHERE ISNULL(ID,'') <> '' ORDER BY NAME";
                using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    rows.Add(new CompanyRow
                    {
                        Id = rd.IsDBNull(0) ? "" : rd.GetString(0).Trim(),
                        Name = rd.IsDBNull(1) ? "" : rd.GetString(1).Trim(),
                        Status = rd.IsDBNull(2) ? "" : rd.GetString(2).Trim()
                    });
                }
            }
            finally { if (opened) await conn.CloseAsync(); }
            return rows;
        }

        /// <summary>NAME map for a set of COMPANY.Id codes (one raw query).</summary>
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

        // ---------------------------------------------------------------- shapes

        /// <summary>List tab row.</summary>
        public class ListRow
        {
            public long Id { get; set; }
            public string DocNo { get; set; }
            public string DocType { get; set; }
            public string CompanyCode { get; set; }
            public string CompanyName { get; set; }
            public DateTime InvoiceDate { get; set; }
            public DateTime? SubmissionDate { get; set; }
            public int? BillMonth { get; set; }
            public int? BillYear { get; set; }
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

        /// <summary>Line row of the popup grid.</summary>
        public class LineRow
        {
            public long Id { get; set; }
            public string RegNo { get; set; }
            public string PatientName { get; set; }
            public decimal BillAmt { get; set; }
            public string Remarks { get; set; }
        }

        /// <summary>Audit row shown under the popup.</summary>
        public class AuditRow
        {
            public long Id { get; set; }
            public string Action { get; set; }
            public string Entity { get; set; }
            public string EntityKey { get; set; }
            public string Field { get; set; }
            public string OldValue { get; set; }
            public string NewValue { get; set; }
            public string Reason { get; set; }
            public string UserName { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        /// <summary>Popup payload.</summary>
        public class InvoiceDetail
        {
            public RecoveryInvoice Header { get; set; }
            public List<LineRow> Lines { get; set; } = new();
            public List<AuditRow> Audit { get; set; } = new();
        }

        /// <summary>Patient candidate for the line-add lookup.</summary>
        public class PatientRow
        {
            public string RegNo { get; set; }
            public string PCode { get; set; }
            public string PatientName { get; set; }
            public DateTime? AdmitDate { get; set; }
            public DateTime? DischargeDate { get; set; }
            public long? BillNo { get; set; }
            public decimal DefaultAmt { get; set; }
            public decimal AlreadyBilledAmt { get; set; }
        }

        /// <summary>Company dropdown row.</summary>
        public class CompanyRow
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Status { get; set; }
        }
    }
}
