using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Lab
{
    /// <summary>
    /// Medicine Classification Setup - groupings for medicines/pharmacy items.
    /// Mirrors VFP Labsys form <c>lb0325</c> (madicls.dbf). A medicine classification is a
    /// <see cref="Category"/> with <see cref="SymanticType.Medicine"/>.
    /// </summary>
    public class MedicineClassificationSetupModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public MedicineClassificationSetupModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<Category> Items { get; set; } = new();
        public string Success { get; set; }
        public string ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadAsync();
            if (TryGetId("edit", out var id) && id > 0)
            {
                var c = await _db.Categories.FindAsync(id);
                if (c != null)
                    Input = new InputModel { Id = c.Id, Name = c.Name, Code = c.Code, Description = c.Description, ShowForm = true };
            }
            if (Request.Query.ContainsKey("add")) Input.ShowForm = true;
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.Name))
            {
                TempData["McsError"] = "Classification name is required.";
                return RedirectToPage();
            }

            var now = DateTime.Now;
            if (Input.Id > 0)
            {
                var c = await _db.Categories.FindAsync(Input.Id);
                if (c == null) { TempData["McsError"] = "Classification not found."; return RedirectToPage(); }
                c.Name = Input.Name.Trim(); c.Code = Input.Code?.Trim(); c.Description = Input.Description?.Trim();
                c.EditerId = _session.WebUserId; c.EditedAt = now;
                TempData["McsSuccess"] = $"Classification '{c.Name}' updated.";
            }
            else
            {
                _db.Categories.Add(new Category
                {
                    Name = Input.Name.Trim(),
                    Code = Input.Code?.Trim(),
                    Description = Input.Description?.Trim(),
                    SymanticType = SymanticType.Medicine,
                    CreaterId = _session.WebUserId,
                    CreatedAt = now
                });
                TempData["McsSuccess"] = $"Classification '{Input.Name.Trim()}' added.";
            }
            await _db.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRetireAsync(long id)
        {
            var c = await _db.Categories.FindAsync(id);
            if (c != null) { c.Retired = true; c.RetiredAt = DateTime.Now; c.RetirerId = _session.WebUserId; await _db.SaveChangesAsync(); TempData["McsSuccess"] = $"Classification '{c.Name}' retired."; }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostActivateAsync(long id)
        {
            var c = await _db.Categories.FindAsync(id);
            if (c != null) { c.Retired = false; c.RetiredAt = null; c.RetirerId = null; c.EditerId = _session.WebUserId; c.EditedAt = DateTime.Now; await _db.SaveChangesAsync(); TempData["McsSuccess"] = $"Classification '{c.Name}' activated."; }
            return RedirectToPage();
        }

        private async Task LoadAsync()
        {
            Success = TempData["McsSuccess"] as string;
            ErrorMessage = TempData["McsError"] as string;
            Items = await _db.Categories.Where(c => c.SymanticType == SymanticType.Medicine).OrderBy(c => c.Name).ToListAsync();
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
            public string Description { get; set; }
            public bool ShowForm { get; set; }
        }
    }
}