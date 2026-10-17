using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Billing
{
    /// <summary>
    /// Recalculate Consultant Payable - mirrors the VFP Billsys forms
    /// <c>recalcon</c> (IPD) and <c>orecalcon</c> (OPD).
    /// <para>
    /// Legacy algorithm, per bill line where CONCODE is not empty and FLAG &lt;&gt; 'P':
    /// <list type="number">
    ///   <item>if the registration's owner (powner.OWNER) equals the line consultant -> CONPAY = NAMT (100%)</item>
    ///   <item>else if a commission row exists for (company, consultant, LEFT(code,5)) ->
    ///         CONPAY = NAMT * (100 - HMCPER) / 100</item>
    ///   <item>else CONPAY = 0</item>
    /// </list>
    /// Date scope: IPD uses the registration's discharge date, OPD uses the slip date.
    /// </para>
    /// </summary>
    public class RecalculateConsultantPayableModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public RecalculateConsultantPayableModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db; _session = session;
        }

        [BindProperty(SupportsGet = true)] public string Type { get; set; } = "inward";
        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; } = DateTime.Today.AddDays(-30);
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; } = DateTime.Today;
        [BindProperty(SupportsGet = true)] public long? CompanyId { get; set; }

        public List<Row> Rows { get; set; } = new();
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> CompanyList { get; set; } = new();
        public string Success { get; set; }
        public string ErrorMessage { get; set; }

        public bool IsInward => string.Equals(Type, "inward", StringComparison.OrdinalIgnoreCase);
        public double TotalPayable => Rows.Sum(r => r.Payable);
        public int TotalLines => Rows.Sum(r => r.LineCount);

        public class Row
        {
            public long? StaffId { get; set; }
            public string Consultant { get; set; } = "";
            public int BillCount { get; set; }
            public int LineCount { get; set; }
            public double Payable { get; set; }
        }

        public async Task OnGetAsync() => await LoadAsync();

        /// <summary>
        /// Runs the legacy recalculation and writes the result into BILLITEM.StaffFee.
        /// </summary>
        public async Task<IActionResult> OnPostRecalculateAsync()
        {
            var from = (FromDate ?? DateTime.Today.AddDays(-30)).Date;
            var toExclusive = (ToDate ?? DateTime.Today).Date.AddDays(1);
            var inward = IsInward;
            var company = CompanyId.GetValueOrDefault();

            var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            // Legacy date source: IPD = discharge date of the registration,
            // OPD = the slip date (falling back to the bill date when the
            // migrated SessionDate is the 1900-01-01 sentinel / NULL).
            var dateExpr = inward
                ? "e.[DateOfDischarge]"
                : "COALESCE(NULLIF(bi.[SessionDate], '1900-01-01'), b.[BillDate])";

            var sql = $@"
UPDATE bi
SET bi.[StaffFee] = x.ConPay
FROM [dbo].[BILLITEM] bi
INNER JOIN [dbo].[BILL] b       ON b.[Id] = bi.[BillId]
INNER JOIN [dbo].[PATIENTENCOUNTER] e ON e.[Id] = b.[PatientEncounterId]
LEFT JOIN [dbo].[INSTITUTION] inst ON inst.[Id] = b.[CreditCompanyId]
CROSS APPLY (
    SELECT
        CASE
            WHEN EXISTS (
                SELECT 1 FROM [dbo].[POWNER] po
                WHERE po.[EncounterId] = e.[Id] AND po.[Retired] = 0
                  AND po.[OwnerCode] = bi.[Concode]
            )
                THEN bi.[NetValue]
            ELSE ISNULL((
                SELECT TOP 1 (bi.[NetValue] * (100 - cc.[HmcPer])) / 100.0
                FROM [dbo].[CONCOMM] cc
                WHERE cc.[Retired] = 0
                  AND cc.[ConCode] = bi.[Concode]
                  AND cc.[SCode] = LEFT(ISNULL(bi.[ItemId], ''), 5)
                  AND cc.[CompanyCode] = ISNULL(inst.[Code], '')
            ), 0)
        END AS ConPay
) x
WHERE bi.[Retired] = 0
  AND b.[Retired] = 0
  AND ISNULL(bi.[Concode], '') <> ''
  AND ISNULL(bi.[LegacyFlag], '') <> 'P'
  AND {dateExpr} >= @from AND {dateExpr} < @to
  AND (b.[BilledBillId] IS NULL OR ISNULL(b.[BillClassType], 0) <> 4)
  AND (@company = 0 OR b.[CreditCompanyId] = @company)
  AND (@inward = 0 OR b.[BillTypeAtomic] IN (4,5,6,7))
  AND (@inward = 1 OR b.[BillTypeAtomic] IN (0,1,2,3));";

            try
            {
                _db.Database.SetCommandTimeout(3600);
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.CommandTimeout = 3600;
                cmd.Parameters.Add(new SqlParameter("@from", SqlDbType.DateTime) { Value = from });
                cmd.Parameters.Add(new SqlParameter("@to", SqlDbType.DateTime) { Value = toExclusive });
                cmd.Parameters.Add(new SqlParameter("@company", SqlDbType.BigInt) { Value = company });
                cmd.Parameters.Add(new SqlParameter("@inward", SqlDbType.Bit) { Value = inward });
                var affected = await cmd.ExecuteNonQueryAsync();
                TempData["RcpSuccess"] = $"Recalculated {affected:N0} bill line(s) for {from:yyyy-MM-dd} to {(toExclusive.AddDays(-1)):yyyy-MM-dd} ({(IsInward ? "IPD" : "OPD")}).";
            }
            catch (Exception ex)
            {
                TempData["RcpError"] = "Recalculation failed: " + ex.Message;
            }

            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostPostAsync()
        {
            await LoadAsync();
            var today = DateTime.Today;
            var created = 0;
            foreach (var r in Rows.Where(x => x.StaffId.HasValue && x.Payable > 0))
            {
                var exists = await _db.ConsultantSettlements.AnyAsync(s => !s.Retired
                    && s.ConsultantId == r.StaffId.Value && s.SettlementDate == today);
                if (exists) continue;

                _db.ConsultantSettlements.Add(new ConsultantSettlement
                {
                    ConsultantId = r.StaffId,
                    SettlementDate = today,
                    BillCount = r.BillCount,
                    AmountTotal = r.Payable,
                    AmountHandedOver = 0,
                    Note = $"Auto recalculated {FromDate:yyyy-MM-dd}..{ToDate:yyyy-MM-dd}",
                    CreaterId = _session.WebUserId,
                    CreatedAt = DateTime.Now
                });
                created++;
            }
            await _db.SaveChangesAsync();
            TempData["RcpSuccess"] = created > 0 ? $"{created} consultant settlement(s) posted." : "No new settlements to post (already posted today or no payable).";
            return RedirectToPage(new { Type, FromDate, ToDate, CompanyId });
        }

        private async Task LoadAsync()
        {
            Success = TempData["RcpSuccess"] as string;
            ErrorMessage = TempData["RcpError"] as string;

            CompanyList = await _db.Institutions.Where(i => !i.Retired && i.Code != null).OrderBy(i => i.Name)
                .Select(i => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = i.Id.ToString(), Text = i.Name }).ToListAsync();

            var from = (FromDate ?? DateTime.Today.AddDays(-30)).Date;
            var toExclusive = (ToDate ?? DateTime.Today).Date.AddDays(1);
            var inward = IsInward;

            _db.Database.SetCommandTimeout(1200);

            var dateExpr = inward
                ? "e.DateOfDischarge"
                : "COALESCE(NULLIF(bi.SessionDate, '1900-01-01'), b.BillDate)";

            var sql = $@"
SELECT TOP 500
    s.[Id]                              AS StaffId,
    ISNULL(p.[Name], s.[Code])          AS Consultant,
    COUNT(DISTINCT b.[Id])              AS BillCount,
    COUNT(*)                            AS LineCount,
    ISNULL(SUM(bi.[StaffFee]), 0)       AS Payable
FROM [dbo].[BILLITEM] bi
INNER JOIN [dbo].[BILL] b ON b.[Id] = bi.[BillId]
INNER JOIN [dbo].[PATIENTENCOUNTER] e ON e.[Id] = b.[PatientEncounterId]
LEFT JOIN [dbo].[STAFF] s ON s.[Code] = bi.[Concode] AND s.[DTYPE] = 'Consultant'
LEFT JOIN [dbo].[PERSON] p ON p.[Id] = s.[PersonId]
WHERE bi.[Retired] = 0
  AND b.[Retired] = 0
  AND ISNULL(bi.[Concode], '') <> ''
  AND ISNULL(bi.[LegacyFlag], '') <> 'P'
  AND {dateExpr} >= @from AND {dateExpr} < @to
  AND (b.[BilledBillId] IS NULL OR ISNULL(b.[BillClassType], 0) <> 4)
  AND (@company = 0 OR b.[CreditCompanyId] = @company)
  AND (@inward = 0 OR b.[BillTypeAtomic] IN (4,5,6,7))
  AND (@inward = 1 OR b.[BillTypeAtomic] IN (0,1,2,3))
GROUP BY s.[Id], p.[Name], s.[Code]
ORDER BY Payable DESC;";

            var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandTimeout = 1200;
            cmd.Parameters.Add(new SqlParameter("@from", SqlDbType.DateTime) { Value = from });
            cmd.Parameters.Add(new SqlParameter("@to", SqlDbType.DateTime) { Value = toExclusive });
            cmd.Parameters.Add(new SqlParameter("@company", SqlDbType.BigInt) { Value = CompanyId.GetValueOrDefault() });
            cmd.Parameters.Add(new SqlParameter("@inward", SqlDbType.Bit) { Value = inward });

            var list = new List<Row>();
            await using (var rdr = await cmd.ExecuteReaderAsync())
            {
                while (await rdr.ReadAsync())
                {
                    list.Add(new Row
                    {
                        StaffId = rdr.IsDBNull(0) ? (long?)null : rdr.GetInt64(0),
                        Consultant = rdr.IsDBNull(1) ? "(unmapped)" : rdr.GetString(1),
                        BillCount = rdr.IsDBNull(2) ? 0 : rdr.GetInt32(2),
                        LineCount = rdr.IsDBNull(3) ? 0 : rdr.GetInt32(3),
                        Payable = rdr.IsDBNull(4) ? 0 : rdr.GetDouble(4)
                    });
                }
            }
            Rows = list;
        }
    }
}