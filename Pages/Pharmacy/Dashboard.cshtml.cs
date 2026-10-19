// ============================================================================
// Developer : Tajwer Jalal
// Date      : 19-Oct-2026
// Time      : 12:50
// Purpose   : Pharmacy multi-store dashboard. Shows, per store: on-hand
//             quantity, ending inventory valuation and the open balance
//             derived from the migrated ledger.
// ============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HmisWeb.Models.Pharmacy;
using HmisWeb.Services.Pharmacy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HmisWeb.Pages.Pharmacy
{
    public class DashboardModel : PageModel
    {
        private readonly PhService _ph;

        public DashboardModel(PhService ph)
        {
            _ph = ph;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? From { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? To { get; set; }

        public DateTime FromDate => From ?? new DateTime(DateTime.Today.Year, 1, 1);
        public DateTime ToDate => To ?? DateTime.Today;

        public List<PhStore> Stores { get; set; } = new();
        public List<PhStoreSummary> Summaries { get; set; } = new();
        public PhLedgerSummary Ledger { get; set; } = new();

        public decimal TotalStockCost => Summaries.Sum(s => s.EndingInventoryCost);
        public decimal TotalStockRetail => Summaries.Sum(s => s.EndingInventoryRetail);
        public decimal TotalQty => Summaries.Sum(s => s.TotalQty);
        public decimal TotalSales => Summaries.Sum(s => s.SalesValue);
        public decimal TotalPurchases => Summaries.Sum(s => s.PurchaseValue);

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
            Summaries = await _ph.GetStoreSummariesAsync(FromDate, ToDate);
            Ledger = await _ph.GetLedgerSummaryAsync(FromDate, ToDate);
        }
    }
}
