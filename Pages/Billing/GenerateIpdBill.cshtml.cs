using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Services;

namespace HmisWeb.Pages.Billing
{
    /// <summary>
    /// Generate Bill (Indoor) - a direct port of the VFP Billsys form
    /// <c>GENBILLI.SCT</c> (frmgenbilli) - indoor.
    /// <para>
    /// VFP procedure reproduced exactly:
    /// <list type="number">
    ///   <item>Candidate list = registrations whose <c>IBIL_MST</c> slips are still
    ///         unbilled (<c>bill = 0</c>) and whose <c>slipdt</c> falls in the period,
    ///         with returns subtracted:
    ///         <c>sum(iif(type&lt;&gt;'R', ntotal, ntotal*-1))</c>. Filtered to
    ///         <c>register.type = "I"</c>, filtered on <c>register.ddate</c> (discharge) rather than slip date.</item>
    ///   <item>On generate, for each ticked row: <c>billno = bcontrol.IPDBILL + 1</c>,
    ///         insert the <c>IPDBILL</c> header, stamp <c>ibil_mst.bill</c> /
    ///         <c>bdate</c> on every unbilled slip in the period, then set
    ///         <c>register.billgen = .T.</c> and <c>register.tbillamt</c>.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class GenerateIpdBillModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public GenerateIpdBillModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db; _session = session;
        }

        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? BillDate { get; set; }
        [BindProperty(SupportsGet = true)] public string CompanyId { get; set; }
        [BindProperty(SupportsGet = true)] public int PageIndex { get; set; } = 1;

        /// <summary>Rows per page of the candidate grid.</summary>
        public const int PageSize = 50;

        /// <summary>Registrations ticked for generation (posted = 'X' in VFP).</summary>
        [BindProperty] public List<string> Selected { get; set; } = new();

        public List<Row> Rows { get; set; } = new();
        public string Success { get; set; }
        public string ErrorMessage { get; set; }

        public int NextBillNo { get; set; }
        public decimal TotalSelected { get; set; }
        public int SelectedCount => Selected?.Count(t => !string.IsNullOrWhiteSpace(t)) ?? 0;

        /// <summary>Total candidate rows matching the filter (all pages).</summary>
        public int TotalRows { get; set; }
        public int TotalPages => TotalRows == 0 ? 1 : (int)Math.Ceiling(TotalRows / (double)PageSize);
        public int FirstRowOnPage => TotalRows == 0 ? 0 : (PageIndex - 1) * PageSize + 1;
        public int LastRowOnPage => Math.Min(PageIndex * PageSize, TotalRows);

        public class Row
        {
            public string RegNo { get; set; }
            public string PCode { get; set; }
            public string PatientName { get; set; }
            public string CompanyCode { get; set; }
            public string CompanyName { get; set; }
            public decimal NetTotal { get; set; }
            public bool IsSelected { get; set; }
        }

        public async Task OnGetAsync()
        {
            FromDate ??= DateTime.Today.AddDays(-30);
            ToDate ??= DateTime.Today;
            BillDate ??= DateTime.Today;
            if (PageIndex < 1) PageIndex = 1;
            await LoadAsync();
        }

        /// <summary>VFP cmdok.Click - bill generation.</summary>
        public async Task<IActionResult> OnPostGenerateAsync()
        {
            var from = (FromDate ?? DateTime.Today.AddDays(-30)).Date;
            var to = (ToDate ?? DateTime.Today).Date;
            var bdate = (BillDate ?? DateTime.Today).Date;
            var picked = (Selected ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();

            if (picked.Count == 0)
            {
                ErrorMessage = "Please select at least one registration.";
                await LoadAsync();
                return Page();
            }

            var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            await using var tx = await conn.BeginTransactionAsync();
            var generated = 0;
            var user = _session.WebUserId?.ToString();

            try
            {
                foreach (var regno in picked)
                {
                    // --- VFP GENBILLI: indoor stamps ALL unbilled slips (no date filter) ---
                    var totalSql = @"
SELECT SUM(CASE WHEN TYPE <> 'R' THEN NTOTAL ELSE NTOTAL * -1 END)
FROM IBIL_MST
WHERE HOS_CODE = @regno
  AND (BILL IS NULL OR BILL = 0);";
                    decimal ntot = 0;
                    await using (var c1 = conn.CreateCommand())
                    {
                        c1.Transaction = tx;
                        c1.CommandText = totalSql;
                        c1.Parameters.Add(new SqlParameter("@regno", SqlDbType.VarChar, 16) { Value = regno });
                        var v = await c1.ExecuteScalarAsync();
                        if (v != null && v != DBNull.Value) ntot = Convert.ToDecimal(v, CultureInfo.InvariantCulture);
                    }

                    // --- VFP: Mbillno = bcontrol.IPDBILL + 1 ---
                    long mbillno;
                    await using (var c2 = conn.CreateCommand())
                    {
                        c2.Transaction = tx;
                        c2.CommandText = "SELECT IPDBILL FROM BCONTROL WITH (UPDLOCK, ROWLOCK) WHERE Id = 1;";
                        var cur = Convert.ToInt64(await c2.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
                        mbillno = cur + 1;
                        await using (var c2b = conn.CreateCommand())
                        {
                            c2b.Transaction = tx;
                            c2b.CommandText = "UPDATE BCONTROL SET IPDBILL = @n WHERE Id = 1;";
                            c2b.Parameters.Add(new SqlParameter("@n", SqlDbType.BigInt) { Value = mbillno });
                            await c2b.ExecuteNonQueryAsync();
                        }
                    }

                    // --- VFP: registration header snapshot ---
                    string pcode = null, cid = null;
                    await using (var c3 = conn.CreateCommand())
                    {
                        c3.Transaction = tx;
                        c3.CommandText = "SELECT TOP 1 PCODE, CID FROM REGISTER WHERE TYPE='I' AND REGNO=@r;";
                        c3.Parameters.Add(new SqlParameter("@r", SqlDbType.VarChar, 16) { Value = regno });
                        await using var rdr = await c3.ExecuteReaderAsync();
                        if (await rdr.ReadAsync())
                        {
                            pcode = rdr.IsDBNull(0) ? null : rdr.GetString(0);
                            cid = rdr.IsDBNull(1) ? null : rdr.GetString(1);
                        }
                    }

                    // --- VFP: append blank into IPDBILL ---
                    await using (var c4 = conn.CreateCommand())
                    {
                        c4.Transaction = tx;
                        c4.CommandText = @"
INSERT INTO IPDBILL (BILLNO, BDATE, SDATE, EDATE, HOS_CODE, PCODE, CID, NTOT, RDATE, STAT, USERID, UDATE, UTIME)
VALUES (@billno, @bdate, @sdate, @edate, @hos, @pcode, @cid, @ntot, GETDATE(), NULL, @uid, GETDATE(), NULL);";
                        c4.Parameters.Add(new SqlParameter("@billno", SqlDbType.BigInt) { Value = mbillno });
                        c4.Parameters.Add(new SqlParameter("@bdate", SqlDbType.Date) { Value = bdate });
                        c4.Parameters.Add(new SqlParameter("@sdate", SqlDbType.Date) { Value = from });
                        c4.Parameters.Add(new SqlParameter("@edate", SqlDbType.Date) { Value = to });
                        c4.Parameters.Add(new SqlParameter("@hos", SqlDbType.VarChar, 16) { Value = regno });
                        c4.Parameters.Add(new SqlParameter("@pcode", SqlDbType.VarChar, 16) { Value = (object)pcode ?? DBNull.Value });
                        c4.Parameters.Add(new SqlParameter("@cid", SqlDbType.VarChar, 8) { Value = (object)cid ?? DBNull.Value });
                        c4.Parameters.Add(new SqlParameter("@ntot", SqlDbType.Decimal) { Value = ntot });
                        c4.Parameters.Add(new SqlParameter("@uid", SqlDbType.VarChar, 16) { Value = (object)user ?? DBNull.Value });
                        await c4.ExecuteNonQueryAsync();
                    }

                    // --- VFP: stamp bill + bdate on the unbilled slips in period ---
                    // --- VFP GENBILLI: "if Bill = 0" only, no slip-date range ---
                        await using (var c5 = conn.CreateCommand())
                        {
                            c5.Transaction = tx;
                            c5.CommandText = @"
UPDATE IBIL_MST
SET BILL = @billno, BDATE = @bdate
WHERE HOS_CODE = @regno
  AND (BILL IS NULL OR BILL = 0);";
                        c5.Parameters.Add(new SqlParameter("@billno", SqlDbType.BigInt) { Value = mbillno });
                        c5.Parameters.Add(new SqlParameter("@bdate", SqlDbType.Date) { Value = bdate });
                        c5.Parameters.Add(new SqlParameter("@regno", SqlDbType.VarChar, 16) { Value = regno });
                        await c5.ExecuteNonQueryAsync();
                    }

                    // --- VFP: register.billgen = .T., register.tbillamt = ntot ---
                    await using (var c6 = conn.CreateCommand())
                    {
                        c6.Transaction = tx;
                        c6.CommandText = "UPDATE REGISTER SET BILLGEN = 1, TBILLAMT = @ntot WHERE TYPE='I' AND REGNO=@r;";
                        c6.Parameters.Add(new SqlParameter("@ntot", SqlDbType.Decimal) { Value = ntot });
                        c6.Parameters.Add(new SqlParameter("@r", SqlDbType.VarChar, 16) { Value = regno });
                        await c6.ExecuteNonQueryAsync();
                    }

                    generated++;
                }

                await tx.CommitAsync();
                Success = $"Bills successfully generated - {generated} registration(s).";
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                ErrorMessage = "Bill generation failed: " + ex.Message;
            }

            await LoadAsync();
            return Page();
        }

        private async Task LoadAsync()
        {
            Success = TempData["GipdSuccess"] as string;
            ErrorMessage ??= TempData["GipdError"] as string;

            var from = (FromDate ?? DateTime.Today.AddDays(-30)).Date;
            var to = (ToDate ?? DateTime.Today).Date;

            var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            // Re-run the query until the page window is valid. A loop, not recursion:
            // recursing here re-entered the method while the previous DataReader was
            // still open on the shared connection, which silently yielded 0 rows.
            for (var attempt = 0; attempt < 3; attempt++)
            {
                var loaded = await LoadPageAsync(conn, from, to);
                if (loaded) break;
            }

            // TotalSelected must cover ticks on OTHER pages too, so recompute from IBIL_MST
            // for every selected regno, not just the ones currently rendered.
            TotalSelected = 0;
            var sel = (Selected ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            if (sel.Count > 0)
            {
                var inList = string.Join(",", sel.Select((_, i) => "@p" + i));
                await using var ts = conn.CreateCommand();
                ts.CommandText = "SELECT HOS_CODE, ISNULL(SUM(CASE WHEN TYPE <> 'R' THEN NTOTAL ELSE NTOTAL * -1 END), 0) FROM IBIL_MST WHERE HOS_CODE IN (" + inList + ") GROUP BY HOS_CODE;";
                for (var i = 0; i < sel.Count; i++)
                    ts.Parameters.Add(new SqlParameter("@p" + i, SqlDbType.VarChar, 16) { Value = sel[i] });
                await using var tr = await ts.ExecuteReaderAsync();
                while (await tr.ReadAsync())
                    if (!tr.IsDBNull(1))
                        TotalSelected += Convert.ToDecimal(tr.GetValue(1), CultureInfo.InvariantCulture);
            }

            await using var nb = conn.CreateCommand();
            nb.CommandText = "SELECT IPDBILL FROM BCONTROL WHERE Id=1;";
            NextBillNo = Convert.ToInt32(await nb.ExecuteScalarAsync(), CultureInfo.InvariantCulture) + 1;
        }

        /// <summary>
        /// Runs the candidate query for the current page window.
        /// Returns false when the requested <see cref="PageIndex"/> was past the end
        /// and has been clamped, so the caller should run the query once more.
        /// </summary>
        private async Task<bool> LoadPageAsync(System.Data.Common.DbConnection conn, DateTime from, DateTime to)
        {
            // The total is a scalar COUNT of DISTINCT registrations, fetched on its
            // own. It cannot ride along with the windowed SELECT: an out-of-range
            // OFFSET returns zero rows, so a column-carried total would be
            // unavailable exactly when the clamp needs it.
            await using (var countCmd = conn.CreateCommand())
            {
                countCmd.CommandText = @"
SELECT COUNT(*)
FROM (
    SELECT r.REGNO
    FROM REGISTER r
    INNER JOIN IBIL_MST o ON r.REGNO = o.HOS_CODE
    WHERE r.TYPE = 'I'
      AND r.DDATE >= @from AND r.DDATE <= @to
      AND (o.BILL IS NULL OR o.BILL = 0)
      AND (@cid = '' OR r.CID = @cid)
    GROUP BY r.REGNO
) g;";
                countCmd.CommandTimeout = 600;
                countCmd.Parameters.Add(new SqlParameter("@from", SqlDbType.Date) { Value = from });
                countCmd.Parameters.Add(new SqlParameter("@to", SqlDbType.Date) { Value = to });
                countCmd.Parameters.Add(new SqlParameter("@cid", SqlDbType.VarChar, 16) { Value = CompanyId ?? "" });
                var totalObj = await countCmd.ExecuteScalarAsync();
                TotalRows = totalObj == null || totalObj == DBNull.Value ? 0 : Convert.ToInt32(totalObj, CultureInfo.InvariantCulture);
            }

            // Paging window over the same candidate set.
            var sql = @"
WITH cand AS (
    SELECT r.REGNO, r.PCODE,
           ISNULL(p.NAME,'')                AS PatientName,
           ISNULL(r.CID,'')                 AS CompanyCode,
           ISNULL(c.NAME,'')                AS CompanyName,
           ISNULL(SUM(CASE WHEN o.TYPE <> 'R' THEN o.NTOTAL ELSE o.NTOTAL * -1 END), 0) AS Ntot
    FROM REGISTER r
    INNER JOIN IBIL_MST o ON r.REGNO = o.HOS_CODE
    LEFT OUTER JOIN Patient p ON r.PCODE = p.CODE
    LEFT OUTER JOIN COMPANY c ON r.CID = c.ID
    WHERE r.TYPE = 'I'
      AND r.DDATE >= @from AND r.DDATE <= @to
      AND (o.BILL IS NULL OR o.BILL = 0)
      AND (@cid = '' OR r.CID = @cid)
    GROUP BY r.REGNO, r.PCODE, p.NAME, r.CID, c.NAME
)
SELECT REGNO, PCODE, PatientName, CompanyCode, CompanyName, Ntot
FROM cand
ORDER BY REGNO
OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;";

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandTimeout = 600;
            cmd.Parameters.Add(new SqlParameter("@from", SqlDbType.Date) { Value = from });
            cmd.Parameters.Add(new SqlParameter("@to", SqlDbType.Date) { Value = to });
            cmd.Parameters.Add(new SqlParameter("@cid", SqlDbType.VarChar, 16) { Value = CompanyId ?? "" });
            cmd.Parameters.Add(new SqlParameter("@offset", SqlDbType.Int) { Value = (PageIndex - 1) * PageSize });
            cmd.Parameters.Add(new SqlParameter("@pageSize", SqlDbType.Int) { Value = PageSize });

            var list = new List<Row>();
            await using (var rdr = await cmd.ExecuteReaderAsync())
            {
                while (await rdr.ReadAsync())
                {
                    var regno = rdr.IsDBNull(0) ? "" : rdr.GetString(0);
                    list.Add(new Row
                    {
                        RegNo = regno,
                        PCode = rdr.IsDBNull(1) ? "" : rdr.GetString(1),
                        PatientName = rdr.IsDBNull(2) ? "" : rdr.GetString(2),
                        CompanyCode = rdr.IsDBNull(3) ? "" : rdr.GetString(3),
                        CompanyName = rdr.IsDBNull(4) ? "" : rdr.GetString(4),
                        NetTotal = rdr.IsDBNull(5) ? 0 : Convert.ToDecimal(rdr.GetValue(5), CultureInfo.InvariantCulture),
                        IsSelected = Selected != null && Selected.Contains(regno)
                    });
                }
            }
            Rows = list;

            // Clamp the page when the filter shrank the result set (e.g. after generate).
            // Return false so LoadAsync re-runs the query at the clamped index.
            if (TotalRows > 0 && PageIndex > TotalPages)
            {
                PageIndex = TotalPages;
                return false;
            }

            return true;
        }
    }
}