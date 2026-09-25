using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf;

namespace HmisWeb.Pages.Cashier
{
    public class ReceiptModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;

        public ReceiptModel(ApplicationDbContext db, IAppSession appSession)
        {
            _db = db;
            _appSession = appSession;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? SessionDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Q { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? PaidId { get; set; }

        [BindProperty]
        public long IndentId { get; set; }

        [BindProperty]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        [BindProperty]
        public double TenderedAmount { get; set; }

        [BindProperty]
        public string ReferenceNo { get; set; }

        public List<IndentCard> PendingIndents { get; set; } = new();
        public List<IndentCard> PaidToday { get; set; } = new();
        public double PendingTotal { get; set; }
        public double CollectedToday { get; set; }
        public PaymentReceipt Receipt { get; set; }
        public string ErrorMessage { get; set; }

        public class IndentCard
        {
            public long Id { get; set; }
            public string InvoiceNumber { get; set; }
            public string PatientName { get; set; }
            public long? Phn { get; set; }
            public long? VisitId { get; set; }
            public string DepartmentName { get; set; }
            public string ItemSummary { get; set; }
            public string BillType { get; set; }
            public DateTime? CreatedAt { get; set; }
            public double Amount { get; set; }
            public double Balance { get; set; }
            public PaymentMethod? PaymentMethod { get; set; }
            public string ReferenceNo { get; set; }
            public DateTime? PaidAt { get; set; }
            public string Cashier { get; set; }
        }

        public class ReceiptLine
        {
            public string Description { get; set; }
            public double? Qty { get; set; }
            public double Rate { get; set; }
            public double Amount { get; set; }
        }

        public class PaymentReceipt
        {
            public long IndentId { get; set; }
            public string InvoiceNumber { get; set; }
            public string HospitalName { get; set; }
            public string HospitalNo { get; set; }
            public string PatientName { get; set; }
            public long? Phn { get; set; }
            public long? VisitId { get; set; }
            public string DepartmentName { get; set; }
            public List<ReceiptLine> Lines { get; set; } = new();
            public double Total { get; set; }
            public PaymentMethod? PaymentMethod { get; set; }
            public string ReferenceNo { get; set; }
            public double Tendered { get; set; }
            public double Change { get; set; }
            public string Cashier { get; set; }
            public DateTime? PaidAt { get; set; }
        }

        public async Task OnGetAsync()
        {
            if (SessionDate == null) SessionDate = DateTime.Today;
            await LoadStateAsync();

            if (PaidId.HasValue)
                Receipt = await BuildReceiptAsync(PaidId.Value);
        }

        public async Task<IActionResult> OnPostPayAsync()
        {
            if (SessionDate == null) SessionDate = DateTime.Today;

            var bill = await _db.Bills
                .Include(b => b.Patient).ThenInclude(p => p.Person)
                .Include(b => b.PatientEncounter)
                .Include(b => b.Department)
                .Include(b => b.BillItems)
                .FirstOrDefaultAsync(b => b.Id == IndentId && !b.Retired && !b.Cancelled);
            if (bill == null)
            {
                ErrorMessage = "The selected indent was not found.";
                await LoadStateAsync();
                return Page();
            }
            if (bill.Paid)
            {
                ErrorMessage = $"Indent #{bill.Id} is already marked as paid.";
                await LoadStateAsync();
                return Page();
            }

            double amount = bill.NetTotal > 0 ? bill.NetTotal : (bill.BillTotal > 0 ? bill.BillTotal : bill.Total);
            if (amount <= 0)
            {
                ErrorMessage = $"Indent #{bill.Id} has no amount to collect.";
                await LoadStateAsync();
                return Page();
            }

            var now = DateTime.Now;
            var userId = _appSession.WebUserId;
            bool cash = PaymentMethod == PaymentMethod.Cash;
            double tendered = cash ? Math.Max(amount, TenderedAmount) : amount;
            double change = cash ? tendered - amount : 0;

            bill.Paid = true;
            bill.PaidAt = now;
            bill.BillPaymentCompletelySettled = true;
            bill.PaymentMethod = PaymentMethod;
            bill.PaidAmount = amount;
            bill.Balance = 0;
            bill.TenderedAmount = cash ? tendered : 0;
            bill.CashPaid = cash ? tendered : 0;
            bill.CashBalance = cash ? change : 0;
            bill.CheckedById = userId;
            bill.Comments = (bill.Comments ?? "") + $" Cashier collection #{now:HHmmss}.";

            var reference = ReferenceNo?.Trim();
            if (!string.IsNullOrWhiteSpace(reference))
            {
                bill.ReferenceNumber = reference;
                if (PaymentMethod == PaymentMethod.CreditCard || PaymentMethod == PaymentMethod.DebitCard)
                    bill.CreditCardRefNo = reference;
                else if (PaymentMethod == PaymentMethod.Cheque)
                    bill.ChequeRefNo = reference;
            }

            _db.Payments.Add(new Payment
            {
                BillId = bill.Id,
                PaymentMethod = PaymentMethod,
                PaidValue = amount,
                Realized = true,
                RealizedAt = now,
                WrittenAt = now,
                CreatedAt = now,
                InstitutionId = bill.InstitutionId ?? _appSession.LoggedSite?.Id,
                DepartmentId = bill.DepartmentId ?? _appSession.LoggedDepartment?.Id,
                CreaterId = userId,
                Comments = $"Cashier receipt for indent #{bill.Id}" + (string.IsNullOrWhiteSpace(reference) ? "" : $" Ref {reference}")
            });
            await _db.SaveChangesAsync();

            Receipt = await BuildReceiptAsync(bill.Id);
            await LoadStateAsync();
            return Page();
        }

        private async Task LoadStateAsync()
        {
            var day = SessionDate?.Date ?? DateTime.Today;

            var pending = _db.Bills
                .Include(b => b.Patient).ThenInclude(p => p.Person)
                .Include(b => b.PatientEncounter)
                .Include(b => b.Department)
                .Where(b => !b.Retired && !b.Cancelled && !b.Refunded && !b.Paid && b.NetTotal > 0);

            if (!string.IsNullOrWhiteSpace(Q))
            {
                var term = Q.Trim().ToLower();
                bool num = long.TryParse(term, out var qid);
                pending = pending.Where(b =>
                    (num && b.Id == qid)
                    || (num && b.Patient.PatientId == qid)
                    || (b.InvoiceNumber != null && b.InvoiceNumber.Contains(term)));
            }

            PendingIndents = await pending
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new IndentCard
                {
                    Id = b.Id,
                    InvoiceNumber = b.InvoiceNumber,
                    PatientName = b.Patient != null && b.Patient.Person != null
                        ? (b.Patient.Person.NameWithInitials ?? b.Patient.Person.Name ?? "")
                        : "Patient",
                    Phn = b.Patient != null ? b.Patient.PatientId : null,
                    VisitId = b.PatientEncounterId,
                    DepartmentName = b.Department != null ? b.Department.Name : "",
                    BillType = (b.BillType ?? BillType.OpdBill).ToString(),
                    CreatedAt = b.CreatedAt,
                    Amount = b.NetTotal,
                    Balance = b.Balance
                })
                .ToListAsync();

            PendingTotal = PendingIndents.Sum(x => x.Amount);

            var pendingIds = PendingIndents.Select(x => x.Id).ToList();
            if (pendingIds.Any())
            {
                var itemMap = await _db.BillItems
                    .Where(bi => bi.BillId != null && pendingIds.Contains(bi.BillId.Value) && bi.ItemId1 != null)
                    .Include(bi => bi.Item)
                    .Select(bi => new { bi.BillId, Name = bi.Item != null ? (bi.Item.PrintName ?? bi.Item.Name ?? "") : "" })
                    .ToListAsync();
                foreach (var x in PendingIndents)
                {
                    var names = itemMap.Where(i => i.BillId == x.Id).Select(i => i.Name).Where(n => n != "").Distinct().Take(3).ToList();
                    x.ItemSummary = string.Join(", ", names);
                }
            }

            var paid = await _db.Bills
                .Include(b => b.Patient).ThenInclude(p => p.Person)
                .Include(b => b.PatientEncounter)
                .Include(b => b.Department)
                .Include(b => b.CheckedBy).ThenInclude(w => w.WebUserPerson)
                .Where(b => !b.Retired && !b.Cancelled && b.Paid && b.PaidAt != null && b.PaidAt.Value.Date == day)
                .OrderByDescending(b => b.PaidAt)
                .Select(b => new IndentCard
                {
                    Id = b.Id,
                    InvoiceNumber = b.InvoiceNumber,
                    PatientName = b.Patient != null && b.Patient.Person != null
                        ? (b.Patient.Person.NameWithInitials ?? b.Patient.Person.Name ?? "")
                        : "Patient",
                    Phn = b.Patient != null ? b.Patient.PatientId : null,
                    VisitId = b.PatientEncounterId,
                    DepartmentName = b.Department != null ? b.Department.Name : "",
                    BillType = (b.BillType ?? BillType.OpdBill).ToString(),
                    Amount = b.NetTotal,
                    PaymentMethod = b.PaymentMethod,
                    ReferenceNo = b.ReferenceNumber,
                    PaidAt = b.PaidAt,
                    Cashier = b.CheckedBy != null
                        ? (b.CheckedBy.WebUserPerson != null
                            ? (b.CheckedBy.WebUserPerson.NameWithInitials ?? b.CheckedBy.WebUserPerson.Name ?? b.CheckedBy.UserName ?? "")
                            : (b.CheckedBy.RealUserName ?? b.CheckedBy.UserName ?? ""))
                        : ""
                })
                .ToListAsync();

            PaidToday = paid;
            CollectedToday = paid.Sum(x => x.Amount);
        }

        private async Task<PaymentReceipt> BuildReceiptAsync(long billId)
        {
            var b = await _db.Bills
                .Include(x => x.Patient).ThenInclude(p => p.Person)
                .Include(x => x.Department)
                .Include(x => x.BillItems)
                .FirstOrDefaultAsync(x => x.Id == billId);
            if (b == null) return null;

            var lines = new List<ReceiptLine>();
            if (b.BillItems != null && b.BillItems.Any())
            {
                foreach (var bi in b.BillItems)
                {
                    var item = bi.ItemId1.HasValue ? await _db.Items.FirstOrDefaultAsync(i => i.Id == bi.ItemId1.Value) : null;
                    lines.Add(new ReceiptLine
                    {
                        Description = item != null
                            ? (item.PrintName ?? item.Name ?? "Service")
                            : "Service / procedure",
                        Qty = bi.Qty,
                        Rate = bi.Rate,
                        Amount = bi.NetValue
                    });
                }
            }
            else
            {
                lines.Add(new ReceiptLine { Description = "Indent total", Qty = 1, Rate = b.NetTotal, Amount = b.NetTotal });
            }

            double amount = b.NetTotal > 0 ? b.NetTotal : (b.BillTotal > 0 ? b.BillTotal : b.Total);
            var tender = b.CashPaid ?? (b.Paid ? amount : 0);

            return new PaymentReceipt
            {
                IndentId = b.Id,
                InvoiceNumber = b.InvoiceNumber,
                HospitalName = _appSession.LoggedSite?.PrintingName ?? _appSession.LoggedSite?.Name ?? "HMIS Hospital",
                HospitalNo = _appSession.LoggedSite?.RegNo ?? _appSession.LoggedSite?.Code ?? "",
                PatientName = b.Patient?.Person?.NameWithInitials ?? b.Patient?.Person?.Name ?? "Patient",
                Phn = b.Patient?.PatientId,
                VisitId = b.PatientEncounterId,
                DepartmentName = b.Department?.Name ?? "",
                Lines = lines,
                Total = amount,
                PaymentMethod = b.PaymentMethod,
                ReferenceNo = !string.IsNullOrWhiteSpace(b.ReferenceNumber) ? b.ReferenceNumber : b.CreditCardRefNo,
                Tendered = tender,
                Change = b.CashBalance ?? 0,
                Cashier = _appSession.LoggedUser?.WebUserPerson?.NameWithInitials
                    ?? _appSession.LoggedUser?.RealUserName
                    ?? _appSession.LoggedUser?.UserName
                    ?? "Cashier",
                PaidAt = b.PaidAt
            };
        }

        public async Task<IActionResult> OnGetPdfAsync(long? id)
        {
            if (!id.HasValue) return NotFound();
            var r = await BuildReceiptAsync(id.Value);
            if (r == null) return NotFound();
            var bytes = BuildReceiptPdf(r);
            return File(bytes, "application/pdf", $"CashierReceipt-{id.Value}.pdf");
        }

        private byte[] BuildReceiptPdf(PaymentReceipt r)
        {
            HmisFontResolver.Install();
            const double PW = 595.28;
            const double PH = 841.89;
            const double margin = 30;
            double ticketH = (PH - margin * 2 - 14) / 2;
            double secondTop = margin + ticketH + 14;

            using var ms = new MemoryStream();
            using (var doc = new PdfDocument())
            {
                var page = doc.AddPage();
                page.Width = PW;
                page.Height = PH;
                using (var g = XGraphics.FromPdfPage(page))
                {
                    DrawTicket(g, r, "OFFICE COPY", margin, margin, PW - margin * 2, ticketH);
                    g.DrawLine(new XPen(XColor.FromArgb(140, 140, 140), 1), margin, secondTop - 8, PW - margin, secondTop - 8);
                    DrawTicket(g, r, "PATIENT COPY", margin, secondTop, PW - margin * 2, ticketH);
                }
                doc.Save(ms, false);
            }
            return ms.ToArray();
        }

        private void DrawTicket(XGraphics g, PaymentReceipt r, string badge, double x, double y, double w, double h)
        {
            var pen = new XPen(XColor.FromArgb(70, 70, 70), 1);
            g.DrawRectangle(pen, XBrushes.White, x, y, w, h);

            double cx = x + 8;
            double cw = w - 16;
            double ty = y + 6;

            var fName = new XFont("Arial", 15, XFontStyle.Bold);
            var fMid = new XFont("Arial", 10.5, XFontStyle.Bold);
            var fSmall = new XFont("Arial", 9);
            var fFoot = new XFont("Arial", 8.5);
            var fHdr = new XFont("Arial", 9, XFontStyle.Bold);

            g.DrawString(badge, fSmall, XBrushes.Gray, new XRect(x + w - 68, ty, 60, 13), XStringFormats.Center);
            g.DrawString(r.HospitalName, fName, XBrushes.Black, new XRect(cx, ty, cw, 22), XStringFormats.Center); ty += 21;
            g.DrawString(r.HospitalNo, fSmall, XBrushes.Black, new XRect(cx, ty, cw, 14), XStringFormats.Center); ty += 14;
            g.DrawString("CASH PAYMENT RECEIPT", fMid, XBrushes.Black, new XRect(cx, ty, cw, 16), XStringFormats.Center); ty += 17;
            g.DrawString($"Receipt No: {r.IndentId}    {r.PaidAt?.ToString("dd MMM yyyy HH:mm")}", fSmall, XBrushes.DarkGray, new XRect(cx, ty, cw, 14), XStringFormats.Center); ty += 16;
            g.DrawLine(pen, cx, ty, x + w - 8, ty); ty += 8;

            ty = Row(g, fSmall, "Indent / Invoice", r.InvoiceNumber ?? ("#" + r.IndentId), cx, ty, cw);
            ty = Row(g, fSmall, "Patient", $"{r.PatientName}  |  PHN {r.Phn?.ToString() ?? "-"}", cx, ty, cw);
            ty = Row(g, fSmall, "Visit No", r.VisitId?.ToString() ?? "-", cx, ty, cw);
            ty = Row(g, fSmall, "Department", string.IsNullOrEmpty(r.DepartmentName) ? "-" : r.DepartmentName, cx, ty, cw);
            ty += 5;

            g.DrawString("Description", fHdr, XBrushes.Black, new XRect(cx, ty, cw * 0.55, 15), XStringFormats.CenterLeft);
            g.DrawString("Qty", fHdr, XBrushes.Black, new XRect(cx + cw * 0.55, ty, cw * 0.12, 15), XStringFormats.Center);
            g.DrawString("Rate", fHdr, XBrushes.Black, new XRect(cx + cw * 0.67, ty, cw * 0.16, 15), XStringFormats.CenterRight);
            g.DrawString("Amount", fHdr, XBrushes.Black, new XRect(cx + cw * 0.83, ty, cw * 0.17, 15), XStringFormats.CenterRight);
            ty += 16;
            g.DrawLine(pen, cx, ty, x + w - 8, ty); ty += 4;

            foreach (var l in r.Lines)
            {
                if (ty > y + h - 78) break;
                g.DrawString(l.Description, fSmall, XBrushes.Black, new XRect(cx, ty, cw * 0.55, 14), XStringFormats.CenterLeft);
                g.DrawString(l.Qty?.ToString("N0") ?? "1", fSmall, XBrushes.Black, new XRect(cx + cw * 0.55, ty, cw * 0.12, 14), XStringFormats.Center);
                g.DrawString(l.Rate.ToString("N2"), fSmall, XBrushes.Black, new XRect(cx + cw * 0.67, ty, cw * 0.16, 14), XStringFormats.CenterRight);
                g.DrawString(l.Amount.ToString("N2"), fSmall, XBrushes.Black, new XRect(cx + cw * 0.83, ty, cw * 0.17, 14), XStringFormats.CenterRight);
                ty += 14;
            }

            g.DrawLine(pen, cx, ty, x + w - 8, ty); ty += 5;
            g.DrawString("Total paid", fMid, XBrushes.Black, new XRect(cx, ty, cw * 0.6, 17), XStringFormats.CenterLeft);
            g.DrawString("Rs " + r.Total.ToString("N2"), fMid, XBrushes.Black, new XRect(cx + cw * 0.6, ty, cw * 0.4, 17), XStringFormats.CenterRight);
            ty += 19;

            if (r.PaymentMethod == PaymentMethod.Cash)
            {
                ty = Row(g, fSmall, "Tendered", r.Tendered.ToString("N2"), cx, ty, cw);
                ty = Row(g, fSmall, "Change", r.Change.ToString("N2"), cx, ty, cw);
            }
            else
            {
                ty = Row(g, fSmall, "Payment method", r.PaymentMethod?.ToString() ?? "", cx, ty, cw);
                if (!string.IsNullOrEmpty(r.ReferenceNo))
                    ty = Row(g, fSmall, "Reference / Txn ID", r.ReferenceNo, cx, ty, cw);
            }

            double fy = y + h - 40;
            var dept = string.IsNullOrEmpty(r.DepartmentName) ? "the concerned" : r.DepartmentName;
            g.DrawString($"Show this receipt in {dept} department to receive the service / procedure / test.", fFoot, XBrushes.DarkGray, new XRect(cx, fy, cw, 13), XStringFormats.Center);
            g.DrawString($"Cashier: {r.Cashier}  |  Thank you", fFoot, XBrushes.DarkGray, new XRect(cx, fy + 13, cw, 13), XStringFormats.Center);
        }

        private static double Row(XGraphics g, XFont f, string label, string value, double x, double y, double w)
        {
            g.DrawString(label, f, XBrushes.Black, new XRect(x, y, w * 0.42, 15), XStringFormats.CenterLeft);
            g.DrawString(value, f, XBrushes.Black, new XRect(x + w * 0.44, y, w * 0.56, 15), XStringFormats.CenterLeft);
            return y + 15;
        }
    }

    internal sealed class HmisFontResolver : IFontResolver
    {
        private static readonly string FontsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");

        public string DefaultFontName => "aria.ttf";

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            return new FontResolverInfo(isBold ? "arialbd.ttf" : "arial.ttf", XStyleSimulations.None);
        }

        public byte[] GetFont(string faceName)
        {
            return File.ReadAllBytes(Path.Combine(FontsDir, faceName));
        }

        public static void Install()
        {
            if (GlobalFontSettings.FontResolver == null)
                GlobalFontSettings.FontResolver = new HmisFontResolver();
        }
    }
}