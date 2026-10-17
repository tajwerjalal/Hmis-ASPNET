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
    /// Test Remarks Setup - canned remarks that can be attached to a test report.
    /// Mirrors VFP Labsys form <c>lb0333</c> (tsremark.dbf).
    /// </summary>
    public class TestRemarksSetupModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public TestRemarksSetupModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<TestRemark> Items { get; set; } = new();
        public List<SelectListItem> TestList { get; set; } = new();
        public string Success { get; set; }
        public string ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadAsync();
            if (TryGetId("edit", out var id) && id > 0)
            {
                var r = await _db.TestRemarks.FindAsync(id);
                if (r != null) Input = new InputModel { Id = r.Id, ItemId = r.ItemId, Remarks = r.Remarks, ShowForm = true };
            }
            if (Request.Query.ContainsKey("add")) Input.ShowForm = true;
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.Remarks))
            {
                TempData["TrsError"] = "Remarks text is required.";
                return RedirectToPage();
            }

            var now = DateTime.Now;
            if (Input.Id > 0)
            {
                var r = await _db.TestRemarks.FindAsync(Input.Id);
                if (r == null) { TempData["TrsError"] = "Remark not found."; return RedirectToPage(); }
                r.ItemId = Input.ItemId; r.Remarks = Input.Remarks.Trim(); r.EditerId = _session.WebUserId; r.EditedAt = now;
                TempData["TrsSuccess"] = "Remark updated.";
            }
            else
            {
                _db.TestRemarks.Add(new TestRemark { ItemId = Input.ItemId, Remarks = Input.Remarks.Trim(), CreaterId = _session.WebUserId, CreatedAt = now });
                TempData["TrsSuccess"] = "Remark added.";
            }
            await _db.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRetireAsync(long id)
        {
            var r = await _db.TestRemarks.FindAsync(id);
            if (r != null) { r.Retired = true; await _db.SaveChangesAsync(); TempData["TrsSuccess"] = "Remark retired."; }
            return RedirectToPage();
        }

        private async Task LoadAsync()
        {
            Success = TempData["TrsSuccess"] as string;
            ErrorMessage = TempData["TrsError"] as string;
            Items = await _db.TestRemarks.Include(r => r.Item).Where(r => !r.Retired).OrderBy(r => r.Id).ToListAsync();
            TestList = await _db.Items
                .Where(i => !i.Retired && (i.ItemType == ItemType.Investigation || i.ItemType == ItemType.Service))
                .OrderBy(i => i.Name)
                .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = (i.Code ?? "") + " - " + i.Name }).ToListAsync();
        }

        private bool TryGetId(string key, out long id)
        {
            id = 0;
            return Request.Query.TryGetValue(key, out var raw) && long.TryParse(raw, out id);
        }

        public class InputModel
        {
            public long Id { get; set; }
            public long? ItemId { get; set; }
            public string Remarks { get; set; }
            public bool ShowForm { get; set; }
        }
    }
}