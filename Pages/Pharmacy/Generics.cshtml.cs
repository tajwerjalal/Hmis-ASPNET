// ============================================================================
// Developer : Tajwer Jalal
// Date      : 19-Oct-2026
// Time      : 16:25
// Purpose   : Generic (molecule) master - port of legacy GENERIC.DBF.
//
//   A generic name is what a drug IS (PARACETAMOL); the medicine master holds
//   the brands that are that molecule. 1,192 rows migrated.
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
    public class GenericsModel : PageModel
    {
        private readonly PhService _ph;
        private readonly HmisWeb.Data.ApplicationDbContext _db;

        public GenericsModel(PhService ph, HmisWeb.Data.ApplicationDbContext db)
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

        public List<PhGenericRow> Rows { get; set; } = new();

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
            Rows = await _ph.GetGenericsAsync(Search);
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
                if (await _db.PhGenerics.AnyAsync(g => g.Code == code && g.Id != Edit.Id))
                {
                    Error = $"Generic code '{code}' already exists.";
                }
            }

            if (Error != null)
            {
                Rows = await _ph.GetGenericsAsync(Search);
                return Page();
            }

            var cleanCode = Edit.Code.Trim();
            var cleanName = Edit.Name.Trim();

            if (Edit.Id == 0)
            {
                _db.PhGenerics.Add(new PhGeneric { Code = cleanCode, Name = cleanName });
                Message = $"Generic '{cleanName}' added.";
            }
            else
            {
                var g = await _db.PhGenerics.FirstOrDefaultAsync(x => x.Id == Edit.Id);
                if (g == null) return NotFound();
                g.Code = cleanCode;
                g.Name = cleanName;
                Message = $"Generic '{cleanName}' updated.";
            }

            await _db.SaveChangesAsync();
            return RedirectToPage(new { Search });
        }

        /// <summary>Retire a generic, refused while drugs still name it.</summary>
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var g = await _db.PhGenerics.FirstOrDefaultAsync(x => x.Id == id);
            if (g == null) return NotFound();

            var used = await _ph.CountItemUsageAsync("GenericCode", g.Code);
            if (used > 0)
            {
                Error = $"'{g.Name}' cannot be deleted: {used} medicine(s) still use it. Re-assign them first.";
            }
            else
            {
                g.Retired = true;
                await _db.SaveChangesAsync();
                Message = $"Generic '{g.Name}' deleted.";
            }

            return RedirectToPage(new { Search });
        }
    }
}
