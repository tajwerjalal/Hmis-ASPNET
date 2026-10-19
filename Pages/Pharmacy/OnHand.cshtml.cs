// ============================================================================
// Developer : Tajwer Jalal
// Date      : 19-Oct-2026
// Time      : 13:00
// Purpose   : On-hand stock and ending inventory, per store. Replaces the old
//             department/batch-scoped Stock page for the multi-store view.
// ============================================================================
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HmisWeb.Models.Pharmacy;
using HmisWeb.Services.Pharmacy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HmisWeb.Pages.Pharmacy
{
    public class OnHandModel : PageModel
    {
        private readonly PhService _ph;

        public OnHandModel(PhService ph)
        {
            _ph = ph;
        }

        [BindProperty]
        public int SelectedStoreId { get; set; }

        [BindProperty]
        public string? Search { get; set; }

        [BindProperty]
        public int? SelectedCategoryId { get; set; }

        [BindProperty]
        public bool BelowReorderOnly { get; set; }

        [BindProperty]
        public bool IncludeZero { get; set; }

        public List<PhStore> Stores { get; set; } = new();
        public List<PhCategory> Categories { get; set; } = new();
        public List<PhStockRow> Rows { get; set; } = new();

        public decimal TotalQty => Sum(r => r.OnHandQty);
        public decimal TotalCost => Sum(r => r.ValueAtCost);
        public decimal TotalRetail => Sum(r => r.ValueAtRetail);
        public int BelowReorderCount => Rows.FindAll(r => r.BelowReorder).Count;
        public bool Truncated { get; set; }
        public int MaxRows { get; set; } = 5000;

        private decimal Sum(Func<PhStockRow, decimal> f)
        {
            decimal t = 0;
            foreach (var r in Rows) t += f(r);
            return t;
        }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadAsync();
            return Page();
        }

        private async Task LoadAsync()
        {
            Stores = await _ph.GetStoresAsync();
            Categories = await _ph.GetCategoriesAsync();
            if (SelectedStoreId == 0 && Stores.Count > 0) SelectedStoreId = Stores[0].Id;

            Rows = await _ph.GetStockAsync(SelectedStoreId, Search, SelectedCategoryId,
                                           BelowReorderOnly, IncludeZero, MaxRows);
            Truncated = Rows.Count >= MaxRows;
        }
    }
}
