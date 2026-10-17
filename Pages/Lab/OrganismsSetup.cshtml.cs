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
    /// Organisms Setup - micro-organisms used in culture reporting. Mirrors VFP Labsys form
    /// <c>lb0338</c> (organisms.dbf).
    /// </summary>
    public class OrganismsSetupModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public OrganismsSetupModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<Organism> Items { get; set; } = new();
        public string Success { get; set; }
        public string ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadAsync();
            if (TryGetId("edit", out var id) && id > 0)
            {
                var o = await _db.Organisms.FindAsync(id);
                if (o != null) Input = new InputModel { Id = o.Id, Code = o.Code, Name = o.Name, ShowForm = true };
            }
            if (Request.Query.ContainsKey("add")) Input.ShowForm = true;
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.Name))
            {
                TempData["OrgsError"] = "Organism name is required.";
                return RedirectToPage();
            }

            var now = DateTime.Now;
            if (Input.Id > 0)
            {
                var o = await _db.Organisms.FindAsync(Input.Id);
                if (o == null) { TempData["OrgsError"] = "Organism not found."; return RedirectToPage(); }
                o.Code = Input.Code?.Trim(); o.Name = Input.Name.Trim(); o.EditerId = _session.WebUserId; o.EditedAt = now;
                TempData["OrgsSuccess"] = "Organism updated.";
            }
            else
            {
                _db.Organisms.Add(new Organism { Code = Input.Code?.Trim(), Name = Input.Name.Trim(), CreaterId = _session.WebUserId, CreatedAt = now });
                TempData["OrgsSuccess"] = "Organism added.";
            }
            await _db.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRetireAsync(long id)
        {
            var o = await _db.Organisms.FindAsync(id);
            if (o != null) { o.Retired = true; await _db.SaveChangesAsync(); TempData["OrgsSuccess"] = "Organism retired."; }
            return RedirectToPage();
        }

        private async Task LoadAsync()
        {
            Success = TempData["OrgsSuccess"] as string;
            ErrorMessage = TempData["OrgsError"] as string;
            Items = await _db.Organisms.Where(o => !o.Retired).OrderBy(o => o.Name).ToListAsync();
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
            public bool ShowForm { get; set; }
        }
    }
}