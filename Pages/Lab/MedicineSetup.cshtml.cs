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
    /// Medicine Setup - the master list of medicines. Mirrors VFP Labsys form
    /// <c>lb0330</c> (madims.dbf). A medicine is an <see cref="Item"/> with
    /// <see cref="ItemType.Medicine"/>.
    /// </summary>
    public class MedicineSetupModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public MedicineSetupModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<Item> Items { get; set; } = new();
        public List<SelectListItem> CategoryList { get; set; } = new();
        public List<SelectListItem> UnitList { get; set; } = new();
        public string Success { get; set; }
        public string ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadAsync();
            if (TryGetId("edit", out var id) && id > 0)
            {
                var m = await _db.Items.FindAsync(id);
                if (m != null)
                {
                    Input = new InputModel
                    {
                        Id = m.Id, Name = m.Name, Code = m.Code, PrintName = m.PrintName, Sname = m.Sname,
                        CategoryId = m.CategoryId, Total = m.Total, TotalForForeigner = m.TotalForForeigner,
                        UnitRate = m.UnitRate, MeasurementUnitId = m.MeasurementUnitId, Billable = m.Billable,
                        Inactive = m.Inactive, ShowForm = true
                    };
                }
            }
            if (Request.Query.ContainsKey("add")) Input.ShowForm = true;
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.Name))
            {
                TempData["MsError"] = "Medicine name is required.";
                return RedirectToPage(new { SearchTerm });
            }

            var now = DateTime.Now;
            if (Input.Id > 0)
            {
                var m = await _db.Items.FindAsync(Input.Id);
                if (m == null) { TempData["MsError"] = "Medicine not found."; return RedirectToPage(new { SearchTerm }); }
                Apply(m);
                m.EditerId = _session.WebUserId; m.EditedAt = now;
                TempData["MsSuccess"] = $"Medicine '{m.Name}' updated.";
            }
            else
            {
                var m = new Item { ItemType = ItemType.Medicine };
                Apply(m);
                m.CreaterId = _session.WebUserId; m.CreatedAt = now;
                _db.Items.Add(m);
                TempData["MsSuccess"] = $"Medicine '{m.Name}' added.";
            }
            await _db.SaveChangesAsync();
            return RedirectToPage(new { SearchTerm });
        }

        public async Task<IActionResult> OnPostRetireAsync(long id)
        {
            var m = await _db.Items.FindAsync(id);
            if (m != null) { m.Retired = true; m.RetiredAt = DateTime.Now; m.RetirerId = _session.WebUserId; await _db.SaveChangesAsync(); TempData["MsSuccess"] = $"Medicine '{m.Name}' retired."; }
            return RedirectToPage(new { SearchTerm });
        }

        public async Task<IActionResult> OnPostActivateAsync(long id)
        {
            var m = await _db.Items.FindAsync(id);
            if (m != null) { m.Retired = false; m.RetiredAt = null; m.RetirerId = null; m.EditerId = _session.WebUserId; m.EditedAt = DateTime.Now; await _db.SaveChangesAsync(); TempData["MsSuccess"] = $"Medicine '{m.Name}' activated."; }
            return RedirectToPage(new { SearchTerm });
        }

        private void Apply(Item m)
        {
            m.Name = Input.Name?.Trim();
            m.Code = Input.Code?.Trim();
            m.PrintName = Input.PrintName?.Trim();
            m.Sname = Input.Sname?.Trim();
            m.CategoryId = Input.CategoryId;
            m.Total = Input.Total;
            m.TotalForForeigner = Input.TotalForForeigner;
            m.UnitRate = Input.UnitRate;
            m.MeasurementUnitId = Input.MeasurementUnitId;
            m.Billable = Input.Billable;
            m.Inactive = Input.Inactive;
        }

        private async Task LoadAsync()
        {
            Success = TempData["MsSuccess"] as string;
            ErrorMessage = TempData["MsError"] as string;

            var q = _db.Items.Include(i => i.Category)
                .Where(i => i.ItemType == ItemType.Medicine && !i.Retired);
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var t = SearchTerm.Trim().ToLower();
                q = q.Where(i => (i.Name != null && i.Name.ToLower().Contains(t)) || (i.Code != null && i.Code.ToLower().Contains(t)));
            }
            Items = await q.OrderBy(i => i.Name).ToListAsync();

            CategoryList = await _db.Categories.Where(c => !c.Retired)
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToListAsync();
            UnitList = await _db.MeasurementUnits.Where(u => !u.Retired)
                .OrderBy(u => u.Name)
                .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Name }).ToListAsync();
        }

        private bool TryGetId(string key, out long id)
        {
            id = 0;
            return Request.Query.TryGetValue(key, out var raw) && long.TryParse(raw, out id);
        }

        public class InputModel
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public string Code { get; set; }
            public string PrintName { get; set; }
            public string Sname { get; set; }
            public long? CategoryId { get; set; }
            public double? Total { get; set; }
            public double? TotalForForeigner { get; set; }
            public double? UnitRate { get; set; }
            public long? MeasurementUnitId { get; set; }
            public bool Billable { get; set; } = true;
            public bool Inactive { get; set; }
            public bool ShowForm { get; set; }
        }
    }
}