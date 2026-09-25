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
    public class PurchaseCartItem
    {
        public long ItemId { get; set; }
        public string ItemName { get; set; } = "";
        public int Qty { get; set; }
        public int FreeQty { get; set; }
        public double PurchasePrice { get; set; }
        public double LineTotal => Qty * PurchasePrice;
    }

    public class PurchaseModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;
        private readonly ICommonService _commonService;
        private const string CartSessionKey = "PharmacyPurchaseCart";

        public PurchaseModel(ApplicationDbContext db, IAppSession appSession, ICommonService commonService)
        {
            _db = db;
            _appSession = appSession;
            _commonService = commonService;
        }

        [BindProperty]
        public string SearchTerm { get; set; }

        [BindProperty]
        public long? SupplierId { get; set; }

        public List<Item> SearchResults { get; set; } = new();
        public List<Institution> Suppliers { get; set; } = new();
        public List<PurchaseCartItem> Cart { get; set; } = new();
        public double SubTotal => Cart.Sum(c => c.LineTotal);
        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            Cart = GetCartFromSession();
            await LoadSuppliersAsync();
        }

        private async Task LoadSuppliersAsync()
        {
            Suppliers = await _db.Institutions
                .Where(i => !i.Retired && (i.InstitutionType == InstitutionType.Distributor || i.InstitutionType == InstitutionType.Dealer))
                .OrderBy(i => i.Name)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostSearchAsync()
        {
            Cart = GetCartFromSession();
            await LoadSuppliersAsync();
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
                    Cart.Add(new PurchaseCartItem
                    {
                        ItemId = itemId,
                        ItemName = item.PrintName ?? item.Name,
                        Qty = 1,
                        FreeQty = 0,
                        PurchasePrice = item.Total ?? 0
                    });
                }
            }
            SaveCartToSession(Cart);
            await LoadSuppliersAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostRemoveFromCartAsync(int index)
        {
            Cart = GetCartFromSession();
            if (index >= 0 && index < Cart.Count)
                Cart.RemoveAt(index);
            SaveCartToSession(Cart);
            await LoadSuppliersAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            Cart = GetCartFromSession();
            if (!Cart.Any())
            {
                ErrorMessage = "Cart is empty. Please add items before saving.";
                await LoadSuppliersAsync();
                return Page();
            }

            if (_appSession.LoggedSite == null || _appSession.LoggedDepartment == null)
            {
                ErrorMessage = "Please log in with a valid site and department.";
                await LoadSuppliersAsync();
                return Page();
            }

            if (SupplierId == null || SupplierId == 0)
            {
                ErrorMessage = "Please select a supplier.";
                await LoadSuppliersAsync();
                return Page();
            }

            var now = DateTime.Now;
            var userId = _appSession.WebUserId;

            var bill = new Bill
            {
                BillClassType = BillClassType.BilledBill,
                BillType = BillType.PharmacyPurchaseBill,
                FromInstitutionId = SupplierId,
                ToInstitutionId = _appSession.LoggedSite.Id,
                InstitutionId = _appSession.LoggedSite.Id,
                DepartmentId = _appSession.LoggedDepartment.Id,
                BillDate = now,
                CreatedAt = now,
                CreaterId = userId,
                Total = SubTotal,
                NetTotal = SubTotal,
                Discount = 0,
                Paid = false,
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
                    Qty = cartItem.Qty + cartItem.FreeQty,
                    Rate = cartItem.PurchasePrice,
                    NetRate = cartItem.PurchasePrice,
                    GrossValue = cartItem.Qty * cartItem.PurchasePrice,
                    NetValue = cartItem.Qty * cartItem.PurchasePrice,
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
                    FreeQty = cartItem.FreeQty,
                    PurchasePrice = cartItem.PurchasePrice,
                    SellingPrice = cartItem.PurchasePrice,
                    CreatedAt = now,
                    CreaterId = userId
                };
                _db.PharmaceuticalBillItems.Add(pbi);

                var batchNo = "B" + now.Ticks;
                var batch = new ItemBatch
                {
                    ItemId = cartItem.ItemId,
                    DepartmentId = _appSession.LoggedDepartment.Id,
                    BatchNo = batchNo,
                    CreatedAt = now,
                    CreaterId = userId,
                    Retired = false
                };
                _db.ItemBatches.Add(batch);
                await _db.SaveChangesAsync();

                var stock = new Stock
                {
                    ItemId = cartItem.ItemId,
                    DepartmentId = _appSession.LoggedDepartment.Id,
                    ItemBatchId = batch.Id,
                    StockQty = cartItem.Qty,
                    StockFreeQty = cartItem.FreeQty,
                    LastPurchaseUnitPrice = cartItem.PurchasePrice,
                    LastPurchaseDate = now,
                    CreatedAt = now,
                    CreaterId = userId,
                    Retired = false
                };
                _db.Stocks.Add(stock);
            }

            await _db.SaveChangesAsync();
            SaveCartToSession(new List<PurchaseCartItem>());
            SuccessMessage = $"Purchase bill #{bill.Id} saved successfully. {Cart.Count} items added to stock.";
            Cart = new List<PurchaseCartItem>();
            await LoadSuppliersAsync();
            return Page();
        }

        private List<PurchaseCartItem> GetCartFromSession()
        {
            var json = HttpContext.Session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(json)) return new List<PurchaseCartItem>();
            return JsonSerializer.Deserialize<List<PurchaseCartItem>>(json) ?? new List<PurchaseCartItem>();
        }

        private void SaveCartToSession(List<PurchaseCartItem> cart)
        {
            HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
        }
    }
}
