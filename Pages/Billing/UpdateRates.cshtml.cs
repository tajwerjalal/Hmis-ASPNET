using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Billing
{
    /// <summary>
    /// Update Rates - edit the billable rates of services/tests in bulk.
    /// Mirrors the VFP Billsys form <c>bs0120</c> (Update Rates).
    /// </summary>
    public class UpdateRatesModel : Shared.PagedPageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public UpdateRatesModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        [BindProperty(SupportsGet = true)] public string SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)] public long? CategoryId { get; set; }

        public List<Item> Items { get; set; } = new();
        public List<SelectListItem> CategoryList { get; set; } = new();
        public string Success { get; set; }
        public string ErrorMessage { get; set; }

        public async Task OnGetAsync() => await LoadAsync();

        public async Task<IActionResult> OnPostSaveAsync(long[] ids, double?[] totals, double?[] foreigner, double?[] staff, double?[] unit)
        {
            if (ids == null || ids.Length == 0)
            {
                TempData["UrError"] = "Nothing to update.";
                return RedirectToPage(new { SearchTerm, CategoryId, Pg });
            }

            for (var i = 0; i < ids.Length; i++)
            {
                var item = await _db.Items.FindAsync(ids[i]);
                if (item == null) continue;
                item.Total = totals != null && i < totals.Length ? totals[i] : item.Total;
                item.TotalForForeigner = foreigner != null && i < foreigner.Length ? foreigner[i] : item.TotalForForeigner;
                item.StaffRate = staff != null && i < staff.Length ? staff[i] : item.StaffRate;
                item.UnitRate = unit != null && i < unit.Length ? unit[i] : item.UnitRate;
                item.EditerId = _session.WebUserId;
                item.EditedAt = DateTime.Now;
            }
            await _db.SaveChangesAsync();
            TempData["UrSuccess"] = $"{ids.Length} rate(s) updated.";
            return RedirectToPage(new { SearchTerm, CategoryId, Pg });
        }

        public async Task<IActionResult> OnPostApplyPercentAsync(double percent)
        {
            var q = BuildQuery();
            var items = await q.ToListAsync();
            foreach (var item in items)
            {
                var baseRate = item.Total ?? 0;
                item.Total = Math.Round(baseRate * (1 + percent / 100.0), 2);
                if (item.TotalForForeigner.HasValue)
                    item.TotalForForeigner = Math.Round(item.TotalForForeigner.Value * (1 + percent / 100.0), 2);
                item.EditerId = _session.WebUserId;
                item.EditedAt = DateTime.Now;
            }
            await _db.SaveChangesAsync();
            TempData["UrSuccess"] = $"Applied {percent:0.##}% to {items.Count} item(s).";
            return RedirectToPage(new { SearchTerm, CategoryId, Pg });
        }

        private IQueryable<Item> BuildQuery()
        {
            var q = _db.Items
                .Where(i => !i.Retired && i.Billable
                    && (i.ItemType == ItemType.Investigation || i.ItemType == ItemType.Service));
            if (CategoryId.HasValue && CategoryId.Value > 0) q = q.Where(i => i.CategoryId == CategoryId.Value);
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var t = SearchTerm.Trim().ToLower();
                q = q.Where(i => (i.Name != null && i.Name.ToLower().Contains(t)) || (i.Code != null && i.Code.ToLower().Contains(t)));
            }
            return q;
        }

        private async Task LoadAsync()
        {
            Success = TempData["UrSuccess"] as string;
            ErrorMessage = TempData["UrError"] as string;

            var ordered = BuildQuery().Include(i => i.Category).OrderBy(i => i.Category.Name).ThenBy(i => i.Name);
            TotalCount = await ordered.CountAsync();
            Items = await ordered.Skip(Skip).Take(Take).ToListAsync();

            CategoryList = await _db.Categories.Where(c => !c.Retired).OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToListAsync();
        }
    }
}