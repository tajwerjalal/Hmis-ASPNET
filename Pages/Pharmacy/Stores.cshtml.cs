// ============================================================================
// Developer : Tajwer Jalal
// Date      : 19-Oct-2026
// Time      : 12:55
// Purpose   : Manage the pharmacy store registry. A store is a row here, not a
//             folder on disk as it was in the legacy BASEMENT / Satell split.
// ============================================================================
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using HmisWeb.Data;
using HmisWeb.Models.Pharmacy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Pharmacy
{
    public class StoresModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public StoresModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<PhStore> Stores { get; set; } = new();

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [TempData]
        public string? Message { get; set; }

        [TempData]
        public string? Error { get; set; }

        public class InputModel
        {
            public int? Id { get; set; }
            [Required, StringLength(10)]
            public string Code { get; set; } = "";
            [Required, StringLength(100)]
            public string Name { get; set; } = "";
            [StringLength(10)]
            public string StoreType { get; set; } = "SUB";
            [StringLength(200)] public string? Address { get; set; }
            [StringLength(50)] public string? Phone { get; set; }
            public bool IsActive { get; set; } = true;
        }

        public async Task OnGetAsync()
        {
            Stores = await _db.PhStores.AsNoTracking().OrderBy(s => s.StoreType).ThenBy(s => s.Code).ToListAsync();
        }

        public IActionResult OnGetEdit(int id)
        {
            var s = _db.PhStores.AsNoTracking().FirstOrDefault(x => x.Id == id);
            if (s == null) return NotFound();
            Input = new InputModel
            {
                Id = s.Id, Code = s.Code, Name = s.Name, StoreType = s.StoreType,
                Address = s.Address, Phone = s.Phone, IsActive = s.IsActive
            };
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.Code) || string.IsNullOrWhiteSpace(Input.Name))
            {
                Error = "Code and Name are both required.";
            }
            else if (Input.StoreType != "MAIN" && Input.StoreType != "SUB")
            {
                Error = "Store Type must be MAIN or SUB.";
            }
            else
            {
                var dup = await _db.PhStores.AnyAsync(s => s.Code == Input.Code.Trim()
                                                            && (Input.Id == null || s.Id != Input.Id));
                if (dup)
                {
                    Error = $"Store code '{Input.Code.Trim()}' already exists.";
                }
                else
                {
                    if (Input.Id == null)
                    {
                        _db.PhStores.Add(new PhStore
                        {
                            Code = Input.Code.Trim(),
                            Name = Input.Name.Trim(),
                            StoreType = Input.StoreType,
                            Address = Input.Address,
                            Phone = Input.Phone,
                            IsActive = Input.IsActive
                        });
                        Message = $"Store '{Input.Name.Trim()}' added.";
                    }
                    else
                    {
                        var s = await _db.PhStores.FirstOrDefaultAsync(x => x.Id == Input.Id);
                        if (s == null) return NotFound();
                        s.Code = Input.Code.Trim();
                        s.Name = Input.Name.Trim();
                        s.StoreType = Input.StoreType;
                        s.Address = Input.Address;
                        s.Phone = Input.Phone;
                        s.IsActive = Input.IsActive;
                        Message = $"Store '{s.Name}' updated.";
                    }
                    await _db.SaveChangesAsync();
                }
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleAsync(int id)
        {
            var s = await _db.PhStores.FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return NotFound();
            s.IsActive = !s.IsActive;
            await _db.SaveChangesAsync();
            Message = $"Store '{s.Name}' is now {(s.IsActive ? "active" : "inactive")}.";
            return RedirectToPage();
        }
    }
}
