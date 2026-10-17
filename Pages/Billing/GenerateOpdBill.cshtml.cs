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
    /// Generate Bill (Outdoor) - a direct port of the VFP Billsys form
    /// <c>GENBILL.SCT</c> (frmgenbill).
    /// <para>
    /// VFP procedure reproduced exactly:
    /// <list type="number">
    ///   <item>Candidate list = registrations whose <c>obil_mst</c> slips are still
    ///         unbilled (<c>bill = 0</c>) and whose <c>slipdt</c> falls in the period,
    ///         with returns subtracted:
    ///         <c>sum(iif(type&lt;&gt;'R', ntotal, ntotal*-1))</c>. Filtered to
    ///         <c>register.type = "O"</c>.</item>
    ///   <item>On generate, for each ticked row: <c>billno = bcontrol.opdbill + 1</c>,
    ///         insert the <c>OPDBILL</c> header, stamp <c>obil_mst.bill</c> /
    ///         <c>bdate</c> on every unbilled slip in the period, then set
    ///         <c>register.billgen = .T.</c> and <c>register.tbillamt</c>.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class GenerateOpdBillModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public GenerateOpdBillModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db; _session = session;
        }

        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? BillDate { get; set; }
        [BindProperty(SupportsGet = true)] public string CompanyId { get; set; }

        /// <summary>Registrations ticked for generation (posted = 'X' in VFP).</summary>
        [BindProperty] public List<string> Selected { get; set; } = new();

        public List<Row> Rows { get; set; } = new();
        public string Success { get; set; }
        public string ErrorMessage { get; set; }

        public int NextBillNo { get; set; }
        public decimal TotalSelected { get; set; }
        public int SelectedCount => Selected?.Count(t => !string.IsNullOrWhiteSpace(t)) ?? 0;

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
                    // --- VFP: recompute the period total for this registration ---
                    var totalSql = @"
SELECT SUM(CASE WHEN TYPE <> 'R' THEN NTOTAL ELSE NTOTAL * -1 END)
FROM OBIL_MST
WHERE HOS_CODE = @regno AND SLIPDT >= @from AND SLIPDT <= @to
  AND (BILL IS NULL OR BILL = 0);";
                    decimal ntot = 0;
                    await using (var c1 = conn.CreateCommand())
                    {
                        c1.Transaction = tx;
                        c1.CommandText = totalSql;
                        c1.Parameters.Add(new SqlParameter("@regno", SqlDbType.VarChar, 16) { Value = regno });
                        c1.Parameters.Add(new SqlParameter("@from", SqlDbType.Date) { Value = from });
                        c1.Parameters.Add(new SqlParameter("@to", SqlDbType.Date) { Value = to });
                        var v = await c1.ExecuteScalarAsync();
                        if (v != null && v != DBNull.Value) ntot = Convert.ToDecimal(v, CultureInfo.InvariantCulture);
                    }

                    // --- VFP: Mbillno = bcontrol.opdbill + 1 ---
                    long mbillno;
                    await using (var c2 = conn.CreateCommand())
                    {
                        c2.Transaction = tx;
                        c2.CommandText = "SELECT OPDBILL FROM BCONTROL WITH (UPDLOCK, ROWLOCK) WHERE Id = 1;";
                        var cur = Convert.ToInt64(await c2.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
                        mbillno = cur + 1;
                        await using (var c2b = conn.CreateCommand())
                        {
                            c2b.Transaction = tx;
                            c2b.CommandText = "UPDATE BCONTROL SET OPDBILL = @n WHERE Id = 1;";
                            c2b.Parameters.Add(new SqlParameter("@n", SqlDbType.BigInt) { Value = mbillno });
                            await c2b.ExecuteNonQueryAsync();
                        }
                    }

                    // --- VFP: registration header snapshot ---
                    string pcode = null, cid = null;
                    await using (var c3 = conn.CreateCommand())
                    {
                        c3.Transaction = tx;
                        c3.CommandText = "SELECT TOP 1 PCODE, CID FROM REGISTER WHERE TYPE='O' AND REGNO=@r;";
                        c3.Parameters.Add(new SqlParameter("@r", SqlDbType.VarChar, 16) { Value = regno });
                        await using var rdr = await c3.ExecuteReaderAsync();
                        if (await rdr.ReadAsync())
                        {
                            pcode = rdr.IsDBNull(0) ? null : rdr.GetString(0);
                            cid = rdr.IsDBNull(1) ? null : rdr.GetString(1);
                        }
                    }

                    // --- VFP: append blank into OPDBILL ---
                    await using (var c4 = conn.CreateCommand())
                    {
                        c4.Transaction = tx;
                        c4.CommandText = @"
INSERT INTO OPDBILL (BILLNO, BDATE, SDATE, EDATE, HOS_CODE, PCODE, CID, NTOT, RDATE, STAT, USERID, UDATE, UTIME)
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
                    await using (var c5 = conn.CreateCommand())
                    {
                        c5.Transaction = tx;
                        c5.CommandText = @"
UPDATE OBIL_MST
SET BILL = @billno, BDATE = @bdate
WHERE HOS_CODE = @regno
  AND SLIPDT >= @from AND SLIPDT <= @to
  AND (BILL IS NULL OR BILL = 0);";
                        c5.Parameters.Add(new SqlParameter("@billno", SqlDbType.BigInt) { Value = mbillno });
                        c5.Parameters.Add(new SqlParameter("@bdate", SqlDbType.Date) { Value = bdate });
                        c5.Parameters.Add(new SqlParameter("@regno", SqlDbType.VarChar, 16) { Value = regno });
                        c5.Parameters.Add(new SqlParameter("@from", SqlDbType.Date) { Value = from });
                        c5.Parameters.Add(new SqlParameter("@to", SqlDbType.Date) { Value = to });
                        await c5.ExecuteNonQueryAsync();
                    }

                    // --- VFP: register.billgen = .T., register.tbillamt = ntot ---
                    await using (var c6 = conn.CreateCommand())
                    {
                        c6.Transaction = tx;
                        c6.CommandText = "UPDATE REGISTER SET BILLGEN = 1, TBILLAMT = @ntot WHERE TYPE='O' AND REGNO=@r;";
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
            Success = TempData["GopdSuccess"] as string;
            ErrorMessage ??= TempData["GopdError"] as string;

            var from = (FromDate ?? DateTime.Today.AddDays(-30)).Date;
            var to = (ToDate ?? DateTime.Today).Date;

            // VFP candidate query (register.type = "O", slip date period, bill = 0)
            var sql = @"
SELECT r.REGNO, r.PCODE,
       ISNULL(p.NAME,'')                AS PatientName,
       ISNULL(r.CID,'')                 AS CompanyCode,
       ISNULL(c.NAME,'')                AS CompanyName,
       ISNULL(SUM(CASE WHEN o.TYPE <> 'R' THEN o.NTOTAL ELSE o.NTOTAL * -1 END), 0) AS Ntot
FROM REGISTER r
INNER JOIN OBIL_MST o ON r.REGNO = o.HOS_CODE
LEFT OUTER JOIN PATIENT_LEGACY p ON r.PCODE = p.CODE
LEFT OUTER JOIN COMPANY c ON r.CID = c.ID
WHERE r.TYPE = 'O'
  AND o.SLIPDT >= @from AND o.SLIPDT <= @to
  AND (o.BILL IS NULL OR o.BILL = 0)
  AND (@cid = '' OR r.CID = @cid)
GROUP BY r.REGNO, r.PCODE, p.NAME, r.CID, c.NAME
ORDER BY r.REGNO;";

            var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandTimeout = 600;
            cmd.Parameters.Add(new SqlParameter("@from", SqlDbType.Date) { Value = from });
            cmd.Parameters.Add(new SqlParameter("@to", SqlDbType.Date) { Value = to });
            cmd.Parameters.Add(new SqlParameter("@cid", SqlDbType.VarChar, 16) { Value = CompanyId ?? "" });

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
            TotalSelected = list.Where(r => r.IsSelected).Sum(r => r.NetTotal);

            await using var nb = conn.CreateCommand();
            nb.CommandText = "SELECT OPDBILL FROM BCONTROL WHERE Id=1;";
            NextBillNo = Convert.ToInt32(await nb.ExecuteScalarAsync(), CultureInfo.InvariantCulture) + 1;
        }
    }
}