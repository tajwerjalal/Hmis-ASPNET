using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Pharmacy
{
    public class SaleCartItem
    {
        public long ItemId { get; set; }
        public string ItemName { get; set; } = "";
        public int Qty { get; set; }
        public double SellingPrice { get; set; }
        public double LineTotal => Qty * SellingPrice;
    }

    public class SaleModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;
        private readonly ICommonService _commonService;
        private const string CartSessionKey = "PharmacySaleCart";

        public SaleModel(ApplicationDbContext db, IAppSession appSession, ICommonService commonService)
        {
            _db = db;
            _appSession = appSession;
            _commonService = commonService;
        }

        [BindProperty]
        public string SearchTerm { get; set; }

        public List<Item> SearchResults { get; set; } = new();
        public List<SaleCartItem> Cart { get; set; } = new();
        public double SubTotal => Cart.Sum(c => c.LineTotal);
        [BindProperty]
        public double DiscountPercent { get; set; }
        public double DiscountAmount => Math.Round(SubTotal * DiscountPercent / 100.0, 2);
        public double NetTotal => SubTotal - DiscountAmount;
        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }

        public void OnGet()
        {
            Cart = GetCartFromSession();
        }

        public async Task<IActionResult> OnPostSearchAsync()
        {
            Cart = GetCartFromSession();
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                SearchResults = await _db.Items
                    .Where(i => !i.Retired && i.ItemType == ItemType.Medicine && i.Billable
                        && (i.Name.ToLower().Contains(term) || i.PrintName.ToLower().Contains(term)
                            || i.Code.ToLower().Contains(term) || i.Barcode.ToLower().Contains(term)))
                    .OrderBy(i => i.Name)
                    .Take(30)
                    .ToListAsync();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAddToCartAsync(long itemId)
        {
            Cart = GetCartFromSession();
            var existing = Cart.FirstOrDefault(c => c.ItemId == itemId);
            if (existing != null)
            {
                existing.Qty += 1;
            }
            else
            {
                var item = await _db.Items.FindAsync(itemId);
                if (item != null)
                {
                    double price = item.Total ?? 0;
                    Cart.Add(new SaleCartItem
                    {
                        ItemId = itemId,
                        ItemName = item.PrintName ?? item.Name,
                        Qty = 1,
                        SellingPrice = price
                    });
                }
            }
            SaveCartToSession(Cart);
            return Page();
        }

        public IActionResult OnPostRemoveFromCart(int index)
        {
            Cart = GetCartFromSession();
            if (index >= 0 && index < Cart.Count)
                Cart.RemoveAt(index);
            SaveCartToSession(Cart);
            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            Cart = GetCartFromSession();
            if (!Cart.Any())
            {
                ErrorMessage = "Cart is empty. Please add items before saving.";
                return Page();
            }

            if (_appSession.LoggedSite == null || _appSession.LoggedDepartment == null)
            {
                ErrorMessage = "Please log in with a valid site and department.";
                return Page();
            }

            var now = DateTime.Now;
            var userId = _appSession.WebUserId;

            var bill = new Bill
            {
                BillClassType = BillClassType.BilledBill,
                BillType = BillType.PharmacySale,
                InstitutionId = _appSession.LoggedSite.Id,
                DepartmentId = _appSession.LoggedDepartment.Id,
                BillDate = now,
                CreatedAt = now,
                CreaterId = userId,
                Total = SubTotal,
                NetTotal = NetTotal,
                Discount = DiscountAmount,
                DiscountPercent = DiscountPercent,
                Paid = true,
                BillPaymentCompletelySettled = true,
                PaidAt = now,
                Retired = false
            };
            _db.Bills.Add(bill);
            await _db.SaveChangesAsync();

            foreach (var cartItem in Cart)
            {
                var bi = new BillItem
                {
                    BillId = bill.Id,
                    ItemId1 = cartItem.ItemId,
                    Qty = cartItem.Qty,
                    Rate = cartItem.SellingPrice,
                    NetRate = cartItem.SellingPrice,
                    GrossValue = cartItem.Qty * cartItem.SellingPrice,
                    NetValue = cartItem.Qty * cartItem.SellingPrice,
                    CreatedAt = now,
                    CreaterId = userId,
                    Retired = false
                };
                _db.BillItems.Add(bi);
                await _db.SaveChangesAsync();

                var pbi = new PharmaceuticalBillItem
                {
                    BillItemId = bi.Id,
                    Qty = cartItem.Qty,
                    SellingPrice = cartItem.SellingPrice,
                    PurchasePrice = cartItem.SellingPrice,
                    CreatedAt = now,
                    CreaterId = userId
                };
                _db.PharmaceuticalBillItems.Add(pbi);

                var stock = await _db.Stocks
                    .Where(s => s.ItemId == cartItem.ItemId && s.DepartmentId == _appSession.LoggedDepartment.Id && !s.Retired)
                    .FirstOrDefaultAsync();
                if (stock != null)
                {
                    stock.StockQty = (stock.StockQty ?? 0) - cartItem.Qty;
                    stock.EditerId = userId;
                    stock.EditedAt = now;
                    _db.Stocks.Update(stock);
                }
            }

            var payment = new Payment
            {
                BillId = bill.Id,
                PaymentMethod = PaymentMethod.Cash,
                PaidValue = NetTotal,
                InstitutionId = _appSession.LoggedSite.Id,
                DepartmentId = _appSession.LoggedDepartment.Id,
                CreatedAt = now,
                CreaterId = userId,
                Retired = false
            };
            _db.Payments.Add(payment);

            await _db.SaveChangesAsync();

            SaveCartToSession(new List<SaleCartItem>());
            SuccessMessage = $"Sale completed. Bill #{bill.Id} saved successfully.";
            return Page();
        }

        private List<SaleCartItem> GetCartFromSession()
        {
            var json = HttpContext.Session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(json)) return new List<SaleCartItem>();
            return JsonSerializer.Deserialize<List<SaleCartItem>>(json) ?? new List<SaleCartItem>();
        }

        private void SaveCartToSession(List<SaleCartItem> cart)
        {
            HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
        }
    }
}
