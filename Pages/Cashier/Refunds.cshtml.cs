using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Cashier
{
    public class RefundsModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;

        public RefundsModel(ApplicationDbContext db, IAppSession appSession)
        {
            _db = db;
            _appSession = appSession;
        }

        public string Tab { get; set; } = "pending";
        public string? Q { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public List<RefundRequest> PendingRefunds { get; set; } = new();
        public List<RefundRequest> HistoryRefunds { get; set; } = new();
        public List<PaidBillSearchResult> SearchResults { get; set; } = new();

        public async Task OnGetAsync(string? tab, string? q, DateTime? from, DateTime? to)
        {
            Tab = tab ?? "pending";
            Q = q;
            FromDate = from ?? DateTime.Today.AddDays(-30);
            ToDate = to ?? DateTime.Today;

            if (TempData["Success"] != null) SuccessMessage = TempData["Success"]!.ToString();
            if (TempData["Error"] != null) ErrorMessage = TempData["Error"]!.ToString();

            await LoadPendingRefundsAsync();
            await LoadHistoryAsync();

            if (Tab == "new" && !string.IsNullOrWhiteSpace(Q))
                await SearchPaidBillsAsync();
        }

        public async Task<IActionResult> OnPostRequestRefundAsync(long billId, string feeType, string reason)
        {
            var bill = await _db.Bills.FindAsync(billId);
            if (bill == null || !bill.Paid || bill.Refunded || bill.Retired)
            {
                TempData["Error"] = "Bill not found or already refunded.";
                return RedirectToPage(new { tab = "new", Q });
            }

            var patient = bill.PatientId.HasValue
                ? await _db.Patients.Include(p => p.Person).FirstOrDefaultAsync(p => p.Id == bill.PatientId.Value)
                : null;

            var request = new RefundRequest
            {
                BillId = billId,
                PatientId = bill.PatientId,
                VisitId = bill.PatientEncounterId,
                InstitutionId = bill.InstitutionId,
                DepartmentId = bill.DepartmentId,
                PatientName = patient != null ? (patient.Person?.NameWithInitials ?? patient.Person?.Name ?? "Unknown").Trim() : "Unknown",
                FeeType = feeType,
                FeeAmount = bill.NetTotal,
                Reason = reason,
                Approved = false,
                Refunded = false,
                Cancelled = false,
                CreaterId = _appSession.WebUserId,
                CreatedAt = DateTime.Now
            };

            _db.RefundRequests.Add(request);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Refund request submitted for {request.PatientName} - Rs {request.FeeAmount:N2}";
            return RedirectToPage(new { tab = "pending" });
        }

        public async Task<IActionResult> OnPostProcessRefundAsync(long refundId)
        {
            var request = await _db.RefundRequests
                .Include(r => r.Bill)
                .FirstOrDefaultAsync(r => r.Id == refundId);

            if (request == null || !request.Approved || request.Refunded)
            {
                TempData["Error"] = "Refund request not found or already processed.";
                return RedirectToPage(new { tab = "pending" });
            }

            using var txn = await _db.Database.BeginTransactionAsync();
            try
            {
                if (request.BillId.HasValue && request.Bill != null)
                {
                    var originalBill = request.Bill;
                    originalBill.Refunded = true;
                    originalBill.RefundAmount = request.FeeAmount;

                    var refundBill = new Bill
                    {
                        BillClassType = BillClassType.RefundBill,
                        BillType = BillType.OpdReturn,
                        PaymentMethod = PaymentMethod.Cash,
                        InvoiceDate = DateTime.Now,
                        InvoiceNumber = $"RF-{DateTime.Now:yyyyMMdd}-{originalBill.Id}",
                        Total = -originalBill.Total,
                        NetTotal = -originalBill.NetTotal,
                        Discount = 0,
                        Vat = 0,
                        VatPlusNetTotal = -originalBill.NetTotal,
                        BillTotal = -originalBill.NetTotal,
                        Paid = true,
                        PaidAmount = request.FeeAmount,
                        Balance = 0,
                        TenderedAmount = 0,
                        RefundAmount = request.FeeAmount,
                        PatientId = originalBill.PatientId,
                        PatientEncounterId = originalBill.PatientEncounterId,
                        InstitutionId = originalBill.InstitutionId,
                        DepartmentId = originalBill.DepartmentId,
                        BillDate = DateTime.Now,
                        BillTime = DateTime.Now,
                        PaidAt = DateTime.Now,
                        CheckedById = _appSession.WebUserId,
                        CreaterId = _appSession.WebUserId,
                        CreatedAt = DateTime.Now,
                        BilledBillId = originalBill.Id,
                        Comments = $"Refund for {request.FeeType}: {request.Reason}"
                    };

                    _db.Bills.Add(refundBill);
                    await _db.SaveChangesAsync();

                    foreach (var bi in originalBill.BillItems.Where(i => !i.Refunded.GetValueOrDefault()))
                    {
                        bi.Refunded = true;
                        var refundItem = new BillItem
                        {
                            BillId = refundBill.Id,
                            ItemId1 = bi.ItemId1,
                            Qty = -bi.Qty,
                            Rate = bi.Rate,
                            NetRate = bi.NetRate,
                            GrossValue = -bi.GrossValue,
                            Discount = 0,
                            NetValue = -bi.NetValue,
                            SearialNo = bi.SearialNo,
                            Descreption = bi.Descreption,
                            CreaterId = _appSession.WebUserId,
                            CreatedAt = DateTime.Now
                        };
                        _db.BillItems.Add(refundItem);
                    }

                    var refundPayment = new Payment
                    {
                        BillId = refundBill.Id,
                        PaymentMethod = PaymentMethod.Cash,
                        PaidValue = request.FeeAmount,
                        Realized = true,
                        RealizedAt = DateTime.Now,
                        InstitutionId = originalBill.InstitutionId,
                        DepartmentId = originalBill.DepartmentId,
                        CreaterId = _appSession.WebUserId,
                        CreatedAt = DateTime.Now
                    };
                    _db.Payments.Add(refundPayment);
                }

                request.Refunded = true;
                request.RefundedById = _appSession.WebUserId;
                request.RefundedAt = DateTime.Now;

                await _db.SaveChangesAsync();
                await txn.CommitAsync();

                TempData["Success"] = $"Refund of Rs {request.FeeAmount:N2} processed successfully for {request.PatientName}.";
            }
            catch (Exception ex)
            {
                await txn.RollbackAsync();
                TempData["Error"] = $"Error processing refund: {ex.Message}";
            }

            return RedirectToPage(new { tab = "pending" });
        }

        private async Task LoadPendingRefundsAsync()
        {
            PendingRefunds = await _db.RefundRequests
                .Include(r => r.Bill)
                .Include(r => r.ApprovedBy)
                .Where(r => !r.Retired && r.Approved && !r.Refunded && !r.Cancelled)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        private async Task LoadHistoryAsync()
        {
            HistoryRefunds = await _db.RefundRequests
                .Include(r => r.Bill)
                .Include(r => r.RefundedBy)
                .Where(r => !r.Retired && r.CreatedAt >= FromDate && r.CreatedAt <= ToDate!.Value.AddDays(1))
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        private async Task SearchPaidBillsAsync()
        {
            var query = _db.Bills
                .Include(b => b.Patient).ThenInclude(p => p.Person)
                .Include(b => b.Department)
                .Where(b => b.Paid && !b.Refunded && !b.Retired && !b.Cancelled && b.BillType == BillType.OpdBill);

            if (long.TryParse(Q, out var id))
            {
                query = query.Where(b => b.Id == id || (b.PatientId.HasValue && b.PatientId.Value == id));
            }
            else
            {
                var term = Q!.ToLower();
                query = query.Where(b =>
                    (b.Patient != null && b.Patient.Person != null &&
                        ((b.Patient.Person.NameWithInitials != null && b.Patient.Person.NameWithInitials.ToLower().Contains(term)) ||
                         (b.Patient.Person.Name != null && b.Patient.Person.Name.ToLower().Contains(term)))) ||
                    (b.InvoiceNumber != null && b.InvoiceNumber.ToLower().Contains(term))
                );
            }

            SearchResults = await query
                .OrderByDescending(b => b.PaidAt)
                .Take(20)
                .Select(b => new PaidBillSearchResult
                {
                    BillId = b.Id,
                    InvoiceNumber = b.InvoiceNumber ?? "",
                    PatientName = (b.Patient != null && b.Patient.Person != null)
                        ? (b.Patient.Person.NameWithInitials ?? b.Patient.Person.Name ?? "").Trim()
                        : "",
                    Phn = b.Patient != null ? b.Patient.Phn ?? "" : "",
                    DepartmentName = b.Department != null ? b.Department.Name : "",
                    PaidAt = b.PaidAt,
                    Amount = b.NetTotal
                })
                .ToListAsync();
        }

        public class PaidBillSearchResult
        {
            public long BillId { get; set; }
            public string InvoiceNumber { get; set; } = "";
            public string PatientName { get; set; } = "";
            public string Phn { get; set; } = "";
            public string DepartmentName { get; set; } = "";
            public DateTime? PaidAt { get; set; }
            public double Amount { get; set; }
        }
    }
}
