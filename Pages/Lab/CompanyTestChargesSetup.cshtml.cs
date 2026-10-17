using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Lab
{
    /// <summary>
    /// Company Test Charges Setup - panel/company rates for laboratory tests.
    /// Mirrors VFP Labsys form <c>lb0340</c>; stored in <see cref="CompanyServiceRate"/>,
    /// scoped here to laboratory tests.
    /// </summary>
    public class CompanyTestChargesSetupModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public CompanyTestChargesSetupModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        [BindProperty(SupportsGet = true)]
        public long? CompanyId { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<CompanyServiceRate> Items { get; set; } = new();
        public List<SelectListItem> CompanyList { get; set; } = new();
        public List<SelectListItem> TestList { get; set; } = new();
        public string Success { get; set; }
        public string ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadAsync();
            if (TryGetId("edit", out var id) && id > 0)
            {
                var r = await _db.CompanyServiceRates.FindAsync(id);
                if (r != null) Input = new InputModel { Id = r.Id, CompanyId = r.CompanyId, ItemId = r.ItemId, Rate = r.Rate, StaffRate = r.StaffRate, UnitRate = r.UnitRate, DiscountPercent = r.DiscountPercent, ShowForm = true };
            }
            if (Request.Query.ContainsKey("add")) Input.ShowForm = true;
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (Input.CompanyId <= 0 || Input.ItemId <= 0)
            {
                TempData["CtcError"] = "Company and test are required.";
                return RedirectToPage(new { CompanyId });
            }

            var now = DateTime.Now;
            if (Input.Id > 0)
            {
                var r = await _db.CompanyServiceRates.FindAsync(Input.Id);
                if (r == null) { TempData["CtcError"] = "Rate not found."; return RedirectToPage(new { CompanyId }); }
                r.CompanyId = Input.CompanyId; r.ItemId = Input.ItemId; r.Rate = Input.Rate;
                r.StaffRate = Input.StaffRate; r.UnitRate = Input.UnitRate; r.DiscountPercent = Input.DiscountPercent;
                r.EditerId = _session.WebUserId; r.EditedAt = now;
                TempData["CtcSuccess"] = "Company test charge updated.";
            }
            else
            {
                var existing = await _db.CompanyServiceRates.FirstOrDefaultAsync(r => !r.Retired && r.CompanyId == Input.CompanyId && r.ItemId == Input.ItemId);
                if (existing != null)
                {
                    existing.Rate = Input.Rate; existing.StaffRate = Input.StaffRate; existing.UnitRate = Input.UnitRate;
                    existing.DiscountPercent = Input.DiscountPercent; existing.EditerId = _session.WebUserId; existing.EditedAt = now;
                    TempData["CtcSuccess"] = "Existing charge updated.";
                }
                else
                {
                    _db.CompanyServiceRates.Add(new CompanyServiceRate
                    {
                        CompanyId = Input.CompanyId, ItemId = Input.ItemId, Rate = Input.Rate,
                        StaffRate = Input.StaffRate, UnitRate = Input.UnitRate, DiscountPercent = Input.DiscountPercent,
                        CreaterId = _session.WebUserId, CreatedAt = now
                    });
                    TempData["CtcSuccess"] = "Company test charge added.";
                }
            }
            await _db.SaveChangesAsync();
            return RedirectToPage(new { CompanyId });
        }

        public async Task<IActionResult> OnPostRetireAsync(long id)
        {
            var r = await _db.CompanyServiceRates.FindAsync(id);
            if (r != null) { r.Retired = true; r.RetireComments = "Retired from Lab Company Test Charges."; r.EditerId = _session.WebUserId; r.EditedAt = DateTime.Now; await _db.SaveChangesAsync(); TempData["CtcSuccess"] = "Charge retired."; }
            return RedirectToPage(new { CompanyId });
        }

        private async Task LoadAsync()
        {
            Success = TempData["CtcSuccess"] as string;
            ErrorMessage = TempData["CtcError"] as string;

            CompanyList = await _db.Institutions
                .Where(i => !i.Retired && i.Code != null)
                .OrderBy(i => i.Name)
                .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = i.Name }).ToListAsync();

            TestList = await _db.Items
                .Where(i => !i.Retired && (i.ItemType == ItemType.Investigation || i.ItemType == ItemType.Service))
                .OrderBy(i => i.Name)
                .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = (i.Code ?? "") + " - " + i.Name }).ToListAsync();

            var q = _db.CompanyServiceRates
                .Include(r => r.Company)
                .Include(r => r.Item)
                .Where(r => !r.Retired && (r.Item.ItemType == ItemType.Investigation || r.Item.ItemType == ItemType.Service));

            if (CompanyId.HasValue && CompanyId.Value > 0) q = q.Where(r => r.CompanyId == CompanyId.Value);

            Items = await q.OrderBy(r => r.Company.Name).ThenBy(r => r.Item.Name).ToListAsync();
        }

        private bool TryGetId(string key, out long id)
        {
            id = 0;
            return Request.Query.TryGetValue(key, out var raw) && long.TryParse(raw, out id);
        }

        public class InputModel
        {
            public long Id { get; set; }
            public long CompanyId { get; set; }
            public long ItemId { get; set; }
            public double Rate { get; set; }
            public double? StaffRate { get; set; }
            public double? UnitRate { get; set; }
            public double? DiscountPercent { get; set; }
            public bool ShowForm { get; set; }
        }
    }
}