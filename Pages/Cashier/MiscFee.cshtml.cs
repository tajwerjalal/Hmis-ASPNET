using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Cashier
{
    public class MiscFeeModel : CashierPageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;
        private readonly ICommonService _common;

        public MiscFeeModel(ApplicationDbContext db, IAppSession appSession, ICommonService common)
        {
            _db = db;
            _appSession = appSession;
            _common = common;
        }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        [BindProperty] public string FormBuyerType { get; set; } = "Patient";
        [BindProperty] public long? FormPatientId { get; set; }
        [BindProperty] public string? FormBuyerName { get; set; }
        [BindProperty] public string FormFeeHead { get; set; } = "";
        [BindProperty] public string? FormDescription { get; set; }
        [BindProperty] public double FormAmount { get; set; }

        public List<MiscFee> TodayFees { get; set; } = new();
        public List<MiscFee> HistoryFees { get; set; } = new();

        public async Task OnGetAsync(DateTime? from, DateTime? to)
        {
            FromDate = from ?? DateTime.Today.AddDays(-7);
            ToDate = to ?? DateTime.Today;

            if (TempData["Success"] != null) SuccessMessage = TempData["Success"]!.ToString();
            if (TempData["Error"] != null) ErrorMessage = TempData["Error"]!.ToString();

            await LoadTodayFeesAsync();
            await LoadHistoryAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (FormAmount <= 0)
            {
                ErrorMessage = "Amount must be greater than zero.";
                await LoadTodayFeesAsync();
                return Page();
            }

            if (string.IsNullOrWhiteSpace(FormFeeHead))
            {
                ErrorMessage = "Please select a fee head.";
                await LoadTodayFeesAsync();
                return Page();
            }

            var buyerName = FormBuyerType switch
            {
                "Patient" => "",
                "Employee" => FormBuyerName?.Trim() ?? "",
                _ => FormBuyerName?.Trim() ?? ""
            };

            if (FormBuyerType == "Patient" && !FormPatientId.HasValue)
            {
                ErrorMessage = "Please select a patient.";
                await LoadTodayFeesAsync();
                return Page();
            }

            if ((FormBuyerType == "Employee" || FormBuyerType == "Other") && string.IsNullOrWhiteSpace(buyerName))
            {
                ErrorMessage = "Please enter a name.";
                await LoadTodayFeesAsync();
                return Page();
            }

            var miscFee = new MiscFee
            {
                BuyerType = FormBuyerType,
                PatientId = FormBuyerType == "Patient" ? FormPatientId : null,
                BuyerName = buyerName,
                FeeHead = FormFeeHead,
                Description = FormDescription?.Trim() ?? "",
                Amount = FormAmount,
                AmountInWords = NumberToWords(FormAmount),
                Paid = false,
                InstitutionId = _appSession.LoggedSite?.Id,
                CreaterId = _appSession.WebUserId,
                CreatedAt = DateTime.Now
            };

            _db.MiscFees.Add(miscFee);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Miscellaneous fee of Rs {FormAmount:N2} created. Please receive payment.";
            return RedirectToPage(new { from = FromDate?.ToString("yyyy-MM-dd"), to = ToDate?.ToString("yyyy-MM-dd") });
        }

        public async Task<IActionResult> OnPostReceivePaymentAsync(long miscFeeId)
        {
            var fee = await _db.MiscFees.FindAsync(miscFeeId);
            if (fee == null || fee.Paid)
            {
                TempData["Error"] = "Fee not found or already paid.";
                return RedirectToPage(new { from = FromDate?.ToString("yyyy-MM-dd"), to = ToDate?.ToString("yyyy-MM-dd") });
            }

            fee.Paid = true;
            fee.PaymentMethod = PaymentMethod.Cash;
            fee.CashReceived = fee.Amount;
            fee.CashReturn = 0;
            fee.PaidAt = DateTime.Now;
            fee.ReceivedById = _appSession.WebUserId;

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Payment of Rs {fee.Amount:N2} received successfully.";
            return RedirectToPage(new { from = FromDate?.ToString("yyyy-MM-dd"), to = ToDate?.ToString("yyyy-MM-dd") });
        }

        public async Task<IActionResult> OnGetSearchPatientAsync(string q)
        {
            var patients = await _common.SearchPatientsAsync(q);
            var results = patients.Select(p => new
            {
                id = p.Id,
                name = (p.Person?.NameWithInitials ?? p.Person?.Name ?? "").Trim(),
                phn = p.Phn ?? ""
            }).ToList();

            return new JsonResult(results);
        }

        private async Task LoadTodayFeesAsync()
        {
            TodayFees = await _db.MiscFees
                .Include(f => f.Patient).ThenInclude(p => p.Person)
                .Where(f => !f.Retired && f.CreatedAt != null && f.CreatedAt!.Value.Date == DateTime.Today)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        private async Task LoadHistoryAsync()
        {
            HistoryFees = await _db.MiscFees
                .Include(f => f.Patient).ThenInclude(p => p.Person)
                .Where(f => !f.Retired && f.CreatedAt >= FromDate && f.CreatedAt <= ToDate!.Value.AddDays(1))
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        private static string NumberToWords(double num)
        {
            if (num == 0) return "Zero";
            var ones = new[] { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
                "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen",
                "Seventeen", "Eighteen", "Nineteen" };
            var tens = new[] { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };
            var n = (long)num;
            var result = "";
            if (n >= 10000000) { result += NumberToWords(n / 10000000) + " Crore "; n %= 10000000; }
            if (n >= 100000) { result += NumberToWords(n / 100000) + " Lakh "; n %= 100000; }
            if (n >= 1000) { result += NumberToWords(n / 1000) + " Thousand "; n %= 1000; }
            if (n >= 100) { result += ones[n / 100] + " Hundred "; n %= 100; }
            if (n >= 20) { result += tens[n / 10] + " "; n %= 10; }
            if (n > 0) { result += ones[n] + " "; }
            return result.Trim();
        }
    }
}
