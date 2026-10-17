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
    /// IPD BILLING REPORTS - query + PDF generation service.
    ///
    /// Ports the two VFP billing report forms selected by the user from
    /// E:\Projects\HMC\billsys\FORMS:
    ///   * BS021040 "Running Bills - IPD Patient" -> IPD-AUDS (summary) / IPD-AUDD (detail)
    ///   * BS021001 "Patient Bills (Final)"       -> IPD-B-SMN (summary) / IPD-B-DT  (detail)
    /// Both are reached from the new Billing > Reports menu.
    ///
    /// Data source = the legacy tables (per user decision): REGISTER, IBIL_MST, IBIL_DET,
    /// IPDBILL, DEPT, COMPANY, CONSULT, PATIENT.  RESOURCE is empty in HMIS and EMPLOYEE /
    /// WARDS / BEDTRANS were not migrated, so those few header fields degrade to the raw
    /// legacy code stored on REGISTER.
    ///
    /// Business rules (identical to the VFP cursors)
    ///   * Running bill scope : REGISTER.TYPE='I' AND (BILLGEN=0 OR BILLGEN IS NULL)
    ///                          AND ISNULL(IBIL_DET.FLAG,'') &lt;&gt; 'P' AND not retired.
    ///   * Final bill scope   : IPDBILL rows whose REGISTER.TYPE='I' and ISNULL(IPDBILL.STAT,'')=''
    ///                          with lines = IBIL_MST where BILL = IPDBILL.BILLNO.
    ///   * TYPE='R' (return) lines are subtracted - qty and amount are negated.
    ///   * dept 01 split by rid: hospital laboratory = rid empty or 'HM', otherwise "other labs".
    ///   * dept 10 split by sign: medicine (>=0) and refund (<0).
    ///   * Amount due = charges - TRCVD + TPYMT; the final bill additionally subtracts the
    ///     cash discount when the file is settled and has no company.
    ///
    /// Side effects: none - read only.
    ///
    /// Developer: Tajwer Jalal | Date: 17-Oct-2026 | Time: 11:40
    /// </summary>
    public class IpdBillReportService
    {
        private readonly ApplicationDbContext _db;

        public IpdBillReportService(ApplicationDbContext db)
        {
            _db = db;
        }

        // ---------------------------------------------------------------- criteria / context

        /// <summary>Criteria shared by both IPD billing reports (mirrors the VFP form filters).</summary>
        public class ReportCriteria
        {
            public DateTime? From { get; set; }
            public DateTime? To { get; set; }
            public string Company { get; set; }
            public string Dept { get; set; }
            public string RegNo { get; set; }
            /// <summary>false = summary report (IPD-AUDS / IPD-B-SMN), true = detail report.</summary>
            public bool Detail { get; set; }
            public int StartPage { get; set; } = 1;
            public int EndPage { get; set; } = 9999;

            public string FromLabel => From.HasValue ? From.Value.ToString("dd/MM/yyyy") : "";
            public string ToLabel => To.HasValue ? To.Value.ToString("dd/MM/yyyy") : "";

            /// <summary>
            /// The VFP forms refused to run without at least one criterion; this page keeps
            /// that rule so an unfiltered run cannot print the whole inpatient book.
            /// </summary>
            public bool HasAnyCriterion =>
                From.HasValue || To.HasValue
                || !string.IsNullOrWhiteSpace(Company)
                || !string.IsNullOrWhiteSpace(Dept)
                || !string.IsNullOrWhiteSpace(RegNo);
        }

        /// <summary>Header context supplied by the page (VFP globals gccompany / gcloginid).</summary>
        public class ReportContext
        {
            public string HospitalName { get; set; } = "HMIS Hospital";
            public string HospitalNo { get; set; } = "";
            public string UserId { get; set; } = "";
        }

        // ---------------------------------------------------------------- row model

        /// <summary>One bill line plus the patient/register header it belongs to (the VFP cursor).</summary>
        private class BillRow
        {
            public string HosCode, Dept, DName, Code, Dsc, Rem, Type, CstId, CstName, Rid, ResoName;
            public string PCode, PName, Sex, Age, AType, Address, GName, GRelation, Diagnose;
            public string ECode, ERelation, RegConc, RegConn, CStat, ComId, ComName, BedEntitle, BedNo;
            public string EmpName, EmpDesg;
            public string BillNo, UserId;
            public DateTime? SlipDt, RDate, DDate, BDate;
            public string RTime, DTime;
            public decimal Qty, Rate, Namt, TRcvd, TPymt, CashDisc;
            public bool Settle;
            public int BillOrder;
        }

        // ================================================================ RUNNING BILL

        /// <summary>Runs the BS021040 query and returns the "IPD Running Bill" PDF bytes.</summary>
        public async Task<byte[]> RenderRunningBillAsync(ReportCriteria c, ReportContext ctx)
        {
            var rows = await QueryRunningRowsAsync(c);
            return BuildRunningBillPdf(rows, c, ctx);
        }

        private async Task<List<BillRow>> QueryRunningRowsAsync(ReportCriteria c)
        {
            var sb = new StringBuilder();
            sb.Append(@"
SELECT m.HOS_CODE, m.DEPT, dept.NAME AS DNAME, dept.BILLORDER, m.SLIPNO, m.SLIPDT, m.TYPE,
       d.CODE, d.[DESC] AS DSC, d.REM, d.RID, d.CONCODE AS CSTID,
       CASE WHEN m.TYPE='R' THEN d.QTY * -1 ELSE d.QTY END AS QTY,
       d.RATE,
       CASE WHEN m.TYPE='R' THEN d.NAMT * -1 ELSE d.NAMT END AS NAMT,
       r.RDATE, r.RTIME, r.DDATE, r.DTIME, r.PCODE, r.GRELATION, r.TRCVD, r.TPYMT, r.GNAME,
       r.DIAGNOSE, r.ECODE, r.ERELATION, r.CONCODE AS REGCONC, r.CSTAT, r.CID AS COMID,
       r.BEDENTITLE, r.BEDNO,
       emp.NAME AS EMPNAME, emp.DESG AS EMPDESG,
       p.NAME AS PNAME, p.SEX, p.AGE, p.ATYPE, p.ADDRESS,
       company.NAME AS COMNAME,
       con1.PREFIX, con1.NAME AS REGCONNAME,
       con2.PREFIX AS CPREFIX, con2.NAME AS CSTNAME,
       rs.NAME AS RESONAME
FROM IBIL_MST m
JOIN IBIL_DET d       ON (m.DEPT = d.DEPT AND m.SLIPNO = d.SLIPNO)
JOIN REGISTER r       ON (m.HOS_CODE = r.REGNO)
JOIN PATIENT p        ON (r.PCODE = p.CODE)
JOIN COMPANY company  ON (r.CID = company.ID)
LEFT JOIN EMPLOYEE emp ON (r.CID = emp.CID AND r.ECODE = emp.CODE)
LEFT JOIN CONSULT con1 ON (r.CONCODE = con1.ID)
LEFT JOIN CONSULT con2 ON (d.CONCODE = con2.ID)
LEFT JOIN RESOURCE rs  ON (d.RID = rs.Code)
JOIN DEPT dept         ON (m.DEPT = dept.ID)
WHERE r.TYPE = 'I'
  AND (r.BILLGEN = 0 OR r.BILLGEN IS NULL)
  AND ISNULL(d.FLAG, '') <> 'P'
  AND ISNULL(d.RETIRED, 0) = 0");

            var ps = new List<(string, object)>();
            AppendCriteria(sb, ps, c, "m.SLIPDT", "r.CID", "m.DEPT", "m.HOS_CODE");
            sb.Append(@"
ORDER BY m.HOS_CODE, dept.BILLORDER, d.CONCODE DESC, m.TYPE, m.SLIPDT, d.CODE");

            return await QueryRowsAsync(sb.ToString(), ps);
        }

        // ================================================================ FINAL BILL

        /// <summary>Runs the BS021001 query and returns the "IPD Bill (Final)" PDF bytes.</summary>
        public async Task<byte[]> RenderFinalBillAsync(ReportCriteria c, ReportContext ctx)
        {
            var rows = await QueryFinalRowsAsync(c);
            return BuildFinalBillPdf(rows, c, ctx);
        }

        private async Task<List<BillRow>> QueryFinalRowsAsync(ReportCriteria c)
        {
            var sb = new StringBuilder();
            sb.Append(@"
SELECT b.HOS_CODE, b.BILLNO, b.BDATE, b.USERID,
       m.DEPT, dept.NAME AS DNAME, dept.BILLORDER, m.SLIPNO, m.SLIPDT, m.TYPE,
       d.CODE, d.[DESC] AS DSC, d.REM, d.RID, d.CONCODE AS CSTID,
       CASE WHEN m.TYPE='R' THEN d.QTY * -1 ELSE d.QTY END AS QTY,
       d.RATE,
       CASE WHEN m.TYPE='R' THEN d.NAMT * -1 ELSE d.NAMT END AS NAMT,
       r.RDATE, r.RTIME, r.DDATE, r.DTIME, r.PCODE, r.GRELATION, r.TRCVD, r.TPYMT, r.GNAME,
       r.DIAGNOSE, r.ECODE, r.ERELATION, r.CONCODE AS REGCONC, r.CSTAT, r.SETTLE,
       r.CASHDISC, r.CID AS COMID, r.BEDENTITLE, r.BEDNO,
       emp.NAME AS EMPNAME, emp.DESG AS EMPDESG,
       p.NAME AS PNAME, p.SEX, p.AGE, p.ATYPE, p.ADDRESS,
       company.NAME AS COMNAME,
       con1.PREFIX, con1.NAME AS REGCONNAME,
       con2.PREFIX AS CPREFIX, con2.NAME AS CSTNAME,
       rs.NAME AS RESONAME
FROM IPDBILL b
JOIN REGISTER r       ON (b.HOS_CODE = r.REGNO)
JOIN IBIL_MST m        ON (m.BILL = b.BILLNO)
JOIN IBIL_DET d       ON (m.DEPT = d.DEPT AND m.SLIPNO = d.SLIPNO)
JOIN DEPT dept         ON (m.DEPT = dept.ID)
JOIN PATIENT p         ON (b.PCODE = p.CODE)
JOIN COMPANY company   ON (b.CID = company.ID)
LEFT JOIN EMPLOYEE emp ON (r.CID = emp.CID AND r.ECODE = emp.CODE)
LEFT JOIN CONSULT con1 ON (r.CONCODE = con1.ID)
LEFT JOIN CONSULT con2 ON (d.CONCODE = con2.ID)
LEFT JOIN RESOURCE rs  ON (d.RID = rs.Code)
WHERE r.TYPE = 'I'
  AND ISNULL(b.STAT, '') = ''
  AND ISNULL(d.FLAG, '') <> 'P'
  AND ISNULL(d.RETIRED, 0) = 0");

            var ps = new List<(string, object)>();
            AppendCriteria(sb, ps, c, "b.BDATE", "r.CID", "m.DEPT", "b.HOS_CODE");
            sb.Append(@"
ORDER BY b.HOS_CODE, b.BILLNO, dept.BILLORDER, d.CONCODE DESC, m.TYPE, m.SLIPDT, d.CODE");

            return await QueryRowsAsync(sb.ToString(), ps);
        }

        // ---------------------------------------------------------------- lookups for the criteria form

        /// <summary>Company list for the criteria drop-down (VFP cbocomp).</summary>
        public async Task<List<(string Id, string Name)>> GetCompaniesAsync()
        {
            var list = new List<(string, string)>();
            var conn = _db.Database.GetDbConnection();
            var opened = false;
            if (conn.State != ConnectionState.Open) { await conn.OpenAsync(); opened = true; }
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT ID, NAME FROM COMPANY WHERE ISNULL(ID,'') <> '' ORDER BY NAME";
                using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                    list.Add((S(rd, "ID"), S(rd, "NAME")));
            }
            finally { if (opened) await conn.CloseAsync(); }
            return list;
        }

        /// <summary>Department list for the criteria drop-down (VFP cbodept), in billing order.</summary>
        public async Task<List<(string Id, string Name)>> GetDepartmentsAsync()
        {
            var list = new List<(string, string)>();
            var conn = _db.Database.GetDbConnection();
            var opened = false;
            if (conn.State != ConnectionState.Open) { await conn.OpenAsync(); opened = true; }
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT ID, NAME FROM DEPT ORDER BY BILLORDER, NAME";
                using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                    list.Add((S(rd, "ID"), S(rd, "NAME")));
            }
            finally { if (opened) await conn.CloseAsync(); }
            return list;
        }

        // ---------------------------------------------------------------- shared SQL

        /// <summary>
        /// Appends the four optional VFP filters (date range / company / dept / reg no).
        /// Only filters the user actually supplied are applied.
        /// </summary>
        private static void AppendCriteria(StringBuilder sb, List<(string, object)> ps,
            ReportCriteria c, string dateCol, string companyCol, string deptCol, string regCol)
        {
            if (c.From.HasValue)
            {
                ps.Add(("@from", c.From.Value.Date));
                sb.Append("\n  AND ").Append(dateCol).Append(" >= @from");
            }
            if (c.To.HasValue)
            {
                ps.Add(("@to", c.To.Value.Date.AddDays(1).AddSeconds(-1)));
                sb.Append("\n  AND ").Append(dateCol).Append(" <= @to");
            }
            if (!string.IsNullOrWhiteSpace(c.Company))
            {
                ps.Add(("@cid", c.Company.Trim()));
                sb.Append("\n  AND ").Append(companyCol).Append(" = @cid");
            }
            if (!string.IsNullOrWhiteSpace(c.Dept))
            {
                ps.Add(("@dept", c.Dept.Trim()));
                sb.Append("\n  AND ").Append(deptCol).Append(" = @dept");
            }
            if (!string.IsNullOrWhiteSpace(c.RegNo))
            {
                ps.Add(("@hos", c.RegNo.Trim()));
                sb.Append("\n  AND ").Append(regCol).Append(" = @hos");
            }
        }

        private async Task<List<BillRow>> QueryRowsAsync(string sql, List<(string, object)> ps)
        {
            var rows = new List<BillRow>();
            var conn = _db.Database.GetDbConnection();
            var opened = false;
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
                opened = true;
            }
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.CommandTimeout = 300;
                foreach (var (name, val) in ps)
                {
                    var p = cmd.CreateParameter();
                    p.ParameterName = name;
                    p.Value = val ?? DBNull.Value;
                    cmd.Parameters.Add(p);
                }
                using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    rows.Add(new BillRow
                    {
                        HosCode = S(rd, "HOS_CODE"),
                        Dept = S(rd, "DEPT"),
                        DName = S(rd, "DNAME"),
                        BillOrder = I(rd, "BILLORDER"),
                        SlipDt = DT(rd, "SLIPDT"),
                        Type = S(rd, "TYPE"),
                        Code = S(rd, "CODE"),
                        Dsc = S(rd, "DSC"),
                        Rem = S(rd, "REM"),
                        Rid = S(rd, "RID"),
                        CstId = S(rd, "CSTID"),
                        Qty = M(rd, "QTY"),
                        Rate = M(rd, "RATE"),
                        Namt = M(rd, "NAMT"),
                        RDate = DT(rd, "RDATE"),
                        RTime = S(rd, "RTIME"),
                        DDate = DT(rd, "DDATE"),
                        DTime = S(rd, "DTIME"),
                        PCode = S(rd, "PCODE"),
                        GRelation = S(rd, "GRELATION"),
                        TRcvd = M(rd, "TRCVD"),
                        TPymt = M(rd, "TPYMT"),
                        GName = S(rd, "GNAME"),
                        Diagnose = S(rd, "DIAGNOSE"),
                        ECode = S(rd, "ECODE"),
                        ERelation = S(rd, "ERELATION"),
                        RegConc = S(rd, "REGCONC"),
                        CStat = S(rd, "CSTAT"),
                        ComId = S(rd, "COMID"),
                        BedEntitle = S(rd, "BEDENTITLE"),
                        BedNo = S(rd, "BEDNO"),
                        EmpName = S(rd, "EMPNAME"),
                        EmpDesg = S(rd, "EMPDESG"),
                        PName = S(rd, "PNAME"),
                        Sex = S(rd, "SEX"),
                        Age = S(rd, "AGE"),
                        AType = S(rd, "ATYPE"),
                        Address = S(rd, "ADDRESS"),
                        ComName = S(rd, "COMNAME"),
                        RegConn = Join(S(rd, "PREFIX"), S(rd, "REGCONNAME")),
                        CstName = Join(S(rd, "CPREFIX"), S(rd, "CSTNAME")),
                        ResoName = S(rd, "RESONAME"),
                        BillNo = S(rd, "BILLNO"),
                        UserId = S(rd, "USERID"),
                        BDate = DT(rd, "BDATE"),
                        CashDisc = M(rd, "CASHDISC"),
                        Settle = B(rd, "SETTLE"),
                    });
                }
            }
            finally
            {
                if (opened) await conn.CloseAsync();
            }
            return rows;
        }

        private static bool Has(DbDataReader rd, string col)
        {
            for (var i = 0; i < rd.FieldCount; i++)
                if (string.Equals(rd.GetName(i), col, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static string S(DbDataReader rd, string col)
        {
            if (!Has(rd, col)) return "";
            var v = rd[col];
            return v == DBNull.Value ? "" : (Convert.ToString(v, CultureInfo.InvariantCulture) ?? "").Trim();
        }

        private static string Join(string a, string b) => ((a + " " + b) ?? "").Trim();

        private static decimal M(DbDataReader rd, string col)
        {
            if (!Has(rd, col)) return 0m;
            var v = rd[col];
            return v == DBNull.Value ? 0m : Convert.ToDecimal(v, CultureInfo.InvariantCulture);
        }

        private static bool B(DbDataReader rd, string col)
        {
            if (!Has(rd, col)) return false;
            var v = rd[col];
            if (v == DBNull.Value) return false;
            try { return Convert.ToBoolean(v); } catch { return false; }
        }

        private static int I(DbDataReader rd, string col)
        {
            if (!Has(rd, col)) return 0;
            var v = rd[col];
            if (v == DBNull.Value) return 0;
            return int.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), out var n) ? n : 0;
        }

        private static DateTime? DT(DbDataReader rd, string col)
        {
            if (!Has(rd, col)) return null;
            var v = rd[col];
            if (v == DBNull.Value) return null;
            if (v is DateTime dt) return dt;
            return DateTime.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), out var d) ? d : (DateTime?)null;
        }

// ================================================================ layout theme
        //
        // Print-first enterprise hospital invoice: white page, navy headings, teal accent,
        // light grey panels and hairline rules.  Every fill is light enough to survive a
        // black-and-white print - hierarchy comes from weight and rules, not heavy colour.

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private static readonly XColor Navy = XColor.FromArgb(0x1F, 0x38, 0x64);   // headings
        private static readonly XColor Teal = XColor.FromArgb(0x0F, 0x76, 0x6E);   // accent
        private static readonly XColor Body = XColor.FromArgb(0x1A, 0x1F, 0x26);   // body text
        private static readonly XColor Muted = XColor.FromArgb(0x5B, 0x66, 0x73);  // labels
        private static readonly XColor Grid = XColor.FromArgb(0xC8, 0xD2, 0xDC);   // hairlines
        private static readonly XColor PanelBg = XColor.FromArgb(0xF6, 0xF8, 0xFA);
        private static readonly XColor HeadBg = XColor.FromArgb(0xE8, 0xEE, 0xF4);
        private static readonly XColor CatBg = XColor.FromArgb(0xDF, 0xEA, 0xEE);
        private static readonly XColor AltBg = XColor.FromArgb(0xF9, 0xFB, 0xFC);
        private static readonly XColor TotalBg = XColor.FromArgb(0xE2, 0xEB, 0xF4);

        private static readonly XBrush NavyB = new XSolidBrush(Navy);
        private static readonly XBrush TealB = new XSolidBrush(Teal);
        private static readonly XBrush BodyB = new XSolidBrush(Body);
        private static readonly XBrush MutedB = new XSolidBrush(Muted);
        private static readonly XBrush PanelB = new XSolidBrush(PanelBg);
        private static readonly XBrush HeadB = new XSolidBrush(HeadBg);
        private static readonly XBrush CatB = new XSolidBrush(CatBg);
        private static readonly XBrush AltB = new XSolidBrush(AltBg);
        private static readonly XBrush TotalB = new XSolidBrush(TotalBg);

        private static readonly XPen HairPen = new XPen(Grid, 0.5);
        private static readonly XPen TealPen = new XPen(Teal, 1.1);
        private static readonly XPen NavyPen = new XPen(Navy, 0.8);

        // ---- page geometry (A4 portrait) ------------------------------------------
        private const double PageW = 595.28;
        private const double PageH = 841.89;
        private const double MG = 34;              // left / right margin
        private const double TopY = 30;           // first content baseline block
        private const double BottomReserve = 58;  // footer strip kept clear of content
        private const double ContentW = PageW - (MG * 2);
        private const double RowH = 9.6;          // one text line inside a table row

        // ---- cell metrics ---------------------------------------------------------
        // Every row is a cell: the text is inset from the cell edges by PadTop/PadBottom
        // vertically and PadX horizontally, and the cell border is stroked exactly on the
        // cell edge.  That is what keeps a rule from ever running through the glyphs.
        private const double PadTop = 3.5;        // clearance above the text
        private const double PadBottom = 4.0;     // clearance below the text
        private const double PadX = 4.0;          // clearance left/right of the text
        private const double RowPad = PadTop + PadBottom;   // total vertical inset
        private const double CellFont = 8.2;      // ONE size for every cell in a row, so
                                                   // all cells share a baseline
        private const double AccentW = 3.0;       // width of the group-band accent bar
        /// <summary>
        /// PDFsharp anchors DrawString on the top of the line box; Arial's cap top
        /// sits about 1.7pt below that at 8.2pt.  Text is therefore placed at
        /// y + PadTop + CapGap so the CAP TOP clears the cell edge by PadTop.
        /// </summary>
        private const double CapGap = 1.7;

        // ---- itemised table columns (sum to ContentW) ------------------------------
        private const double CCode = 52;
        private const double CDesc = 206;
        private const double CRem = 100;
        private const double CQty = 42;
        private const double CRate = 60;
        private const double CAmt = ContentW - (CCode + CDesc + CRem + CQty + CRate);

        private static readonly double XCode = MG;
        private static readonly double XDesc = MG + CCode;
        private static readonly double XRem = MG + CCode + CDesc;
        private static readonly double XQty = MG + CCode + CDesc + CRem;
        private static readonly double XRate = MG + CCode + CDesc + CRem + CQty;
        private static readonly double XAmt = MG + CCode + CDesc + CRem + CQty + CRate;

        // fixed row heights
        private const double TableHeadH = 18;
        private const double CategoryRowH = CellFont + RowPad;
        // informational note row and subtotal row: same cell padding as an item row
        private const double NoteRowH = CellFont + RowPad;
        private const double SubtotalRowH = CellFont + RowPad;
        private const double SignatureH = 40;

        private static XFont F(double size, bool bold = false) =>
            new XFont("Arial", size, bold ? XFontStyle.Bold : XFontStyle.Regular);


        // ================================================================ text measuring

        /// <summary>
        /// Splits text into lines that each fit inside <paramref name="maxWidth"/>.
        /// Wrapping - rather than truncating - is what stops a long service description,
        /// address or diagnosis from colliding with the numeric columns.
        /// </summary>
        private static List<string> Wrap(XGraphics g, XFont font, string text, double maxWidth)
        {
            var lines = new List<string>();
            text = (text ?? "").Replace("\r", " ").Trim();
            if (text.Length == 0) { lines.Add(""); return lines; }

            foreach (var hard in text.Split('\n'))
            {
                var words = hard.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 0) { lines.Add(""); continue; }

                var cur = words[0];
                for (var i = 1; i < words.Length; i++)
                {
                    var candidate = cur + " " + words[i];
                    if (g.MeasureString(candidate, font).Width <= maxWidth) cur = candidate;
                    else { lines.Add(cur); cur = words[i]; }
                }
                lines.Add(cur);
            }
            return lines;
        }

        private static double LinesH(List<string> lines, double lineH = RowH) =>
            Math.Max(1, lines.Count) * lineH;

        /// <summary>
        /// Shortens text with a trailing ".." until it fits <paramref name="maxWidth"/>.
        /// Used for the single-line cells (item code, category heading, subtotal caption)
        /// where wrapping is not an option and an overrun would collide with the next column.
        /// Wrapped cells never need this.
        /// </summary>
        private static string Fit(XGraphics g, XFont font, string text, double maxWidth)
        {
            text ??= "";
            if (maxWidth <= 0 || g.MeasureString(text, font).Width <= maxWidth) return text;
            var t = text;
            while (t.Length > 2 && g.MeasureString(t + "..", font).Width > maxWidth)
                t = t.Substring(0, t.Length - 1);
            return t + "..";
        }

        /// <summary>Draws wrapped lines top-down; returns the y just past the last line.</summary>
        private static double DrawWrapped(XGraphics g, List<string> lines, double x, double y,
                                          XFont font, XBrush brush, double lineH = RowH)
        {
            foreach (var line in lines)
            {
                g.DrawString(line, font, brush, x: x, y: y);
                y += lineH;
            }
            return y;
        }

        /// <summary>Alternating-row state shared across a whole document.</summary>
        private class Zebra
        {
            private bool _on;
            public bool Next() { var v = _on; _on = !_on; return v; }
        }

        // ================================================================ page flow

        /// <summary>
        /// Owns the current graphics object and write cursor, and starts a continuation page
        /// (re-drawing a compact header plus the column headings) when the next block would not
        /// fit above the footer strip.
        /// </summary>
        private class Flow
        {
            private readonly PdfDocument _doc;
            private readonly Action<Flow> _drawHeader;
            private readonly List<XGraphics> _canvases;
            private XGraphics _g;

            public int PageNo { get; private set; }
            public int EndPage { get; }
            public double Y { get; set; }
            public XGraphics G => _g;
            public bool PageLimitReached => PageNo >= EndPage;

            public Flow(PdfDocument doc, int pageNo, int endPage, Action<Flow> drawHeader,
                        List<XGraphics> canvases)
            {
                _doc = doc;
                PageNo = pageNo;
                EndPage = endPage < pageNo ? pageNo : endPage;
                _drawHeader = drawHeader;
                _canvases = canvases;
                _g = NewCanvas();
                Y = TopY;
                _drawHeader(this);
            }

            private XGraphics NewCanvas()
            {
                var p = _doc.AddPage();
                p.Width = PageW;
                p.Height = PageH;
                var canvas = XGraphics.FromPdfPage(p);
                // the canvas is kept open so a later pass can stamp the page footer;
                // XGraphics flushes its content on Dispose
                _canvases.Add(canvas);
                return canvas;
            }

            /// <summary>True when the requested points would overflow the printable text area.</summary>
            public bool Need(double needed) => Y + needed > PageH - BottomReserve;

            public void NextPage()
            {
                _g = NewCanvas();
                PageNo++;
                Y = TopY;
                _drawHeader(this);
            }
        }

        private static byte[] EmptyPdf(string message)
        {
            HmisFontResolver.Install();
            using var ms = new MemoryStream();
            using var doc = new PdfDocument();
            var page = doc.AddPage();
            page.Width = PageW;
            page.Height = PageH;
            using var g = XGraphics.FromPdfPage(page);
            g.DrawString(message, F(12, true), NavyB, new XRect(0, PageH / 2 - 12, PageW, 18), XStringFormats.TopCenter);
            g.DrawString("No records matched the selected criteria.",
                F(9), MutedB, new XRect(0, PageH / 2 + 8, PageW, 14), XStringFormats.TopCenter);
            doc.Save(ms, false);
            return ms.ToArray();
        }

        // ================================================================ masthead

        /// <summary>
        /// Full first-page masthead: hospital identity, a title band carrying the report title
        /// and the document reference, and an admission meta row.  Only values that exist in the
        /// data are printed - nothing is invented.  Returns the y just below the header rule.
        /// </summary>
        private static double DrawMasthead(XGraphics g, ReportContext ctx, string title, BillRow p,
                                           ReportCriteria c, int pageNo)
        {
            double y = TopY;

            g.DrawString(ctx.HospitalName ?? "", F(15, true), NavyB,
                new XRect(0, y, PageW, 20), XStringFormats.TopCenter);
            y += 19;

            if (!string.IsNullOrWhiteSpace(ctx.HospitalNo))
            {
                g.DrawString(ctx.HospitalNo, F(8), MutedB, new XRect(0, y, PageW, 11), XStringFormats.TopCenter);
                y += 12;
            }
            y += 5;

            const double bandH = 26;
            g.DrawRectangle(HeadB, new XRect(MG, y, ContentW, bandH));
            g.DrawRectangle(TealB, new XRect(MG, y, 3.2, bandH));
            g.DrawString(title, F(12, true), NavyB, x: MG + 12, y: y + 7.5);

            var isBill = !string.IsNullOrWhiteSpace(p.BillNo);
            g.DrawString(isBill ? "BILL NO." : "IPD NO.", F(7), MutedB,
                new XRect(MG + ContentW - 210, y + 5, 60, 9), XStringFormats.TopRight);
            g.DrawString(isBill ? p.BillNo : p.HosCode, F(9, true), NavyB,
                new XRect(MG + ContentW - 145, y + 3.5, 135, 12), XStringFormats.TopRight);
            g.DrawString("Page " + pageNo, F(7), MutedB,
                new XRect(MG + ContentW - 210, y + 15, 200, 9), XStringFormats.TopRight);
            // 8pt of air between the title band and the Admission line so the
            // text never appears to touch the band.
            y += bandH + 8;

            var fMeta = F(8.2);
            var adm = p.RDate.HasValue
                ? "Admission : " + p.RDate.Value.ToString("dd MMM yyyy", Inv) +
                  (string.IsNullOrWhiteSpace(p.RTime) ? "" : "  " + p.RTime.Trim())
                : "";
            var rightMeta = new List<string>();
            if (p.BDate.HasValue) rightMeta.Add("Bill Date : " + p.BDate.Value.ToString("dd MMM yyyy", Inv));
            if (c.From.HasValue || c.To.HasValue)
                rightMeta.Add("Period : " + c.FromLabel + (c.ToLabel.Length > 0 ? " to " + c.ToLabel : ""));

            if (adm.Length > 0)
                g.DrawString(adm, fMeta, BodyB, x: MG, y: y);
            if (rightMeta.Count > 0)
                g.DrawString(string.Join("        ", rightMeta), fMeta, BodyB,
                    new XRect(MG, y, ContentW, 12), XStringFormats.TopRight);
            // 18pt of air between the Admission line and the teal divider, then 9pt
            // before the panel - previously 14pt/9pt which read as cramped.
            y += 18;

            g.DrawLine(TealPen, MG, y, MG + ContentW, y);
            return y + 11;
        }

        /// <summary>Compact header repeated on every continuation page. Returns its height.</summary>
        private static double DrawContinuationHeader(XGraphics g, ReportContext ctx, string title,
                                                     BillRow p, int pageNo)
        {
            double y = TopY;
            g.DrawString(ctx.HospitalName ?? "", F(10, true), NavyB, x: MG, y: y);
            g.DrawString("Page " + pageNo, F(8), MutedB, new XRect(MG, y, ContentW, 12), XStringFormats.TopRight);
            y += 13;

            var bits = new List<string>();
            bits.Add(string.IsNullOrWhiteSpace(p.BillNo) ? "IPD No. " + p.HosCode : "Bill No. " + p.BillNo);
            if (!string.IsNullOrWhiteSpace(p.PCode)) bits.Add("MR No. " + p.PCode);
            if (!string.IsNullOrWhiteSpace(p.PName)) bits.Add(p.PName);
            bits.Add(title);
            g.DrawString(string.Join("   |   ", bits), F(7.4), MutedB, x: MG, y: y);
            y += 11;
            g.DrawLine(TealPen, MG, y, MG + ContentW, y);
            return y - TopY + 8;
        }

        // ================================================================ patient panel

        /// <summary>
        /// Three-column identity panel.  Labels sit above their values and every value is
        /// word-wrapped to its column width, so long names, addresses and diagnoses can never
        /// overlap the neighbouring column.  Fields with no value are omitted entirely.
        /// Returns the y just below the panel.
        /// </summary>
        private static double DrawInfoPanel(XGraphics g, double y, BillRow p, bool includeStatus)
        {
            var cols = new List<List<KeyValuePair<string, string>>>
            {
                Prune(new List<KeyValuePair<string, string>>
                {
                    F2("Patient Name", p.PName),
                    F2("MR Number", p.PCode),
                    F2("Age / Sex", (p.Age + "  " + ATypeWord(p.AType) +
                        (string.IsNullOrWhiteSpace(p.Sex) ? "" : "  /  " + p.Sex.Trim())).Trim()),
                    F2("Company / Panel", ((p.ComId + " " + p.ComName) ?? "").Trim()),
                }),
                Prune(new List<KeyValuePair<string, string>>
                {
                    F2("Employee Name", p.EmpName),
                    F2("Employee ID", p.ECode),
                    F2("Relation", RelationWord(p.ERelation)),
                    F2("Designation", p.EmpDesg),
                }),
                Prune(new List<KeyValuePair<string, string>>
                {
                    F2("Consultant", p.RegConc),
                    F2("Bed No.", p.BedNo),
                    F2("Address", p.Address),
                    F2("Diagnosis", p.Diagnose),
                }),
            };
            if (includeStatus)
                cols[2].Add(F2("Status", StatusWord(p.CStat)));

            const double pad = 10;
            const double gap = 12;
            var colW = (ContentW - (pad * 2) - (gap * 2)) / 3d;
            var fLab = F(7.2, true);
            var fVal = F(8.6);
            const double labH = 9.4;

            // measure first so the panel can be sized to its tallest column
            var measured = new List<List<KeyValuePair<string, List<string>>>>();
            var heights = new List<double>();
            foreach (var col in cols)
            {
                var items = new List<KeyValuePair<string, List<string>>>();
                double h = 0;
                foreach (var kv in col)
                {
                    var lines = Wrap(g, fVal, kv.Value, colW);
                    items.Add(new KeyValuePair<string, List<string>>(kv.Key, lines));
                    h += labH + LinesH(lines) + 2;
                }
                measured.Add(items);
                heights.Add(h);
            }

            var panelH = (heights.Count > 0 ? heights.Max() : 0) + pad;
            var panelRect = new XRect(MG, y, ContentW, panelH);
            g.DrawRectangle(PanelB, panelRect);
            g.DrawRectangle(HairPen, panelRect);

            for (var ci = 0; ci < measured.Count; ci++)
            {
                var x = MG + pad + (ci * (colW + gap));
                double yy = y + pad;
                foreach (var item in measured[ci])
                {
                    g.DrawString(item.Key.ToUpperInvariant(), fLab, MutedB, x: x, y: yy);
                    yy += labH;
                    yy = DrawWrapped(g, item.Value, x, yy, fVal, BodyB);
                    yy += 2;
                }
                if (ci > 0)
                    g.DrawLine(HairPen, x - gap / 2, y + 4, x - gap / 2, y + panelH - 4);
            }
            return y + panelH;
        }

        private static KeyValuePair<string, string> F2(string label, string value) =>
            new KeyValuePair<string, string>(label, value ?? "");

        private static List<KeyValuePair<string, string>> Prune(List<KeyValuePair<string, string>> src)
        {
            var res = new List<KeyValuePair<string, string>>();
            foreach (var kv in src)
                if (!string.IsNullOrWhiteSpace(kv.Value)) res.Add(kv);
            return res;
        }

        // ================================================================ table chrome

        private static double DrawTableHeader(XGraphics g, double y)
        {
            // Header is itself a cell row: rules on the cell edges, text inset by PadTop.
            g.DrawRectangle(HeadB, new XRect(MG, y, ContentW, TableHeadH));
            g.DrawLine(NavyPen, MG, y, MG + ContentW, y);                       // top edge
            g.DrawLine(NavyPen, MG, y + TableHeadH, MG + ContentW, y + TableHeadH); // bottom edge

            var f = F(CellFont, true);
            // every heading is placed on the same line, whatever its column alignment,
            // and its cap top clears the band rule by PadTop.
            var ty = y + PadTop + CapGap;
            g.DrawString("CODE", f, NavyB, new XRect(XCode + PadX, ty, CCode - (PadX * 2), CellFont + 2),
                         XStringFormats.TopLeft);
            g.DrawString("SERVICE / ITEM DESCRIPTION", f, NavyB,
                         new XRect(XDesc + PadX, ty, CDesc - (PadX * 2), CellFont + 2), XStringFormats.TopLeft);
            g.DrawString("CONSULTANT / REMARKS", f, NavyB,
                         new XRect(XRem + PadX, ty, CRem - (PadX * 2), CellFont + 2), XStringFormats.TopLeft);
            g.DrawString("QTY", f, NavyB, new XRect(XQty + PadX, ty, CQty - (PadX * 2), CellFont + 2),
                         XStringFormats.TopRight);
            g.DrawString("RATE", f, NavyB, new XRect(XRate + PadX, ty, CRate - (PadX * 2), CellFont + 2),
                         XStringFormats.TopRight);
            g.DrawString("AMOUNT", f, NavyB, new XRect(XAmt + PadX, ty, CAmt - (PadX * 2), CellFont + 2),
                         XStringFormats.TopRight);
            return y + TableHeadH;
        }

        // ================================================================ table rows

        private static double CategoryBandHeight() =>
            CellFont + RowPad;   // auto height: one text line + the standard cell padding

        /// <summary>
        /// Department / group band.  The teal accent is drawn as its OWN narrow leading
        /// column (MG .. MG+AccentW) and the band fill starts after it, so the bar can
        /// never overlap the caption regardless of caption length.  The hairline is on
        /// the cell's bottom edge, PadBottom clear of the glyphs.
        /// </summary>
        private static void DrawCategoryRow(XGraphics g, double y, string category)
        {
            var h = CategoryBandHeight();
            g.DrawRectangle(CatB, new XRect(MG + AccentW, y, ContentW - AccentW, h));
            g.DrawRectangle(TealB, new XRect(MG, y, AccentW, h));
            g.DrawLine(HairPen, MG, y + h, MG + ContentW, y + h);
            g.DrawString(Fit(g, F(CellFont, true), category, ContentW - AccentW - (PadX * 2)),
                         F(CellFont, true), NavyB,
                         new XRect(MG + AccentW + PadX, y + PadTop + CapGap,
                                   ContentW - AccentW - (PadX * 2), CellFont + 2),
                         XStringFormats.TopLeft);
        }


        /// <summary>Auto height: wrapped content plus the standard cell padding.</summary>
        private static double ItemRowHeight(List<string> desc, List<string> rem) =>
            Math.Max(LinesH(desc), LinesH(rem)) + RowPad;

        /// <summary>
        /// One billable line.  Every cell uses CellFont and the same text top
        /// (rowTop + PadTop), so CODE / DESCRIPTION / QTY / RATE / AMOUNT share one
        /// baseline.  Zebra fill and hairline are both stroked on the cell rectangle so
        /// no rule can pass through the text.
        /// </summary>
        private static void DrawItemRow(XGraphics g, double y, double h, string code,
                                        List<string> desc, List<string> rem,
                                        decimal qty, decimal rate, decimal amount, bool shaded)
        {
            if (shaded) g.DrawRectangle(AltB, new XRect(MG, y, ContentW, h));

            var f = F(CellFont);
            var ty = y + PadTop + CapGap;         // ONE text top for every cell in the row
            var boxH = h - RowPad;               // usable height inside the padding

            // same font instance as every other cell in the row, so all cells share one
            // baseline (a bold code shifted it ~1.2pt against the regular description)
            g.DrawString(Fit(g, f, code, CCode - (PadX * 2)), f, NavyB,
                         new XRect(XCode + PadX, ty, CCode - (PadX * 2), boxH), XStringFormats.TopLeft);

            double dy = ty;
            foreach (var line in desc)
            {
                g.DrawString(line, f, BodyB,
                             new XRect(XDesc + PadX, dy, CDesc - (PadX * 2), boxH), XStringFormats.TopLeft);
                dy += RowH;
            }
            double ry = ty;
            foreach (var line in rem)
            {
                g.DrawString(line, f, BodyB,
                             new XRect(XRem + PadX, ry, CRem - (PadX * 2), boxH), XStringFormats.TopLeft);
                ry += RowH;
            }

            g.DrawString(Num(qty), f, BodyB,
                         new XRect(XQty + PadX, ty, CQty - (PadX * 2), boxH), XStringFormats.TopRight);
            g.DrawString(rate.ToString("N2", Inv), f, BodyB,
                         new XRect(XRate + PadX, ty, CRate - (PadX * 2), boxH), XStringFormats.TopRight);
            g.DrawString(amount.ToString("N2", Inv), f, BodyB,
                         new XRect(XAmt + PadX, ty, CAmt - (PadX * 2), boxH), XStringFormats.TopRight);

            g.DrawLine(HairPen, MG, y + h, MG + ContentW, y + h);
        }

        /// <summary>Informational split line (laboratory / pharmacy) - not a charge of its own.</summary>
        /// <summary>
        /// Informational split line (laboratory / pharmacy).  Not a charge of its own:
        /// the label and the figure sit on one baseline with the standard cell padding,
        /// and the hairline is on the cell's bottom edge.
        /// </summary>
        private static void DrawNoteRow(XGraphics g, double y, string label, decimal amount)
        {
            var f = F(CellFont);
            var ty = y + PadTop + CapGap;
            var boxH = NoteRowH - RowPad;
            g.DrawString(Fit(g, f, label, XAmt - XDesc - (PadX * 2)), f, MutedB,
                         new XRect(XDesc + PadX, ty, XAmt - XDesc - (PadX * 2), boxH), XStringFormats.TopLeft);
            g.DrawString(amount.ToString("N2", Inv), f, MutedB,
                         new XRect(XAmt + PadX, ty, CAmt - (PadX * 2), boxH), XStringFormats.TopRight);
            g.DrawLine(HairPen, MG, y + NoteRowH, MG + ContentW, y + NoteRowH);
        }

        /// <summary>
        /// Category subtotal.  Own top rule (the cell's top edge) and auto height, with
        /// PadTop of clearance above the text so the rule cannot cut it.  Label and
        /// amount use the SAME font size and the SAME text top, so they share a baseline.
        /// </summary>
        private static void DrawSubtotalRow(XGraphics g, double y, string label, decimal amount)
        {
            var f = F(CellFont, true);
            g.DrawRectangle(PanelB, new XRect(MG, y, ContentW, SubtotalRowH));
            g.DrawLine(NavyPen, MG, y, MG + ContentW, y);                       // cell top edge
            g.DrawLine(NavyPen, MG, y + SubtotalRowH, MG + ContentW, y + SubtotalRowH); // bottom edge
            var ty = y + PadTop + CapGap;
            var boxH = SubtotalRowH - RowPad;
            g.DrawString(Fit(g, f, label, XAmt - XDesc - (PadX * 2)), f, NavyB,
                         new XRect(XDesc + PadX, ty, XAmt - XDesc - (PadX * 2), boxH), XStringFormats.TopLeft);
            g.DrawString(amount.ToString("N2", Inv), f, NavyB,
                         new XRect(XAmt + PadX, ty, CAmt - (PadX * 2), boxH), XStringFormats.TopRight);
        }

        private static void DrawConsultantRow(XGraphics g, double y, double h, string name)
        {
            g.DrawRectangle(PanelB, new XRect(XDesc, y, CDesc + CRem, h));
            DrawWrapped(g, Wrap(g, F(8, true), name, CDesc + CRem - 12), XDesc + 6, y + 2.4, F(8, true), TealB, 10.4);
            g.DrawLine(HairPen, XDesc, y + h, XDesc + CDesc + CRem, y + h);
        }

        // ================================================================ financial summary

        private const double PanelW = 320;
        private static readonly double PanelX = MG + ContentW - PanelW;
        private const double SumHeadH = 17;
        private const double SumRowH = 17.0;

        /// <summary>
        /// Draws the right-aligned "Bill Summary" panel and returns its total height.
        /// The labels and figures are the legacy calculation unchanged - only the layout moved.
        /// </summary>
        private static double DrawSummaryPanel(XGraphics g, double y, BillRow p, decimal charges,
                                               decimal discount, bool showDiscount, string wordsLine)
        {
            var bodyRows = 3 + (showDiscount ? 1 : 0) + 2;
            var wordsH = string.IsNullOrEmpty(wordsLine) ? 0 : 14;
            var panelH = SumHeadH + (bodyRows * SumRowH) + wordsH + 12;

            g.DrawRectangle(XBrushes.White, new XRect(PanelX, y, PanelW, panelH));
            g.DrawRectangle(NavyB, new XRect(PanelX, y, PanelW, SumHeadH));
            g.DrawString("BILL SUMMARY", F(8.2, true), XBrushes.White, x: PanelX + 10, y: y + 4.9);

            double yy = y + SumHeadH;

            // label and figure share one text top; the hairline sits on the row's
            // bottom edge with SumRowH tall enough to clear the glyphs underneath it.
            void Row(string label, decimal value)
            {
                var fRow = F(8.4);
                var ty = yy + 5;
                g.DrawString(label, fRow, BodyB, new XRect(PanelX + 10, ty, PanelW - 20, 11), XStringFormats.TopLeft);
                g.DrawString(value.ToString("N2", Inv), fRow, BodyB,
                    new XRect(PanelX, ty, PanelW - 10, 11), XStringFormats.TopRight);
                yy += SumRowH;
                g.DrawLine(HairPen, PanelX + 10, yy, PanelX + PanelW - 10, yy);
            }

            Row("Total Charges", charges);
            Row("Amount Deposited", p.TRcvd);
            Row("Amount Refunded", p.TPymt);
            if (showDiscount) Row("Discount / Adjustment", discount);
            Row("Net Amount Deposited", p.TRcvd - p.TPymt);

            // final payable / refund, visually prominent
            g.DrawRectangle(TotalB, new XRect(PanelX + 1, yy + 1, PanelW - 2, SumRowH));
            g.DrawRectangle(TealB, new XRect(PanelX + 1, yy + 1, 3.2, SumRowH));
            var balance = charges - p.TRcvd + p.TPymt - discount;
            g.DrawString(balance < 0 ? "Refund Amount" : "Outstanding Balance", F(9, true), NavyB,
                x: PanelX + 10, y: yy + 3.0);
            g.DrawString(balance.ToString("N2", Inv), F(10, true), NavyB,
                new XRect(PanelX, yy + 3.0, PanelW - 10, 12), XStringFormats.TopRight);
            yy += SumRowH;

            if (wordsH > 0)
            {
                g.DrawString(wordsLine, F(7.3), MutedB, x: PanelX + 10, y: yy + 3.2);
                yy += wordsH;
            }

            g.DrawRectangle(NavyPen, new XRect(PanelX, y, PanelW, yy - y + 4));
            return yy + 4 - y;
        }

        // ================================================================ signatures

        /// <summary>
        /// Signature strip with real writing space.  Only roles the legacy report carries are
        /// shown (Issued By, Checked / Verified By) plus the patient / attendant line.
        /// </summary>
        private static void DrawSignatures(XGraphics g, double y, ReportContext ctx, BillRow p)
        {
            const double colGap = 20;
            var colW = (ContentW - (colGap * 2)) / 3d;
            const double ruleY = 26;   // writing space above the rule

            var issuedBy = !string.IsNullOrWhiteSpace(ctx.UserId)
                ? ctx.UserId
                : (p.UserId ?? "");
            var labels = new[] { "Issued By", "Checked / Verified By", "Patient / Attendant Signature" };
            var values = new[] { issuedBy, "", "" };

            for (var i = 0; i < labels.Length; i++)
            {
                var x = MG + (i * (colW + colGap));
                var value = values[i];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var lines = Wrap(g, F(8.2, true), value, colW);
                    double vy = y + ruleY - (lines.Count * 9) - 3;
                    DrawWrapped(g, lines, x, vy, F(8.2, true), BodyB, 9);
                }
                g.DrawLine(HairPen, x, y + ruleY, x + colW, y + ruleY);
                g.DrawString(labels[i], F(7.4), MutedB, x: x, y: y + ruleY + 9);
            }
        }

        // ================================================================ page footer

        /// <summary>
        /// Second pass: stamps a slim footer and "Page X of Y" on every page once the total
        /// page count is known.  PdfSharpCore cannot reopen a finished page, so the per-page
        /// canvases are deliberately held open by <see cref="Flow"/> until this point.
        /// </summary>
        private static void StampFooters(List<XGraphics> canvases, ReportContext ctx, string title)
        {
            var total = canvases.Count;
            for (var i = 0; i < total; i++)
            {
                var g = canvases[i];
                var y = PageH - 40;
                g.DrawLine(HairPen, MG, y, MG + ContentW, y);
                g.DrawString(title, F(7), MutedB, x: MG, y: y + 6);
                g.DrawString(ctx.HospitalName ?? "", F(7), MutedB,
                    new XRect(MG, y + 6, ContentW, 10), XStringFormats.TopCenter);
                g.DrawString("Page " + (i + 1) + " of " + total, F(7, true), MutedB,
                    new XRect(MG, y + 6, ContentW, 10), XStringFormats.TopRight);
            }
            foreach (var c in canvases) c.Dispose();
        }

        // ================================================================ running bill pdf

        private byte[] BuildRunningBillPdf(List<BillRow> rows, ReportCriteria c, ReportContext ctx)
        {
            HmisFontResolver.Install();
            if (rows.Count == 0) return EmptyPdf("IPD Running Bill");

            var title = "IPD RUNNING BILL" + (c.Detail ? "  (DETAIL)" : "");
            using var ms = new MemoryStream();
            using var doc = new PdfDocument();
            var zebra = new Zebra();
            var canvases = new List<XGraphics>();

            foreach (var pat in rows.GroupBy(r => r.HosCode))
            {
                var p0 = pat.First();
                var flow = new Flow(doc, c.StartPage, c.EndPage, f =>
                {
                    double y;
                    if (f.PageNo == c.StartPage)
                    {
                        y = DrawMasthead(f.G, ctx, title, p0, c, f.PageNo);
                        y = DrawInfoPanel(f.G, y, p0, false) + 11;
                    }
                    else
                    {
                        y = TopY + DrawContinuationHeader(f.G, ctx, title, p0, f.PageNo) + 6;
                    }
                    f.Y = DrawTableHeader(f.G, y);
                }, canvases);

                var charges = 0m;
                foreach (var dg in pat.GroupBy(r => new { r.BillOrder, r.Dept })
                                       .OrderBy(x => x.Key.BillOrder))
                {
                    if (flow.PageLimitReached) break;
                    charges += RenderCategory(flow, dg.ToList(), c.Detail, dg.Key.Dept, zebra);
                }

                if (!flow.PageLimitReached)
                {
                    flow.Y += 16;
                    var summaryH = SumHeadH + (4 * SumRowH) + 12;
                    if (flow.Need(summaryH + SignatureH + 6)) flow.NextPage();
                    var h = DrawSummaryPanel(flow.G, flow.Y, p0, charges, 0m, false, null);
                    flow.Y += h + 18;
                    DrawSignatures(flow.G, flow.Y, ctx, p0);
                    flow.Y += SignatureH;
                }
            }

            StampFooters(canvases, ctx, title);
            doc.Save(ms, false);
            return ms.ToArray();
        }

        /// <summary>
        /// Emits one category (department) block: heading, item rows, the legacy lab / pharmacy
        /// split notes and the category subtotal.  Returns the category subtotal.
        /// </summary>
        private static decimal RenderCategory(Flow flow, List<BillRow> dRows, bool detail,
                                              string dept, Zebra zebra)
        {
            var deptName = dRows[0].DName;
            var isLab = dept == "01";
            var isPharm = dept == "10";
            var showConsult = dept == "11" || dept == "06";

            var fItem = F(8.2);
            var subtotal = dRows.Sum(r => r.Namt);

            // NB: the canvas must be resolved from flow.G at draw time - a page break
            // replaces it, and a captured XGraphics would keep drawing on the old page.
            if (flow.Need(CategoryRowH + 26)) flow.NextPage();
            DrawCategoryRow(flow.G, flow.Y, (deptName ?? dept).ToUpperInvariant());
            flow.Y += CategoryRowH;

            void Emit(BillRow sample, decimal qty, decimal amt, List<string> desc, List<string> rem)
            {
                var h = ItemRowHeight(desc, rem);
                if (flow.Need(h)) flow.NextPage();
                var rate = qty != 0m ? Math.Round(amt / qty, 2) : sample.Rate;
                DrawItemRow(flow.G, flow.Y, h, sample.Code, desc, rem, qty, rate, amt, zebra.Next());
                flow.Y += h;
            }

            if (detail)
            {
                foreach (var r in dRows)
                {
                    Emit(r, r.Qty, r.Namt,
                         Wrap(flow.G, fItem, r.Dsc, CDesc - 12),
                         Wrap(flow.G, fItem, !string.IsNullOrWhiteSpace(r.Rem) ? r.Rem : r.CstName, CRem - 12));
                }
            }
            else
            {
                foreach (var cg in dRows.GroupBy(r => r.CstId ?? "").OrderByDescending(x => x.Key))
                {
                    if (showConsult && !string.IsNullOrWhiteSpace(cg.First().CstName))
                    {
                        var cl = Wrap(flow.G, F(8, true), cg.First().CstName, CDesc + CRem - 14);
                        var hh = LinesH(cl, 10.4) + 4.8;
                        if (flow.Need(hh + 14)) flow.NextPage();
                        DrawConsultantRow(flow.G, flow.Y, hh, cg.First().CstName);
                        flow.Y += hh;
                    }
                    foreach (var lg in cg.GroupBy(r => r.Code))
                    {
                        var sample = lg.First();
                        Emit(sample, lg.Sum(r => r.Qty), lg.Sum(r => r.Namt),
                             Wrap(flow.G, fItem, sample.Dsc, CDesc - 12),
                             Wrap(flow.G, fItem, !string.IsNullOrWhiteSpace(sample.Rem) ? sample.Rem : sample.CstName, CRem - 12));
                    }
                }
            }

            // legacy department splits, shown as informational notes (they do not re-charge)
            if (isLab)
            {
                var tlab = dRows.Where(r => string.IsNullOrEmpty(r.Rid) || r.Rid == "HM").Sum(r => r.Namt);
                var external = subtotal - tlab;
                if (external != 0m)
                {
                    if (flow.Need(NoteRowH * 2 + 2)) flow.NextPage();
                    DrawNoteRow(flow.G, flow.Y, "Hospital Laboratory Charges", tlab);
                    flow.Y += NoteRowH;
                    DrawNoteRow(flow.G, flow.Y, "External Laboratory Charges", external);
                    flow.Y += NoteRowH;
                }
            }
            else if (isPharm)
            {
                var medicine = dRows.Where(r => r.Namt >= 0).Sum(r => r.Namt);
                var refund = dRows.Where(r => r.Namt < 0).Sum(r => r.Namt);
                if (refund != 0m)
                {
                    if (flow.Need(NoteRowH * 2 + 2)) flow.NextPage();
                    DrawNoteRow(flow.G, flow.Y, "Medicine Charges", medicine);
                    flow.Y += NoteRowH;
                    DrawNoteRow(flow.G, flow.Y, "Medicine Refund", refund);
                    flow.Y += NoteRowH;
                }
            }

            if (flow.Need(SubtotalRowH + 2)) flow.NextPage();
            DrawSubtotalRow(flow.G, flow.Y, "Subtotal  -  " + deptName, subtotal);
            flow.Y += SubtotalRowH;
            return subtotal;
        }

        // ================================================================ final bill pdf

        private byte[] BuildFinalBillPdf(List<BillRow> rows, ReportCriteria c, ReportContext ctx)
        {
            HmisFontResolver.Install();
            if (rows.Count == 0) return EmptyPdf("Patient Bill (Final) - IPD");

            var title = "IPD BILL (FINAL)" + (c.Detail ? "  (DETAIL)" : "");
            using var ms = new MemoryStream();
            using var doc = new PdfDocument();
            var zebra = new Zebra();
            var canvases = new List<XGraphics>();

            foreach (var bill in rows.GroupBy(r => new { r.HosCode, r.BillNo }))
            {
                var p0 = bill.First();
                var flow = new Flow(doc, c.StartPage, c.EndPage, f =>
                {
                    double y;
                    if (f.PageNo == c.StartPage)
                    {
                        y = DrawMasthead(f.G, ctx, title, p0, c, f.PageNo);
                        y = DrawInfoPanel(f.G, y, p0, true) + 11;
                    }
                    else
                    {
                        y = TopY + DrawContinuationHeader(f.G, ctx, title, p0, f.PageNo) + 6;
                    }
                    f.Y = DrawTableHeader(f.G, y);
                }, canvases);

                var charges = 0m;
                foreach (var dg in bill.GroupBy(r => new { r.BillOrder, r.Dept })
                                       .OrderBy(x => x.Key.BillOrder))
                {
                    if (flow.PageLimitReached) break;
                    charges += RenderCategory(flow, dg.ToList(), c.Detail, dg.Key.Dept, zebra);
                }

                if (!flow.PageLimitReached)
                {
                    flow.Y += 16;
                    var discount = (p0.Settle && string.IsNullOrWhiteSpace(p0.ComId)) ? p0.CashDisc : 0m;
                    var showDiscount = discount != 0m;
                    var rowsCount = 3 + (showDiscount ? 1 : 0) + 2;
                    var summaryH = SumHeadH + (rowsCount * SumRowH) + 14 + 12;
                    if (flow.Need(summaryH + SignatureH + 6)) flow.NextPage();
                    var h = DrawSummaryPanel(flow.G, flow.Y, p0, charges, discount, showDiscount,
                                             "Amount in words : " + MoneyWords(charges));
                    flow.Y += h + 18;
                    DrawSignatures(flow.G, flow.Y, ctx, p0);
                    flow.Y += SignatureH;
                }
            }

            StampFooters(canvases, ctx, title);
            doc.Save(ms, false);
            return ms.ToArray();
        }

        // ================================================================ shared helpers

        private static string Num(decimal d) =>
            d == Math.Truncate(d) ? ((long)d).ToString(Inv) : d.ToString("0.##", Inv);

        private static string ATypeWord(string a)
        {
            switch ((a ?? "").Trim().ToUpperInvariant())
            {
                case "Y": return "Years";
                case "MG": return "Months";
                case "W": return "Weeks";
                case "D": return "Days";
                case "H": return "Hours";
                default: return a ?? "";
            }
        }

        private static string RelationWord(string r)
        {
            switch ((r ?? "").Trim().ToUpperInvariant())
            {
                case "S": return "Son";
                case "D": return "Daughter";
                case "W": return "Wife";
                case "H": return "Husband";
                case "MG": return "Mother";
                case "F": return "Father";
                default: return r ?? "";
            }
        }

        private static string StatusWord(string s)
        {
            switch ((s ?? "").Trim().ToUpperInvariant())
            {
                case "D": return "Discharged";
                case "E": return "Expired";
                case "L": return "L.A.MG.A.";
                case "T": return "Transferred";
                case "R": return "D.O.R.";
                default: return s ?? "";
            }
        }

        private static readonly string[] Ones =
        {
            "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
            "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen",
            "Eighteen", "Nineteen"
        };

        private static readonly string[] Tens =
        {
            "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
        };

        /// <summary>Amount in English words - the VFP proper(money(n)) equivalent.</summary>
        public static string MoneyWords(decimal amount)
        {
            var abs = Math.Abs(amount);
            var whole = (long)Math.Truncate(abs);
            var cents = (int)Math.Round((abs - whole) * 100, MidpointRounding.AwayFromZero);
            var words = whole == 0 ? "Zero" : NumberToWords(whole);
            if (cents > 0) words += " and " + NumberToWords(cents) + " Cents";
            return words + " Only";
        }

        private static string NumberToWords(long n)
        {
            if (n == 0) return "Zero";
            var sb = new StringBuilder();
            if (n >= 10000000) { sb.Append(NumberToWords(n / 10000000)).Append(" Crore "); n %= 10000000; }
            if (n >= 100000) { sb.Append(NumberToWords(n / 100000)).Append(" Lakh "); n %= 100000; }
            if (n >= 1000) { sb.Append(NumberToWords(n / 1000)).Append(" Thousand "); n %= 1000; }
            if (n >= 100) { sb.Append(Ones[n / 100]).Append(" Hundred "); n %= 100; }
            if (n > 0)
            {
                if (sb.Length > 0) sb.Append(' ');
                if (n < 20) sb.Append(Ones[n]);
                else
                {
                    sb.Append(Tens[n / 10]);
                    if (n % 10 > 0) sb.Append(' ').Append(Ones[n % 10]);
                }
            }
            return sb.ToString().Replace("  ", " ").Trim();
        }
    }
}
