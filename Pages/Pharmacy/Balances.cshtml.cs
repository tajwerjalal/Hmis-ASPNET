// ============================================================================
// Developer : Tajwer Jalal
// Date      : 19-Oct-2026
// Time      : 13:05
// Purpose   : Open balance per ledger account, for the period selected.
//             Derived from the migrated MAINHEAD / MAINDET vouchers.
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
    public class BalancesModel : PageModel
    {
        private readonly PhService _ph;

        public BalancesModel(PhService ph)
        {
            _ph = ph;
        }

        [BindProperty]
        public DateTime? From { get; set; }

        [BindProperty]
        public DateTime? To { get; set; }

        [BindProperty]
        public string? Search { get; set; }

        public DateTime FromDate => From ?? new DateTime(DateTime.Today.Year - 1, 1, 1);
        public DateTime ToDate => To ?? DateTime.Today;

        public List<PhBalanceRow> Rows { get; set; } = new();
        public PhLedgerSummary Summary { get; set; } = new();

        public decimal TotalDebit { get { decimal d = 0; foreach (var r in Rows) d += r.Debit; return d; } }
        public decimal TotalCredit { get { decimal d = 0; foreach (var r in Rows) d += r.Credit; return d; } }

        public async Task OnGetAsync() { await LoadAsync(); }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadAsync();
            return Page();
        }

        private async Task LoadAsync()
        {
            Rows = await _ph.GetBalancesAsync(FromDate, ToDate, Search);
            Summary = await _ph.GetLedgerSummaryAsync(FromDate, ToDate);
        }
    }
}
