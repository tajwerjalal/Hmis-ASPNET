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
    /// Lab Test Setup - the master list of laboratory tests.
    /// Mirrors the VFP Labsys form <c>lb0301</c> (test master), which stored one row per
    /// test with its classification, rates and specimen requirements. In the migrated HMIS
    /// a lab test is an <see cref="Item"/> with <see cref="ItemType.Investigation"/>.
    /// </summary>
    public class TestSetupModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public TestSetupModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? CategoryId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Status { get; set; } = "active";

        [BindProperty]
        public TestInput Input { get; set; } = new();

        public List<Item> Tests { get; set; } = new();
        public List<SelectListItem> CategoryList { get; set; } = new();
        public string Success { get; set; }
        public string ErrorMessage { get; set; }
        public int ActiveCount { get; set; }
        public int RetiredCount { get; set; }

        public async Task OnGetAsync()
        {
            await LoadCategoriesAsync();
            await LoadTestsAsync();
            Success = TempData["TestSetupSuccess"] as string;
            ErrorMessage = TempData["TestSetupError"] as string;

            if (TryGetId("edit", out var editId) && editId > 0)
            {
                var item = await _db.Items.FindAsync(editId);
                if (item != null)
                {
                    Input = new TestInput
                    {
                        Id = item.Id,
                        Code = item.Code,
                        Name = item.Name,
                        PrintName = item.PrintName,
                        Sname = item.Sname,
                        CategoryId = item.CategoryId,
                        ItemType = item.ItemType ?? ItemType.Investigation,
                        Total = item.Total,
                        TotalForForeigner = item.TotalForForeigner,
                        StaffRate = item.StaffRate,
                        UnitRate = item.UnitRate,
                        Requirements = item.Requirements,
                        MinDuration = item.MinDuration,
                        MaxDuration = item.MaxDuration,
                        ConsultantRequired = item.ConsultantRequired ?? false,
                        Billable = item.Billable,
                        Inactive = item.Inactive,
                        Descreption = item.Descreption,
                        ShowForm = true
                    };
                }
            }

            if (Request.Query.ContainsKey("add"))
                Input.ShowForm = true;
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            await LoadCategoriesAsync();

            if (string.IsNullOrWhiteSpace(Input.Name))
            {
                TempData["TestSetupError"] = "Test name is required.";
                return RedirectToPage(new { SearchTerm, CategoryId, Status });
            }

            var now = DateTime.Now;

            if (Input.Id > 0)
            {
                var item = await _db.Items.FindAsync(Input.Id);
                if (item == null)
                {
                    TempData["TestSetupError"] = "That test no longer exists.";
                    return RedirectToPage(new { SearchTerm, CategoryId, Status });
                }

                ApplyInput(item);
                item.EditerId = _session.WebUserId;
                item.EditedAt = now;
                await _db.SaveChangesAsync();
                TempData["TestSetupSuccess"] = $"Test '{item.Name}' updated.";
            }
            else
            {
                var item = new Item { ItemType = ItemType.Investigation };
                ApplyInput(item);
                item.CreaterId = _session.WebUserId;
                item.CreatedAt = now;
                _db.Items.Add(item);
                await _db.SaveChangesAsync();
                TempData["TestSetupSuccess"] = $"Test '{item.Name}' added.";
            }

            return RedirectToPage(new { SearchTerm, CategoryId, Status });
        }

        public async Task<IActionResult> OnPostRetireAsync(long id)
        {
            var item = await _db.Items.FindAsync(id);
            if (item != null)
            {
                item.Retired = true;
                item.RetiredAt = DateTime.Now;
                item.RetirerId = _session.WebUserId;
                await _db.SaveChangesAsync();
                TempData["TestSetupSuccess"] = $"Test '{item.Name}' retired.";
            }
            return RedirectToPage(new { SearchTerm, CategoryId, Status });
        }

        public async Task<IActionResult> OnPostActivateAsync(long id)
        {
            var item = await _db.Items.FindAsync(id);
            if (item != null)
            {
                item.Retired = false;
                item.RetiredAt = null;
                item.RetirerId = null;
                item.EditerId = _session.WebUserId;
                item.EditedAt = DateTime.Now;
                await _db.SaveChangesAsync();
                TempData["TestSetupSuccess"] = $"Test '{item.Name}' activated.";
            }
            return RedirectToPage(new { SearchTerm, CategoryId, Status });
        }

        private void ApplyInput(Item item)
        {
            item.Code = Input.Code?.Trim();
            item.Name = Input.Name?.Trim();
            item.PrintName = Input.PrintName?.Trim();
            item.Sname = Input.Sname?.Trim();
            item.CategoryId = Input.CategoryId;
            item.ItemType = Input.ItemType;
            item.Total = Input.Total;
            item.TotalForForeigner = Input.TotalForForeigner;
            item.StaffRate = Input.StaffRate;
            item.UnitRate = Input.UnitRate;
            item.Requirements = Input.Requirements?.Trim();
            item.MinDuration = Input.MinDuration?.Trim();
            item.MaxDuration = Input.MaxDuration?.Trim();
            item.ConsultantRequired = Input.ConsultantRequired;
            item.Billable = Input.Billable;
            item.Inactive = Input.Inactive;
            item.Descreption = Input.Descreption?.Trim();
        }

        private async Task LoadCategoriesAsync()
        {
            CategoryList = await _db.Categories
                .Where(c => !c.Retired)
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToListAsync();
        }

        private async Task LoadTestsAsync()
        {
            var baseQuery = _db.Items
                .Include(i => i.Category)
                .Where(i => i.ItemType == ItemType.Investigation || i.ItemType == ItemType.Service);

            ActiveCount = await baseQuery.CountAsync(i => !i.Retired);
            RetiredCount = await baseQuery.CountAsync(i => i.Retired);

            var query = baseQuery;

            switch (Status)
            {
                case "retired":
                    query = query.Where(i => i.Retired);
                    break;
                case "all":
                    break;
                default:
                    query = query.Where(i => !i.Retired);
                    break;
            }

            if (CategoryId.HasValue && CategoryId.Value > 0)
                query = query.Where(i => i.CategoryId == CategoryId.Value);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(i =>
                    (i.Name != null && i.Name.ToLower().Contains(term)) ||
                    (i.Code != null && i.Code.ToLower().Contains(term)) ||
                    (i.PrintName != null && i.PrintName.ToLower().Contains(term)));
            }

            Tests = await query
                .OrderBy(i => i.Code)
                .ThenBy(i => i.Name)
                .ToListAsync();
        }

        private bool TryGetId(string key, out long id)
        {
            id = 0;
            if (Request.Query.TryGetValue(key, out var raw))
                return long.TryParse(raw, out id);
            return false;
        }

        public class TestInput
        {
            public long Id { get; set; }
            public string Code { get; set; }
            public string Name { get; set; }
            public string PrintName { get; set; }
            public string Sname { get; set; }
            public string Descreption { get; set; }
            public long? CategoryId { get; set; }
            public ItemType ItemType { get; set; } = ItemType.Investigation;
            public double? Total { get; set; }
            public double? TotalForForeigner { get; set; }
            public double? StaffRate { get; set; }
            public double? UnitRate { get; set; }
            public string Requirements { get; set; }
            public string MinDuration { get; set; }
            public string MaxDuration { get; set; }
            public bool ConsultantRequired { get; set; }
            public bool Billable { get; set; } = true;
            public bool Inactive { get; set; }
            public bool ShowForm { get; set; }
        }
    }
}