using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;
using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf;

namespace HmisWeb.Pages.Lab
{
    public class IndentModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ICommonService _common;
        private readonly IAppSession _appSession;

        public IndentModel(ApplicationDbContext db, ICommonService common, IAppSession appSession)
        {
            _db = db;
            _common = common;
            _appSession = appSession;
        }

        private const string CartKey = "LabIndentCart";
        private const string PatientKey = "LabIndentPatientId";

        [BindProperty]
        public string PatientSearch { get; set; }

        [BindProperty]
        public string NewName { get; set; }

        [BindProperty]
        public string NewNameWithInitials { get; set; }

        [BindProperty]
        public string NewNic { get; set; }

        [BindProperty]
        public string NewMobile { get; set; }

        [BindProperty]
        public string NewPhone { get; set; }

        [BindProperty]
        public Sex? NewSex { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? SelectedPatientId { get; set; }

        [BindProperty]
        public string ItemSearch { get; set; }

        [BindProperty]
        public long? SelectedItemId { get; set; }

        [BindProperty]
        public long? ItemToRemove { get; set; }

        [BindProperty]
        public List<IndentCartItem> Cart { get; set; } = new();

        [BindProperty]
        public long? SelectedEncounterId { get; set; }

        [BindProperty]
        public string Comments { get; set; }

        public Patient SelectedPatient { get; set; }
        public List<Patient> PatientResults { get; set; } = new();
        public List<Item> ItemResults { get; set; } = new();
        public List<IndentCartItem> DisplayCart { get; set; } = new();
        public List<PatientEncounter> ActiveEncounters { get; set; } = new();
        public PatientEncounter SelectedEncounter { get; set; }
        public Department LabDepartment { get; set; }
        public List<Bill> UnpaidBills { get; set; } = new();
        public bool HasUnpaidBills => UnpaidBills.Any();
        public double Total { get; set; }
        public double DiscountTotal { get; set; }
        public double NetTotal { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string Success { get; set; }

        // Invoice for printing
        public LabInvoiceModel Invoice { get; set; }

        public class LabInvoiceModel
        {
            public long BillId { get; set; }
            public string InvoiceNumber { get; set; }
            public string HospitalName { get; set; }
            public string HospitalNo { get; set; }
            public DateTime InvoiceDate { get; set; }
            public string PatientName { get; set; }
            public long? PatientPhn { get; set; }
            public string VisitNumber { get; set; }
            public string DepartmentName { get; set; }
            public List<InvoiceLine> Lines { get; set; } = new();
            public double SubTotal { get; set; }
            public double Discount { get; set; }
            public double NetTotal { get; set; }
            public string Cashier { get; set; }
            public bool IsIPD { get; set; }
        }

        public class InvoiceLine
        {
            public string ItemName { get; set; }
            public int Qty { get; set; }
            public double Rate { get; set; }
            public double Discount { get; set; }
            public double NetValue { get; set; }
        }

        public async Task OnGetAsync()
        {
            if (SelectedPatientId.HasValue)
            {
                var patient = await _db.Patients.FirstOrDefaultAsync(p => p.Id == SelectedPatientId.Value && !p.Retired);
                if (patient != null)
                {
                    var current = GetSelectedPatientId();
                    if (current != patient.Id)
                        HttpContext.Session.Remove(CartKey);
                    HttpContext.Session.SetString(PatientKey, patient.Id.ToString());
                }
            }
            await LoadStateAsync();
            if (TempData["IndentSuccess"] != null)
                Success = TempData["IndentSuccess"].ToString();
        }

        public async Task<IActionResult> OnPostSearchPatientsAsync()
        {
            await LoadStateAsync();
            PatientResults = await _common.SearchPatientsAsync(PatientSearch);
            return Page();
        }

        public async Task<IActionResult> OnPostSelectPatientAsync()
        {
            if (SelectedPatientId == null)
                return RedirectToPage();

            var patient = await _db.Patients
                .Include(p => p.Person)
                .FirstOrDefaultAsync(p => p.Id == SelectedPatientId && !p.Retired);
            if (patient == null)
                return RedirectToPage();

            HttpContext.Session.SetString(PatientKey, patient.Id.ToString());
            return RedirectToPage("/Lab/Indent");
        }

        public IActionResult OnPostStartNewAsync()
        {
            HttpContext.Session.Remove(PatientKey);
            HttpContext.Session.Remove(CartKey);
            return RedirectToPage("/Lab/Indent");
        }

        public async Task<IActionResult> OnPostCreatePatientAsync()
        {
            if (string.IsNullOrWhiteSpace(NewName))
            {
                ErrorMessage = "Patient name is required.";
                return Page();
            }

            var person = new Person
            {
                Name = NewName.Trim(),
                NameWithInitials = string.IsNullOrWhiteSpace(NewNameWithInitials) ? NewName.Trim() : NewNameWithInitials.Trim(),
                Nic = NewNic?.Trim() ?? "",
                Mobile = NewMobile?.Trim() ?? "",
                Phone = NewPhone?.Trim() ?? "",
                Sex = NewSex,
                CreatedAt = DateTime.Now,
                CreaterId = _appSession.WebUserId
            };
            _db.Persons.Add(person);
            await _db.SaveChangesAsync();

            long? maxPatientId = await _db.Patients.MaxAsync(p => (long?)p.PatientId);
            var patient = new Patient
            {
                PersonId = person.Id,
                PatientId = (maxPatientId ?? 0) + 1,
                CreatedAt = DateTime.Now,
                CreaterId = _appSession.WebUserId,
                CreatedInstitutionId = _appSession.LoggedSite?.Id
            };
            _db.Patients.Add(patient);
            await _db.SaveChangesAsync();

            HttpContext.Session.Remove(CartKey);
            HttpContext.Session.SetString(PatientKey, patient.Id.ToString());
            return RedirectToPage("/Lab/Indent");
        }

        public async Task<IActionResult> OnPostSearchItemsAsync()
        {
            await LoadStateAsync();
            await SearchItemsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAddItemAsync()
        {
            await LoadStateAsync();
            if (SelectedItemId == null)
                return Page();

            var departmentId = LabDepartment?.Id ?? _appSession.LoggedDepartment?.Id;
            var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == SelectedItemId && !i.Retired && i.Billable
                && (i.ItemType == ItemType.Investigation || i.ItemType == ItemType.Service)
                && (departmentId == null || i.DepartmentId == departmentId));
            if (item == null)
                return Page();

            var patientId = GetSelectedPatientId();
            var companyId = patientId.HasValue
                ? await _common.GetPatientCompanyIdAsync(
                    await _db.Patients.Where(p => p.Id == patientId.Value && !p.Retired).Select(p => p.PersonId).FirstOrDefaultAsync() ?? 0)
                : null;
            var rate = await _common.GetAppliedRateAsync(item, companyId);
            var name = string.IsNullOrWhiteSpace(item.PrintName) ? item.Name : item.PrintName;
            var cart = GetCart();
            var existing = cart.FirstOrDefault(c => c.ItemId == item.Id);
            if (existing != null)
            {
                existing.Qty += 1;
            }
            else
            {
                cart.Add(new IndentCartItem
                {
                    ItemId = item.Id,
                    Code = item.Code,
                    ItemName = name,
                    Qty = 1,
                    Rate = rate,
                    Discount = 0
                });
            }
            SaveCart(cart);
            TempData["IndentMessage"] = $"{name} added to the indent.";
            return RedirectToPage("/Lab/Indent");
        }

        public IActionResult OnPostUpdateCartAsync()
        {
            if (ItemToRemove.HasValue)
            {
                var cart = GetCart();
                cart.RemoveAll(c => c.ItemId == ItemToRemove.Value);
                SaveCart(cart);
                return RedirectToPage("/Lab/Indent");
            }

            var rows = Cart ?? new List<IndentCartItem>();
            foreach (var r in rows)
            {
                if (r.Qty <= 0) r.Qty = 1;
                if (r.Rate < 0) r.Rate = 0;
                if (r.Discount < 0) r.Discount = 0;
                if (r.Discount > r.Qty * r.Rate) r.Discount = r.Qty * r.Rate;
            }
            rows = rows.Where(r => r.ItemId > 0).ToList();
            SaveCart(rows);
            return RedirectToPage("/Lab/Indent");
        }

        public IActionResult OnPostClearAsync()
        {
            HttpContext.Session.Remove(CartKey);
            return RedirectToPage("/Lab/Indent");
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            await LoadLookupsAsync();

            var patientId = GetSelectedPatientId();
            var cart = GetCart();
            if (patientId == null)
            {
                Message = "Please select a patient before saving the indent.";
                return Page();
            }
            if (!cart.Any())
            {
                Message = "Add at least one investigation / service before saving.";
                return Page();
            }

            var patient = await _db.Patients
                .Include(p => p.Person)
                .FirstOrDefaultAsync(p => p.Id == patientId.Value && !p.Retired);
            if (patient == null)
            {
                Message = "Selected patient not found.";
                return Page();
            }

            var encounter = await GetSelectedEncounterAsync(patient.Id);

            double total = Math.Round(cart.Sum(c => c.GrossValue), 2);
            double itemDiscount = Math.Round(cart.Sum(c => c.Discount), 2);
            double maxBillDiscount = Math.Max(total - itemDiscount, 0);
            double billDiscount = 0; // Bill-level discount removed; only item-level discounts
            double netTotal = Math.Round(total - itemDiscount - billDiscount, 2);
            if (netTotal < 0) netTotal = 0;

            var now = DateTime.Now;
            var userId = _appSession.WebUserId;
            var site = _appSession.LoggedSite;
            var dept = LabDepartment ?? _appSession.LoggedDepartment ?? await _db.Departments.OrderBy(d => d.Id).FirstOrDefaultAsync();

            bool ipd = encounter != null;

            var bill = new Bill
            {
                BillType = ipd ? BillType.InwardServiceBill : BillType.OpdBill,
                BillClassType = ipd ? BillClassType.PreBill : BillClassType.BilledBill,
                PatientId = patient.Id,
                PersonId = patient.PersonId,
                PatientEncounterId = ipd ? encounter.Id : null,
                InstitutionId = site?.Id ?? patient.CreatedInstitutionId,
                DepartmentId = dept?.Id,
                CreaterId = userId,
                CreatedAt = now,
                BillDate = now,
                GrantTotal = total,
                Total = total,
                BillTotal = total,
                Discount = itemDiscount + billDiscount,
                NetTotal = netTotal,
                Balance = netTotal,
                Paid = false,
                BillPaymentCompletelySettled = false,
                PaymentMethod = null,
                TenderedAmount = 0,
                CashPaid = 0,
                CashBalance = 0,
                ReferenceNumber = ipd ? encounter.BhtNo : null,
                Comments = "Lab service indent. " + (Comments ?? string.Empty)
            };
            _db.Bills.Add(bill);
            await _db.SaveChangesAsync();

            int serial = 1;
            foreach (var c in cart)
            {
                var gross = Math.Round(c.Qty * c.Rate, 2);
                var lineDiscount = c.Discount > 0 ? Math.Round(c.Discount, 2) : Math.Round(gross - c.NetValue, 2);
                bill.BillItems.Add(new BillItem
                {
                    ItemId1 = c.ItemId,
                    Qty = c.Qty,
                    Rate = c.Rate,
                    NetRate = c.Rate,
                    GrossValue = gross,
                    Discount = lineDiscount,
                    NetValue = Math.Round(gross - lineDiscount, 2),
                    SearialNo = serial++,
                    CreatedAt = now,
                    CreaterId = userId
                });
            }
            await _db.SaveChangesAsync();

            HttpContext.Session.Remove(CartKey);

            TempData["IndentSuccess"] = ipd
                ? $"Lab indent #{bill.Id} saved to IPD bill of #{encounter.Id} ({encounter.BhtNo}). Amount {netTotal:N2} added to the patient account."
                : $"Lab bill #{bill.Id} created as UNPAID. Patient must pay at Cashier. Amount due: {netTotal:N2}.";

            return RedirectToPage("/Lab/Indent");
        }

        public async Task<IActionResult> OnGetPrintInvoiceAsync(long billId)
        {
            var bill = await _db.Bills
                .Include(b => b.Patient).ThenInclude(p => p.Person)
                .Include(b => b.Department)
                .Include(b => b.BillItems).ThenInclude(bi => bi.Item)
                .Include(b => b.PatientEncounter)
                .FirstOrDefaultAsync(b => b.Id == billId && !b.Retired);

            if (bill == null) return NotFound();

            var invoice = BuildInvoiceModel(bill);
            var pdfBytes = BuildInvoicePdf(invoice);
            return base.File(pdfBytes, "application/pdf", $"LabInvoice-{bill.Id}.pdf");
        }

        public async Task<IActionResult> OnGetViewInvoiceAsync(long billId)
        {
            var bill = await _db.Bills
                .Include(b => b.Patient).ThenInclude(p => p.Person)
                .Include(b => b.Department)
                .Include(b => b.BillItems).ThenInclude(bi => bi.Item)
                .Include(b => b.PatientEncounter)
                .FirstOrDefaultAsync(b => b.Id == billId && !b.Retired);

            if (bill == null) return NotFound();

            Invoice = BuildInvoiceModel(bill);
            await LoadStateAsync();
            return Page();
        }

        private LabInvoiceModel BuildInvoiceModel(Bill bill)
        {
            var lines = new List<InvoiceLine>();
            if (bill.BillItems != null)
            {
                foreach (var bi in bill.BillItems)
                {
                    lines.Add(new InvoiceLine
                    {
                        ItemName = bi.Item?.PrintName ?? bi.Item?.Name ?? "Service",
                        Qty = (int)(bi.Qty ?? 1),
                        Rate = bi.Rate,
                        Discount = bi.Discount,
                        NetValue = bi.NetValue
                    });
                }
            }

            return new LabInvoiceModel
            {
                BillId = bill.Id,
                InvoiceNumber = bill.InvoiceNumber ?? $"INV-{bill.Id:D6}",
                HospitalName = _appSession.LoggedSite?.PrintingName ?? _appSession.LoggedSite?.Name ?? "HMIS Hospital",
                HospitalNo = _appSession.LoggedSite?.RegNo ?? _appSession.LoggedSite?.Code ?? "",
                InvoiceDate = bill.CreatedAt ?? DateTime.Now,
                PatientName = bill.Patient?.Person?.NameWithInitials ?? bill.Patient?.Person?.Name ?? "Patient",
                PatientPhn = bill.Patient?.PatientId,
                VisitNumber = bill.PatientEncounterId?.ToString() ?? bill.ReferenceNumber ?? "-",
                DepartmentName = bill.Department?.Name ?? "Laboratory",
                Lines = lines,
                SubTotal = bill.GrantTotal,
                Discount = bill.Discount,
                NetTotal = bill.NetTotal,
                Cashier = _appSession.LoggedUser?.WebUserPerson?.NameWithInitials
                    ?? _appSession.LoggedUser?.RealUserName
                    ?? _appSession.LoggedUser?.UserName
                    ?? "Staff",
                IsIPD = bill.BillType == BillType.InwardServiceBill
            };
        }

        private byte[] BuildInvoicePdf(LabInvoiceModel inv)
        {
            HmisFontResolver.Install();
            const double PW = 595.28; // A4 width
            const double PH = 841.89; // A4 height
            const double margin = 30;

            using var ms = new MemoryStream();
            using (var doc = new PdfDocument())
            {
                var page = doc.AddPage();
                page.Width = PW;
                page.Height = PH;
                using (var g = XGraphics.FromPdfPage(page))
                {
                    DrawInvoice(g, inv, margin, margin, PW - margin * 2, PH - margin * 2);
                }
                doc.Save(ms, false);
            }
            return ms.ToArray();
        }

        private void DrawInvoice(XGraphics g, LabInvoiceModel inv, double x, double y, double w, double h)
        {
            var pen = new XPen(XColor.FromArgb(70, 70, 70), 1);
            var brush = XBrushes.Black;
            var brushGray = XBrushes.Gray;
            var brushLight = new XSolidBrush(XColor.FromArgb(240, 240, 240));

            double cx = x;
            double cw = w;
            double ty = y;

            var fTitle = new XFont("Arial", 18, XFontStyle.Bold);
            var fSub = new XFont("Arial", 10, XFontStyle.Bold);
            var fNormal = new XFont("Arial", 9);
            var fSmall = new XFont("Arial", 8);
            var fBold = new XFont("Arial", 9, XFontStyle.Bold);
            var fHeader = new XFont("Arial", 9, XFontStyle.Bold);

            // Header - Hospital Name
            g.DrawString(inv.HospitalName, fTitle, brush, new XRect(cx, ty, cw, 28), XStringFormats.Center);
            ty += 26;
            g.DrawString(inv.HospitalNo, fSmall, brushGray, new XRect(cx, ty, cw, 14), XStringFormats.Center);
            ty += 16;
            g.DrawString(inv.IsIPD ? "IPD LAB SERVICE INVOICE" : "LAB SERVICE INVOICE (OPD)", fSub, brush, new XRect(cx, ty, cw, 18), XStringFormats.Center);
            ty += 20;

            // Line
            g.DrawLine(pen, cx, ty, cx + cw, ty);
            ty += 10;

            // Invoice info row
            var colW = cw / 2;
            ty = DrawRow(g, fSmall, "Invoice No:", inv.InvoiceNumber, cx, ty, colW);
            ty = DrawRow(g, fSmall, "Date:", inv.InvoiceDate.ToString("dd MMM yyyy HH:mm"), cx + colW, ty - 14, colW);
            ty += 4;

            ty = DrawRow(g, fSmall, "Patient:", inv.PatientName, cx, ty, colW);
            ty = DrawRow(g, fSmall, "PHN:", inv.PatientPhn?.ToString() ?? "-", cx + colW, ty - 14, colW);
            ty += 4;

            ty = DrawRow(g, fSmall, "Visit/Ref:", inv.VisitNumber, cx, ty, colW);
            ty = DrawRow(g, fSmall, "Department:", inv.DepartmentName, cx + colW, ty - 14, colW);
            ty += 4;

            if (inv.IsIPD)
            {
                ty = DrawRow(g, fSmall, "Type:", "IPD (Added to patient account)", cx, ty, cw);
                ty += 4;
            }

            g.DrawLine(pen, cx, ty, cx + cw, ty);
            ty += 8;

            // Table header
            double col1 = cw * 0.50;
            double col2 = cw * 0.10;
            double col3 = cw * 0.15;
            double col4 = cw * 0.12;
            double col5 = cw * 0.13;

            g.DrawRectangle(pen, brushLight, cx, ty, cw, 18);
            g.DrawString("Description", fHeader, brush, new XRect(cx + 4, ty, col1 - 8, 18), XStringFormats.CenterLeft);
            g.DrawString("Qty", fHeader, brush, new XRect(cx + col1, ty, col2, 18), XStringFormats.Center);
            g.DrawString("Rate", fHeader, brush, new XRect(cx + col1 + col2, ty, col3, 18), XStringFormats.CenterRight);
            g.DrawString("Disc", fHeader, brush, new XRect(cx + col1 + col2 + col3, ty, col4, 18), XStringFormats.CenterRight);
            g.DrawString("Amount", fHeader, brush, new XRect(cx + col1 + col2 + col3 + col4, ty, col5, 18), XStringFormats.CenterRight);
            ty += 20;
            g.DrawLine(pen, cx, ty, cx + cw, ty);

            // Table rows
            foreach (var line in inv.Lines)
            {
                if (ty > y + h - 80)
                {
                    g.DrawString("... (more items)", fSmall, brushGray, new XRect(cx + 4, ty, cw, 14), XStringFormats.CenterLeft);
                    ty += 16;
                    break;
                }

                g.DrawString(line.ItemName, fNormal, brush, new XRect(cx + 4, ty, col1 - 8, 14), XStringFormats.CenterLeft);
                g.DrawString(line.Qty.ToString(), fNormal, brush, new XRect(cx + col1, ty, col2, 14), XStringFormats.Center);
                g.DrawString(line.Rate.ToString("N2"), fNormal, brush, new XRect(cx + col1 + col2, ty, col3, 14), XStringFormats.CenterRight);
                g.DrawString(line.Discount > 0 ? line.Discount.ToString("N2") : "-", fNormal, brush, new XRect(cx + col1 + col2 + col3, ty, col4, 14), XStringFormats.CenterRight);
                g.DrawString(line.NetValue.ToString("N2"), fBold, brush, new XRect(cx + col1 + col2 + col3 + col4, ty, col5, 14), XStringFormats.CenterRight);
                ty += 16;
            }

            g.DrawLine(pen, cx, ty, cx + cw, ty);
            ty += 6;

            // Totals
            double totalCol = cw * 0.7;
            double amtCol = cw * 0.3;

            ty = DrawTotalRow(g, fNormal, "Sub Total", inv.SubTotal.ToString("N2"), cx, ty, totalCol, amtCol);
            ty = DrawTotalRow(g, fNormal, "Discount", inv.Discount.ToString("N2"), cx, ty, totalCol, amtCol);
            ty += 2;
            g.DrawLine(new XPen(XColor.FromArgb(100, 100, 100), 1), cx + totalCol, ty, cx + cw, ty);
            ty += 4;
            ty = DrawTotalRow(g, fBold, "NET TOTAL", inv.NetTotal.ToString("N2"), cx, ty, totalCol, amtCol);
            ty += 10;

            if (inv.IsIPD)
            {
                g.DrawString("*** IPD Patient - Amount added to account. Settle at discharge ***", fSmall, brushGray, new XRect(cx, ty, cw, 14), XStringFormats.Center);
                ty += 16;
            }
            else
            {
                g.DrawString("*** NOT PAID - Present this invoice at Cashier for payment ***", fSmall, XBrushes.Red, new XRect(cx, ty, cw, 14), XStringFormats.Center);
                ty += 16;
            }

            // Footer
            ty = y + h - 50;
            g.DrawLine(pen, cx, ty, cx + cw, ty);
            ty += 6;
            g.DrawString($"Generated by: {inv.Cashier}  |  {DateTime.Now:dd MMM yyyy HH:mm}", fSmall, brushGray, new XRect(cx, ty, cw, 14), XStringFormats.Center);
            ty += 12;
            g.DrawString("Thank you for choosing our laboratory services", fSmall, brushGray, new XRect(cx, ty, cw, 14), XStringFormats.Center);
        }

        private double DrawRow(XGraphics g, XFont f, string label, string value, double x, double y, double w)
        {
            g.DrawString(label, f, XBrushes.DarkGray, new XRect(x + 4, y, w * 0.35, 14), XStringFormats.CenterLeft);
            g.DrawString(value ?? "-", f, XBrushes.Black, new XRect(x + w * 0.38, y, w * 0.6, 14), XStringFormats.CenterLeft);
            return y + 14;
        }

        private double DrawTotalRow(XGraphics g, XFont f, string label, string value, double x, double y, double labelW, double valW)
        {
            g.DrawString(label, f, XBrushes.Black, new XRect(x + labelW - 120, y, 115, 16), XStringFormats.CenterRight);
            g.DrawString(value, f, XBrushes.Black, new XRect(x + labelW, y, valW, 16), XStringFormats.CenterRight);
            return y + 16;
        }

        private async Task LoadStateAsync()
        {
            await LoadLookupsAsync();
            SelectedPatient = await GetSelectedPatientAsync();
            DisplayCart = GetCart();
            Cart = DisplayCart;
            if (SelectedPatient != null)
            {
                ActiveEncounters = await GetActiveEncountersAsync(SelectedPatient.Id);
                if (SelectedEncounterId.HasValue && ActiveEncounters.Any(e => e.Id == SelectedEncounterId.Value))
                    SelectedEncounter = ActiveEncounters.First(e => e.Id == SelectedEncounterId.Value);
                else if (ActiveEncounters.Any())
                    SelectedEncounter = ActiveEncounters.First();
                else
                    SelectedEncounter = null;

                UnpaidBills = await _db.Bills
                    .Where(b => b.PatientId == SelectedPatient.Id && !b.Retired && !b.Cancelled && !b.Paid)
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync();
            }
            else
            {
                ActiveEncounters = new();
                SelectedEncounter = null;
                UnpaidBills = new();
            }
            ComputeTotals();
            if (TempData["IndentMessage"] != null)
                Message = TempData["IndentMessage"].ToString();
        }

        private void ComputeTotals()
        {
            Total = Math.Round(DisplayCart.Sum(c => c.GrossValue), 2);
            DiscountTotal = Math.Round(DisplayCart.Sum(c => c.Discount), 2);
            NetTotal = Math.Round(Total - DiscountTotal, 2);
            if (NetTotal < 0) NetTotal = 0;
        }

        private async Task SearchItemsAsync()
        {
            var term = ItemSearch?.Trim();
            var q = _db.Items.Where(i => !i.Retired && i.Billable
                && (i.ItemType == ItemType.Investigation || i.ItemType == ItemType.Service));

            if (LabDepartment != null)
                q = q.Where(i => i.DepartmentId == LabDepartment.Id);

            if (!string.IsNullOrWhiteSpace(term))
            {
                q = q.Where(i => (i.Name != null && i.Name.Contains(term))
                    || (i.Sname != null && i.Sname.Contains(term))
                    || (i.PrintName != null && i.PrintName.Contains(term))
                    || (i.Code != null && i.Code.Contains(term)));
            }

            ItemResults = await q.OrderBy(i => i.ItemType).ThenBy(i => i.Name).Take(50).ToListAsync();
        }

        private async Task<PatientEncounter> GetSelectedEncounterAsync(long patientId)
        {
            if (!SelectedEncounterId.HasValue) return null;
            var encounters = await GetActiveEncountersAsync(patientId);
            return encounters.FirstOrDefault(e => e.Id == SelectedEncounterId.Value);
        }

        private async Task<List<PatientEncounter>> GetActiveEncountersAsync(long patientId)
        {
            return await _db.PatientEncounters
                .Where(e => e.PatientId == patientId && !e.Retired
                    && e.EncounterType == EncounterType.Inward && e.DateOfDischarge == null)
                .OrderByDescending(e => e.DateOfAdmission)
                .ToListAsync();
        }

        private long? GetSelectedPatientId()
        {
            var val = HttpContext.Session.GetString(PatientKey);
            return long.TryParse(val, out var id) ? id : null;
        }

        private async Task<Patient> GetSelectedPatientAsync()
        {
            var id = GetSelectedPatientId();
            if (id == null) return null;
            return await _db.Patients
                .Include(p => p.Person)
                .FirstOrDefaultAsync(p => p.Id == id && !p.Retired);
        }

        private List<IndentCartItem> GetCart()
        {
            var raw = HttpContext.Session.GetString(CartKey);
            if (string.IsNullOrWhiteSpace(raw)) return new List<IndentCartItem>();
            try
            {
                return JsonSerializer.Deserialize<List<IndentCartItem>>(raw) ?? new List<IndentCartItem>();
            }
            catch
            {
                return new List<IndentCartItem>();
            }
        }

        private void SaveCart(List<IndentCartItem> cart)
        {
            HttpContext.Session.SetString(CartKey, JsonSerializer.Serialize(cart));
        }

        private async Task LoadLookupsAsync()
        {
            // Find laboratory department (DepartmentType == Laboratory)
            LabDepartment = await _db.Departments
                .Where(d => !d.Retired && d.DepartmentType == DepartmentType.Laboratory)
                .FirstOrDefaultAsync();

            // Fallback: if no lab department found, use logged department or first available
            if (LabDepartment == null)
            {
                LabDepartment = _appSession.LoggedDepartment ?? await _db.Departments
                    .Where(d => !d.Retired)
                    .OrderBy(d => d.Name)
                    .FirstOrDefaultAsync();
            }
        }

        public class IndentCartItem
        {
            public long ItemId { get; set; }
            public string Code { get; set; }
            public string ItemName { get; set; }
            public int Qty { get; set; }
            public double Rate { get; set; }
            public double Discount { get; set; }
            public double GrossValue => Math.Round(Qty * Rate, 2);
            public double NetValue => Math.Round(GrossValue - Discount, 2);
        }

        internal sealed class HmisFontResolver : IFontResolver
        {
            private static readonly string FontsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");

            public string DefaultFontName => "arial.ttf";

            public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
            {
                return new FontResolverInfo(isBold ? "arialbd.ttf" : "arial.ttf", XStyleSimulations.None);
            }

            public byte[] GetFont(string faceName)
            {
                return System.IO.File.ReadAllBytes(Path.Combine(FontsDir, faceName));
            }

            public static void Install()
            {
                if (GlobalFontSettings.FontResolver == null)
                    GlobalFontSettings.FontResolver = new HmisFontResolver();
            }
        }
    }
}