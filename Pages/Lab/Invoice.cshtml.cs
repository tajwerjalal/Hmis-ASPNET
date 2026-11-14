// Developer : HMIS Assistant
// Date/Time : 28-Oct-2026 13:00
// Description : Page model for printable lab service invoice (HTML view).
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HmisWeb.Data;
using HmisWeb.Services;
using System;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Lab
{
    public class InvoiceModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ICommonService _common;
        private readonly IAppSession _appSession;

        public InvoiceModel(ApplicationDbContext db, ICommonService common, IAppSession appSession)
        {
            _db = db;
            _common = common;
            _appSession = appSession;
        }

        public string HospitalName => Invoice?.HospitalName;
        public string HospitalAddress => Invoice?.HospitalAddress;
        public string HospitalPhone => Invoice?.HospitalPhone;
        public string HospitalNo => Invoice?.HospitalNo;
        public string InvoiceNumber => Invoice?.InvoiceNumber;
        public DateTime InvoiceDate => Invoice?.InvoiceDate ?? DateTime.Now;
        public string PatientName => Invoice?.PatientName;
        public long? PatientPhn => Invoice?.PatientPhn;
        public string PatientRegNo => Invoice?.PatientRegNo;
        public string PatientAge => Invoice?.PatientAge;
        public string PatientSex => Invoice?.PatientSex;
        public string PatientPhone => Invoice?.PatientPhone;
        public string PatientAddress => Invoice?.PatientAddress;
        public string Guardian => Invoice?.Guardian;
        public string Diagnosis => Invoice?.Diagnosis;
        public string Bed => Invoice?.Bed;
        public string VisitNumber => Invoice?.VisitNumber;
        public string DepartmentName => Invoice?.DepartmentName;
        public List<InvoiceLine> Lines => Invoice?.Lines ?? new();
        public double SubTotal => Invoice?.SubTotal ?? 0;
        public double Discount => Invoice?.Discount ?? 0;
        public double NetTotal => Invoice?.NetTotal ?? 0;
        public string Cashier => Invoice?.Cashier;
        public bool IsIPD => Invoice?.IsIPD ?? false;
        public int ReprintCount => Invoice?.ReprintCount ?? 0;
        public string Comments => Invoice?.Comments;
        public string IndentNo => Invoice?.IndentNo;
        public bool Duplicate => Invoice?.Duplicate ?? false;
        public string PaymentStatus => Invoice?.PaymentStatus ?? "UNPAID";
        public string Mode => Invoice?.Mode ?? "OPD";
        public string ReceiptNo => Invoice?.ReceiptNo;
        public DateTime? PaidAt => Invoice?.PaidAt;
        public DateTime? PrintedAt => Invoice?.PrintedAt;
        public DateTime? DuplicatePrintedAt => Invoice?.DuplicatePrintedAt;
        public long PrintedUserId => Invoice?.PrintedUserId ?? 0;
        public long DuplicatePrintedUserId => Invoice?.DuplicatePrintedUserId ?? 0;
        public int PrintCount => Invoice?.PrintCount ?? 0;

        public LabInvoiceModel Invoice { get; set; }

        public async Task<IActionResult> OnGetAsync(long billId, string format = "a5", bool? print = null, bool duplicate = false)
        {
            if (billId <= 0) return NotFound();

            ViewData["InvoiceFormat"] = format == "thermal" ? "thermal" : "a5";

            var bill = await _db.Bills
                .AsNoTracking()
                .Include(b => b.Patient).ThenInclude(p => p.Person)
                .Include(b => b.Department)
                .Include(b => b.BillItems).ThenInclude(bi => bi.Item)
                .FirstOrDefaultAsync(b => b.Id == billId && !b.Retired);

            if (bill == null) return NotFound();

            Invoice = await BuildFromBill(bill, duplicate);
            if (print == true)
            {
                // Mark original / duplicate print and audit-log the duplicate count.
                Invoice.PrintedAt = bill.PrintedAt ?? DateTime.Now;
                Invoice.PrintedUserId = _appSession.WebUserId ?? 0;
                Invoice.DuplicatePrintedAt = bill.DuplicatePrintedAt;
                Invoice.DuplicatePrintedUserId = bill.DuplicatePrintedUserId ?? 0;
                Invoice.PrintCount = await GetPrintCountAsync(bill.Id, duplicate);
                if (duplicate)
                {
                    bill.DuplicatePrintedAt = DateTime.Now;
                    bill.DuplicatePrintedUserId = _appSession.WebUserId;
                    await _db.SaveChangesAsync();
                }
            }
            return Page();
        }

        /// <summary>
        /// Returns the cumulative print count for the (bill, duplicate) pair from the
        /// INDENT_PRINT_LOG audit table. Increments the duplicate counter on every reprint.
        /// Exception-safe: a missing/unavailable audit table must never crash the print flow.
        /// </summary>
        private async Task<int> GetPrintCountAsync(long billId, bool duplicate)
        {
            try
            {
                var log = await _db.IndentPrintLogs.AsNoTracking()
                    .FirstOrDefaultAsync(l => l.BillId == billId && l.Duplicate == duplicate);
                if (log == null) return 0;
                return log.PrintCount;
            }
            catch (Exception)
            {
                // Audit table not provisioned yet - fail open with a zero print count.
                return 0;
            }
        }

        private async Task<LabInvoiceModel> BuildFromBill(Bill bill, bool duplicate)
        {
            // Minimal mirror of Indent.BuildInvoiceModel for the invoice view.
            var lines = new System.Collections.Generic.List<InvoiceLine>();
            if (bill.BillItems != null)
            {
                foreach (var bi in bill.BillItems)
                {
                    lines.Add(new InvoiceLine
                    {
                        Code = bi.ItemId?.ToString(),
                        ItemName = bi.Item?.PrintName ?? bi.Item?.Name ?? bi.Descreption ?? "Service",
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
                IndentNo = $"LI-{bill.Id:D6}",
                HospitalName = "HMIS Hospital",
                HospitalAddress = "-",
                HospitalPhone = "-",
                HospitalNo = "",
                InvoiceDate = bill.BillDate ?? bill.CreatedAt ?? DateTime.Now,
                PatientName = bill.Patient?.Person?.NameWithInitials ?? bill.Patient?.Person?.Name ?? "Patient",
                PatientPhn = bill.Patient?.PatientId,
                PatientRegNo = bill.Patient?.PatientId?.ToString(),
                PatientAge = null,
                PatientSex = null,
                PatientPhone = bill.Patient?.Person?.Mobile ?? "-",
                PatientAddress = bill.Patient?.Person?.Address ?? "-",
                Guardian = bill.Patient?.Person?.NameWithInitials ?? "-",
                Diagnosis = "-",
                Bed = "-",
                VisitNumber = bill.ReferenceNumber ?? "-",
                DepartmentName = bill.Department?.Name ?? "Laboratory",
                Lines = lines,
                SubTotal = bill.GrantTotal,
                Discount = bill.Discount,
                NetTotal = bill.NetTotal,
                Cashier = "Staff",
                IsIPD = bill.BillType == BillType.InwardServiceBill,
                Comments = bill.Comments,
                PaymentStatus = bill.Paid ? "PAID" : "UNPAID",
                Mode = bill.BillType == BillType.InwardServiceBill ? "IPD" : "OPD",
                ReceiptNo = "-",
                PaidAt = bill.PaidAt,
                PrintedAt = bill.PrintedAt,
                DuplicatePrintedAt = bill.DuplicatePrintedAt,
                PrintedUserId = bill.PrintedUserId ?? 0,
                DuplicatePrintedUserId = bill.DuplicatePrintedUserId ?? 0,
                PrintCount = await GetPrintCountAsync(bill.Id, duplicate),
                Duplicate = duplicate
            };
        }
    }
}
