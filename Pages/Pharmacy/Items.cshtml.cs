// ============================================================================
// Developer : Tajwer Jalal
// Date      : 19-Oct-2026
// Time      : 14:30
// Purpose   : Medicine / Product master - port of legacy BASEMENT\SPR\INVM AST.SPR
//             (invmast.spr). Browse + edit the single merged PHITEM master.
//
// WHY A SINGLE MASTER
//   The legacy screen was run once per store against that store's own INVMAST.
//   Here the drug is stored once and the store's quantity comes from
//   PHITEMSTOCK, so the same screen serves every store.
// ============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HmisWeb.Models.Pharmacy;
using HmisWeb.Services.Pharmacy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Pharmacy
{
    public class ItemsModel : PageModel
    {
        private readonly PhService _ph;
        private readonly HmisWeb.Data.ApplicationDbContext _db;

        public ItemsModel(PhService ph, HmisWeb.Data.ApplicationDbContext db)
        {
            _ph = ph;
            _db = db;
        }

        // SupportsGet is required here: the filter bar is a GET form, and a
        // plain [BindProperty] only binds POST. Without it the filters were
        // silently ignored and the page always showed the unfiltered list.
        [BindProperty(SupportsGet = true)]
        public int SelectedStoreId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SelectedCategoryCode { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool ActiveOnly { get; set; } = true;

        [BindProperty]
        public int? EditId { get; set; }

        [BindProperty]
        public EditModel Edit { get; set; } = new();

        [TempData]
        public string? Message { get; set; }

        [TempData]
        public string? Error { get; set; }

        public List<PhStore> Stores { get; set; } = new();
        public List<PhCategory> Categories { get; set; } = new();
        public List<PhService.PhItemRow> Rows { get; set; } = new();

        /// <summary>Per-store quantity when an item is open for editing.</summary>
        public List<(int StoreId, string StoreName, decimal OnHand, decimal AvgCost)> EditStock { get; set; } = new();

        public class EditModel
        {
            public int Id { get; set; }
            [System.ComponentModel.DataAnnotations.Required]
            public string Code { get; set; } = "";
            [System.ComponentModel.DataAnnotations.Required]
            public string Name { get; set; } = "";
            public string? ItemType { get; set; }
            public string? GenericCode { get; set; }
            public string? SupplierCode { get; set; }
            public string? ManufacturerCode { get; set; }
            public string? CategoryCode { get; set; }
            public string? PackType { get; set; }
            public decimal PackUnit { get; set; }
            public decimal TradeCost { get; set; }
            public decimal TradePrice { get; set; }
            public decimal RetailPrice { get; set; }
            public decimal TaxRate { get; set; }
            public decimal UnitCost { get; set; }
            public decimal HospitalCost { get; set; }
            public decimal ReorderLevel { get; set; }
            public decimal MinStock { get; set; }
            public decimal MaxStock { get; set; }
            public bool IsActive { get; set; } = true;
            public bool IsTop { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        /// <summary>
        /// No-JavaScript fallback for "edit this row". With JS available the row
        /// is intercepted and the popup is filled client-side instead, so this
        /// only runs when scripting is off.
        /// </summary>
        public async Task<IActionResult> OnGetEditAsync(int id)
        {
            await LoadLookupsAsync();
            var item = await _ph.GetItemAsync(id);
            if (item == null) return NotFound();

            Edit = new EditModel
            {
                Id = item.Id,
                Code = item.Code,
                Name = item.Name,
                ItemType = item.ItemType,
                GenericCode = item.GenericCode,
                SupplierCode = item.SupplierCode,
                ManufacturerCode = item.ManufacturerCode,
                CategoryCode = item.CategoryCode,
                PackType = item.PackType,
                PackUnit = item.PackUnit ?? 0,
                TradeCost = item.TradeCost,
                TradePrice = item.TradePrice,
                RetailPrice = item.RetailPrice,
                TaxRate = item.TaxRate,
                UnitCost = item.UnitCost,
                HospitalCost = item.HospitalCost,
                ReorderLevel = item.ReorderLevel,
                MinStock = item.MinStock,
                MaxStock = item.MaxStock,
                IsActive = item.IsActive,
                IsTop = item.IsTop
            };
            EditId = id;
            EditStock = await _ph.GetItemStockAsync(id);
            Rows = await _ph.GetItemsAsync(SelectedStoreId, Search, SelectedCategoryCode, ActiveOnly);
            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Edit.Code) || string.IsNullOrWhiteSpace(Edit.Name))
            {
                Error = "Code and Name are both required.";
            }
            else
            {
                var code = Edit.Code.Trim();
                var dup = await _db.PhItems.AnyAsync(i => i.Code == code && i.Id != Edit.Id);
                if (dup)
                {
                    Error = $"Medicine code '{code}' already exists.";
                }
            }

            // On failure the popup is reopened with the values that were typed.
            // Losing a filled-in form because of one bad field is worse than
            // the error itself, so nothing is discarded here.
            if (Error != null)
            {
                await LoadAsync();
                EditId = Edit.Id;
                if (Edit.Id > 0) EditStock = await _ph.GetItemStockAsync(Edit.Id);
                return Page();
            }

            var cleanCode = Edit.Code.Trim();
            var cleanName = Edit.Name.Trim();

            if (Edit.Id == 0)
            {
                _db.PhItems.Add(new PhItem
                {
                    Code = cleanCode,
                    Name = cleanName,
                    ItemType = Edit.ItemType,
                    GenericCode = Edit.GenericCode,
                    SupplierCode = Edit.SupplierCode,
                    ManufacturerCode = Edit.ManufacturerCode,
                    CategoryCode = Edit.CategoryCode,
                    PackType = Edit.PackType,
                    PackUnit = Edit.PackUnit,
                    TradeCost = Edit.TradeCost,
                    TradePrice = Edit.TradePrice,
                    RetailPrice = Edit.RetailPrice,
                    TaxRate = Edit.TaxRate,
                    UnitCost = Edit.UnitCost,
                    HospitalCost = Edit.HospitalCost,
                    ReorderLevel = Edit.ReorderLevel,
                    MinStock = Edit.MinStock,
                    MaxStock = Edit.MaxStock,
                    IsActive = Edit.IsActive,
                    IsTop = Edit.IsTop
                });
                Message = $"'{cleanName}' added to the medicine master.";
            }
            else
            {
                var item = await _db.PhItems.FirstOrDefaultAsync(i => i.Id == Edit.Id);
                if (item == null) return NotFound();
                item.Code = cleanCode;
                item.Name = cleanName;
                item.ItemType = Edit.ItemType;
                item.GenericCode = Edit.GenericCode;
                item.SupplierCode = Edit.SupplierCode;
                item.ManufacturerCode = Edit.ManufacturerCode;
                item.CategoryCode = Edit.CategoryCode;
                item.PackType = Edit.PackType;
                item.PackUnit = Edit.PackUnit;
                item.TradeCost = Edit.TradeCost;
                item.TradePrice = Edit.TradePrice;
                item.RetailPrice = Edit.RetailPrice;
                item.TaxRate = Edit.TaxRate;
                item.UnitCost = Edit.UnitCost;
                item.HospitalCost = Edit.HospitalCost;
                item.ReorderLevel = Edit.ReorderLevel;
                item.MinStock = Edit.MinStock;
                item.MaxStock = Edit.MaxStock;
                item.IsActive = Edit.IsActive;
                item.IsTop = Edit.IsTop;
                Message = $"'{cleanName}' updated.";
            }
            await _db.SaveChangesAsync();

            // Post-Redirect-Get. The list is rebuilt from the same filters the
            // user was looking at, and a browser refresh can no longer replay
            // the save. Without this, F5 on the result page re-posts the form.
            return RedirectToPage(new
            {
                SelectedStoreId,
                Search,
                SelectedCategoryCode,
                ActiveOnly
            });
        }

        /// <summary>
        /// Retire a drug. This is a soft delete, not a row delete: purchase
        /// lines, sale lines and the stock ledger all point at PHITEM with
        /// ON DELETE RESTRICT, so a physical delete would either fail or
        /// destroy transaction history.
        ///
        /// Deleting is refused while any store still holds quantity. Hiding a
        /// drug that has stock on hand would make that stock vanish from the
        /// on-hand report while still sitting in the ledger.
        /// </summary>
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var item = await _db.PhItems.FirstOrDefaultAsync(i => i.Id == id);
            if (item == null) return NotFound();

            var holding = await (from st in _db.PhItemStocks.AsNoTracking()
                                 where st.ItemId == id && !st.Retired && st.OnHandQty != 0
                                 join s in _db.PhStores.AsNoTracking() on st.StoreId equals s.Id
                                 select new { s.Name, st.OnHandQty })
                                 .ToListAsync();

            if (holding.Count > 0)
            {
                var detail = string.Join(", ", holding.Select(h => $"{h.Name}: {h.OnHandQty:N2}"));
                Error = $"'{item.Name}' cannot be deleted while stock is on hand ({detail}). " +
                        "Issue or adjust the stock to zero first.";
            }
            else
            {
                // Soft delete. The row stays so every historic document keeps
                // resolving its item name and code.
                item.Retired = true;
                item.IsActive = false;
                await _db.SaveChangesAsync();
                Message = $"'{item.Name}' deleted from the master.";
            }

            return RedirectToPage(new
            {
                SelectedStoreId,
                Search,
                SelectedCategoryCode,
                ActiveOnly
            });
        }

        /// <summary>
        /// Per-store stock position for one drug, used to fill the popup's
        /// stock panel. Fetched on demand rather than for all 1,000 list rows,
        /// which would cost 1,000 queries to open one popup.
        /// </summary>
        public async Task<IActionResult> OnGetStockAsync(int id)
        {
            var stock = await _ph.GetItemStockAsync(id);
            return new JsonResult(stock.Select(s => new
            {
                store = s.StoreName,
                onHand = s.OnHand,
                avgCost = s.AvgCost
            }));
        }

        private async Task LoadLookupsAsync()
        {
            Stores = await _ph.GetStoresAsync();
            Categories = await _ph.GetCategoriesAsync();
        }

        private async Task LoadAsync()
        {
            await LoadLookupsAsync();
            if (SelectedStoreId == 0 && Stores.Count > 0) SelectedStoreId = Stores[0].Id;
            Rows = await _ph.GetItemsAsync(SelectedStoreId, Search, SelectedCategoryCode, ActiveOnly);
            if (EditId.HasValue && EditStock.Count == 0)
                EditStock = await _ph.GetItemStockAsync(EditId.Value);
        }
    }
}
