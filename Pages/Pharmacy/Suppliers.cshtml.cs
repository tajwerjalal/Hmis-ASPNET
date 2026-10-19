// ============================================================================
// Developer : Tajwer Jalal
// Date      : 19-Oct-2026
// Time      : 15:55
// Purpose   : Supplier master - port of legacy BASEMENT\SPR\SUPPLIER.SPR.
//
// ONE MASTER, NOT ONE PER STORE
//   Both stores had their own supplier file. The migrated PHSUPPLIER is a
//   single merged list (821 rows) because a supplier is the same trading
//   entity regardless of which store buys from it.
//
// DELETE IS A SOFT DELETE
//   PHITEM.SUPPLIERCODE holds this supplier's code, and purchase documents
//   reference it too. Retiring the row keeps every historic document
//   resolvable while removing it from the pick list.
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
    public class SuppliersModel : PageModel
    {
        private readonly PhService _ph;
        private readonly HmisWeb.Data.ApplicationDbContext _db;

        public SuppliersModel(PhService ph, HmisWeb.Data.ApplicationDbContext db)
        {
            _ph = ph;
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool ActiveOnly { get; set; }

        [BindProperty]
        public EditModel Edit { get; set; } = new();

        [TempData] public string? Message { get; set; }
        [TempData] public string? Error { get; set; }

        public List<PhSupplierRow> Rows { get; set; } = new();

        public class EditModel
        {
            public int Id { get; set; }
            [System.ComponentModel.DataAnnotations.Required]
            public string Code { get; set; } = "";
            [System.ComponentModel.DataAnnotations.Required]
            public string Name { get; set; } = "";
            public string? ShortName { get; set; }
            public string? Address { get; set; }
            public string? Phone { get; set; }
            public string? City { get; set; }
            public string? SupplierType { get; set; }
            public bool Active { get; set; } = true;
        }

        public async Task OnGetAsync()
        {
            Rows = await _ph.GetSuppliersAsync(Search, ActiveOnly);
        }

        /// <summary>Save (insert or update) one supplier, then refresh the list.</summary>
        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Edit.Code) || string.IsNullOrWhiteSpace(Edit.Name))
            {
                Error = "Code and Name are both required.";
            }
            else
            {
                var code = Edit.Code.Trim();
                if (await _db.PhSuppliers.AnyAsync(s => s.Code == code && s.Id != Edit.Id))
                {
                    Error = $"Supplier code '{code}' already exists.";
                }
            }

            // A failed save keeps what was typed and reopens the popup.
            if (Error != null)
            {
                Rows = await _ph.GetSuppliersAsync(Search, ActiveOnly);
                return Page();
            }

            var cleanCode = Edit.Code.Trim();
            var cleanName = Edit.Name.Trim();

            if (Edit.Id == 0)
            {
                _db.PhSuppliers.Add(new PhSupplier
                {
                    Code = cleanCode,
                    Name = cleanName,
                    ShortName = Edit.ShortName,
                    Address = Edit.Address,
                    Phone = Edit.Phone,
                    City = Edit.City,
                    SupplierType = Edit.SupplierType,
                    Active = Edit.Active
                });
                Message = $"Supplier '{cleanName}' added.";
            }
            else
            {
                var s = await _db.PhSuppliers.FirstOrDefaultAsync(x => x.Id == Edit.Id);
                if (s == null) return NotFound();
                s.Code = cleanCode;
                s.Name = cleanName;
                s.ShortName = Edit.ShortName;
                s.Address = Edit.Address;
                s.Phone = Edit.Phone;
                s.City = Edit.City;
                s.SupplierType = Edit.SupplierType;
                s.Active = Edit.Active;
                Message = $"Supplier '{cleanName}' updated.";
            }

            await _db.SaveChangesAsync();
            return RedirectToPage(new { Search, ActiveOnly });
        }

        /// <summary>
        /// Retire a supplier. Refused while drugs still point at it, because
        /// those drugs would then show a supplier code with no supplier behind
        /// it - the purchase screen would silently lose the vendor name.
        /// </summary>
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var s = await _db.PhSuppliers.FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return NotFound();

            var used = await _ph.CountItemUsageAsync("SupplierCode", s.Code);
            if (used > 0)
            {
                Error = $"'{s.Name}' cannot be deleted: {used} medicine(s) still name it as their supplier. " +
                        "Re-assign them first.";
            }
            else
            {
                s.Retired = true;
                s.Active = false;
                await _db.SaveChangesAsync();
                Message = $"Supplier '{s.Name}' deleted.";
            }

            return RedirectToPage(new { Search, ActiveOnly });
        }
    }
}
