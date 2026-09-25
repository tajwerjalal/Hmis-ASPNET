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
    public class GrnCartItem
    {
        public long ItemId { get; set; }
        public string ItemName { get; set; } = "";
        public int Qty { get; set; }
        public int FreeQty { get; set; }
        public double CostPrice { get; set; }
        public double LineTotal => Qty * CostPrice;
    }

    public class GrnModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;
        private readonly ICommonService _commonService;
        private const string GrnCartSessionKey = "PharmacyGrnCart";

        public GrnModel(ApplicationDbContext db, IAppSession appSession, ICommonService commonService)
        {
            _db = db;
            _appSession = appSession;
            _commonService = commonService;
        }

        public List<Bill> PendingBills { get; set; } = new();

        [BindProperty]
        public long? SelectedBillId { get; set; }

        public string SearchTerm { get; set; }
        public List<Item> SearchResults { get; set; } = new();
        public List<GrnCartItem> ManualCart { get; set; } = new();
        public List<Institution> Suppliers { get; set; } = new();

        [BindProperty]
        public long? ManualSupplierId { get; set; }

        public double ManualTotal => ManualCart.Sum(c => c.LineTotal);
        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            ManualCart = GetGrnCartFromSession();
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            long? deptId = _appSession.LoggedDepartment?.Id;
            PendingBills = await _db.Bills
                .Where(b => b.BillType == BillType.PharmacyPurchaseBill
                    && !b.Paid && !b.Retired
                    && (deptId == null || b.DepartmentId == deptId))
                .OrderByDescending(b => b.BillDate)
                .Take(50)
                .ToListAsync();

            Suppliers = await _db.Institutions
                .Where(i => !i.Retired && (i.InstitutionType == InstitutionType.Distributor || i.InstitutionType == InstitutionType.Dealer))
                .OrderBy(i => i.Name)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostReceiveAsync()
        {
            if (SelectedBillId == null || SelectedBillId == 0)
            {
                ErrorMessage = "Please select a bill to receive.";
                await LoadDataAsync();
                return Page();
            }

            if (_appSession.LoggedDepartment == null)
            {
                ErrorMessage = "Please log in with a valid department.";
                await LoadDataAsync();
                return Page();
            }

            var bill = await _db.Bills.FindAsync(SelectedBillId);
            if (bill == null || bill.Retired)
            {
                ErrorMessage = "Bill not found.";
                await LoadDataAsync();
                return Page();
            }

            var now = DateTime.Now;
            var userId = _appSession.WebUserId;

            bill.Paid = true;
            bill.BillPaymentCompletelySettled = true;
            bill.PaidAt = now;
            bill.EditorId = userId;
            bill.EditedAt = now;
            _db.Bills.Update(bill);

            var billItems = await _db.BillItems.Where(bi => bi.BillId == bill.Id && !bi.Retired).ToListAsync();
            foreach (var bi in billItems)
            {
                var stock = await _db.Stocks
                    .Where(s => s.ItemId == bi.ItemId1 && s.DepartmentId == _appSession.LoggedDepartment.Id && !s.Retired)
                    .FirstOrDefaultAsync();
                if (stock != null)
                {
                    stock.StockQty = (stock.StockQty ?? 0) + (bi.Qty ?? 0);
                    stock.EditerId = userId;
                    stock.EditedAt = now;
                    _db.Stocks.Update(stock);
                }
                else
                {
                    var batch = new ItemBatch
                    {
                        ItemId = bi.ItemId1,
                        DepartmentId = _appSession.LoggedDepartment.Id,
                        BatchNo = "B" + now.Ticks,
                        CreatedAt = now,
                        CreaterId = userId,
                        Retired = false
                    };
                    _db.ItemBatches.Add(batch);
                    await _db.SaveChangesAsync();

                    var newStock = new Stock
                    {
                        ItemId = bi.ItemId1,
                        DepartmentId = _appSession.LoggedDepartment.Id,
                        ItemBatchId = batch.Id,
                        StockQty = bi.Qty ?? 0,
                        LastPurchaseUnitPrice = bi.Rate,
                        LastPurchaseDate = now,
                        CreatedAt = now,
                        CreaterId = userId,
                        Retired = false
                    };
                    _db.Stocks.Add(newStock);
                }
            }

            await _db.SaveChangesAsync();
            SuccessMessage = $"Bill #{bill.Id} received successfully. Stock updated.";
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSearchManualAsync()
        {
            ManualCart = GetGrnCartFromSession();
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                SearchResults = await _db.Items
                    .Where(i => !i.Retired && i.ItemType == ItemType.Medicine && i.Billable
                        && (i.Name.ToLower().Contains(term) || i.PrintName.ToLower().Contains(term)
                            || i.Code.ToLower().Contains(term)))
                    .OrderBy(i => i.Name)
                    .Take(30)
                    .ToListAsync();
            }
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAddManualToCartAsync(long itemId)
        {
            ManualCart = GetGrnCartFromSession();
            var existing = ManualCart.FirstOrDefault(c => c.ItemId == itemId);
            if (existing != null)
            {
                existing.Qty += 1;
            }
            else
            {
                var item = await _db.Items.FindAsync(itemId);
                if (item != null)
                {
                    ManualCart.Add(new GrnCartItem
                    {
                        ItemId = itemId,
                        ItemName = item.PrintName ?? item.Name,
                        Qty = 1,
                        FreeQty = 0,
                        CostPrice = item.Total ?? 0
                    });
                }
            }
            SaveGrnCartToSession(ManualCart);
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostRemoveManualFromCartAsync(int index)
        {
            ManualCart = GetGrnCartFromSession();
            if (index >= 0 && index < ManualCart.Count)
                ManualCart.RemoveAt(index);
            SaveGrnCartToSession(ManualCart);
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSaveManualGrnAsync()
        {
            ManualCart = GetGrnCartFromSession();
            if (!ManualCart.Any())
            {
                ErrorMessage = "No items in the manual GRN cart.";
                await LoadDataAsync();
                return Page();
            }
            if (ManualSupplierId == null || ManualSupplierId == 0)
            {
                ErrorMessage = "Please select a supplier.";
                await LoadDataAsync();
                return Page();
            }
            if (_appSession.LoggedSite == null || _appSession.LoggedDepartment == null)
            {
                ErrorMessage = "Please log in with a valid site and department.";
                await LoadDataAsync();
                return Page();
            }

            var now = DateTime.Now;
            var userId = _appSession.WebUserId;

            var bill = new Bill
            {
                BillClassType = BillClassType.BilledBill,
                BillType = BillType.PharmacyPurchaseBill,
                FromInstitutionId = ManualSupplierId,
                ToInstitutionId = _appSession.LoggedSite.Id,
                InstitutionId = _appSession.LoggedSite.Id,
                DepartmentId = _appSession.LoggedDepartment.Id,
                BillDate = now,
                CreatedAt = now,
                CreaterId = userId,
                Total = ManualTotal,
                NetTotal = ManualTotal,
                Discount = 0,
                Paid = true,
                BillPaymentCompletelySettled = true,
                PaidAt = now,
                Retired = false
            };
            _db.Bills.Add(bill);
            await _db.SaveChangesAsync();

            foreach (var cartItem in ManualCart)
            {
                var bi = new BillItem
                {
                    BillId = bill.Id,
                    ItemId1 = cartItem.ItemId,
                    Qty = cartItem.Qty + cartItem.FreeQty,
                    Rate = cartItem.CostPrice,
                    NetRate = cartItem.CostPrice,
                    GrossValue = cartItem.Qty * cartItem.CostPrice,
                    NetValue = cartItem.Qty * cartItem.CostPrice,
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
                    PurchasePrice = cartItem.CostPrice,
                    SellingPrice = cartItem.CostPrice,
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
                    LastPurchaseUnitPrice = cartItem.CostPrice,
                    LastPurchaseDate = now,
                    CreatedAt = now,
                    CreaterId = userId,
                    Retired = false
                };
                _db.Stocks.Add(stock);
            }

            await _db.SaveChangesAsync();
            SaveGrnCartToSession(new List<GrnCartItem>());
            SuccessMessage = $"Manual GRN completed. Bill #{bill.Id} saved. Stock updated.";
            ManualCart = new List<GrnCartItem>();
            await LoadDataAsync();
            return Page();
        }

        private List<GrnCartItem> GetGrnCartFromSession()
        {
            var json = HttpContext.Session.GetString(GrnCartSessionKey);
            if (string.IsNullOrEmpty(json)) return new List<GrnCartItem>();
            return JsonSerializer.Deserialize<List<GrnCartItem>>(json) ?? new List<GrnCartItem>();
        }

        private void SaveGrnCartToSession(List<GrnCartItem> cart)
        {
            HttpContext.Session.SetString(GrnCartSessionKey, JsonSerializer.Serialize(cart));
        }
    }
}
