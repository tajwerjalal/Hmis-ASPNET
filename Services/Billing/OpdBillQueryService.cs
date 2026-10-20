using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Models.Legacy;

namespace HmisWeb.Services.Billing
{
    /// <summary>
    /// READ service for the Outdoor Patient (OPD Bill) form /opd/Billing.
    /// Ports the reads of VFP form BS0101 (frmopdtran) against the legacy tables
    /// REGISTER / OBIL_MST / OBIL_DET / OPDBILL plus the app PatientEncounter.
    ///
    /// Business rules
    ///   * Only REGISTER rows with TYPE='O' are outpatient files.
    ///   * Department totals use the VFP formula (BS0101.updbuttons):
    ///       SUM(IIF(TYPE<>'R', NTOTAL, NTOTAL * -1))  -> returns subtracted
    ///   * Amount due (VFP) = used - TRCVD + TPYMT - CASHDISC.
    ///   * Lines are addressed by the surrogate Id, never by (dept,slipno,code),
    ///     because those business keys repeat in the data.
    ///   * No data is written by this service - it is side-effect free.
    ///
    /// Developer: Tajwer Jalal | Date: 20-Oct-2026 | Time: 15:00
    /// </summary>
    public class OpdBillQueryService
    {
        private readonly ApplicationDbContext _db;

        public OpdBillQueryService(ApplicationDbContext db) => _db = db;

        /// <summary>
        /// Loads a single outpatient file header.
        /// </summary>
        /// <param name="regNo">REGISTER.REGNO (TYPE='O').</param>
        /// <returns>The header, or null when the registration does not exist.</returns>
        public Task<RegisterLegacy> GetHeaderAsync(string regNo)
            => _db.LegacyRegisters.FirstOrDefaultAsync(r => r.Type == "O" && r.RegNo == regNo);

        /// <summary>
        /// Department summary for one file - the VFP updbuttons() loop, set based.
        /// </summary>
        /// <param name="regNo">Registration number.</param>
        /// <returns>One row per department with slip count and signed net amount.</returns>
        public async Task<List<DeptTotal>> GetDeptTotalsAsync(string regNo)
        {
            var rows = await _db.ObilMsts
                .Where(m => m.HosCode == regNo)
                .GroupBy(m => m.Dept)
                .Select(g => new DeptTotal
                {
                    Dept = g.Key,
                    SlipCount = g.Count(),
                    Amount = g.Sum(m => m.Type == "R" ? -(m.NTotal ?? 0) : (m.NTotal ?? 0))
                })
                .OrderBy(x => x.Dept)
                .ToListAsync();
            return rows;
        }

        /// <summary>
        /// Detail lines of one department for one file (soft-deleted lines excluded).
        /// Joins OBIL_DET to the parent slip because OBIL_DET has no HOS_CODE column.
        /// </summary>
        /// <param name="regNo">Registration number.</param>
        /// <param name="dept">2-char legacy department code.</param>
        /// <returns>Line rows ordered by slip number then row id.</returns>
        public async Task<List<BillLine>> GetLinesAsync(string regNo, string dept)
        {
            var q =
                from m in _db.ObilMsts.AsNoTracking().Where(m => m.HosCode == regNo && m.Dept == dept)
                join d in _db.ObilDets.AsNoTracking().Where(d => d.Retired == null || d.Retired == false)
                    on new { m.Dept, m.SlipNo } equals new { d.Dept, d.SlipNo }
                orderby m.SlipNo, d.Id
                select new { m, d };

            var rows = await q.ToListAsync();
            return rows.Select(x => new BillLine
            {
                Id = x.d.Id,
                Dept = x.d.Dept,
                SlipNo = x.d.SlipNo,
                SlipDate = x.m.SlipDt,
                Code = x.d.Code,
                Description = x.d.Desc,
                Qty = x.d.Qty ?? 0,
                Rate = x.d.Rate ?? 0,
                Amount = x.d.NAmt ?? 0,
                Remarks = x.d.Rem,
                ConCode = x.d.ConCode,
                IsReturn = x.m.Type == "R",
                SlipBillNo = x.m.Bill
            }).ToList();
        }

        /// <summary>
        /// Generated bill headers for a file (newest first).
        /// </summary>
        public Task<List<OpdBillHeader>> GetBillHeadersAsync(string regNo)
            => _db.OpdBillHeaders.AsNoTracking()
                .Where(h => h.HosCode == regNo && h.Stat != "X")
                .OrderByDescending(h => h.BillNo)
                .ToListAsync();

        /// <summary>
        /// Audited history for a file (newest first, capped).
        /// </summary>
        public Task<List<OpdBillAudit>> GetAuditAsync(string regNo, int take = 50)
            => _db.OpdBillAudits.AsNoTracking()
                .Where(a => a.RegNo == regNo)
                .OrderByDescending(a => a.CreatedAt)
                .Take(take)
                .ToListAsync();

        /// <summary>Rows per page of the List tab.</summary>
        public const int PageSize = 50;

        /// <summary>
        /// Searchable List tab. Only outpatient files (TYPE='O'); optional status filter.
        /// </summary>
        /// <param name="term">Registration number, patient code or company code fragment.</param>
        /// <param name="status">Optional status filter: 0=Open, 1=Generated, 2=Reversed, 3=Settled.</param>
        /// <param name="take">Row cap, only used by the non-paged overload.</param>
        public Task<List<FileRow>> SearchAsync(string term, int? status, int take = 200)
            => SearchPageAsync(term, status, 1, take).ContinueWith(t => t.Result.Rows);

        /// <summary>
        /// One page of the List tab. The total is counted on its own so that a page
        /// index past the end still reports the real total and can be clamped.
        /// </summary>
        public async Task<SearchResult> SearchPageAsync(string term, int? status, int pageIndex, int pageSize = 0)
        {
            if (pageSize <= 0) pageSize = PageSize;
            if (pageIndex < 1) pageIndex = 1;

            var q = BuildSearchQuery(term, status);

            // Count before the window. An out-of-range OFFSET returns no rows, so a
            // total carried on the rows would be unavailable exactly when it is needed.
            var total = await q.CountAsync();

            // Clamp before querying so a past-the-end page returns the last page
            // rather than an empty grid.
            var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
            if (pageIndex > totalPages) pageIndex = totalPages;

            var page = await q
                .OrderByDescending(r => r.RDate)
                .ThenByDescending(r => r.RegNo)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    r.RegNo,
                    r.PCode,
                    r.RDate,
                    r.TBillAmt,
                    Receipts = r.TRcvd ?? 0,
                    Payments = r.TPymt ?? 0,
                    Discount = r.CashDisc ?? 0,
                    BillGen = r.BillGen ?? false,
                    Settle = r.Settle ?? false,
                    BillNo = _db.OpdBillHeaders
                        .Where(h => h.HosCode == r.RegNo && h.Stat != "X")
                        .OrderByDescending(h => h.BillNo)
                        .Select(h => (long?)h.BillNo)
                        .FirstOrDefault()
                })
                .ToListAsync();

            // Reversed (STAT='X') registrations for the page, fetched in one query.
            // Auctioning them inside the row projection would need an async lambda,
            // which Select does not accept.
            var pageRegNos = page.Select(p => p.RegNo).ToList();
            var reversedSet = (await _db.OpdBillHeaders.AsNoTracking()
                    .Where(h => h.Stat == "X" && pageRegNos.Contains(h.HosCode))
                    .Select(h => h.HosCode)
                    .Distinct()
                    .ToListAsync())
                .ToHashSet();

            // Amount information must agree with the Update tab. REGISTER.TBILLAMT stays
            // NULL until a bill is generated, so on its own the list showed Billed = blank
            // and Due = 0.00 while the Update tab showed the indent total. The used amount
            // (signed sum of the file's OBIL_MST.NTOTAL rows, TYPE='R' subtracting) is
            // therefore looked up for the whole page in one grouped query.
            var regNos = page.Select(p => p.RegNo).ToList();
            var used = await _db.ObilMsts
                .Where(m => regNos.Contains(m.HosCode))
                .GroupBy(m => m.HosCode)
                .Select(g => new
                {
                    HosCode = g.Key,
                    Amount = g.Sum(m => m.Type == "R" ? -(m.NTotal ?? 0) : (m.NTotal ?? 0)),
                    Slips = g.Count()
                })
                .ToListAsync();
            var usedMap = used.ToDictionary(x => x.HosCode);

            var rows = page.Select(p =>
            {
                usedMap.TryGetValue(p.RegNo, out var u);
                var usedAmount = u?.Amount ?? 0m;
                var slipCount = u?.Slips ?? 0;
                var billed = p.TBillAmt ?? 0;
                // Same fallback rule as LoadFileAsync: a billed total with no slips uses it.
                var indentDetailMissing = billed != 0 && slipCount == 0;

                // Compute derived status: 0=Open, 1=Generated, 2=Reversed, 3=Settled
                int derivedStatus;
                if (p.Settle) derivedStatus = 3;
                else if (p.BillGen) derivedStatus = 1;
                else derivedStatus = 0;

                // Check for reversed bill (STAT='X')
                var hasReversed = reversedSet.Contains(p.RegNo);
                if (hasReversed) derivedStatus = 2;

                return new FileRow
                {
                    RegNo = p.RegNo,
                    PCode = p.PCode,
                    RDate = p.RDate,
                    BillGen = p.BillGen,
                    Settle = p.Settle,
                    BillNo = p.BillNo,
                    BillAmt = p.TBillAmt,
                    UsedAmt = usedAmount,
                    Due = (indentDetailMissing ? billed : usedAmount) - p.Receipts + p.Payments - p.Discount,
                    Status = derivedStatus
                };
            }).ToList();

            return new SearchResult
            {
                Rows = rows,
                TotalRows = total,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Base search query. Term filter plus the derived status filter, applied in the
        /// database so both the count and the page agree with each other:
        /// 0=Open (bill not generated), 1=Generated (BILLGEN=1, no reversed bill),
        /// 2=Reversed (an OPDBILL.Stat='X' row exists), 3=Settled (REGISTER.Settle=1).
        /// </summary>
        private IQueryable<RegisterLegacy> BuildSearchQuery(string term, int? status)
        {
            var q = _db.LegacyRegisters.AsNoTracking().Where(r => r.Type == "O");
            if (!string.IsNullOrWhiteSpace(term))
            {
                var t = term.Trim();
                q = q.Where(r => r.RegNo.Contains(t) || r.PCode.Contains(t)
                    || (r.Cid != null && r.Cid.Contains(t)));
            }

            if (status.HasValue)
            {
                var s = status.Value;
                if (s == 3)
                    q = q.Where(r => r.Settle == true);
                else if (s == 2)
                    q = q.Where(r => _db.OpdBillHeaders.Any(h => h.HosCode == r.RegNo && h.Stat == "X"));
                else if (s == 1)
                    q = q.Where(r => r.BillGen == true && r.Settle != true
                        && !_db.OpdBillHeaders.Any(h => h.HosCode == r.RegNo && h.Stat == "X"));
                else if (s == 0)
                    q = q.Where(r => r.BillGen != true && r.Settle != true
                        && !_db.OpdBillHeaders.Any(h => h.HosCode == r.RegNo && h.Stat == "X"));
            }

            return q;
        }

        /// <summary>One department total row.</summary>
        public class DeptTotal
        {
            public string Dept { get; set; }
            public int SlipCount { get; set; }
            public decimal Amount { get; set; }
        }

        /// <summary>One detail line of the detail grid.</summary>
        public class BillLine
        {
            public long Id { get; set; }
            public string Dept { get; set; }
            public string SlipNo { get; set; }
            public DateTime? SlipDate { get; set; }
            public string Code { get; set; }
            public string Description { get; set; }
            public decimal Qty { get; set; }
            public decimal Rate { get; set; }
            public decimal Amount { get; set; }
            public string Remarks { get; set; }
            public string ConCode { get; set; }
            public bool IsReturn { get; set; }
            public long? SlipBillNo { get; set; }
        }

        /// <summary>One row of the List tab.</summary>
        public class FileRow
        {
            public string RegNo { get; set; }
            public string PCode { get; set; }
            public DateTime? RDate { get; set; }
            public bool BillGen { get; set; }
            public bool Settle { get; set; }
            public long? BillNo { get; set; }
            public decimal? BillAmt { get; set; }
            /// <summary>Signed sum of the file's slip totals (OBIL_MST.NTOTAL; TYPE='R' subtracts).</summary>
            public decimal UsedAmt { get; set; }
            public decimal Due { get; set; }
            /// <summary>Derived status: 0=Open, 1=Generated, 2=Reversed, 3=Settled.</summary>
            public int Status { get; set; }
        }

        /// <summary>One page of List tab rows plus the paging metadata.</summary>
        public class SearchResult
        {
            public List<FileRow> Rows { get; set; } = new();
            public int TotalRows { get; set; }
            public int PageIndex { get; set; }
            public int PageSize { get; set; }
            public int TotalPages => TotalRows == 0 ? 1 : (int)Math.Ceiling(TotalRows / (double)PageSize);
            public int FirstRowOnPage => TotalRows == 0 ? 0 : (PageIndex - 1) * PageSize + 1;
            public int LastRowOnPage => Math.Min(PageIndex * PageSize, TotalRows);
        }
    }
}