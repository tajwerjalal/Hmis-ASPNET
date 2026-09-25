using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Corporate
{
    public class RatesModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public RatesModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        public List<Institution> Companies { get; set; } = new();
        public List<Item> Services { get; set; } = new();
        public List<Item> PopupServices { get; set; } = new();
        public List<CompanyServiceRate> Rates { get; set; } = new();
        public List<SelectListItem> DepartmentList { get; set; } = new();
        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }

        public int TotalCompaniesWithRates { get; set; }
        public int TotalServicesWithRates { get; set; }
        public int TotalRateConfigurations { get; set; }

        [BindProperty]
        public long? FilterDepartmentId { get; set; }

        [BindProperty]
        public string ViewMode { get; set; } = "matrix";

        [BindProperty]
        public RateInputModel RateInput { get; set; } = new();

        public class RateInputModel
        {
            public long? Id { get; set; }
            public long CompanyId { get; set; }
            public long ItemId { get; set; }
            public long? DepartmentId { get; set; }
            public double Rate { get; set; }
            public double? UnitRate { get; set; }
            public double? DiscountPercent { get; set; }
            public DateTime? EffectiveFrom { get; set; }
            public DateTime? EffectiveTo { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostFilterAsync()
        {
            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSaveRateAsync()
        {
            if (RateInput.CompanyId <= 0 || RateInput.ItemId <= 0)
            {
                ErrorMessage = "Company and Service are required.";
                await LoadAsync();
                return Page();
            }

            if (RateInput.Id.HasValue && RateInput.Id.Value > 0)
            {
                var existing = await _db.CompanyServiceRates.FindAsync(RateInput.Id.Value);
                if (existing != null)
                {
                    existing.Rate = RateInput.Rate;
                    existing.UnitRate = RateInput.UnitRate;
                    existing.DepartmentId = RateInput.DepartmentId;
                    existing.DiscountPercent = RateInput.DiscountPercent;
                    existing.EffectiveFrom = RateInput.EffectiveFrom;
                    existing.EffectiveTo = RateInput.EffectiveTo;
                    existing.EditerId = _session.WebUserId;
                    existing.EditedAt = DateTime.Now;
                }
                SuccessMessage = "Rate updated successfully.";
            }
            else
            {
                var existingRate = await _db.CompanyServiceRates
                    .FirstOrDefaultAsync(r => r.CompanyId == RateInput.CompanyId
                        && r.ItemId == RateInput.ItemId
                        && !r.Retired);

                if (existingRate != null)
                {
                    existingRate.Rate = RateInput.Rate;
                    existingRate.UnitRate = RateInput.UnitRate;
                    existingRate.DepartmentId = RateInput.DepartmentId;
                    existingRate.DiscountPercent = RateInput.DiscountPercent;
                    existingRate.EffectiveFrom = RateInput.EffectiveFrom;
                    existingRate.EffectiveTo = RateInput.EffectiveTo;
                    existingRate.EditerId = _session.WebUserId;
                    existingRate.EditedAt = DateTime.Now;
                    SuccessMessage = "Existing rate updated for this company+service.";
                }
                else
                {
                    var entity = new CompanyServiceRate
                    {
                        CompanyId = RateInput.CompanyId,
                        ItemId = RateInput.ItemId,
                        DepartmentId = RateInput.DepartmentId,
                        Rate = RateInput.Rate,
                        UnitRate = RateInput.UnitRate,
                        DiscountPercent = RateInput.DiscountPercent,
                        EffectiveFrom = RateInput.EffectiveFrom,
                        EffectiveTo = RateInput.EffectiveTo,
                        Retired = false,
                        CreaterId = _session.WebUserId,
                        CreatedAt = DateTime.Now
                    };
                    _db.CompanyServiceRates.Add(entity);
                    SuccessMessage = "Rate saved successfully.";
                }
            }

            await _db.SaveChangesAsync();
            ViewMode = "matrix";
            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteRateAsync(long id)
        {
            var entity = await _db.CompanyServiceRates.FindAsync(id);
            if (entity != null)
            {
                entity.Retired = true;
                entity.RetireComments = "Retired from Rates page";
                entity.EditerId = _session.WebUserId;
                entity.EditedAt = DateTime.Now;
                await _db.SaveChangesAsync();
                SuccessMessage = "Rate retired.";
            }

            ViewMode = "list";
            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeactivateRateAsync(long id)
        {
            var entity = await _db.CompanyServiceRates.FindAsync(id);
            if (entity != null)
            {
                entity.EffectiveTo = DateTime.Now.AddDays(-1);
                entity.EditerId = _session.WebUserId;
                entity.EditedAt = DateTime.Now;
                await _db.SaveChangesAsync();
                SuccessMessage = "Rate deactivated.";
            }

            ViewMode = "list";
            await LoadAsync();
            return Page();
        }

        private async Task LoadAsync()
        {
            Companies = await _db.Institutions
                .Where(i => !i.Retired && i.InstitutionType == InstitutionType.InsuranceCompany)
                .OrderBy(i => i.Name)
                .ToListAsync();

            DepartmentList = await _db.Departments
                .Where(d => !d.Retired)
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .ToListAsync();

            var itemQuery = _db.Items
                .Include(i => i.Department)
                .Where(i => !i.Retired && i.Billable);

            if (FilterDepartmentId.HasValue && FilterDepartmentId.Value > 0)
            {
                itemQuery = itemQuery.Where(i => i.DepartmentId == FilterDepartmentId.Value);
            }

            Services = await itemQuery.OrderBy(i => i.Name).ToListAsync();

            PopupServices = await _db.Items
                .Include(i => i.Department)
                .Where(i => !i.Retired && i.Billable)
                .OrderBy(i => i.Name)
                .ToListAsync();

            Rates = await _db.CompanyServiceRates
                .Include(r => r.Company)
                .Include(r => r.Item)
                .Where(r => !r.Retired)
                .ToListAsync();

            TotalCompaniesWithRates = Rates.Select(r => r.CompanyId).Distinct().Count();
            TotalServicesWithRates = Rates.Select(r => r.ItemId).Distinct().Count();
            TotalRateConfigurations = Rates.Count;
        }
    }
}
