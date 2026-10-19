// ============================================================================
// Developer : Tajwer Jalal
// Date      : 19-Oct-2026
// Time      : 16:05
// Purpose   : Manufacturer master - port of legacy BASEMENT\SPR\MANUFAC.SPR.
//
//   Single merged master (398 rows) for the same reason as Supplier: a
//   manufacturer is one trading entity, not one per store.
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
    public class ManufacturersModel : PageModel
    {
        private readonly PhService _ph;
        private readonly HmisWeb.Data.ApplicationDbContext _db;

        public ManufacturersModel(PhService ph, HmisWeb.Data.ApplicationDbContext db)
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

        public List<PhManufacturerRow> Rows { get; set; } = new();

        public class EditModel
        {
            public int Id { get; set; }
            [System.ComponentModel.DataAnnotations.Required]
            public string Code { get; set; } = "";
            [System.ComponentModel.DataAnnotations.Required]
            public string Name { get; set; } = "";
            public string? ShortName { get; set; }
            public string? Address { get; set; }
        }

        public async Task OnGetAsync()
        {
            Rows = await _ph.GetManufacturersAsync(Search);
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
                if (await _db.PhManufacturers.AnyAsync(m => m.Code == code && m.Id != Edit.Id))
                {
                    Error = $"Manufacturer code '{code}' already exists.";
                }
            }

            if (Error != null)
            {
                Rows = await _ph.GetManufacturersAsync(Search);
                return Page();
            }

            var cleanCode = Edit.Code.Trim();
            var cleanName = Edit.Name.Trim();

            if (Edit.Id == 0)
            {
                _db.PhManufacturers.Add(new PhManufacturer
                {
                    Code = cleanCode,
                    Name = cleanName,
                    ShortName = Edit.ShortName,
                    Address = Edit.Address
                });
                Message = $"Manufacturer '{cleanName}' added.";
            }
            else
            {
                var m = await _db.PhManufacturers.FirstOrDefaultAsync(x => x.Id == Edit.Id);
                if (m == null) return NotFound();
                m.Code = cleanCode;
                m.Name = cleanName;
                m.ShortName = Edit.ShortName;
                m.Address = Edit.Address;
                Message = $"Manufacturer '{cleanName}' updated.";
            }

            await _db.SaveChangesAsync();
            return RedirectToPage(new { Search });
        }

        /// <summary>Retire a manufacturer, refused while drugs still name it.</summary>
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var m = await _db.PhManufacturers.FirstOrDefaultAsync(x => x.Id == id);
            if (m == null) return NotFound();

            var used = await _ph.CountItemUsageAsync("ManufacturerCode", m.Code);
            if (used > 0)
            {
                Error = $"'{m.Name}' cannot be deleted: {used} medicine(s) still name it as their manufacturer. " +
                        "Re-assign them first.";
            }
            else
            {
                m.Retired = true;
                await _db.SaveChangesAsync();
                Message = $"Manufacturer '{m.Name}' deleted.";
            }

            return RedirectToPage(new { Search });
        }
    }
}
