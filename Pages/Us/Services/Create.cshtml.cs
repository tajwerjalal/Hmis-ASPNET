using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Us.Services
{
    public class CreateModel : UsServiceFormModel
    {
        public CreateModel(ApplicationDbContext db, IAppSession appSession) : base(db, appSession)
        {
        }

        [BindProperty]
        public UsService Service { get; set; } = new();

        public string AutoCode { get; set; }

        public string ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            Service.ItemType = ItemType.Service;
            Service.Billable = true;
            Service.Active = true;

            await LoadCategoriesAsync();
            AutoCode = await GenerateNextCodeAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadCategoriesAsync();
            AutoCode = await GenerateNextCodeAsync();

            if (string.IsNullOrWhiteSpace(Service.Name))
            {
                ErrorMessage = "Service name is required.";
                return Page();
            }

            var duplicate = await Db.UsServices
                .AnyAsync(s => s.Name != null && s.Name == Service.Name.Trim() && !s.Retired);

            if (duplicate)
            {
                ErrorMessage = $"A service named '{Service.Name.Trim()}' already exists.";
                return Page();
            }

            if (Service.Rate < 0 || Service.RateForeigner < 0)
            {
                ErrorMessage = "Rates cannot be negative.";
                return Page();
            }

            var departmentId = await ResolveFixedDepartmentIdAsync();

            var service = new UsService();
            CopyEditableFields(service, Service);
            service.Code = AutoCode;
            service.DepartmentId = departmentId;
            service.InstitutionId = AppSession.LoggedSite?.Id;
            service.CreatedAt = DateTime.Now;
            service.CreaterId = AppSession.WebUserId;
            service.Retired = false;

            Db.UsServices.Add(service);
            await Db.SaveChangesAsync();

            TempData["UsServiceSuccess"] = $"Service '{service.Name}' created successfully with code {service.Code}.";
            return RedirectToPage("/Us/Services");
        }
    }
}
