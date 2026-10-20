using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using HmisWeb.Data;
using HmisWeb.Models.Recovery;

namespace HmisWeb.Services.Billing
{
    /// <summary>
    /// Covering-letter PDF for one company invoice - ports the VFP report
    /// IPDCOVLT.FRX (BillSys bs0601 Report button) for the new Recovery invoice.
    ///
    /// Kept from the legacy letter
    ///   * Subject "SUBMISSION OF INPATIENT BILLS", Ref = the invoice DocNo
    ///     (legacy built it from cid/month/year - DocNo is the same thing now),
    ///   * To-block (attn / designation / company / address / city) from COMPANY,
    ///   * "N bills ... for Rs X (Rupees : words)" body,
    ///   * list of the attached bills (regno / patient / amount) with the total,
    ///   * "Yours faithfully" signature strip (legacy used free variables
    ///     Fsign/Fdesig - here the logged user is named, per the drift note).
    ///
    /// Deliberately dropped from the legacy letter (review finding R-15)
    ///   * the hard-coded printer binding (Panasonic/LPT1), and
    ///   * the hard-coded "NTN : 27-12-2855790" line - the site NTN belongs to
    ///     loggen site settings, not to a report; it returns here once an NTN
    ///     field exists on INSTITUTION (flagged in the proposal).
    ///
    /// Layout follows the workspace PDF spec: A4 portrait, Arial
    /// (HmisFontResolver), white background, navy headings, teal accents,
    /// black-and-white safe, "Page X of Y".
    ///
    /// Developer: Tajwer Jalal | Date: 20-Oct-2026 | Time: 18:50
    /// </summary>
    public class RecoveryInvoiceReportService
    {
        private readonly ApplicationDbContext _db;
        private readonly RecoveryInvoiceQueryService _query;

        public RecoveryInvoiceReportService(ApplicationDbContext db,
            RecoveryInvoiceQueryService query)
        {
            _db = db;
            _query = query;
        }

        /// <summary>Rendered letter payload - Pdf bytes + Wrows for the NoData path.</summary>
        public sealed class LetterResult
        {
            public byte[] Pdf { get; set; }
            public int Rows { get; set; }
            public string Message { get; set; }
        }

        /// <summary>Header visuals supplied by the page (logged site / user).</summary>
        public class LetterContext
        {
            public string HospitalName { get; set; }
            public string UserId { get; set; }
        }

        // ---- layout constants (BillingReportService house style) ----
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private static readonly XColor Navy = XColor.FromArgb(0x1F, 0x38, 0x64);
        private static readonly XColor Teal = XColor.FromArgb(0x0F, 0x76, 0x6E);
        private static readonly XColor Body = XColor.FromArgb(0x1A, 0x1F, 0x26);
        private static readonly XColor Muted = XColor.FromArgb(0x5B, 0x66, 0x73);
        private static readonly XColor Grid = XColor.FromArgb(0xC8, 0xD2, 0xDC);
        private static readonly XColor HeadBg = XColor.FromArgb(0xE8, 0xEE, 0xF4);
        private static readonly XBrush NavyB = new XSolidBrush(Navy);
        private static readonly XBrush TealB = new XSolidBrush(Teal);
        private static readonly XBrush BodyB = new XSolidBrush(Body);
        private static readonly XBrush MutedB = new XSolidBrush(Muted);
        private static readonly XBrush HeadB = new XSolidBrush(HeadBg);
        private static readonly XPen HairPen = new XPen(Grid, 0.5);
        private static readonly XPen TealPen = new XPen(Teal, 1.1);
        private const double PageW = 595.28;   // A4 portrait, points
        private const double PageH = 841.89;
        private const double M = 42;
        private const double ContentW = PageW - (M * 2);

        private static XFont F(double size, bool bold = false)
            => new XFont("Arial", size, bold ? XFontStyle.Bold : XFontStyle.Regular);

        /// <summary>
        /// Renders the covering letter of one invoice to PDF bytes.
        /// </summary>
        public async Task<LetterResult> RenderLetterAsync(long invoiceId, LetterContext ctx)
        {
            var h = await _db.RecoveryInvoices.AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == invoiceId);
            if (h == null)
                return new LetterResult { Rows = 0, Message = "Invoice " + invoiceId + " was not found." };

            var detail = await _query.GetDetailAsync(invoiceId);
            if (detail == null || detail.Lines.Count == 0)
                return new LetterResult
                {
                    Rows = 0,
                    Message = "Invoice " + h.DocNo + " has no patient lines to submit."
                };

            // ---- company to-block (legacy read COMPANY.ATTN/DESG/NAME/...) ----
            (string Name, string Attn, string Desg, string Address, string City) co
                = ("", "", "", "", "");
            {
                var conn = _db.Database.GetDbConnection();
                var opened = false;
                if (conn.State != System.Data.ConnectionState.Open)
                { await conn.OpenAsync(); opened = true; }
                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT ISNULL(NAME,''), ISNULL(ATTN,''), ISNULL(DESG,''), "
                        + "ISNULL(ADDRESS,''), ISNULL(CITY,'') FROM COMPANY WHERE ID = @id";
                    var p = cmd.CreateParameter(); p.ParameterName = "@id"; p.Value = h.CompanyCode;
                    cmd.Parameters.Add(p);
                    using var rd = await cmd.ExecuteReaderAsync();
                    if (await rd.ReadAsync())
                        co = (rd.GetString(0), rd.GetString(1), rd.GetString(2),
                              rd.GetString(3), rd.GetString(4));
                }
                finally { if (opened) await conn.CloseAsync(); }
            }

            HmisFontResolver.Install();
            var canvases = new List<XGraphics>();
            var doc = new PdfDocument();
            var page = doc.AddPage();
            page.Width = PageW; page.Height = PageH;
            var g = XGraphics.FromPdfPage(page);
            canvases.Add(g);

            double y = 44;

            // ---- letter head ----
            g.DrawString(ctx?.HospitalName ?? "HOSPITAL", F(15, true), NavyB,
                new XRect(0, y, PageW, 20), XStringFormats.TopCenter);
            y += 24;
            g.DrawLine(TealPen, M, y, M + ContentW, y);
            y += 20;

            // ---- ref + date ----
            g.DrawString("Ref : " + (h.DocNo ?? ""), F(10, true), BodyB, x: M, y: y);
            g.DrawString("Date : " + h.SubmissionDate?.ToString("dd/MM/yyyy", Inv)
                ?? h.InvoiceDate.ToString("dd/MM/yyyy", Inv),
                F(10), BodyB, new XRect(M, y, ContentW, 14), XStringFormats.TopRight);
            y += 20;

            // ---- To-block ----
            if (!string.IsNullOrWhiteSpace(co.Attn)) { g.DrawString(co.Attn, F(11, true), BodyB, x: M, y: y); y += 14; }
            if (!string.IsNullOrWhiteSpace(co.Desg)) { g.DrawString(co.Desg, F(10.5), BodyB, x: M, y: y); y += 13; }
            foreach (var line in SplitLines(co.Name, 92))
            { g.DrawString(line, F(11, true), BodyB, x: M, y: y); y += 14; }
            foreach (var line in SplitLines(co.Address, 92))
            { g.DrawString(line, F(10), BodyB, x: M, y: y); y += 13; }
            if (!string.IsNullOrWhiteSpace(co.City)) { g.DrawString(co.City, F(10), BodyB, x: M, y: y); y += 13; }
            y += 8;
            g.DrawString("Kind Attention : " + (string.IsNullOrWhiteSpace(co.Attn) ? "Manager" : co.Attn),
                F(10.5), MutedB, x: M, y: y);
            y += 18;

            // ---- subject ----
            var subj = h.DocType == "O" ? "SUBMISSION OF OUTPATIENT BILLS" : "SUBMISSION OF INPATIENT BILLS";
            g.DrawString("Sub : " + subj, F(11, true), NavyB, x: M, y: y);
            y += 18;
            g.DrawLine(HairPen, M, y, M + ContentW, y);
            y += 16;

            // ---- body ----
            var monthName = h.BillMonth.HasValue && h.BillYear.HasValue
                ? new DateTime(h.BillYear.Value, h.BillMonth.Value, 1).ToString("MMMM yyyy", Inv)
                : h.InvoiceDate.ToString("MMMM yyyy", Inv);
            var nRec = detail.Lines.Count;
            var gross = h.TotAmt;
            var body = new StringBuilder();
            body.Append("We are submitting ").Append(nRec).Append(" bills of various consultations under your panel for ")
                .Append(monthName).Append(" amounting to Rs. ")
                .Append(gross.ToString("N2", Inv))
                .Append(" (Rupees : ").Append(IpdBillReportService.MoneyWords(gross))
                .Append(").");
            foreach (var line in Wrap(body.ToString(), 78))
            { g.DrawString(line, F(10.2), BodyB, x: M, y: y); y += 14; }
            y += 8;
            var body2 = "The detailed statement of the bills is annexed. We shall appreciate an early "
                + "settlement of the amount. In case of any clarification regarding the enclosed bills, "
                + "kindly contact this office quoting the reference number above.";
            foreach (var line in Wrap(body2, 78))
            { g.DrawString(line, F(10.2), BodyB, x: M, y: y); y += 14; }
            y += 14;

            // ---- annexure table (bills attached) ----
            if (y + 24 + detail.Lines.Count * 14 + 40 > PageH - 90) { y = PageH - 90; }
            g.DrawRectangle(HeadB, new XRect(M, y, ContentW, 18));
            g.DrawString("BILL NO. / IPD NO.", F(8, true), NavyB, x: M + 4, y: y + 5);
            g.DrawString("PATIENT", F(8, true), NavyB, x: M + 150, y: y + 5);
            g.DrawString("AMOUNT (RS.)", F(8, true), NavyB,
                new XRect(M + ContentW - 130, y + 5, 126, 11), XStringFormats.TopRight);
            y += 18;

            foreach (var l in detail.Lines)
            {
                if (y > PageH - 110) break;   // letter lists up to the sheet; the statement follows
                g.DrawString(l.RegNo, F(8.6), BodyB, x: M + 4, y: y);
                var name = l.PatientName ?? "-";
                while (name.Length > 3 && g.MeasureString(name, F(8.6)).Width > M + 320)
                    name = name.Substring(0, name.Length - 1);
                g.DrawString(name, F(8.6), BodyB, x: M + 150, y: y);
                g.DrawString(l.BillAmt.ToString("N2", Inv), F(8.6), BodyB,
                    new XRect(M + ContentW - 130, y, 126, 11), XStringFormats.TopRight);
                g.DrawLine(HairPen, M, y + 11, M + ContentW, y + 11);
                y += 14;
            }
            y += 6;
            g.DrawString("TOTAL", F(9.5, true), NavyB, x: M + 150, y: y);
            g.DrawString(gross.ToString("N2", Inv), F(9.5, true), NavyB,
                new XRect(M + ContentW - 130, y, 126, 11), XStringFormats.TopRight);
            g.DrawLine(TealPen, M, y + 13, M + ContentW, y + 13);
            y += 26;

            // ---- closing ----
            if (y + 60 > PageH - 90) y = PageH - 90;
            g.DrawString("Thanking you.", F(10.2), BodyB, x: M, y: y); y += 16;
            g.DrawString("Yours faithfully,", F(10.2), BodyB, x: M, y: y); y += 34;
            g.DrawLine(HairPen, M, y, M + 180, y); y += 13;
            var signer = string.IsNullOrWhiteSpace(ctx?.UserId) ? "Authorised Signatory" : ctx.UserId;
            g.DrawString("For " + (ctx?.HospitalName ?? "HOSPITAL"), F(9.5, true), BodyB, x: M, y: y);
            g.DrawString(signer, F(9), MutedB, new XRect(M + ContentW / 2, y + 13, ContentW / 2, 12),
                XStringFormats.TopRight);

            // ---- page footer ----
            var fy = PageH - 40;
            g.DrawLine(HairPen, M, fy, M + ContentW, fy);
            g.DrawString("Ref " + h.DocNo + "  -  " + h.CompanyCode, F(7), MutedB, x: M, y: fy + 6);
            g.DrawString(ctx?.HospitalName ?? "", F(7), MutedB,
                new XRect(M, fy + 6, ContentW, 10), XStringFormats.TopCenter);
            g.DrawString("Page 1 of 1", F(7, true), MutedB,
                new XRect(M, fy + 6, ContentW, 10), XStringFormats.TopRight);

            g.Dispose();
            using var ms = new System.IO.MemoryStream();
            doc.Save(ms, false);
            return new LetterResult { Pdf = ms.ToArray(), Rows = detail.Lines.Count };
        }

        // ---------------------------------------------------------------- helpers

        private static List<string> SplitLines(string text, int maxChars)
        {
            var outp = new List<string>();
            var t = (text ?? "").Trim();
            for (var i = 0; i < t.Length; i += maxChars)
                outp.Add(t.Substring(i, Math.Min(maxChars, t.Length - i)));
            return outp;
        }

        private static List<string> Wrap(string text, int maxChars)
        {
            var outp = new List<string>();
            var words = (text ?? "").Split(' ');
            var line = new StringBuilder();
            foreach (var w in words)
            {
                if (line.Length == 0) { line.Append(w); continue; }
                if (line.Length + 1 + w.Length <= maxChars) { line.Append(' ').Append(w); }
                else { outp.Add(line.ToString()); line.Clear().Append(w); }
            }
            if (line.Length > 0) outp.Add(line.ToString());
            return outp;
        }
    }
}
