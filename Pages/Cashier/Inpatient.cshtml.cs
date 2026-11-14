using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Cashier
{
    public class InpatientModel : CashierPageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;

        public InpatientModel(ApplicationDbContext db, IAppSession appSession)
        {
            _db = db;
            _appSession = appSession;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? SessionDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Q { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? EncounterId { get; set; }

        [BindProperty]
        public long SelectedEncounterId { get; set; }

        [BindProperty]
        public double ReceivedAmount { get; set; }

        [BindProperty]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        [BindProperty]
        public double TenderedAmount { get; set; }

        [BindProperty]
        public string ReferenceNo { get; set; }

        [BindProperty]
        public double CashDiscount { get; set; }

        [BindProperty]
        public bool SettleAccount { get; set; }

        public List<InpatientAccountCard> PendingAccounts { get; set; } = new();
        public List<InpatientAccountCard> PaidToday { get; set; } = new();
        public PatientEncounter Encounter { get; set; }
        public List<AccountLine> AccountLines { get; set; } = new();
        public double ChargesTotal { get; set; }
        public double ReceivedTotal { get; set; }
        public double DiscountTotal { get; set; }
        public double BalanceDue { get; set; }
        public bool AccountTargetIsFinal { get; set; }
        public InpatientReceipt Receipt { get; set; }
        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }

        public class InpatientAccountCard
        {
            public long EncounterId { get; set; }
            public string BhtNo { get; set; }
            public string PatientName { get; set; }
            public string Phn { get; set; }
            public long? PatientId { get; set; }
            public string DepartmentName { get; set; }
            public string Consultation { get; set; }
            public DateTime? AdmittedAt { get; set; }
            public bool Discharged { get; set; }
            public double ChargesTotal { get; set; }
            public double ReceivedTotal { get; set; }
            public double Balance { get; set; }
            public bool Paid { get; set; }
            public DateTime? PaidAt { get; set; }
            public PaymentMethod? PaymentMethod { get; set; }
        }

        public class AccountLine
        {
            public long BillId { get; set; }
            public string InvoiceNumber { get; set; }
            public string Description { get; set; }
            public BillType? BillType { get; set; }
            public double NetTotal { get; set; }
            public double PaidAmount { get; set; }
            public double Discount { get; set; }
            public double Outstanding { get; set; }
            public bool Paid { get; set; }
            public DateTime? PaidAt { get; set; }
            public PaymentMethod? PaymentMethod { get; set; }
        }

        public class ReceiptLine
        {
            public string Description { get; set; }
            public double? Qty { get; set; }
            public double Rate { get; set; }
            public double Amount { get; set; }
        }

        public class InpatientReceipt
        {
            public long EncounterId { get; set; }
            public string ReceiptNo { get; set; }
            public string HospitalName { get; set; }
            public string HospitalNo { get; set; }
            public string PatientName { get; set; }
            public string Phn { get; set; }
            public string BhtNo { get; set; }
            public string DepartmentName { get; set; }
            public List<ReceiptLine> Lines { get; set; } = new();
            public double Total { get; set; }
            public double Discount { get; set; }
            public PaymentMethod? PaymentMethod { get; set; }
            public string ReferenceNo { get; set; }
            public double Tendered { get; set; }
            public double Change { get; set; }
            public double BalanceAfter { get; set; }
            public string Cashier { get; set; }
            public DateTime? PaidAt { get; set; }
        }

        public async Task OnGetAsync()
        {
            if (SessionDate == null) SessionDate = DateTime.Today;
            await LoadAccountsAsync();

            if (EncounterId.HasValue)
                await LoadAccountLinesAsync(EncounterId.Value);
        }

        public async Task<IActionResult> OnPostReceiveAsync()
        {
            if (SessionDate == null) SessionDate = DateTime.Today;

            var encounter = await _db.PatientEncounters
                .Include(e => e.Patient).ThenInclude(p => p.Person)
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == SelectedEncounterId && !e.Retired);
            if (encounter == null)
            {
                ErrorMessage = "The selected inpatient account was not found.";
                await LoadAccountsAsync();
                return Page();
            }

            var (bills, accountTargetIsFinal) = await GetScopeAsync(encounter);
            if (!bills.Any())
            {
                ErrorMessage = $"Account for BHT {encounter.BhtNo} has no outstanding bills to collect.";
                await LoadAccountsAsync();
                if (EncounterId == null) EncounterId = encounter.Id;
                await LoadAccountLinesAsync(encounter.Id);
                return Page();
            }

            var now = DateTime.Now;
            var userId = _appSession.WebUserId;
            var site = _appSession.LoggedSite;

            double due = bills.Sum(b => Math.Max(0, b.NetTotal - b.PaidAmount));
            due = Math.Round(due, 2);
            double discount = SettleAccount ? Math.Round(Math.Max(0, Math.Min(CashDiscount, due)), 2) : 0;
            double required = Math.Round(Math.Max(0, due - discount), 2);

            if (ReceivedAmount <= 0)
            {
                ErrorMessage = "Enter an amount to receive.";
                await LoadAccountsAsync();
                EncounterId = encounter.Id;
                await LoadAccountLinesAsync(encounter.Id);
                return Page();
            }

            bool cash = PaymentMethod == PaymentMethod.Cash;

            if (SettleAccount && ReceivedAmount < required - 0.001)
            {
                ErrorMessage = $"To settle the account a minimum of Rs {required:N2} is required (outstanding {due:N2} less discount {discount:N2}).";
                await LoadAccountsAsync();
                EncounterId = encounter.Id;
                await LoadAccountLinesAsync(encounter.Id);
                return Page();
            }

            if (!SettleAccount && ReceivedAmount > due + 0.001)
            {
                ErrorMessage = $"The received amount Rs {ReceivedAmount:N2} exceeds the outstanding balance of Rs {due:N2}.";
                await LoadAccountsAsync();
                EncounterId = encounter.Id;
                await LoadAccountLinesAsync(encounter.Id);
                return Page();
            }

            double tendered = cash ? Math.Max(ReceivedAmount, TenderedAmount) : ReceivedAmount;
            double change = cash ? tendered - ReceivedAmount : 0;

            // Allocate the received amount across the outstanding bills, oldest first.
            double remaining = ReceivedAmount;
            foreach (var bill in bills.OrderBy(b => b.CreatedAt ?? DateTime.MinValue).ThenBy(b => b.Id))
            {
                if (remaining <= 0.004 && !(SettleAccount && bill.PaidAmount < bill.NetTotal - 0.004)) continue;

                double billDue = Math.Max(0, bill.NetTotal - bill.PaidAmount);
                if (billDue <= 0.004)
                {
                    bill.Paid = true;
                    bill.PaidAt = bill.PaidAt ?? now;
                    bill.BillPaymentCompletelySettled = true;
                    bill.Balance = 0;
                    continue;
                }

                double applied = Math.Min(billDue, remaining);
                remaining = Math.Round(Math.Max(0, remaining - applied), 2);

                bill.PaidAmount = Math.Round(bill.PaidAmount + applied, 2);
                bill.Paid = bill.PaidAmount >= bill.NetTotal - 0.004;
                bill.Balance = Math.Round(Math.Max(0, bill.NetTotal - bill.PaidAmount), 2);
                if (bill.Paid)
                {
                    bill.PaidAt = now;
                    bill.BillPaymentCompletelySettled = true;
                    bill.PaymentMethod = PaymentMethod;
                }
                else
                {
                    bill.BillPaymentCompletelySettled = false;
                    bill.PaymentMethod = PaymentMethod;
                }
                bill.TenderedAmount = cash ? tendered : 0;
                bill.CashPaid = cash ? tendered : 0;
                bill.CashBalance = cash ? change : 0;
                bill.CheckedById = userId;
                bill.Comments = (bill.Comments ?? "") + $" Inpatient cashier collection {now:HHmmss} (BHT {encounter.BhtNo}).";

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
                    PaidValue = applied,
                    Realized = true,
                    RealizedAt = now,
                    WrittenAt = now,
                    CreatedAt = now,
                    InstitutionId = bill.InstitutionId ?? site?.Id,
                    DepartmentId = bill.DepartmentId ?? _appSession.LoggedDepartment?.Id,
                    CreaterId = userId,
                    Comments = $"Inpatient receipt for BHT {encounter.BhtNo}" + (string.IsNullOrWhiteSpace(reference) ? "" : $" Ref {reference}")
                });
            }

            // When settling, write the remaining outstanding amount off as bill discount so
            // downstream totals (discharge/final bill computation) stay consistent.
            if (SettleAccount)
            {
                double discountLeft = Math.Round(Math.Max(0, due - ReceivedAmount), 2);
                foreach (var bill in bills.OrderByDescending(b => b.Id))
                {
                    if (discountLeft <= 0.004) break;
                    double billDue = Math.Max(0, bill.NetTotal - bill.PaidAmount);
                    if (billDue <= 0.004) continue;

                    double share = Math.Min(billDue, discountLeft);
                    discountLeft = Math.Round(Math.Max(0, discountLeft - share), 2);

                    bill.NetTotal = Math.Round(bill.NetTotal - share, 2);
                    bill.BillTotal = Math.Round(bill.BillTotal - share, 2);
                    bill.Discount = Math.Round(bill.Discount + share, 2);
                    bill.PaidAmount = Math.Round(bill.PaidAmount + share, 2);
                    bill.Paid = true;
                    bill.PaidAt = now;
                    bill.BillPaymentCompletelySettled = true;
                    bill.Balance = 0;
                }

                Encounter.PaymentFinalized = true;
                Encounter.EditerId = userId;
                Encounter.EditedAt = now;
            }

            await _db.SaveChangesAsync();

            EncounterId = encounter.Id;
            await LoadAccountLinesAsync(encounter.Id);

            var receiptNo = await ReceiveReceiptNoAsync(encounter, now);
            Receipt = BuildReceipt(encounter, receiptNo, now, tendered, change,
                PaymentMethod, ReferenceNo?.Trim(), ReceivedAmount, discount);
            SuccessMessage = SettleAccount
                ? $"Inpatient account BHT {encounter.BhtNo} settled for Rs {ReceivedAmount:N2}."
                : $"Received Rs {ReceivedAmount:N2} against inpatient account BHT {encounter.BhtNo}.";

            await LoadAccountsAsync();
            return Page();
        }

        private async Task<string> ReceiveReceiptNoAsync(PatientEncounter encounter, DateTime now)
        {
            var last = await _db.Payments
                .Where(p => p.Bill != null && p.Bill.PatientEncounterId == encounter.Id && p.CreatedAt != null && p.CreatedAt.Value.Date == now.Date)
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();
            var seq = last != null ? (int)(last.Id % 100000) + 1 : 1;
            return $"INP-{now:yyyyMMdd}-{seq}";
        }

        public async Task<IActionResult> OnGetPdfAsync(long? id)
        {
            if (!id.HasValue) return NotFound();
            var encounter = await _db.PatientEncounters
                .Include(e => e.Patient).ThenInclude(p => p.Person)
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == id.Value && !e.Retired);
            if (encounter == null) return NotFound();

            await LoadAccountLinesAsync(encounter.Id);
            var totalReceived = Math.Round(AccountLines.Sum(l => l.PaidAmount), 2);
            var method = AccountLines.Where(l => l.Paid).Select(l => l.PaymentMethod).LastOrDefault();
            var receipt = new InpatientReceipt
            {
                EncounterId = encounter.Id,
                ReceiptNo = $"INP-{encounter.Id}",
                HospitalName = _appSession.LoggedSite?.PrintingName ?? _appSession.LoggedSite?.Name ?? "HMIS Hospital",
                HospitalNo = _appSession.LoggedSite?.RegNo ?? _appSession.LoggedSite?.Code ?? "",
                PatientName = encounter.Patient?.Person?.NameWithInitials ?? encounter.Patient?.Person?.Name ?? "Patient",
                Phn = encounter.Patient?.Phn ?? "",
                BhtNo = encounter.BhtNo ?? "",
                DepartmentName = encounter.Department?.Name ?? "",
                Lines = AccountLines.Select(l => new ReceiptLine
                {
                    Description = l.Description,
                    Qty = 1,
                    Rate = l.PaidAmount,
                    Amount = l.PaidAmount
                }).ToList(),
                Total = totalReceived,
                PaymentMethod = method,
                PaidAt = AccountLines.Select(l => l.PaidAt).OrderByDescending(p => p).FirstOrDefault(),
                Cashier = _appSession.LoggedUser?.WebUserPerson?.NameWithInitials
                    ?? _appSession.LoggedUser?.RealUserName
                    ?? _appSession.LoggedUser?.UserName
                    ?? "Cashier"
            };

            var bytes = BuildReceiptPdf(receipt);
            return File(bytes, "application/pdf", $"InpatientReceipt-{encounter.Id}.pdf");
        }

        private InpatientReceipt BuildReceipt(PatientEncounter encounter, string receiptNo, DateTime now,
            double tendered, double change, PaymentMethod method, string reference, double total, double discount)
        {
            return new InpatientReceipt
            {
                EncounterId = encounter.Id,
                ReceiptNo = receiptNo,
                HospitalName = _appSession.LoggedSite?.PrintingName ?? _appSession.LoggedSite?.Name ?? "HMIS Hospital",
                HospitalNo = _appSession.LoggedSite?.RegNo ?? _appSession.LoggedSite?.Code ?? "",
                PatientName = encounter.Patient?.Person?.NameWithInitials ?? encounter.Patient?.Person?.Name ?? "Patient",
                Phn = encounter.Patient?.Phn ?? "",
                BhtNo = encounter.BhtNo ?? "",
                DepartmentName = encounter.Department?.Name ?? "",
                Lines = AccountLines.Select(l => new ReceiptLine
                {
                    Description = l.Description,
                    Qty = 1,
                    Rate = l.PaidAmount,
                    Amount = l.PaidAmount
                }).Where(l => l.Amount > 0).ToList(),
                Total = total,
                Discount = discount,
                PaymentMethod = method,
                ReferenceNo = reference,
                Tendered = tendered,
                Change = change,
                BalanceAfter = Math.Round(Math.Max(0, BalanceDue - total), 2),
                Cashier = _appSession.LoggedUser?.WebUserPerson?.NameWithInitials
                    ?? _appSession.LoggedUser?.RealUserName
                    ?? _appSession.LoggedUser?.UserName
                    ?? "Cashier",
                PaidAt = now
            };
        }

        private async Task LoadAccountsAsync()
        {
            var day = SessionDate?.Date ?? DateTime.Today;

            var encounters = _db.PatientEncounters
                .Include(e => e.Patient).ThenInclude(p => p.Person)
                .Include(e => e.Department)
                .Where(e => e.EncounterType == EncounterType.Inward && !e.Retired);

            if (!string.IsNullOrWhiteSpace(Q))
            {
                var term = Q.Trim();
                var lower = term.ToLower();
                bool isNumber = long.TryParse(term, out var qn);
                encounters = encounters.Where(e =>
                    (isNumber && e.Patient.PatientId == qn)
                    || (e.BhtNo != null && e.BhtNo.Contains(term))
                    || (e.BhtLong != null && e.BhtLong.Contains(term))
                    || (e.Patient != null && e.Patient.Person != null &&
                        ((e.Patient.Person.NameWithInitials != null && e.Patient.Person.NameWithInitials.ToLower().Contains(lower))
                         || (e.Patient.Person.Name != null && e.Patient.Person.Name.ToLower().Contains(lower))))
                );
            }

            var list = await encounters
                .OrderByDescending(e => e.DateOfAdmission)
                .Take(200)
                .ToListAsync();

            var ids = list.Select(e => e.Id).ToList();
            var bills = await _db.Bills
                .Where(b => b.PatientEncounterId != null && ids.Contains(b.PatientEncounterId.Value)
                    && !b.Retired && !b.Cancelled)
                .ToListAsync();

            var consultMap = await _db.Staffs
                .Where(s => list.Select(e => e.ReferringConsultantId).Contains(s.Id))
                .Include(s => s.Person)
                .ToDictionaryAsync(s => s.Id, s => s);

            var cards = list.Select(e =>
            {
                var ebills = bills.Where(b => b.PatientEncounterId == e.Id).ToList();
                bool hasFinal = ebills.Any(b => b.BillType == BillType.InwardFinalBill);
                double charges;
                if (hasFinal)
                {
                    var finalBill = ebills.First(b => b.BillType == BillType.InwardFinalBill);
                    charges = Math.Round(finalBill.NetTotal, 2);
                }
                else
                {
                    charges = Math.Round(ebills.Sum(b => b.NetTotal), 2);
                }
                double received = Math.Round(ebills.Sum(b => Math.Max(0, b.PaidAmount)), 2);
                double discount = Math.Round(ebills.Sum(b => b.Discount), 2);
                double butPaid = Math.Round(charges - received, 2);
                var paidAt = ebills.Where(b => b.Paid).Select(b => b.PaidAt).OrderByDescending(p => p).FirstOrDefault();
                var method = ebills.Where(b => b.Paid).Select(b => b.PaymentMethod).LastOrDefault();

                return new InpatientAccountCard
                {
                    EncounterId = e.Id,
                    BhtNo = e.BhtNo ?? "",
                    PatientName = e.Patient?.Person?.NameWithInitials ?? e.Patient?.Person?.Name ?? "Patient",
                    Phn = e.Patient?.Phn ?? "",
                    PatientId = e.PatientId,
                    DepartmentName = e.Department?.Name ?? "",
                    Consultation = e.ReferringConsultantId.HasValue && consultMap.TryGetValue(e.ReferringConsultantId.Value, out var c)
                        ? (c.Person?.NameWithInitials ?? c.Person?.Name ?? "")
                        : "",
                    AdmittedAt = e.DateOfAdmission,
                    Discharged = e.DateOfDischarge != null,
                    ChargesTotal = charges,
                    ReceivedTotal = received,
                    Balance = butPaid,
                    Paid = butPaid <= 0.004 || e.PaymentFinalized,
                    PaidAt = paidAt,
                    PaymentMethod = method
                };
            }).OrderByDescending(c => c.Balance > 0).ThenByDescending(c => c.AdmittedAt).ToList();

            PendingAccounts = cards.Where(c => c.Balance > 0.004 && !c.Paid).ToList();
            PaidToday = cards
                .Where(c => c.Paid && c.PaidAt != null && c.PaidAt.Value.Date == day)
                .OrderByDescending(c => c.PaidAt)
                .ToList();
        }

        private async Task<(List<Bill>, bool accountTargetIsFinal)> GetScopeAsync(PatientEncounter encounter)
        {
            var bills = await _db.Bills
                .Where(b => b.PatientEncounterId == encounter.Id && !b.Retired && !b.Cancelled)
                .ToListAsync();

            var finalBill = bills.FirstOrDefault(b => b.BillType == BillType.InwardFinalBill);
            if (finalBill != null)
            {
                var outstandingFinal = Math.Max(0, finalBill.NetTotal - finalBill.PaidAmount) > 0.004 && !finalBill.Paid;
                return (outstandingFinal ? new List<Bill> { finalBill } : new List<Bill>(), true);
            }

            var unpaid = bills
                .Where(b => Math.Max(0, b.NetTotal - b.PaidAmount) > 0.004 && !b.Paid)
                .ToList();
            return (unpaid, false);
        }

        private async Task LoadAccountLinesAsync(long encounterId)
        {
            Encounter = await _db.PatientEncounters
                .Include(e => e.Patient).ThenInclude(p => p.Person)
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == encounterId && !e.Retired);
            if (Encounter == null) return;

            var bills = await _db.Bills
                .Where(b => b.PatientEncounterId == encounterId && !b.Retired && !b.Cancelled)
                .OrderBy(b => b.CreatedAt ?? DateTime.MinValue)
                .ToListAsync();

            var billIds = bills.Select(b => b.Id).ToList();
            var items = await _db.BillItems
                .Where(bi => bi.BillId != null && billIds.Contains(bi.BillId.Value) && bi.Retired == false)
                .Include(bi => bi.Item)
                .ToListAsync();

            AccountLines = bills.Select(b =>
            {
                var desc = items
                    .Where(i => i.BillId == b.Id && i.Item != null)
                    .Select(i => i.Item.PrintName ?? i.Item.Name ?? "")
                    .Where(n => n != "")
                    .Distinct()
                    .Take(3)
                    .ToList();
                return new AccountLine
                {
                    BillId = b.Id,
                    InvoiceNumber = b.InvoiceNumber,
                    Description = desc.Any()
                        ? string.Join(", ", desc)
                        : (b.BillType ?? BillType.OpdBill).ToString(),
                    BillType = b.BillType,
                    NetTotal = Math.Round(b.NetTotal, 2),
                    PaidAmount = Math.Round(b.PaidAmount, 2),
                    Discount = Math.Round(b.Discount, 2),
                    Outstanding = Math.Round(Math.Max(0, b.NetTotal - b.PaidAmount), 2),
                    Paid = b.Paid,
                    PaidAt = b.PaidAt,
                    PaymentMethod = b.PaymentMethod
                };
            }).ToList();

            bool hasFinal = AccountLines.Any(l => l.BillType == BillType.InwardFinalBill);
            if (hasFinal)
            {
                var finalBill = bills.First(b => b.BillType == BillType.InwardFinalBill);
                ChargesTotal = Math.Round(finalBill.NetTotal, 2);
                AccountTargetIsFinal = true;
            }
            else
            {
                ChargesTotal = Math.Round(bills.Sum(b => b.NetTotal), 2);
                AccountTargetIsFinal = false;
            }
            ReceivedTotal = Math.Round(bills.Sum(b => Math.Max(0, b.PaidAmount)), 2);
            DiscountTotal = Math.Round(bills.Sum(b => b.Discount), 2);
            BalanceDue = Math.Round(Math.Max(0, ChargesTotal - ReceivedTotal), 2);
        }

        private byte[] BuildReceiptPdf(InpatientReceipt r)
        {
            HmisFontResolverInternal.Install();
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

        private void DrawTicket(XGraphics g, InpatientReceipt r, string badge, double x, double y, double w, double h)
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
            g.DrawString("INPATIENT CASH RECEIPT", fMid, XBrushes.Black, new XRect(cx, ty, cw, 16), XStringFormats.Center); ty += 17;
            g.DrawString($"Receipt No: {r.ReceiptNo}    {r.PaidAt?.ToString("dd MMM yyyy HH:mm")}", fSmall, XBrushes.DarkGray, new XRect(cx, ty, cw, 14), XStringFormats.Center); ty += 16;
            g.DrawLine(pen, cx, ty, x + w - 8, ty); ty += 8;

            ty = Row(g, fSmall, "BHT / Inpatient", r.BhtNo, cx, ty, cw);
            ty = Row(g, fSmall, "Patient", $"{r.PatientName}  |  PHN {r.Phn}", cx, ty, cw);
            ty = Row(g, fSmall, "Department", string.IsNullOrEmpty(r.DepartmentName) ? "-" : r.DepartmentName, cx, ty, cw);
            ty += 5;

            g.DrawString("Description", fHdr, XBrushes.Black, new XRect(cx, ty, cw * 0.62, 15), XStringFormats.CenterLeft);
            g.DrawString("Amount", fHdr, XBrushes.Black, new XRect(cx + cw * 0.83, ty, cw * 0.17, 15), XStringFormats.CenterRight);
            ty += 16;
            g.DrawLine(pen, cx, ty, x + w - 8, ty); ty += 4;

            foreach (var l in r.Lines)
            {
                if (ty > y + h - 120) break;
                g.DrawString(l.Description, fSmall, XBrushes.Black, new XRect(cx, ty, cw * 0.8, 14), XStringFormats.CenterLeft);
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
            g.DrawString("This receipt confirms the cash received against the inpatient account.", fFoot, XBrushes.DarkGray, new XRect(cx, fy, cw, 13), XStringFormats.Center);
            g.DrawString($"Cashier: {r.Cashier}  |  Thank you", fFoot, XBrushes.DarkGray, new XRect(cx, fy + 13, cw, 13), XStringFormats.Center);
        }

        private static double Row(XGraphics g, XFont f, string label, string value, double x, double y, double w)
        {
            g.DrawString(label, f, XBrushes.Black, new XRect(x, y, w * 0.42, 15), XStringFormats.CenterLeft);
            g.DrawString(value, f, XBrushes.Black, new XRect(x + w * 0.44, y, w * 0.56, 15), XStringFormats.CenterLeft);
            return y + 15;
        }
    }

    internal sealed class HmisFontResolverInternal : IFontResolver
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
                GlobalFontSettings.FontResolver = new HmisFontResolverInternal();
        }
    }
}