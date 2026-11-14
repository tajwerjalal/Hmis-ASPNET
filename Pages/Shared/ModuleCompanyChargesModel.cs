using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Shared
{
    /// <summary>
    /// Shared page model of the four module company-charge setup screens
    /// (/cnp, /dental, /hospital, /ot CompanyCharges).
    ///
    /// Every screen shows one department's mirrored billing items and the
    /// company rates stored against them. All four screens were byte-identical
    /// apart from the mirror prefix, so the logic lives here once.
    ///
    /// Why this class exists (fixed 22-Oct-2026):
    ///   * the old screens loaded EVERY company rate of the whole database
    ///     (21,000+ rows, 23 MB of HTML per request) instead of their own
    ///     department - the list is now scoped to the module's mirror prefix
    ///     and paged;
    ///   * the old save wrote only CompanyId/ItemId, leaving CompanyCode,
    ///     ServiceCode and DEPT empty. Those three columns are the legacy key
    ///     of COMPANYSERVICERATE (unique among the live rows) and the key the
    ///     corporate rate card and billing read, so a saved charge was
    ///     invisible to both - and the second such insert collided on the
    ///     filtered unique index. The save now resolves the company code from
    ///     the company row and the service code + department from the mirror
    ///     item, and updates the matching live rate instead of inserting a
    ///     blind duplicate.
    ///
    /// Developer : [TAJWER JALAL]
    /// Created   : 22-Oct-2026 11:10
    /// Change Log: 22-Oct-2026 11:10 - created (one base for cnp / dental /
    ///             hospital / ot); list scoped + paged, legacy key written,
    ///             duplicate handled with a message instead of a 500.
    /// </summary>
    public abstract class ModuleCompanyChargesModel : PagedPageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        protected ModuleCompanyChargesModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        /// <summary>Mirror prefix of this module on [ITEM] (INV-CNP- etc.).</summary>
        protected abstract string ItemPrefix { get; }

        /// <summary>
        /// Legacy SERVICE departments this module may own. Used to resolve the
        /// DEPT code of a mirror item (hospitalisation owns 07 and 08).
        /// </summary>
        protected abstract string[] DeptCodes { get; }

        /// <summary>Live company rates of this module's items, current page.</summary>
        public List<CompanyServiceRate> Items { get; set; } = new();

        public List<SelectListItem> CompanyList { get; set; } = new();
        public List<SelectListItem> ItemList { get; set; } = new();

        public string ErrorMessage { get; set; }
        public string Success { get; set; }

        /// <summary>Company filter of the grid (query key FilterCompanyId).</summary>
        [BindProperty(SupportsGet = true)]
        public long? FilterCompanyId { get; set; }

        /// <summary>Free-text filter on the service code / name.</summary>
        [BindProperty(SupportsGet = true)]
        public string Q { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public long? CompanyId { get; set; }
            public long? ItemId { get; set; }
            public decimal? Rate { get; set; }
            public decimal? UrgentRate { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        /// <summary>
        /// Stores (or updates) the company charge of one service.
        /// </summary>
        public async Task<IActionResult> OnPostAsync()
        {
            if (!Input.CompanyId.HasValue || !Input.ItemId.HasValue)
            {
                ErrorMessage = "Company and service are required.";
                await LoadAsync();
                return Page();
            }

            // the legacy company code (CID) of the selected company
            var companyCode = await _db.Institutions
                .Where(i => i.Id == Input.CompanyId.Value)
                .Select(i => i.Code)
                .FirstOrDefaultAsync();

            var item = await _db.Items
                .Where(i => i.Id == Input.ItemId.Value)
                .Select(i => new { i.Code, i.Name })
                .FirstOrDefaultAsync();

            var serviceCode = StripPrefix(item?.Code);
            if (string.IsNullOrWhiteSpace(companyCode) || string.IsNullOrWhiteSpace(serviceCode))
            {
                ErrorMessage = "The selected company or service has no legacy code, so the charge cannot be stored.";
                await LoadAsync();
                return Page();
            }

            var dept = await ResolveDeptAsync(serviceCode);

            // one live rate per company + service + department
            var existing = await _db.CompanyServiceRates
                .FirstOrDefaultAsync(r => !r.Retired
                                          && r.CompanyCode == companyCode
                                          && r.ServiceCode == serviceCode
                                          && (r.DEPT ?? "") == (dept ?? ""));

            if (existing != null)
            {
                existing.CompanyId = Input.CompanyId.Value;
                existing.ItemId = Input.ItemId.Value;
                existing.Rate = Input.Rate ?? 0;
                existing.UnitRate = Input.UrgentRate;
                existing.EditerId = _session.WebUserId;
                existing.EditedAt = DateTime.Now;
            }
            else
            {
                _db.CompanyServiceRates.Add(new CompanyServiceRate
                {
                    CompanyId = Input.CompanyId.Value,
                    ItemId = Input.ItemId.Value,
                    CompanyCode = companyCode,
                    ServiceCode = serviceCode,
                    DEPT = dept,
                    Rate = Input.Rate ?? 0,
                    UnitRate = Input.UrgentRate,
                    CreaterId = _session.WebUserId,
                    CreatedAt = DateTime.Now,
                    Retired = false
                });
            }

            try
            {
                await _db.SaveChangesAsync();
                Success = $"Company charge for {serviceCode} saved.";
            }
            catch (DbUpdateException)
            {
                // the filtered unique index keeps one live rate per company +
                // service + department - report the clash, never a 500
                ErrorMessage = "That company already has a live rate for this service and department.";
            }

            Input = new InputModel();
            await LoadAsync();
            return Page();
        }

        /// <summary>Soft-retires one company charge.</summary>
        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            var entity = await _db.CompanyServiceRates.FindAsync(id);
            if (entity != null)
            {
                entity.Retired = true;
                entity.RetireComments = "Retired via Company Charges Setup";
                entity.EditerId = _session.WebUserId;
                entity.EditedAt = DateTime.Now;
                await _db.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        /// <summary>Removes the module mirror prefix from an ITEM code.</summary>
        private string StripPrefix(string itemCode)
        {
            if (string.IsNullOrWhiteSpace(itemCode)) return null;
            var code = itemCode.Trim();
            return code.StartsWith(ItemPrefix, StringComparison.OrdinalIgnoreCase)
                ? code.Substring(ItemPrefix.Length).Trim()
                : null;
        }

        /// <summary>
        /// Resolves the SERVICE department of a mirror code inside this
        /// module; falls back to the module's only department when it has one.
        /// </summary>
        private async Task<string> ResolveDeptAsync(string serviceCode)
        {
            var depts = DeptCodes;
            var dept = await _db.DeptServices
                .Where(s => s.Code == serviceCode && depts.Contains(s.Dept))
                .OrderBy(s => s.Dept)
                .Select(s => s.Dept)
                .FirstOrDefaultAsync();

            return dept ?? (depts.Length == 1 ? depts[0] : null);
        }

        /// <summary>
        /// Loads the pickers and the paged grid of this module's charges.
        /// </summary>
        private async Task LoadAsync()
        {
            CompanyList = await _db.Institutions
                .Where(x => !x.Retired)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name })
                .ToListAsync();

            ItemList = await _db.Items
                .Where(x => !x.Retired && x.Billable && x.Code.StartsWith(ItemPrefix))
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name + " (" + x.Code + ")" })
                .ToListAsync();

            // the grid is the module's own charges only - never the whole table
            var query = _db.CompanyServiceRates
                .Include(x => x.Company)
                .Include(x => x.Item)
                .Where(x => !x.Retired
                            && x.Item != null
                            && x.Item.Code != null
                            && x.Item.Code.StartsWith(ItemPrefix));

            if (FilterCompanyId.HasValue)
                query = query.Where(x => x.CompanyId == FilterCompanyId.Value);

            if (!string.IsNullOrWhiteSpace(Q))
            {
                var term = Q.Trim();
                query = query.Where(x => x.Item.Code.Contains(term) || x.Item.Name.Contains(term));
            }

            TotalCount = await query.CountAsync();
            ClampPaging();

            Items = await ApplyPaging(query
                .OrderBy(x => x.Company.Name)
                .ThenBy(x => x.Item.Name)
                .ThenBy(x => x.Id))
                .ToListAsync();
        }
    }
}
