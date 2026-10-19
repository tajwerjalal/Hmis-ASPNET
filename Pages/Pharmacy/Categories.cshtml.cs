// ============================================================================
// Developer : Tajwer Jalal
// Date      : 19-Oct-2026
// Time      : 16:30
// Purpose   : Item catalogue master - port of legacy CATALOG.DBF (18 rows).
//
//   Catalogues group drugs for reporting and filtering (ANTIBIOTIC, ACE
//   INHIBITORS, ...). The medicine screen filters on these.
// ============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HmisWeb.Models.Pharmacy;
using HmisWeb.Services.Pharmacy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Pharmacy
{
    public class CategoriesModel : PageModel
    {
        private readonly PhService _ph;
        private readonly HmisWeb.Data.ApplicationDbContext _db;

        public CategoriesModel(PhService ph, HmisWeb.Data.ApplicationDbContext db)
        {
            _ph = ph;
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty]
        public EditModel Edit { get; set; } = new();

        [TempData] public string? Message { get; set; }
        [TempData] public string? Error { get; set; }

        public List<PhCategoryRow> Rows { get; set; } = new();

        public class EditModel
        {
            public int Id { get; set; }
            [System.ComponentModel.DataAnnotations.Required]
            public string Code { get; set; } = "";
            [System.ComponentModel.DataAnnotations.Required]
            public string Name { get; set; } = "";
        }

        public async Task OnGetAsync()
        {
            Rows = await _ph.GetCategoryRowsAsync(Search);
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Edit.Code) || string.IsNullOrWhiteSpace(Edit.Name))
            {
                Error = "Code and Name are both required.";
            }
            else
            {
                var code = Edit.Code.Trim();
                if (await _db.PhCategories.AnyAsync(c => c.Code == code && c.Id != Edit.Id))
                {
                    Error = $"Catalogue code '{code}' already exists.";
                }
            }

            if (Error != null)
            {
                Rows = await _ph.GetCategoryRowsAsync(Search);
                return Page();
            }

            var cleanCode = Edit.Code.Trim();
            var cleanName = Edit.Name.Trim();

            if (Edit.Id == 0)
            {
                _db.PhCategories.Add(new PhCategory { Code = cleanCode, Name = cleanName });
                Message = $"Catalogue '{cleanName}' added.";
            }
            else
            {
                var c = await _db.PhCategories.FirstOrDefaultAsync(x => x.Id == Edit.Id);
                if (c == null) return NotFound();
                c.Code = cleanCode;
                c.Name = cleanName;
                Message = $"Catalogue '{cleanName}' updated.";
            }

            await _db.SaveChangesAsync();
            return RedirectToPage(new { Search });
        }

        /// <summary>Retire a catalogue, refused while drugs still use it.</summary>
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var c = await _db.PhCategories.FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) return NotFound();

            var used = await _ph.CountItemUsageAsync("CategoryCode", c.Code);
            if (used > 0)
            {
                Error = $"'{c.Name}' cannot be deleted: {used} medicine(s) still use it. Re-assign them first.";
            }
            else
            {
                c.Retired = true;
                await _db.SaveChangesAsync();
                Message = $"Catalogue '{c.Name}' deleted.";
            }

            return RedirectToPage(new { Search });
        }
    }
}
