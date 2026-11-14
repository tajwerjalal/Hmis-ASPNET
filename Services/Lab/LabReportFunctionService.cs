using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace HmisWeb.Services.Lab
{
    /*
     * ============================================================================
     * File........: Services/Lab/LabReportFunctionService.cs
     * Purpose.....: C# ports of the legacy VFP report helper functions
     *               medicine(), period(), F_REC() and L_REC().
     * Inputs......: LABNO/TID/TESTNO, REPORT.TYPE + REPORT.HRNO and the current
     *               REPORT.ReportID.
     * Outputs.....: Fixed-width culture medicine text, the "From ... To ..."
     *               period string and first/last-row flags.
     * Business
     *   rules.....: 1. medicine() joins CUL_TR to MADIMS (or PHITEM when the
     *                  legacy master is absent) and formats each row exactly
     *                  like VFP:
     *                    PADR(id + ' ' + name, 28, '.') +
     *                    PADC(s1, 15) + PADC(s2, 25) + PADC(s3, 20)
     *               2. period() matches TYPE + ALLTRIM(STR(HRNO)) and returns
     *                  the min/max REPORT.DATE as dd/MM/yyyy.
     *               3. F_REC/L_REC treat ReportID as the report's record order.
     *                  The caller passes the current REPORT.ReportID.
     *               4. The equivalent SQL functions are created by
     *                  workspace/scripts/AddLabsysLabMasterTables.sql for
     *                  SQL-based reports.
     * Developer...: Tajwer Jalal
     * Date/Time...: 2026-11-03 13:55
     * ============================================================================
     */
    public class LabReportFunctionService
    {
        private readonly LabSchemaService _schema;

        public LabReportFunctionService(LabSchemaService schema)
        {
            _schema = schema;
        }

        /// <summary>
        /// Port of the VFP medicine(labno, tid, testno) function. Returns the
        /// fixed-width sensitivity string used by the legacy culture report.
        /// </summary>
        public async Task<string> GetMedicineTextAsync(string labNo, string tid, string testNo)
        {
            labNo = (labNo ?? "").Trim();
            tid = (tid ?? "").Trim();
            testNo = (testNo ?? "").Trim();
            if (labNo.Length == 0 || tid.Length == 0) return "";

            var schema = await _schema.GetAsync();
            var join = schema.HasMadims
                ? "LEFT JOIN dbo.MADIMS m ON m.ID = c.ID"
                : "LEFT JOIN dbo.PHITEM m ON m.CODE = c.ID";

            await using var conn = _schema.OpenConnection();
            await conn.OpenAsync();

            var rows = new List<(string Id, string Name, string S1, string S2, string S3)>();
            await ReadMedicineRowsAsync(conn, join, labNo, tid, testNo, rows);

            // Legacy cul_tr.dbf had no TESTNO. Fall back to blank-testno rows.
            if (rows.Count == 0 && testNo.Length > 0)
                await ReadMedicineRowsAsync(conn, join, labNo, tid, "", rows);

            if (rows.Count == 0) return "";

            var sb = new StringBuilder(rows.Count * 88);
            foreach (var row in rows)
            {
                var idName = (row.Id + " " + row.Name).Trim();
                sb.Append(PadRight(idName, 28, '.'));
                sb.Append(PadCenter(row.S1, 15));
                sb.Append(PadCenter(row.S2, 25));
                sb.Append(PadCenter(row.S3, 20));
            }
            return sb.ToString();
        }

        private static async Task ReadMedicineRowsAsync(SqlConnection conn, string join,
            string labNo, string tid, string testNo,
            List<(string Id, string Name, string S1, string S2, string S3)> rows)
        {
            var sql = $@"
SELECT c.ID, ISNULL(m.NAME, c.ID), ISNULL(c.S1,''), ISNULL(c.S2,''), ISNULL(c.S3,'')
FROM dbo.CUL_TR c
{join}
WHERE c.LABNO = @labno AND c.TID = @tid AND ISNULL(c.TESTNO,'') = @testno
ORDER BY c.ID, ISNULL(c.S1,'')";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@labno", labNo);
            cmd.Parameters.AddWithValue("@tid", tid);
            cmd.Parameters.AddWithValue("@testno", testNo);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                rows.Add((
                    r.IsDBNull(0) ? "" : r.GetValue(0).ToString().Trim(),
                    r.IsDBNull(1) ? "" : r.GetValue(1).ToString().Trim(),
                    r.IsDBNull(2) ? "" : r.GetValue(2).ToString().Trim(),
                    r.IsDBNull(3) ? "" : r.GetValue(3).ToString().Trim(),
                    r.IsDBNull(4) ? "" : r.GetValue(4).ToString().Trim()));
            }
        }

        /// <summary>
        /// Port of the VFP period(type + allt(str(hrno))) function.
        /// </summary>
        public async Task<string> GetPeriodAsync(string typeHrno)
        {
            typeHrno = (typeHrno ?? "").Trim();
            if (typeHrno.Length == 0) return "";

            await using var conn = _schema.OpenConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(@"
SELECT MIN([DATE]), MAX([DATE])
FROM dbo.REPORT
WHERE ISNULL([TYPE],'') + CONVERT(VARCHAR(20), CONVERT(INT, ISNULL([HRNO],0))) = @tp", conn);
            cmd.Parameters.AddWithValue("@tp", typeHrno);
            await using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync() || r.IsDBNull(0) || r.IsDBNull(1)) return "";

            var min = Convert.ToDateTime(r.GetValue(0));
            var max = Convert.ToDateTime(r.GetValue(1));
            return "From " + min.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                 + " To " + max.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        }

        /// <summary>Port of VFP F_REC for a report ordered by REPORT.ReportID.</summary>
        public async Task<bool> IsFirstRecordAsync(string labNo, int reportId)
        {
            await using var conn = _schema.OpenConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(@"
SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.REPORT
                         WHERE LABNO = @labno AND ReportID < @reportId)
            THEN 0 ELSE 1 END", conn);
            cmd.Parameters.AddWithValue("@labno", (labNo ?? "").Trim());
            cmd.Parameters.AddWithValue("@reportId", reportId);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 1) == 1;
        }

        /// <summary>Port of VFP L_REC for a report ordered by REPORT.ReportID.</summary>
        public async Task<bool> IsLastRecordAsync(string labNo, int reportId)
        {
            await using var conn = _schema.OpenConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(@"
SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.REPORT
                         WHERE LABNO = @labno AND ReportID > @reportId)
            THEN 0 ELSE 1 END", conn);
            cmd.Parameters.AddWithValue("@labno", (labNo ?? "").Trim());
            cmd.Parameters.AddWithValue("@reportId", reportId);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 1) == 1;
        }

        private static string PadRight(string text, int width, char pad)
        {
            text = text ?? "";
            if (text.Length >= width) return text.Substring(0, width);
            return text + new string(pad, width - text.Length);
        }

        private static string PadCenter(string text, int width)
        {
            text = text ?? "";
            if (text.Length >= width) return text.Substring(0, width);
            var total = width - text.Length;
            var left = total / 2;
            return new string(' ', left) + text + new string(' ', total - left);
        }
    }
}
