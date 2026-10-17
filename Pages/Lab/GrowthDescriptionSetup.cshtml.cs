using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Lab
{
    /// <summary>
    /// Growth Description Setup - culture growth descriptions. Mirrors VFP Labsys form
    /// <c>lb0336</c> (grremark.dbf).
    /// </summary>
    public class GrowthDescriptionSetupModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public GrowthDescriptionSetupModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<GrowthDescription> Items { get; set; } = new();
        public string Success { get; set; }
        public string ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadAsync();
            if (TryGetId("edit", out var id) && id > 0)
            {
                var g = await _db.GrowthDescriptions.FindAsync(id);
                if (g != null) Input = new InputModel { Id = g.Id, Code = g.Code, Description = g.Description, ShowForm = true };
            }
            if (Request.Query.ContainsKey("add")) Input.ShowForm = true;
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.Description))
            {
                TempData["GdsError"] = "Description is required.";
                return RedirectToPage();
            }

            var now = DateTime.Now;
            if (Input.Id > 0)
            {
                var g = await _db.GrowthDescriptions.FindAsync(Input.Id);
                if (g == null) { TempData["GdsError"] = "Growth description not found."; return RedirectToPage(); }
                g.Code = Input.Code?.Trim(); g.Description = Input.Description.Trim(); g.EditerId = _session.WebUserId; g.EditedAt = now;
                TempData["GdsSuccess"] = "Growth description updated.";
            }
            else
            {
                _db.GrowthDescriptions.Add(new GrowthDescription { Code = Input.Code?.Trim(), Description = Input.Description.Trim(), CreaterId = _session.WebUserId, CreatedAt = now });
                TempData["GdsSuccess"] = "Growth description added.";
            }
            await _db.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRetireAsync(long id)
        {
            var g = await _db.GrowthDescriptions.FindAsync(id);
            if (g != null) { g.Retired = true; await _db.SaveChangesAsync(); TempData["GdsSuccess"] = "Growth description retired."; }
            return RedirectToPage();
        }

        private async Task LoadAsync()
        {
            Success = TempData["GdsSuccess"] as string;
            ErrorMessage = TempData["GdsError"] as string;
            Items = await _db.GrowthDescriptions.Where(g => !g.Retired).OrderBy(g => g.Code).ToListAsync();
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
            public string Description { get; set; }
            public bool ShowForm { get; set; }
        }
    }
}