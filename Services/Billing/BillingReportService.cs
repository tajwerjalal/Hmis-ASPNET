using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using HmisWeb.Data;
using HmisWeb.Services;

#nullable disable

namespace HmisWeb.Services.Billing
{
    /// <summary>
    /// LEGACY IPD REPORT MIGRATION - generic list/summary report engine.
    ///
    /// Covers the remaining menu items under Reports > IPD Patient that are *not* per-patient
    /// bills (those are BS021001 / BS021040, handled by <see cref="IpdBillReportService"/>):
    ///
    ///   bs021010  Bills List                    -> IPD-BLSS / IPD-BLSD
    ///   bs021020  Consolidated Bills Report     -> IPD-CONS / IPD-COND
    ///   bs021030  Periodic Bills Summary        -> IPTSUMC / IPTSUM  / IPTSUME
    ///   bs021050  IPD Audit Report              -> IAUDITS / IAUDITD / IDEPADT
    ///   bs021060  Covering Letter               -> IPDCOVLT
    ///   bs0220    Consultant Commission List    -> CONCOMMR
    ///   cdischgrpt Company Discharges Report   -> CDISCHGRPT
    ///   ibillstat Billing Status Report        -> IBILLSTAT
    ///   billstat  Bill Generation Status       -> BILLSTAT
    ///
    /// Business rules carried over from the VFP cursors
    ///   * Returns (IBIL_MST.TYPE='R') are negated everywhere - including in the summary
    ///     reports, which the original bs021020/bs021030 forgot (review finding C3).
    ///   * Lines flagged FLAG='P' are excluded everywhere; app-side RETIRED lines too.
    ///   * Employee / consultant / company joins are LEFT joins.  The originals used INNER
    ///     joins on a concatenated (cid+code) expression, which silently dropped every
    ///     patient that has no employee code (review findings B1/B5/B34).
    ///   * IPDBILL inclusion is `empty(stat)`, matching the bill reports.
    ///
    /// Presentation follows the same premium layout as the bill reports: navy headings, teal
    /// accent, hairline rules, repeating column headings, "Page X of Y", A4 portrait.
    ///
    /// Side effects: none - read only.
    ///
    /// Developer: Tajwer Jalal | Date: 17-Oct-2026 | Time: 15:10
    /// </summary>
    public class BillingReportService
    {
        private readonly ApplicationDbContext _db;
        public BillingReportService(ApplicationDbContext db) { _db = db; }

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // ---- palette (kept identical to the bill reports) -------------------------
        private static readonly XColor Navy = XColor.FromArgb(0x1F, 0x38, 0x64);
        private static readonly XColor Teal = XColor.FromArgb(0x0F, 0x76, 0x6E);
        private static readonly XColor Body = XColor.FromArgb(0x1A, 0x1F, 0x26);
        private static readonly XColor Muted = XColor.FromArgb(0x5B, 0x66, 0x73);
        private static readonly XColor Grid = XColor.FromArgb(0xC8, 0xD2, 0xDC);
        private static readonly XColor HeadBg = XColor.FromArgb(0xE8, 0xEE, 0xF4);
        private static readonly XColor CatBg = XColor.FromArgb(0xDF, 0xEA, 0xEE);
        private static readonly XColor AltBg = XColor.FromArgb(0xF9, 0xFB, 0xFC);

        private static readonly XBrush NavyB = new XSolidBrush(Navy);
        private static readonly XBrush TealB = new XSolidBrush(Teal);
        private static readonly XBrush BodyB = new XSolidBrush(Body);
        private static readonly XBrush MutedB = new XSolidBrush(Muted);
        private static readonly XBrush HeadB = new XSolidBrush(HeadBg);
        private static readonly XBrush CatB = new XSolidBrush(CatBg);
        private static readonly XBrush AltB = new XSolidBrush(AltBg);
        private static readonly XPen HairPen = new XPen(Grid, 0.5);
        private static readonly XPen TealPen = new XPen(Teal, 1.1);
        private static readonly XPen NavyPen = new XPen(Navy, 0.8);

        private const double PageW = 595.28;
        private const double PageH = 841.89;
        private const double M = 34;
        private const double TopY = 30;
        private const double BottomReserve = 58;
        private const double ContentW = PageW - (M * 2);
        private const double RowH = 9.6;
        private const double TableHeadH = 18;
        private const double CatRowH = 16.5;

        /// <summary>
        /// Safety cap on rendered rows.  The legacy VFP reports had no cap, so a wide
        /// detail selection over a long period produced thousands of printed pages.
        /// </summary>
        private const int MaxRows = 12000;

        private static XFont F(double size, bool bold = false) =>
            new XFont("Arial", size, bold ? XFontStyle.Bold : XFontStyle.Regular);

        // ================================================================ model

        /// <summary>Criteria shared by the list reports (mirrors the VFP criteria controls).</summary>
        public class Criteria
        {
            public DateTime? From { get; set; }
            public DateTime? To { get; set; }
            public string Company { get; set; }
            public string Dept { get; set; }
            public string RegNo { get; set; }
            public string BillNo { get; set; }
            /// <summary>1 = first option, 2 = second, 3 = third; 0 = not offered.</summary>
            public int Mode { get; set; } = 1;
            public int StartPage { get; set; } = 1;
            public int EndPage { get; set; } = 9999;
        }

        public class ReportContext
        {
            public string HospitalName { get; set; } = "HMIS Hospital";
            public string HospitalNo { get; set; } = "";
            public string UserId { get; set; } = "";
        }

        /// <summary>One output column of a list report.</summary>
        public class Column
        {
            public string Header;
            /// <summary>
            /// SQL alias this column reads from.  Defaults to the heading with
            /// punctuation stripped, but must be set when the two differ
            /// (e.g. heading "ADMISSION" reads alias ADMITDATE).
            /// </summary>
            public string Key;
            /// <summary>Numeric columns are right-aligned and formatted 2dp.</summary>
            public bool Numeric;
            /// <summary>Relative width; the engine scales these to the content width.</summary>
            public double Weight = 1;
            public string Format = "N2";
        }

        /// <summary>A report definition: what it is called, how it is filtered, what it prints.</summary>
        public class Definition
        {
            public string Code;
            public string Title;
            public string LegacyForm;
            public string LegacyReport;
            /// <summary>Short label for each of the up-to-three mode radio buttons.</summary>
            public string[] Modes = new string[0];
            /// <summary>Mode -> legacy report name, for the log/notes.</summary>
            public string[] ModeReports = new string[0];
            public List<Column> Columns = new List<Column>();
            /// <summary>Optional column to group by; group header rows are drawn for it.</summary>
            public string GroupBy;
            /// <summary>Numeric column summed into a grand total (null = no total row).</summary>
            public string TotalColumn;
            /// <summary>Builds the SQL and returns the ordered parameter list.</summary>
            public Func<Criteria, (string Sql, List<(string, object)> Ps)> Build;
            /// <summary>Extra criteria lines printed under the title (VFP HD1/HD2/HD3).</summary>
            public Func<Criteria, string> CriteriaEcho;
        }

        /// <summary>One result row: ordinal -> display string.</summary>
        private class Row
        {
            public Dictionary<string, string> Cells = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        // ================================================================ definitions

        /// <summary>
        /// The migrated report registry.  One entry per remaining legacy menu form.
        /// Keys match the aliases exposed by the Reports &gt; Billing menu.
        /// </summary>
        public static Definition Find(string code) => All.FirstOrDefault(d =>
            string.Equals(d.Code, code, StringComparison.OrdinalIgnoreCase));

        public static readonly List<Definition> All = new List<Definition>
        {
            // ---------------------------------------------------------------- bs021010
            new Definition
            {
                Code = "BILLSLIST", Title = "IPD BILLS LIST", LegacyForm = "bs021010",
                LegacyReport = "IPD-BLSS / IPD-BLSD",
                Modes = new[] { "Summary", "Detail" },
                ModeReports = new[] { "IPD-BLSS", "IPD-BLSD" },
                Columns = new List<Column>
                {
                    new Column { Header = "IPD NO.", Weight = 0.8 },
                    new Column { Header = "BILL NO.", Key = "BILLNO", Weight = 0.8 },
                    new Column { Header = "BILL DATE", Weight = 1.0 },
                    new Column { Header = "PATIENT", Weight = 2.0 },
                    new Column { Header = "EMPLOYEE", Weight = 1.6 },
                    new Column { Header = "COMPANY", Weight = 1.6 },
                    new Column { Header = "NTOT", Numeric = true, Weight = 1.0 },
                },
                GroupBy = null,
                TotalColumn = "NTOT",
                CriteriaEcho = c => Echo(c, "Bill Date"),
                Build = (c) =>
                {
                    var sb = new StringBuilder();
                    var ps = new List<(string, object)>();
                    if (c.Mode == 1)
                    {
                        // VFP: summary = the bill header list (IPD-BLSS)
                        sb.Append(@"
SELECT b.HOS_CODE AS IPDNO, b.BILLNO, b.BDATE AS BILLEDATE,
       ISNULL(p.NAME,'') AS PATIENT,
       ISNULL(e.NAME,'') AS EMPLOYEE,
       ISNULL(co.NAME,'') AS COMPANY,
       b.NTOT AS NTOT
FROM IPDBILL b
JOIN REGISTER r      ON (b.HOS_CODE = r.REGNO)
LEFT JOIN PATIENT p   ON (b.PCODE = p.CODE)
LEFT JOIN EMPLOYEE e  ON (r.CID = e.CID AND r.ECODE = e.CODE)
LEFT JOIN COMPANY co   ON (b.CID = co.ID)
WHERE r.TYPE='I' AND ISNULL(b.STAT,'') = ''");
                        AddCommon(sb, ps, c, "b.BDATE", "b.CID", false, false);
                        if (!string.IsNullOrWhiteSpace(c.BillNo)) { ps.Add(("@bill", c.BillNo.Trim())); sb.Append("\n  AND b.BILLNO = @bill"); }
                        sb.Append("\nORDER BY b.BDATE, b.BILLNO");
                    }
                    else
                    {
                        // VFP: detail = the same list joined down to slip lines (IPD-BLSD)
                        sb.Append(@"
SELECT b.HOS_CODE AS IPDNO, b.BILLNO, b.BDATE AS BILLEDATE,
       ISNULL(p.NAME,'') AS PATIENT,
       ISNULL(e.NAME,'') AS EMPLOYEE,
       ISNULL(co.NAME,'') AS COMPANY,
       ISNULL(m.DEPT,'') AS DEPT,
       d.CODE AS SRVCODE, ISNULL(d.[DESC],'') AS SRVDESC,
       CASE WHEN m.TYPE='R' THEN d.QTY*-1 ELSE d.QTY END AS QTY,
       d.RATE AS RATE,
       CASE WHEN m.TYPE='R' THEN d.NAMT*-1 ELSE d.NAMT END AS NTOT
FROM IPDBILL b
JOIN REGISTER r      ON (b.HOS_CODE = r.REGNO)
JOIN IBIL_MST m       ON (m.BILL = b.BILLNO)
JOIN IBIL_DET d       ON (m.DEPT = d.DEPT AND m.SLIPNO = d.SLIPNO)
LEFT JOIN PATIENT p   ON (b.PCODE = p.CODE)
LEFT JOIN EMPLOYEE e  ON (r.CID = e.CID AND r.ECODE = e.CODE)
LEFT JOIN COMPANY co   ON (b.CID = co.ID)
WHERE r.TYPE='I' AND ISNULL(b.STAT,'') = ''
  AND ISNULL(d.FLAG,'') <> 'P' AND ISNULL(d.RETIRED,0) = 0");
                        AddCommon(sb, ps, c, "b.BDATE", "b.CID", true, true);
                        if (!string.IsNullOrWhiteSpace(c.BillNo)) { ps.Add(("@bill", c.BillNo.Trim())); sb.Append("\n  AND b.BILLNO = @bill"); }
                        sb.Append("\nORDER BY b.BDATE, b.BILLNO, m.DEPT, d.CODE");
                    }
                    return (sb.ToString(), ps);
                },
            },

            // ---------------------------------------------------------------- bs021020
            new Definition
            {
                Code = "CONSOLIDATED", Title = "CONSOLIDATED BILLS REPORT", LegacyForm = "bs021020",
                LegacyReport = "IPD-CONS / IPD-COND",
                Modes = new[] { "Company Wise", "Employee Wise" },
                ModeReports = new[] { "IPD-CONS", "IPD-COND" },
                Columns = new List<Column>
                {
                    new Column { Header = "COMPANY", Weight = 2.2 },
                    new Column { Header = "EMPLOYEE ID", Weight = 0.9 },
                    new Column { Header = "EMPLOYEE", Weight = 1.8 },
                    new Column { Header = "PATIENT", Weight = 1.8 },
                    new Column { Header = "LAB", Numeric = true, Weight = 0.85 },
                    new Column { Header = "X-RAY", Numeric = true, Weight = 0.85 },
                    new Column { Header = "USG", Numeric = true, Weight = 0.85 },
                    new Column { Header = "DENTAL", Numeric = true, Weight = 0.85 },
                    new Column { Header = "PHARMACY", Numeric = true, Weight = 0.95 },
                    new Column { Header = "OPERATION", Numeric = true, Weight = 0.95 },
                    new Column { Header = "HOSPITAL", Numeric = true, Weight = 0.95 },
                    new Column { Header = "PROCEDURE", Numeric = true, Weight = 0.95 },
                },
                TotalColumn = "LAB",
                CriteriaEcho = c => Echo(c, "Bill Date"),
                Build = (c) =>
                {
                    var sb = new StringBuilder();
                    var ps = new List<(string, object)>();
                    if (c.Mode == 1)
                    {
                        sb.Append(@"
SELECT ISNULL(co.NAME,'') AS COMPANY,
       CAST(0 AS varchar(10)) AS EMPID,
       CAST('' AS varchar(60)) AS EMPLOYEE,
       CAST('' AS varchar(60)) AS PATIENT,
       0.00 AS LAB, 0.00 AS XRAY, 0.00 AS USG, 0.00 AS DENTAL,
       0.00 AS PHARMACY, 0.00 AS OPERATION, 0.00 AS HOSPITAL, 0.00 AS [PROCEDURE],
       SUM(b.NTOT) AS TOTAL, COUNT(*) AS BILLS
FROM IPDBILL b
JOIN REGISTER r    ON (b.HOS_CODE = r.REGNO)
LEFT JOIN COMPANY co ON (b.CID = co.ID)
WHERE r.TYPE='I' AND ISNULL(b.STAT,'') = ''");
                        AddCommon(sb, ps, c, "b.BDATE", "b.CID", false, false);
                        sb.Append("\nGROUP BY co.NAME, b.CID\nORDER BY co.NAME, b.CID");
                    }
                    else
                    {
                        // NOTE: the original inner-joined Employee, dropping every patient
                        // without an employee code (review finding B5). LEFT join here.
                        sb.Append(@"
SELECT ISNULL(co.NAME,'') AS COMPANY,
       ISNULL(r.ECODE,'') AS EMPID,
       ISNULL(e.NAME,'') AS EMPLOYEE,
       ISNULL(p.NAME,'') AS PATIENT,
       SUM(CASE WHEN d.DEPT='01' THEN AMT ELSE 0 END) AS LAB,
       SUM(CASE WHEN d.DEPT='02' THEN AMT ELSE 0 END) AS XRAY,
       SUM(CASE WHEN d.DEPT='03' THEN AMT ELSE 0 END) AS USG,
       SUM(CASE WHEN d.DEPT='04' THEN AMT ELSE 0 END) AS DENTAL,
       SUM(CASE WHEN d.DEPT='10' THEN AMT ELSE 0 END) AS PHARMACY,
       SUM(CASE WHEN d.DEPT='06' THEN AMT ELSE 0 END) AS OPERATION,
       SUM(CASE WHEN d.DEPT='07' THEN AMT ELSE 0 END) AS HOSPITAL,
       SUM(CASE WHEN d.DEPT='11' THEN AMT ELSE 0 END) AS [PROCEDURE]
FROM IBIL_MST m
JOIN IBIL_DET d   ON (m.DEPT = d.DEPT AND m.SLIPNO = d.SLIPNO)
JOIN REGISTER r   ON (m.HOS_CODE = r.REGNO)
LEFT JOIN PATIENT p  ON (r.PCODE = p.CODE)
LEFT JOIN COMPANY co ON (r.CID = co.ID)
LEFT JOIN EMPLOYEE e ON (r.CID = e.CID AND r.ECODE = e.CODE)
CROSS APPLY (SELECT CASE WHEN m.TYPE='R' THEN d.NAMT * -1 ELSE d.NAMT END AS AMT) x
WHERE r.TYPE='I' AND ISNULL(d.FLAG,'') <> 'P' AND ISNULL(d.RETIRED,0) = 0");
                        AddCommon(sb, ps, c, "m.BDATE", "r.CID", true, true);
                        sb.Append("\nGROUP BY co.NAME, r.ECODE, e.NAME, p.NAME, r.PCODE\nORDER BY co.NAME, r.ECODE, p.NAME");
                    }
                    return (sb.ToString(), ps);
                },
            },

            // ---------------------------------------------------------------- bs021030
            new Definition
            {
                Code = "PERIODIC", Title = "PERIODIC BILLS SUMMARY", LegacyForm = "bs021030",
                LegacyReport = "IPTSUMC / IPTSUM / IPTSUME",
                Modes = new[] { "Company Wise", "Patient Wise", "Employee Wise" },
                ModeReports = new[] { "IPTSUMC", "IPTSUM", "IPTSUME" },
                Columns = new List<Column>
                {
                    new Column { Header = "COMPANY", Weight = 2.0 },
                    new Column { Header = "EMPLOYEE ID", Weight = 0.9 },
                    new Column { Header = "EMPLOYEE", Weight = 1.8 },
                    new Column { Header = "IPD / PATIENT", Key = "IPDNO", Weight = 1.0 },
                    new Column { Header = "PATIENT", Weight = 1.8 },
                    new Column { Header = "LAB", Numeric = true, Weight = 0.85 },
                    new Column { Header = "X-RAY", Numeric = true, Weight = 0.85 },
                    new Column { Header = "USG", Numeric = true, Weight = 0.85 },
                    new Column { Header = "DENTAL", Numeric = true, Weight = 0.85 },
                    new Column { Header = "PHARMACY", Numeric = true, Weight = 0.95 },
                    new Column { Header = "OPERATION", Numeric = true, Weight = 0.95 },
                    new Column { Header = "HOSPITAL", Numeric = true, Weight = 0.95 },
                    new Column { Header = "PROCEDURE", Numeric = true, Weight = 0.95 },
                },
                GroupBy = "COMPANY",
                CriteriaEcho = c => Echo(c, "Slip Date"),
                Build = (c) =>
                {
                    // Review finding B8: the original company-wise branch printed the COMPANY
                    // name in the pname and ename columns. Corrected below.
                    var group = c.Mode == 1
                        ? "r.CID, co.NAME, r.ECODE, e.NAME, m.HOS_CODE, p.NAME, r.PCODE"
                        : (c.Mode == 2 ? "m.HOS_CODE, co.NAME, r.PCODE, p.NAME, r.ECODE, e.NAME"
                                       : "r.CID, r.ECODE, m.HOS_CODE, co.NAME, e.NAME, r.PCODE, p.NAME");
                    var order = c.Mode == 1
                        ? "co.NAME, r.CID"
                        : (c.Mode == 2 ? "co.NAME, m.HOS_CODE" : "co.NAME, r.ECODE, m.HOS_CODE");

                    var sb = new StringBuilder();
                    sb.Append(@"
SELECT ISNULL(co.NAME,'') AS COMPANY,
       ISNULL(r.ECODE,'') AS EMPID,
       ISNULL(e.NAME,'') AS EMPLOYEE,
       ISNULL(m.HOS_CODE,'') AS IPDNO,
       ISNULL(p.NAME,'') AS PATIENT,
       SUM(CASE WHEN m.DEPT='01' THEN AMT ELSE 0 END) AS LAB,
       SUM(CASE WHEN m.DEPT='02' THEN AMT ELSE 0 END) AS XRAY,
       SUM(CASE WHEN m.DEPT='03' THEN AMT ELSE 0 END) AS USG,
       SUM(CASE WHEN m.DEPT='04' THEN AMT ELSE 0 END) AS DENTAL,
       SUM(CASE WHEN m.DEPT='10' THEN AMT ELSE 0 END) AS PHARMACY,
       SUM(CASE WHEN m.DEPT='06' THEN AMT ELSE 0 END) AS OPERATION,
       SUM(CASE WHEN m.DEPT='07' THEN AMT ELSE 0 END) AS HOSPITAL,
       SUM(CASE WHEN m.DEPT='11' THEN AMT ELSE 0 END) AS [PROCEDURE]
FROM IBIL_MST m
JOIN IBIL_DET d   ON (m.DEPT = d.DEPT AND m.SLIPNO = d.SLIPNO)
JOIN REGISTER r   ON (m.HOS_CODE = r.REGNO)
LEFT JOIN PATIENT p  ON (r.PCODE = p.CODE)
LEFT JOIN COMPANY co ON (r.CID = co.ID)
LEFT JOIN EMPLOYEE e ON (r.CID = e.CID AND r.ECODE = e.CODE)
CROSS APPLY (SELECT CASE WHEN m.TYPE='R' THEN d.NAMT * -1 ELSE d.NAMT END AS AMT) x
WHERE r.TYPE='I' AND ISNULL(d.FLAG,'') <> 'P' AND ISNULL(d.RETIRED,0) = 0");
                    var ps = new List<(string, object)>();
                    AddCommon(sb, ps, c, "m.SLIPDT", "r.CID", true, true);
                    sb.Append("\nGROUP BY " + group + "\nORDER BY " + order);
                    return (sb.ToString(), ps);
                },
            },

            // ---------------------------------------------------------------- bs021050
            new Definition
            {
                Code = "IPDAUDIT", Title = "IPD AUDIT REPORT", LegacyForm = "bs021050",
                LegacyReport = "IAUDITS / IAUDITD / IDEPADT",
                Modes = new[] { "Summary", "Detail", "Department Wise" },
                ModeReports = new[] { "IAUDITS", "IAUDITD", "IDEPADT" },
                Columns = new List<Column>
                {
                    new Column { Header = "IPD NO.", Weight = 0.8 },
                    new Column { Header = "DEPT", Weight = 0.6 },
                    new Column { Header = "SLIP DATE", Weight = 1.0 },
                    new Column { Header = "CODE", Weight = 0.7 },
                    new Column { Header = "DESCRIPTION", Weight = 2.6 },
                    new Column { Header = "CONSULTANT", Weight = 1.4 },
                    new Column { Header = "PATIENT", Weight = 1.6 },
                    new Column { Header = "QTY", Numeric = true, Weight = 0.6 },
                    new Column { Header = "RATE", Numeric = true, Weight = 0.8 },
                    new Column { Header = "AMOUNT", Numeric = true, Weight = 1.0 },
                },
                GroupBy = "IPD NO.",
                TotalColumn = "AMOUNT",
                CriteriaEcho = c => Echo(c, "Slip Date"),
                Build = (c) =>
                {
                    var sb = new StringBuilder();
                    sb.Append(@"
SELECT m.HOS_CODE AS IPDNO, ISNULL(m.DEPT,'') AS DEPT, m.SLIPDT AS SLIPDATE,
       d.CODE AS CODE, ISNULL(d.[DESC],'') AS DESCRIPTION,
       ISNULL(c2.NAME,'') AS CONSULTANT,
       ISNULL(p.NAME,'') AS PATIENT,
       CASE WHEN m.TYPE='R' THEN d.QTY * -1 ELSE d.QTY END AS QTY,
       d.RATE AS RATE,
       CASE WHEN m.TYPE='R' THEN d.NAMT * -1 ELSE d.NAMT END AS AMOUNT
FROM IBIL_MST m
JOIN IBIL_DET d   ON (m.DEPT = d.DEPT AND m.SLIPNO = d.SLIPNO)
JOIN REGISTER r   ON (m.HOS_CODE = r.REGNO)
LEFT JOIN PATIENT p  ON (r.PCODE = p.CODE)
LEFT JOIN CONSULT c2 ON (d.CONCODE = c2.ID)
WHERE r.TYPE='I' AND ISNULL(d.FLAG,'') <> 'P' AND ISNULL(d.RETIRED,0) = 0");
                    var ps = new List<(string, object)>();
                    // NOTE: bs021050 deliberately does NOT filter !billgen - it audits every slip.
                    AddCommon(sb, ps, c, "m.SLIPDT", "r.CID", true, true);
                    var order = c.Mode == 3
                        ? "m.DEPT, m.HOS_CODE, m.SLIPDT"
                        : (c.Mode == 2 ? "r.CID, m.HOS_CODE, m.DEPT, m.SLIPDT"
                                       : "r.CID, m.HOS_CODE, m.DEPT, m.SLIPDT, d.CODE");
                    sb.Append("\nORDER BY " + order);
                    return (sb.ToString(), ps);
                },
            },

            // ---------------------------------------------------------------- bs021060
            new Definition
            {
                Code = "COVERING", Title = "COVERING LETTER - STATEMENT OF ACCOUNT", LegacyForm = "bs021060",
                LegacyReport = "IPDCOVLT",
                Modes = new string[0],
                Columns = new List<Column>
                {
                    new Column { Header = "COMPANY", Weight = 2.4 },
                    new Column { Header = "ATTENTION", Weight = 1.6 },
                    new Column { Header = "CITY", Weight = 1.2 },
                    new Column { Header = "MONTH", Weight = 1.2 },
                    new Column { Header = "BILLS", Numeric = true, Weight = 0.7 },
                    new Column { Header = "FROM BILL", Weight = 0.9 },
                    new Column { Header = "TO BILL", Weight = 0.9 },
                    new Column { Header = "TOTAL", Numeric = true, Weight = 1.1 },
                },
                GroupBy = null,
                TotalColumn = "TOTAL",
                // Review finding B17: the legacy form hard-coded a signatory name into a text
                // box ("ASHFAQUE AHMED"). That is deliberately not reproduced here.
                CriteriaEcho = c => Echo(c, "Bill Date"),
                Build = (c) =>
                {
                    var sb = new StringBuilder();
                    sb.Append(@"
SELECT ISNULL(co.NAME,'') AS COMPANY, ISNULL(co.ATTN,'') AS ATTENTION,
       ISNULL(co.CITY,'') AS CITY,
       LEFT(CONVERT(varchar(7), b.BDATE, 120), 7) AS MONTH,
       COUNT(b.BILLNO) AS BILLS,
       MIN(b.BILLNO) AS FROMBILL, MAX(b.BILLNO) AS TOBILL,
       SUM(b.NTOT) AS TOTAL
FROM IPDBILL b
JOIN REGISTER r      ON (b.HOS_CODE = r.REGNO)
LEFT JOIN COMPANY co ON (b.CID = co.ID)
WHERE r.TYPE='I' AND ISNULL(b.STAT,'') = ''");
                    var ps = new List<(string, object)>();
                    AddCommon(sb, ps, c, "b.BDATE", "b.CID", false, false);
                    sb.Append("\nGROUP BY co.NAME, co.ATTN, co.CITY, LEFT(CONVERT(varchar(7), b.BDATE, 120), 7), b.CID"
                            + "\nORDER BY co.NAME, MONTH");
                    return (sb.ToString(), ps);
                },
            },

            // ---------------------------------------------------------------- ibillstat
            new Definition
            {
                Code = "BILLSTATUS", Title = "BILLING STATUS REPORT", LegacyForm = "ibillstat",
                LegacyReport = "IBILLSTAT",
                Modes = new[] { "All Patients", "Private Patients", "Panel Patients" },
                ModeReports = new[] { "IBILLSTAT", "IBILLSTAT", "IBILLSTAT" },
                Columns = new List<Column>
                {
                    new Column { Header = "IPD NO.", Weight = 0.8 },
                    new Column { Header = "BILL NO.", Key = "BILLNO", Weight = 0.8 },
                    new Column { Header = "ADMISSION", Key = "ADMITDATE", Weight = 1.0 },
                    new Column { Header = "DISCHARGE", Key = "DISCHARGEDATE", Weight = 1.0 },
                    new Column { Header = "PATIENT", Weight = 1.9 },
                    new Column { Header = "COMPANY", Weight = 1.7 },
                    new Column { Header = "CHARGES", Numeric = true, Weight = 1.0 },
                    new Column { Header = "DEPOSITED", Numeric = true, Weight = 1.0 },
                    new Column { Header = "REFUNDED", Numeric = true, Weight = 0.95 },
                    new Column { Header = "DISCOUNT", Numeric = true, Weight = 0.9 },
                    new Column { Header = "BALANCE", Numeric = true, Weight = 1.0 },
                },
                TotalColumn = "BALANCE",
                CriteriaEcho = c => Echo(c, "Discharge Date"),
                Build = (c) =>
                {
                    var sb = new StringBuilder();
                    sb.Append(@"
SELECT r.REGNO AS IPDNO, ISNULL(b.BILLNO,'') AS BILLNO,
       r.RDATE AS ADMITDATE, r.DDATE AS DISCHARGEDATE,
       ISNULL(p.NAME,'') AS PATIENT, ISNULL(co.NAME,'') AS COMPANY,
       ISNULL(r.TBILLAMT,0) AS CHARGES,
       ISNULL(r.TRCVD,0) AS DEPOSITED,
       ISNULL(r.TPYMT,0) AS REFUNDED,
       ISNULL(r.CASHDISC,0) AS DISCOUNT,
       ISNULL(r.TBILLAMT,0) - ISNULL(r.TRCVD,0) + ISNULL(r.TPYMT,0) - ISNULL(r.CASHDISC,0) AS BALANCE
FROM REGISTER r
LEFT JOIN PATIENT p   ON (r.PCODE = p.CODE)
LEFT JOIN COMPANY co  ON (r.CID = co.ID)
OUTER APPLY (SELECT TOP 1 b.BILLNO, b.BDATE FROM IPDBILL b
             WHERE b.HOS_CODE = r.REGNO AND ISNULL(b.STAT,'') = ''
             ORDER BY b.BDATE DESC) b
WHERE r.TYPE='I'");
                    var ps = new List<(string, object)>();
                    // Review finding B28: the legacy form inner-joined COMPANY, so its own
                    // "Private Patients" option could never return a row. LEFT join here.
                    if (c.From.HasValue) { ps.Add(("@df", c.From.Value.Date)); sb.Append("\n  AND r.DDATE >= @df"); }
                    if (c.To.HasValue) { ps.Add(("@dt", c.To.Value.Date.AddDays(1).AddSeconds(-1))); sb.Append("\n  AND r.DDATE <= @dt"); }
                    if (c.Mode == 2) sb.Append("\n  AND ISNULL(r.CID,'') = ''");
                    if (c.Mode == 3) sb.Append("\n  AND ISNULL(r.CID,'') <> ''");
                    if (!string.IsNullOrWhiteSpace(c.Company)) { ps.Add(("@cid", c.Company.Trim())); sb.Append("\n  AND r.CID = @cid"); }
                    sb.Append("\nORDER BY r.REGNO");
                    return (sb.ToString(), ps);
                },
            },

            // ---------------------------------------------------------------- billstat
            new Definition
            {
                Code = "BILLGENSTATUS", Title = "BILL GENERATION STATUS", LegacyForm = "billstat",
                LegacyReport = "BILLSTAT",
                Modes = new[] { "All Patients", "Private Only", "Company Only" },
                ModeReports = new[] { "BILLSTAT", "BILLSTAT", "BILLSTAT" },
                Columns = new List<Column>
                {
                    new Column { Header = "IPD NO.", Weight = 0.8 },
                    new Column { Header = "TYPE", Weight = 0.6 },
                    new Column { Header = "ADMISSION", Key = "ADMITDATE", Weight = 1.0 },
                    new Column { Header = "DISCHARGE", Key = "DISCHARGEDATE", Weight = 1.0 },
                    new Column { Header = "PATIENT", Weight = 1.9 },
                    new Column { Header = "COMPANY", Weight = 1.7 },
                    new Column { Header = "BILL NO.", Key = "BILLNO", Weight = 0.8 },
                    new Column { Header = "BILL GEN", Key = "BILLGEN", Weight = 0.75 },
                    new Column { Header = "CHARGES", Numeric = true, Weight = 1.0 },
                    new Column { Header = "DEPOSITED", Numeric = true, Weight = 1.0 },
                },
                TotalColumn = "CHARGES",
                CriteriaEcho = c => Echo(c, "Discharge Date"),
                Build = (c) =>
                {
                    var sb = new StringBuilder();
                    sb.Append(@"
SELECT r.REGNO AS IPDNO, ISNULL(r.TYPE,'') AS TYPE,
       r.RDATE AS ADMITDATE, r.DDATE AS DISCHARGEDATE,
       ISNULL(p.NAME,'') AS PATIENT, ISNULL(co.NAME,'') AS COMPANY,
       ISNULL(b.BILLNO,'') AS BILLNO,
       CASE WHEN ISNULL(r.BILLGEN,0) = 0 THEN 'NO' ELSE 'YES' END AS BILLGEN,
       ISNULL(r.TBILLAMT,0) AS CHARGES,
       ISNULL(r.TRCVD,0) AS DEPOSITED
FROM REGISTER r
LEFT JOIN PATIENT p   ON (r.PCODE = p.CODE)
LEFT JOIN COMPANY co  ON (r.CID = co.ID)
OUTER APPLY (SELECT TOP 1 b.BILLNO FROM IPDBILL b
             WHERE b.HOS_CODE = r.REGNO AND ISNULL(b.STAT,'') = ''
             ORDER BY b.BDATE DESC) b
WHERE r.TYPE='I' AND ISNULL(r.CSTAT,'') <> ''");
                    var ps = new List<(string, object)>();
                    if (c.From.HasValue) { ps.Add(("@df", c.From.Value.Date)); sb.Append("\n  AND r.DDATE >= @df"); }
                    if (c.To.HasValue) { ps.Add(("@dt", c.To.Value.Date.AddDays(1).AddSeconds(-1))); sb.Append("\n  AND r.DDATE <= @dt"); }
                    if (c.Mode == 2) sb.Append("\n  AND ISNULL(r.CID,'') = ''");
                    if (c.Mode == 3) sb.Append("\n  AND ISNULL(r.CID,'') <> ''");
                    if (!string.IsNullOrWhiteSpace(c.Company)) { ps.Add(("@cid", c.Company.Trim())); sb.Append("\n  AND r.CID = @cid"); }
                    sb.Append("\nORDER BY co.NAME, p.NAME");
                    return (sb.ToString(), ps);
                },
            },

            // ---------------------------------------------------------------- cdischgrpt
            new Definition
            {
                Code = "DISCHARGES", Title = "COMPANY DISCHARGES REPORT", LegacyForm = "cdischgrpt",
                LegacyReport = "CDISCHGRPT",
                Modes = new[] { "By Discharge Date", "By IPD No.", "By Patient Name" },
                ModeReports = new[] { "CDISCHGRPT", "CDISCHGRPT", "CDISCHGRPT" },
                Columns = new List<Column>
                {
                    new Column { Header = "COMPANY", Weight = 1.9 },
                    new Column { Header = "IPD NO.", Weight = 0.75 },
                    new Column { Header = "PATIENT", Weight = 1.9 },
                    new Column { Header = "EMPLOYEE", Weight = 1.5 },
                    new Column { Header = "ADMISSION", Key = "ADMITDATE", Weight = 1.0 },
                    new Column { Header = "DISCHARGE", Key = "DISCHARGEDATE", Weight = 1.0 },
                    new Column { Header = "BED", Weight = 0.6 },
                    new Column { Header = "CHARGES", Numeric = true, Weight = 0.95 },
                    new Column { Header = "NET PAID", Numeric = true, Weight = 0.95 },
                    new Column { Header = "BALANCE", Numeric = true, Weight = 0.95 },
                },
                GroupBy = "COMPANY",
                TotalColumn = "BALANCE",
                CriteriaEcho = c => Echo(c, "Discharge Date"),
                Build = (c) =>
                {
                    var sb = new StringBuilder();
                    sb.Append(@"
SELECT ISNULL(co.NAME,'') AS COMPANY, r.REGNO AS IPDNO,
       ISNULL(p.NAME,'') AS PATIENT, ISNULL(e.NAME,'') AS EMPLOYEE,
       r.RDATE AS ADMITDATE, r.DDATE AS DISCHARGEDATE, ISNULL(r.BEDNO,'') AS BED,
       ISNULL(ch.CHARGE,0) AS CHARGES,
       ISNULL(r.TRCVD,0) - ISNULL(r.TPYMT,0) AS NETPAID,
       ISNULL(ch.CHARGE,0) - (ISNULL(r.TRCVD,0) - ISNULL(r.TPYMT,0)) AS BALANCE
FROM REGISTER r
LEFT JOIN PATIENT p   ON (r.PCODE = p.CODE)
LEFT JOIN COMPANY co  ON (r.CID = co.ID)
LEFT JOIN EMPLOYEE e  ON (r.CID = e.CID AND r.ECODE = e.CODE)
LEFT JOIN (SELECT m.HOS_CODE AS HOSCODE,
                  SUM(CASE WHEN m.TYPE='R' THEN d.NAMT * -1 ELSE d.NAMT END) AS CHARGE
           FROM IBIL_MST m
           JOIN IBIL_DET d ON (m.DEPT = d.DEPT AND m.SLIPNO = d.SLIPNO)
           WHERE ISNULL(d.FLAG,'') <> 'P' AND ISNULL(d.RETIRED,0) = 0
           GROUP BY m.HOS_CODE) ch ON (ch.HOSCODE = r.REGNO)
WHERE r.TYPE='I' AND ISNULL(r.CID,'') <> ''");
                    var ps = new List<(string, object)>();
                    AddCommon(sb, ps, c, "r.DDATE", "r.CID", false, false);
                    if (c.Mode == 2 && !string.IsNullOrWhiteSpace(c.RegNo))
                    { ps.Add(("@hos", c.RegNo.Trim())); sb.Append("\n  AND r.REGNO = @hos"); }
                    var order = c.Mode == 1 ? "co.NAME, r.DDATE" : (c.Mode == 2 ? "co.NAME, r.REGNO" : "co.NAME, p.NAME");
                    sb.Append("\nORDER BY " + order);
                    return (sb.ToString(), ps);
                },
            },
        };

        private static string SqlSafe(string s) => (s ?? "").Replace("'", "''");

        /// <summary>
        /// Criteria echo line under the title - reproduces the legacy HD1/HD2/HD3 headings.
        /// </summary>
        private static string Echo(Criteria c, string dateLabel)
        {
            var parts = new List<string>();
            if (c.From.HasValue || c.To.HasValue)
                parts.Add((string.IsNullOrEmpty(dateLabel) ? "Date" : dateLabel) + " : "
                          + (c.From.HasValue ? c.From.Value.ToString("dd/MM/yyyy") : "...")
                          + "  To  "
                          + (c.To.HasValue ? c.To.Value.ToString("dd/MM/yyyy") : "..."));
            if (!string.IsNullOrWhiteSpace(c.Company)) parts.Add("Company : " + c.Company.Trim());
            if (!string.IsNullOrWhiteSpace(c.Dept)) parts.Add("Department : " + c.Dept.Trim());
            if (!string.IsNullOrWhiteSpace(c.RegNo)) parts.Add("IPD No. : " + c.RegNo.Trim());
            if (!string.IsNullOrWhiteSpace(c.BillNo)) parts.Add("Bill No. : " + c.BillNo.Trim());
            return string.Join("     ", parts);
        }

        /// <summary>
        /// Appends the optional company / department / IPD-no filters.  <paramref name="allowReg"/>
        /// is false for the bill reports, which filter on IPDBILL.HOS_CODE instead.
        /// </summary>
        private static void AddCommon(StringBuilder sb, List<(string, object)> ps, Criteria c,
                                      string dateCol, string companyCol,
                                      bool applyDept, bool applyRegNo)
        {
            if (c.From.HasValue) { ps.Add(("@df", c.From.Value.Date)); sb.Append("\n  AND ").Append(dateCol).Append(" >= @df"); }
            if (c.To.HasValue) { ps.Add(("@dt", c.To.Value.Date.AddDays(1).AddSeconds(-1))); sb.Append("\n  AND ").Append(dateCol).Append(" <= @dt"); }
            if (!string.IsNullOrWhiteSpace(c.Company)) { ps.Add(("@cid", c.Company.Trim())); sb.Append("\n  AND ").Append(companyCol).Append(" = @cid"); }
            // dept / IPD-no only exist once IBIL_MST is in the FROM list
            if (applyDept && !string.IsNullOrWhiteSpace(c.Dept))
            { ps.Add(("@dept", c.Dept.Trim())); sb.Append("\n  AND m.DEPT = @dept"); }
            if (applyRegNo && !string.IsNullOrWhiteSpace(c.RegNo))
            { ps.Add(("@hos", c.RegNo.Trim())); sb.Append("\n  AND m.HOS_CODE = @hos"); }
        }

        // ================================================================ criteria lookups

        /// <summary>Company list for the criteria drop-down (VFP cbocomp / txtcompany).</summary>
        public async Task<List<(string Id, string Name)>> GetCompaniesAsync()
        {
            var list = new List<(string, string)>();
            await FillAsync("SELECT ID, NAME FROM COMPANY WHERE ISNULL(ID,'') <> '' ORDER BY NAME", list);
            return list;
        }

        /// <summary>Department list for the criteria drop-down (VFP cbodept).</summary>
        public async Task<List<(string Id, string Name)>> GetDepartmentsAsync()
        {
            var list = new List<(string, string)>();
            await FillAsync("SELECT ID, NAME FROM DEPT ORDER BY BILLORDER, NAME", list);
            return list;
        }

        private async Task FillAsync(string sql, List<(string Id, string Name)> into)
        {
            var conn = _db.Database.GetDbConnection();
            var opened = false;
            if (conn.State != ConnectionState.Open) { await conn.OpenAsync(); opened = true; }
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                    into.Add((S(rd, "ID"), S(rd, "NAME")));
            }
            finally { if (opened) await conn.CloseAsync(); }
        }

        private static string S(DbDataReader rd, string col)
        {
            for (var i = 0; i < rd.FieldCount; i++)
                if (string.Equals(rd.GetName(i), col, StringComparison.OrdinalIgnoreCase))
                    return rd[i] == DBNull.Value ? "" : (Convert.ToString(rd[i], Inv) ?? "").Trim();
            return "";
        }

        // ================================================================ execution

        /// <summary>Runs a report definition and returns the rendered PDF plus the row count.</summary>
        public async Task<IpdBillReportService.RenderResult> RenderAsync(Definition def, Criteria c, ReportContext ctx)
        {
            if (def == null) return new IpdBillReportService.RenderResult { Pdf = Empty("Unknown report"), Rows = 0 };
            HmisFontResolver.Install();

            if (!c.From.HasValue && !c.To.HasValue && string.IsNullOrWhiteSpace(c.Company)
                && string.IsNullOrWhiteSpace(c.Dept) && string.IsNullOrWhiteSpace(c.RegNo)
                && string.IsNullOrWhiteSpace(c.BillNo))
            {
                // every one of these legacy forms refused to run without a criterion
                return new IpdBillReportService.RenderResult { Rows = 0, Pdf = Empty(def.Title + "\nNo criteria specified - choose at least one filter.") };
            }

            var (sql, ps) = def.Build(c);
            var rows = await QueryAsync(sql, ps, def.Columns);

            // A wide detail selection over a long date range can return millions of lines
            // (the legacy VFP reports had the same problem).  Cap the row count so the PDF
            // stays printable, and say so on the document rather than truncating silently.
            var capped = false;
            if (rows.Count > MaxRows)
            {
                rows.RemoveRange(MaxRows, rows.Count - MaxRows);
                capped = true;
            }

            using var ms = new MemoryStream();
            using var doc = new PdfDocument();
            var canvases = new List<XGraphics>();

            var flow = new FlowState(doc, c.StartPage, c.EndPage, canvases, ctx, def, c);
            try
            {
                if (rows.Count == 0)
                {
                    var msg = def.Title + " - no records matched the selected criteria.";
                    flow.Current.DrawString(msg, F(11, true), NavyB,
                        new XRect(M, PageH / 2 - 10, ContentW, 18), XStringFormats.TopCenter);
                }
                else
                {
                    RenderRows(flow, def, rows);
                    if (capped)
                        flow.Current.DrawString(
                            "NOTE: limited to the first " + MaxRows.ToString("N0", Inv)
                            + " rows - narrow the date range, company or IPD number and run again.",
                            F(8, true), NavyB, x: M, y: flow.Y);
                }
            }
            finally
            {
                StampFooters(canvases, def, ctx);
                flow.Current?.Dispose();
            }
            doc.Save(ms, false);
            return new IpdBillReportService.RenderResult { Pdf = ms.ToArray(), Rows = rows.Count };
        }

        private static byte[] Empty(string message)
        {
            HmisFontResolver.Install();
            using var ms = new MemoryStream();
            using var doc = new PdfDocument();
            var page = doc.AddPage();
            page.Width = PageW; page.Height = PageH;
            using var g = XGraphics.FromPdfPage(page);
            var lines = (message ?? "").Split('\n');
            double y = PageH / 2 - (lines.Length * 12);
            foreach (var l in lines)
            {
                g.DrawString(l, F(11, true), NavyB, new XRect(0, y, PageW, 18), XStringFormats.TopCenter);
                y += 14;
            }
            doc.Save(ms, false);
            return ms.ToArray();
        }

        private async Task<List<Row>> QueryAsync(string sql, List<(string, object)> ps, List<Column> cols)
        {
            var rows = new List<Row>();
            var conn = _db.Database.GetDbConnection();
            var opened = false;
            if (conn.State != ConnectionState.Open) { await conn.OpenAsync(); opened = true; }
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.CommandTimeout = 300;
                foreach (var (n, v) in ps)
                {
                    var p = cmd.CreateParameter();
                    p.ParameterName = n;
                    p.Value = v ?? DBNull.Value;
                    cmd.Parameters.Add(p);
                }
                using var rd = await cmd.ExecuteReaderAsync();
                var names = new List<string>();
                for (var i = 0; i < rd.FieldCount; i++) names.Add(rd.GetName(i));
                while (await rd.ReadAsync())
                {
                    var row = new Row();
                    for (var i = 0; i < rd.FieldCount; i++)
                    {
                        var col = cols.FirstOrDefault(c => string.Equals(c.Header.Replace(" ", ""),
                                                                            names[i].Replace(" ", ""),
                                                                            StringComparison.OrdinalIgnoreCase));
                        var fmt = col?.Numeric == true ? "N2" : null;
                        row.Cells[names[i]] = rd[i] == DBNull.Value ? ""
                            : FormatValue(rd[i], fmt);
                    }
                    rows.Add(row);
                }
            }
            finally { if (opened) await conn.CloseAsync(); }
            return rows;
        }

        private static string FormatValue(object v, string fmt)
        {
            if (v is decimal d) return fmt == "N2" ? d.ToString("N2", Inv) : d.ToString(Inv);
            if (v is double dd) return fmt == "N2" ? dd.ToString("N2", Inv) : dd.ToString(Inv);
            if (v is int i) return i.ToString(Inv);
            if (v is long l) return l.ToString(Inv);
            if (v is DateTime dt)
            {
                if (dt.TimeOfDay == TimeSpan.Zero) return dt.ToString("dd/MM/yyyy", Inv);
                return dt.ToString("dd/MM/yyyy HH:mm", Inv);
            }
            if (v is bool b) return b ? "YES" : "NO";
            return Convert.ToString(v, Inv) ?? "";
        }

        // ================================================================ layout

        private class FlowState
        {
            private readonly PdfDocument _doc;
            private readonly List<XGraphics> _canvases;
            private readonly Definition _def;
            private readonly Criteria _c;
            private readonly ReportContext _ctx;
            public XGraphics Current { get; private set; }
            public double Y { get; set; }
            public int PageNo { get; private set; }
            public int EndPage { get; }
            /// <summary>Logged user, printed on the signature strip.</summary>
            public string UserId => _ctx?.UserId ?? "";

            public FlowState(PdfDocument doc, int start, int end, List<XGraphics> canvases,
                             ReportContext ctx, Definition def, Criteria c)
            {
                _doc = doc; _canvases = canvases; _ctx = ctx; _def = def; _c = c;
                PageNo = start; EndPage = end < start ? start : end;
                Current = NewCanvas();
                Y = DrawHeader(Current, true);
            }

            private XGraphics NewCanvas()
            {
                var p = _doc.AddPage();
                p.Width = PageW; p.Height = PageH;
                var g = XGraphics.FromPdfPage(p);
                _canvases.Add(g);
                return g;
            }

            public bool Need(double h) => Y + h > PageH - BottomReserve;

            public void NextPage()
            {
                Current = NewCanvas();
                PageNo++;
                Y = DrawHeader(Current, false);
            }

            private double DrawHeader(XGraphics g, bool first)
            {
                double y = TopY;
                g.DrawString(_ctx.HospitalName ?? "", F(15, true), NavyB, new XRect(0, y, PageW, 20), XStringFormats.TopCenter);
                y += 19;
                if (!string.IsNullOrWhiteSpace(_ctx.HospitalNo))
                {
                    g.DrawString(_ctx.HospitalNo, F(8), MutedB, new XRect(0, y, PageW, 11), XStringFormats.TopCenter);
                    y += 12;
                }
                y += 5;

                var title = _def.Title;
                if (_def.Modes.Length > 1)
                    title += "   -   " + (_def.Modes[Math.Max(0, Math.Min(_def.Modes.Length - 1, _c.Mode - 1))] ?? "");
                const double bandH = 26;
                g.DrawRectangle(HeadB, new XRect(M, y, ContentW, bandH));
                g.DrawRectangle(TealB, new XRect(M, y, 3.2, bandH));
                g.DrawString(title, F(12, true), NavyB, x: M + 12, y: y + 7.5);
                g.DrawString("PAGE", F(7), MutedB, new XRect(M + ContentW - 210, y + 5, 60, 9), XStringFormats.TopRight);
                g.DrawString(PageNo.ToString(Inv), F(9, true), NavyB,
                    new XRect(M + ContentW - 145, y + 3.5, 135, 12), XStringFormats.TopRight);
                y += bandH + 5;

                var echo = _def.CriteriaEcho?.Invoke(_c);
                var left = (_def.LegacyForm + "   /   " + _def.LegacyReport);
                if (!string.IsNullOrWhiteSpace(echo))
                {
                    g.DrawString(echo, F(8.2), BodyB, x: M, y: y);
                    y += 12;
                }
                g.DrawString("Source form : " + left, F(7.2), MutedB, x: M, y: y);
                g.DrawString("Generated : " + DateTime.Now.ToString("dd MMM yyyy HH:mm", Inv),
                    F(7.2), MutedB, new XRect(M, y, ContentW, 10), XStringFormats.TopRight);
                y += 12;
                g.DrawLine(TealPen, M, y, M + ContentW, y);
                return y + 9;
            }
        }

        private static double[] ColumnWidths(Definition def)
        {
            var total = def.Columns.Sum(c => c.Weight <= 0 ? 1 : c.Weight);
            var w = new double[def.Columns.Count];
            for (var i = 0; i < def.Columns.Count; i++)
                w[i] = ContentW * (def.Columns[i].Weight <= 0 ? 1 : def.Columns[i].Weight) / total;
            return w;
        }

        private static double DrawTableHeader(XGraphics g, double y, Definition def, double[] w)
        {
            g.DrawRectangle(HeadB, new XRect(M, y, ContentW, TableHeadH));
            g.DrawLine(NavyPen, M, y, M + ContentW, y);
            g.DrawLine(NavyPen, M, y + TableHeadH, M + ContentW, y + TableHeadH);
            var f = F(7.6, true);
            double x = M;
            for (var i = 0; i < def.Columns.Count; i++)
            {
                var col = def.Columns[i];
                var head = col.Header;
                if (col.Numeric)
                    g.DrawString(head, f, NavyB, new XRect(x, y + 5.5, w[i] - 5, 11), XStringFormats.TopRight);
                else
                {
                    // shrink the heading until it fits its column
                    while (head.Length > 3 && g.MeasureString(head, f).Width > w[i] - 9)
                        head = head.Substring(0, head.Length - 1);
                    g.DrawString(head, f, NavyB, x: x + 4, y: y + 5.5);
                }
                x += w[i];
            }
            return y + TableHeadH;
        }

        private static void RenderRows(FlowState flow, Definition def, List<Row> rows)
        {
            var g = flow.Current;
            var w = ColumnWidths(def);
            var fRow = F(7.9);
            var fBold = F(7.9, true);
            flow.Y = DrawTableHeader(g, flow.Y, def, w);

            var totalIdx = string.IsNullOrWhiteSpace(def.TotalColumn)
                ? -1
                : def.Columns.FindIndex(c => string.Equals(c.Header, def.TotalColumn, StringComparison.OrdinalIgnoreCase));
            var grand = 0m;
            string currentGroup = null;
            var zebra = false;

            for (var r = 0; r < rows.Count; r++)
            {
                var row = rows[r];

                // group header (kept with its first data row)
                if (!string.IsNullOrWhiteSpace(def.GroupBy)
                    && row.Cells.TryGetValue(def.GroupBy, out var gv))
                {
                    if (gv != currentGroup)
                    {
                        if (flow.Need(CatRowH + 20)) { flow.NextPage(); g = flow.Current; flow.Y = DrawTableHeader(g, flow.Y, def, w); }
                        g.DrawRectangle(CatB, new XRect(M, flow.Y, ContentW, CatRowH));
                        g.DrawRectangle(TealB, new XRect(M, flow.Y, 2.4, CatRowH));
                        g.DrawString(gv, F(8.4, true), NavyB, x: M + 7, y: flow.Y + 4.7);
                        flow.Y += CatRowH;
                        currentGroup = gv;
                        zebra = false;
                    }
                }

                // measure wrapped row
                var descIdx = def.Columns.FindIndex(c => !c.Numeric && c.Weight >= 1.4);
                var lines = 1;
                if (descIdx >= 0)
                {
                    var cell = ValueOf(row, AliasOf(def.Columns[descIdx]));
                    lines = Math.Max(1, WrapCell(g, fRow, cell, w[descIdx] - 8).Count);
                }
                var h = Math.Max(13.5, lines * RowH + 5);

                if (flow.Need(h + (totalIdx >= 0 ? 18 : 0))) { flow.NextPage(); g = flow.Current; flow.Y = DrawTableHeader(g, flow.Y, def, w); }
                if (zebra) g.DrawRectangle(AltB, new XRect(M, flow.Y, ContentW, h));

                double x = M;
                for (var i = 0; i < def.Columns.Count; i++)
                {
                    var col = def.Columns[i];
                    var want = AliasOf(col);
                    var raw = "";
                    foreach (var kv in row.Cells)
                    {
                        if (string.Equals(Norm(kv.Key), want, StringComparison.Ordinal))
                        { raw = kv.Value; break; }
                    }
                    if (col.Numeric && decimal.TryParse(raw, NumberStyles.Any, Inv, out var dv))
                    {
                        if (i == totalIdx) grand += dv;
                        g.DrawString(raw, col.Numeric ? fBold : fRow, BodyB,
                            new XRect(x, flow.Y + 3.6, w[i] - 5, 11), XStringFormats.TopRight);
                    }
                    else if (i == descIdx)
                    {
                        double yy = flow.Y + 3.4;
                        foreach (var l in WrapCell(g, fRow, raw, w[i] - 8))
                        { g.DrawString(l, fRow, BodyB, x: x + 4, y: yy); yy += RowH; }
                    }
                    else if (!string.IsNullOrWhiteSpace(raw))
                    {
                        var txt = raw;
                        while (txt.Length > 2 && g.MeasureString(txt, fRow).Width > w[i] - 8)
                            txt = txt.Substring(0, txt.Length - 1);
                        g.DrawString(txt, fRow, BodyB, x: x + 4, y: flow.Y + 3.4);
                    }
                    x += w[i];
                }
                g.DrawLine(HairPen, M, flow.Y + h, M + ContentW, flow.Y + h);
                flow.Y += h;
                zebra = !zebra;
            }

            // grand total strip
            if (totalIdx >= 0)
            {
                if (flow.Need(22)) { flow.NextPage(); g = flow.Current; flow.Y = DrawTableHeader(g, flow.Y, def, w); }
                g.DrawRectangle(HeadB, new XRect(M, flow.Y, ContentW, 16));
                g.DrawLine(NavyPen, M, flow.Y, M + ContentW, flow.Y);
                g.DrawLine(NavyPen, M, flow.Y + 16, M + ContentW, flow.Y + 16);
                g.DrawString("GRAND TOTAL  (" + rows.Count.ToString("N0", Inv) + " rows)", F(8.2, true), NavyB,
                    x: M + 5, y: flow.Y + 4.4);
                double x2 = M;
                for (var i = 0; i < def.Columns.Count; i++)
                {
                    if (i == totalIdx)
                        g.DrawString(grand.ToString("N2", Inv), F(8.6, true), NavyB,
                            new XRect(x2, flow.Y + 4, w[i] - 5, 11), XStringFormats.TopRight);
                    x2 += w[i];
                }
                flow.Y += 16;
            }

            flow.Y += 24;
            var f8 = F(8);
            flow.Current.DrawString("Issued By : " + flow.UserId, f8, BodyB, x: M, y: flow.Y);
            flow.Current.DrawString("Checked By : ____________________", f8, BodyB, x: M + 200, y: flow.Y);
            flow.Current.DrawString("Patient / Attendant Signature : ____________________", f8, BodyB,
                x: M + 340, y: flow.Y);
            flow.Y += 20;
        }

        /// <summary>Strips punctuation so "IPD NO." and "IPDNO" compare equal.</summary>
        private static string Norm(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (var ch in s)
                if (char.IsLetterOrDigit(ch)) sb.Append(char.ToUpperInvariant(ch));
            return sb.ToString();
        }

        /// <summary>SQL alias a column reads from (explicit Key, else the heading).</summary>
        private static string AliasOf(Column c) =>
            string.IsNullOrWhiteSpace(c.Key) ? Norm(c.Header) : Norm(c.Key);

        private static string ValueOf(Row row, string alias)
        {
            foreach (var kv in row.Cells)
                if (string.Equals(Norm(kv.Key), alias, StringComparison.Ordinal)) return kv.Value;
            return "";
        }

        private static List<string> WrapCell(XGraphics g, XFont f, string text, double maxWidth)
        {
            var res = new List<string>();
            text = (text ?? "").Trim();
            if (text.Length == 0) { res.Add(""); return res; }
            var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var cur = words[0];
            for (var i = 1; i < words.Length; i++)
            {
                var cand = cur + " " + words[i];
                if (g.MeasureString(cand, f).Width <= maxWidth) cur = cand;
                else { res.Add(cur); cur = words[i]; }
            }
            res.Add(cur);
            return res;
        }

        private static void StampFooters(List<XGraphics> canvases, Definition def, ReportContext ctx)
        {
            var total = canvases.Count;
            for (var i = 0; i < total; i++)
            {
                var g = canvases[i];
                var y = PageH - 40;
                g.DrawLine(HairPen, M, y, M + ContentW, y);
                g.DrawString(def.Title, F(7), MutedB, x: M, y: y + 6);
                g.DrawString(ctx.HospitalName ?? "", F(7), MutedB,
                    new XRect(M, y + 6, ContentW, 10), XStringFormats.TopCenter);
                g.DrawString("Page " + (i + 1) + " of " + total, F(7, true), MutedB,
                    new XRect(M, y + 6, ContentW, 10), XStringFormats.TopRight);
            }
            foreach (var c in canvases) c.Dispose();
        }
    }
}
