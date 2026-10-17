using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Lab
{
    /// <summary>
    /// Test Data Setup - the measurable parameters of a test with their unit and normal range.
    /// Mirrors VFP Labsys form <c>lb0310</c> (readings.dbf / nrange.dbf).
    /// </summary>
    public class TestDataSetupModel : LabListPageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public TestDataSetupModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? CategoryId { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<TestData> Items { get; set; } = new();
        public List<SelectListItem> CategoryList { get; set; } = new();
        public string Success { get; set; }
        public string ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadAsync();
            if (TryGetId("edit", out var id) && id > 0)
            {
                var d = await _db.TestData.FindAsync(id);
                if (d != null)
                    Input = new InputModel { Id = d.Id, Code = d.Code, Name = d.Name, Unit = d.Unit, Range = d.Range, CategoryId = d.CategoryId, OrderNo = d.OrderNo, ShowForm = true };
            }
            if (Request.Query.ContainsKey("add")) Input.ShowForm = true;
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.Name))
            {
                TempData["TdsError"] = "Test data name is required.";
                return RedirectToPage(new { SearchTerm, CategoryId });
            }

            var now = DateTime.Now;
            if (Input.Id > 0)
            {
                var d = await _db.TestData.FindAsync(Input.Id);
                if (d == null) { TempData["TdsError"] = "Test data not found."; return RedirectToPage(new { SearchTerm, CategoryId }); }
                Apply(d); d.EditerId = _session.WebUserId; d.EditedAt = now;
                TempData["TdsSuccess"] = $"Test data '{d.Name}' updated.";
            }
            else
            {
                var d = new TestData();
                Apply(d); d.CreaterId = _session.WebUserId; d.CreatedAt = now;
                _db.TestData.Add(d);
                TempData["TdsSuccess"] = $"Test data '{d.Name}' added.";
            }
            await _db.SaveChangesAsync();
            return RedirectToPage(new { SearchTerm, CategoryId });
        }

        public async Task<IActionResult> OnPostRetireAsync(long id)
        {
            var d = await _db.TestData.FindAsync(id);
            if (d != null) { d.Retired = true; d.RetiredAt = DateTime.Now; d.RetirerId = _session.WebUserId; await _db.SaveChangesAsync(); TempData["TdsSuccess"] = $"Test data '{d.Name}' retired."; }
            return RedirectToPage(new { SearchTerm, CategoryId });
        }

        public async Task<IActionResult> OnPostActivateAsync(long id)
        {
            var d = await _db.TestData.FindAsync(id);
            if (d != null) { d.Retired = false; d.RetiredAt = null; d.RetirerId = null; d.EditerId = _session.WebUserId; d.EditedAt = DateTime.Now; await _db.SaveChangesAsync(); TempData["TdsSuccess"] = $"Test data '{d.Name}' activated."; }
            return RedirectToPage(new { SearchTerm, CategoryId });
        }

        private void Apply(TestData d)
        {
            d.Code = Input.Code?.Trim();
            d.Name = Input.Name?.Trim();
            d.Unit = Input.Unit?.Trim();
            d.Range = Input.Range?.Trim();
            d.CategoryId = Input.CategoryId;
            d.OrderNo = Input.OrderNo;
        }

        private async Task LoadAsync()
        {
            Success = TempData["TdsSuccess"] as string;
            ErrorMessage = TempData["TdsError"] as string;

            var q = _db.TestData.Include(d => d.Category).Where(d => !d.Retired);
            if (CategoryId.HasValue && CategoryId.Value > 0) q = q.Where(d => d.CategoryId == CategoryId.Value);
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var t = SearchTerm.Trim().ToLower();
                q = q.Where(d => (d.Name != null && d.Name.ToLower().Contains(t)) || (d.Code != null && d.Code.ToLower().Contains(t)));
            }
            var ordered = q.OrderBy(d => d.OrderNo).ThenBy(d => d.Name);
            TotalCount = await ordered.CountAsync();
            Items = await ordered.Skip(Skip).Take(PageSize).ToListAsync();

            CategoryList = await _db.Categories.Where(c => !c.Retired)
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToListAsync();
        }

        private bool TryGetId(string key, out long id)
        {
            id = 0;
            return Request.Query.TryGetValue(key, out var raw) && long.TryParse(raw, out id);
        }

        public class InputModel
        {
            public long Id { get; set; }
            public string Code { get; set; }
            public string Name { get; set; }
            public string Unit { get; set; }
            public string Range { get; set; }
            public long? CategoryId { get; set; }
            public int OrderNo { get; set; }
            public bool ShowForm { get; set; }
        }
    }
}