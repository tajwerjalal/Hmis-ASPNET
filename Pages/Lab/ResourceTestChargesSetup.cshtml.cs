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
    /// Resource Test Charges Setup - staff/resource rates for laboratory tests.
    /// Mirrors VFP Labsys form <c>lb0350</c>; stored in <see cref="ResourceServiceRate"/>,
    /// scoped here to laboratory tests.
    /// </summary>
    public class ResourceTestChargesSetupModel : PageModel
    {
        /// <summary>Mirror prefix of the laboratory bank on the shared services master.</summary>

        private const string ItemPrefix = "INV-LB-";


        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public ResourceTestChargesSetupModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        [BindProperty(SupportsGet = true)]
        public long? ResourceId { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<ResourceServiceRate> Items { get; set; } = new();
        public List<SelectListItem> ResourceList { get; set; } = new();
        public List<SelectListItem> TestList { get; set; } = new();
        public string Success { get; set; }
        public string ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadAsync();
            if (TryGetId("edit", out var id) && id > 0)
            {
                var r = await _db.ResourceServiceRates.FindAsync(id);
                if (r != null) Input = new InputModel { Id = r.Id, ResourceId = r.ResourceId, ItemId = r.ItemId, StaffRate = r.StaffRate, UnitRate = r.UnitRate, ShowForm = true };
            }
            if (Request.Query.ContainsKey("add")) Input.ShowForm = true;
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (Input.ResourceId <= 0 || Input.ItemId <= 0)
            {
                TempData["RtcError"] = "Resource and test are required.";
                return RedirectToPage(new { ResourceId });
            }

            var now = DateTime.Now;
            if (Input.Id > 0)
            {
                var r = await _db.ResourceServiceRates.FindAsync(Input.Id);
                if (r == null) { TempData["RtcError"] = "Rate not found."; return RedirectToPage(new { ResourceId }); }
                r.ResourceId = Input.ResourceId; r.ItemId = Input.ItemId; r.StaffRate = Input.StaffRate; r.UnitRate = Input.UnitRate;
                r.EditerId = _session.WebUserId; r.EditedAt = now;
                TempData["RtcSuccess"] = "Resource test charge updated.";
            }
            else
            {
                var existing = await _db.ResourceServiceRates.FirstOrDefaultAsync(r => !r.Retired && r.ResourceId == Input.ResourceId && r.ItemId == Input.ItemId);
                if (existing != null)
                {
                    existing.StaffRate = Input.StaffRate; existing.UnitRate = Input.UnitRate;
                    existing.EditerId = _session.WebUserId; existing.EditedAt = now;
                    TempData["RtcSuccess"] = "Existing charge updated.";
                }
                else
                {
                    _db.ResourceServiceRates.Add(new ResourceServiceRate
                    {
                        ResourceId = Input.ResourceId, ItemId = Input.ItemId, StaffRate = Input.StaffRate, UnitRate = Input.UnitRate,
                        CreaterId = _session.WebUserId, CreatedAt = now
                    });
                    TempData["RtcSuccess"] = "Resource test charge added.";
                }
            }
            await _db.SaveChangesAsync();
            return RedirectToPage(new { ResourceId });
        }

        public async Task<IActionResult> OnPostRetireAsync(long id)
        {
            var r = await _db.ResourceServiceRates.FindAsync(id);
            if (r != null) { r.Retired = true; r.RetireComments = "Retired from Lab Resource Test Charges."; r.EditerId = _session.WebUserId; r.EditedAt = DateTime.Now; await _db.SaveChangesAsync(); TempData["RtcSuccess"] = "Charge retired."; }
            return RedirectToPage(new { ResourceId });
        }

        private async Task LoadAsync()
        {
            Success = TempData["RtcSuccess"] as string;
            ErrorMessage = TempData["RtcError"] as string;

            ResourceList = await _db.Resources
                .Where(r => !r.Retired)
                .OrderBy(r => r.Name)
                .Select(r => new SelectListItem { Value = r.Id.ToString(), Text = (r.Code ?? "") + " - " + r.Name }).ToListAsync();

            TestList = await _db.Items
                .Where(i => !i.Retired && (i.ItemType == ItemType.Investigation || i.ItemType == ItemType.Service)
                && i.Code.StartsWith(ItemPrefix))
                .OrderBy(i => i.Name)
                .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = (i.Code ?? "") + " - " + i.Name }).ToListAsync();

            var q = _db.ResourceServiceRates
                .Include(r => r.Resource)
                .Include(r => r.Item)
                .Where(r => !r.Retired && (r.Item.ItemType == ItemType.Investigation || r.Item.ItemType == ItemType.Service)
                && r.Item.Code.StartsWith(ItemPrefix));

            if (ResourceId.HasValue && ResourceId.Value > 0) q = q.Where(r => r.ResourceId == ResourceId.Value);

            Items = await q.OrderBy(r => r.Resource.Name).ThenBy(r => r.Item.Name).ToListAsync();
        }

        private bool TryGetId(string key, out long id)
        {
            id = 0;
            return Request.Query.TryGetValue(key, out var raw) && long.TryParse(raw, out id);
        }

        public class InputModel
        {
            public long Id { get; set; }
            public long ResourceId { get; set; }
            public long ItemId { get; set; }
            public double StaffRate { get; set; }
            public double UnitRate { get; set; }
            public bool ShowForm { get; set; }
        }
    }
}