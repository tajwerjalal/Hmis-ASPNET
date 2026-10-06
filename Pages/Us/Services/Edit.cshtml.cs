using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Us.Services
{
    public class EditModel : UsServiceFormModel
    {
        public EditModel(ApplicationDbContext db, IAppSession appSession) : base(db, appSession)
        {
        }

        [BindProperty]
        public UsService Service { get; set; } = new();

        public long Id { get; set; }

        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            Id = id;

            var existing = await Db.UsServices.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
            if (existing == null)
                return NotFound();

            Service = existing;

            await LoadCategoriesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long id)
        {
            Id = id;

            var existing = await Db.UsServices.FirstOrDefaultAsync(s => s.Id == id);
            if (existing == null)
                return NotFound();

            await LoadCategoriesAsync();

            if (string.IsNullOrWhiteSpace(Service.Name))
            {
                ErrorMessage = "Service name is required.";
                Service.Code = existing.Code;
                return Page();
            }

            var trimmedName = Service.Name.Trim();

            var duplicate = await Db.UsServices
                .AnyAsync(s => s.Id != id && s.Name != null && s.Name == trimmedName && !s.Retired);

            if (duplicate)
            {
                ErrorMessage = $"A service named '{trimmedName}' already exists.";
                Service.Code = existing.Code;
                return Page();
            }

            if (Service.Rate < 0 || Service.RateForeigner < 0)
            {
                ErrorMessage = "Rates cannot be negative.";
                Service.Code = existing.Code;
                return Page();
            }

            // CopyEditableFields deliberately does not touch Code, so the auto-generated
            // code issued at creation is preserved regardless of what was posted.
            CopyEditableFields(existing, Service);
            existing.DepartmentId = await ResolveFixedDepartmentIdAsync();
            existing.InstitutionId = AppSession.LoggedSite?.Id;
            existing.EditerId = AppSession.WebUserId;
            existing.EditedAt = DateTime.Now;

            await Db.SaveChangesAsync();

            TempData["UsServiceSuccess"] = $"Service '{existing.Name}' updated successfully.";
            return RedirectToPage("/Us/Services");
        }
    }
}
