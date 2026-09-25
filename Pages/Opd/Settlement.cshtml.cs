using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Opd
{
    public class SettlementModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;

        public SettlementModel(ApplicationDbContext db, IAppSession appSession)
        {
            _db = db;
            _appSession = appSession;
        }

        [BindProperty(SupportsGet = true)]
        public long BillId { get; set; }

        [BindProperty]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        [BindProperty]
        public double TenderedAmount { get; set; }

        [BindProperty]
        public string ChequeRefNo { get; set; }

        [BindProperty]
        public string CreditCardRefNo { get; set; }

        public Bill Bill { get; set; }
        public List<BillItem> BillItems { get; set; } = new();
        public string Message { get; set; }
        public string Success { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (BillId > 0)
            {
                await LoadBillAsync();
                var success = TempData["Success"];
                if (success != null) Success = success.ToString();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadBillAsync();
            if (Bill == null)
                return RedirectToPage("/Opd/PreBilling");

            if (TenderedAmount <= 0)
                TenderedAmount = Bill.NetTotal;

            if (TenderedAmount < Bill.NetTotal - 0.01)
            {
                Message = $"Tendered amount ({TenderedAmount:N2}) is less than bill total ({Bill.NetTotal:N2}).";
                return Page();
            }

            Bill.Paid = true;
            Bill.PaymentMethod = PaymentMethod;
            Bill.PaidAt = DateTime.Now;
            Bill.TenderedAmount = TenderedAmount;
            Bill.CashBalance = TenderedAmount - Bill.NetTotal;
            Bill.BillPaymentCompletelySettled = true;

            _db.Payments.Add(new Payment
            {
                BillId = Bill.Id,
                PaymentMethod = PaymentMethod,
                PaidValue = Bill.NetTotal,
                WrittenAt = DateTime.Now,
                Realized = true,
                RealizedAt = DateTime.Now,
                ChequeRefNo = ChequeRefNo,
                CreditCardRefNo = CreditCardRefNo,
                InstitutionId = _appSession.LoggedSite?.Id,
                DepartmentId = _appSession.LoggedDepartment?.Id,
                CreatedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Bill {Bill.Id} settled successfully. Change due: {(TenderedAmount - Bill.NetTotal):N2}";
            return RedirectToPage("/Opd/Settlement", new { billId = Bill.Id });
        }

        private async Task LoadBillAsync()
        {
            Bill = await _db.Bills
                .Include(b => b.Patient)
                .ThenInclude(p => p.Person)
                .Include(b => b.Institution)
                .Include(b => b.Department)
                .FirstOrDefaultAsync(b => b.Id == BillId && !b.Retired);

            if (Bill != null)
            {
                BillItems = await _db.BillItems
                    .Include(bi => bi.Item)
                    .Where(bi => bi.BillId == Bill.Id && !bi.Retired)
                    .OrderBy(bi => bi.SearialNo)
                    .ToListAsync();
            }
        }
    }
}