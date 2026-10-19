// ============================================================================
// Developer : Tajwer Jalal
// Date      : 19-Oct-2026
// Time      : 16:15
// Purpose   : Chart of accounts - port of legacy BASEMENT\SPR\CHART.SPR.
//
// WHY THIS SCREEN MATTERS
//   The open-balance figure on the dashboard comes from PHVOUCHERLINE grouped
//   by account code. Those codes come from here. If an account is retired or
//   re-coded, the balance report silently loses the postings that referenced
//   the old code, so deletion is guarded.
//
// HIERARCHY
//   Legacy ACTYPE 'G' rows are group headings (e.g. 14 CURRENT ASSETS) and
//   'D' rows are detail accounts that actually receive postings. Groups are
//   indented by LEVEL and cannot be deleted while children exist.
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
    public class AccountsModel : PageModel
    {
        private readonly PhService _ph;
        private readonly HmisWeb.Data.ApplicationDbContext _db;

        public AccountsModel(PhService ph, HmisWeb.Data.ApplicationDbContext db)
        {
            _ph = ph;
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TypeFilter { get; set; }

        [BindProperty]
        public EditModel Edit { get; set; } = new();

        [TempData] public string? Message { get; set; }
        [TempData] public string? Error { get; set; }

        public List<PhAccountRow> Rows { get; set; } = new();

        /// <summary>Existing accounts, for the parent picker.</summary>
        public List<PhAccountRow> AllAccounts { get; set; } = new();

        public class EditModel
        {
            public int Id { get; set; }
            [System.ComponentModel.DataAnnotations.Required]
            public string Code { get; set; } = "";
            [System.ComponentModel.DataAnnotations.Required]
            public string Name { get; set; } = "";
            public string? AccountType { get; set; }
            public int Level { get; set; } = 1;
            public string? ParentCode { get; set; }
            public decimal OpeningBalance { get; set; }
            public decimal YearDebit { get; set; }
            public decimal YearCredit { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            Rows = await _ph.GetAccountsAsync(Search, TypeFilter);
            AllAccounts = await _ph.GetAccountsAsync(null, null);
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
                if (await _db.PhAccounts.AnyAsync(a => a.Code == code && a.Id != Edit.Id))
                {
                    Error = $"Account code '{code}' already exists.";
                }
                else if (!string.IsNullOrWhiteSpace(Edit.ParentCode)
                         && !await _db.PhAccounts.AnyAsync(a => a.Code == Edit.ParentCode))
                {
                    Error = $"Parent account '{Edit.ParentCode}' does not exist.";
                }
                else if (Edit.ParentCode != null && Edit.ParentCode == code)
                {
                    Error = "An account cannot be its own parent.";
                }
            }

            if (Error != null)
            {
                await LoadAsync();
                return Page();
            }

            var cleanCode = Edit.Code.Trim();
            var cleanName = Edit.Name.Trim();

            if (Edit.Id == 0)
            {
                _db.PhAccounts.Add(new PhAccount
                {
                    Code = cleanCode,
                    Name = cleanName,
                    AccountType = Edit.AccountType,
                    Level = Edit.Level,
                    ParentCode = string.IsNullOrWhiteSpace(Edit.ParentCode) ? null : Edit.ParentCode,
                    OpeningBalance = Edit.OpeningBalance,
                    YearDebit = Edit.YearDebit,
                    YearCredit = Edit.YearCredit
                });
                Message = $"Account '{cleanCode} {cleanName}' added.";
            }
            else
            {
                var a = await _db.PhAccounts.FirstOrDefaultAsync(x => x.Id == Edit.Id);
                if (a == null) return NotFound();

                // Re-coding an account would orphan every posting that used the
                // old code. Refuse it outright rather than silently breaking the
                // balance report.
                if (a.Code != cleanCode)
                {
                    var postings = await _db.PhVoucherLines.CountAsync(l => l.AccountCode == a.Code);
                    if (postings > 0)
                    {
                        Error = $"Account '{a.Code}' has {postings:N0} ledger posting(s) and cannot be re-coded. " +
                                "Create a new account instead.";
                        await LoadAsync();
                        return Page();
                    }
                }

                a.Code = cleanCode;
                a.Name = cleanName;
                a.AccountType = Edit.AccountType;
                a.Level = Edit.Level;
                a.ParentCode = string.IsNullOrWhiteSpace(Edit.ParentCode) ? null : Edit.ParentCode;
                a.OpeningBalance = Edit.OpeningBalance;
                a.YearDebit = Edit.YearDebit;
                a.YearCredit = Edit.YearCredit;
                Message = $"Account '{cleanCode} {cleanName}' updated.";
            }

            await _db.SaveChangesAsync();
            return RedirectToPage(new { Search, TypeFilter });
        }

        /// <summary>
        /// Accounts have no Retired flag in the legacy schema, so deletion is a
        /// real delete - and is therefore refused whenever the account carries
        /// postings or has child accounts. Deleting a posted account would make
        /// that money disappear from the trial balance.
        /// </summary>
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var a = await _db.PhAccounts.FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return NotFound();

            var postings = await _db.PhVoucherLines.CountAsync(l => l.AccountCode == a.Code);
            var children = await _db.PhAccounts.CountAsync(x => x.ParentCode == a.Code);

            if (postings > 0)
            {
                Error = $"'{a.Code} {a.Name}' cannot be deleted: {postings:N0} ledger posting(s) reference it. " +
                        "That money would vanish from the trial balance.";
            }
            else if (children > 0)
            {
                Error = $"'{a.Code} {a.Name}' cannot be deleted: {children} child account(s) sit under it. " +
                        "Delete or re-parent them first.";
            }
            else
            {
                _db.PhAccounts.Remove(a);
                await _db.SaveChangesAsync();
                Message = $"Account '{a.Code} {a.Name}' deleted.";
            }

            return RedirectToPage(new { Search, TypeFilter });
        }
    }
}
