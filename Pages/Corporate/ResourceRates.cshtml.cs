using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Corporate
{
    public class ResourceRatesModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public ResourceRatesModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        public List<Resource> Resources { get; set; } = new();
        public List<Item> Services { get; set; } = new();
        public List<ResourceServiceRate> Rates { get; set; } = new();
        public List<SelectListItem> DepartmentList { get; set; } = new();
        public List<SelectListItem> ResourceList { get; set; } = new();
        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? FilterResourceId { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? FilterDepartmentId { get; set; }

        [BindProperty]
        public RateInputModel Rate { get; set; } = new();

        public class RateInputModel
        {
            public long? Id { get; set; }
            public long ResourceId { get; set; }
            public long ItemId { get; set; }
            public double StaffRate { get; set; }
            public double UnitRate { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (Rate.ResourceId <= 0 || Rate.ItemId <= 0)
            {
                ErrorMessage = "Resource and Service are required.";
                await LoadAsync();
                return Page();
            }

            var existing = Rate.Id.HasValue
                ? await _db.ResourceServiceRates.FindAsync(Rate.Id.Value)
                : await _db.ResourceServiceRates
                    .FirstOrDefaultAsync(r => r.ResourceId == Rate.ResourceId
                        && r.ItemId == Rate.ItemId
                        && !r.Retired);

            if (existing != null)
            {
                existing.StaffRate = Rate.StaffRate;
                existing.UnitRate = Rate.UnitRate;
                existing.EditedAt = DateTime.Now;
                existing.EditerId = _session.WebUserId;
                SuccessMessage = "Resource rate updated.";
            }
            else
            {
                _db.ResourceServiceRates.Add(new ResourceServiceRate
                {
                    ResourceId = Rate.ResourceId,
                    ItemId = Rate.ItemId,
                    StaffRate = Rate.StaffRate,
                    UnitRate = Rate.UnitRate,
                    CreatedAt = DateTime.Now,
                    CreaterId = _session.WebUserId
                });
                SuccessMessage = "Resource rate saved.";
            }

            await _db.SaveChangesAsync();
            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            var entity = await _db.ResourceServiceRates.FindAsync(id);
            if (entity != null)
            {
                entity.Retired = true;
                entity.RetireComments = "Retired from Resource Rates page.";
                entity.EditedAt = DateTime.Now;
                entity.EditerId = _session.WebUserId;
                await _db.SaveChangesAsync();
                SuccessMessage = "Resource rate retired.";
            }

            await LoadAsync();
            return Page();
        }

        private async Task LoadAsync()
        {
            ResourceList = await _db.Resources
                .Where(r => !r.Retired && r.IsActive)
                .OrderBy(r => r.Name)
                .Select(r => new SelectListItem { Value = r.Id.ToString(), Text = $"{r.Code} - {r.Name}" })
                .ToListAsync();

            DepartmentList = await _db.Departments
                .Where(d => !d.Retired)
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .ToListAsync();

            Resources = await _db.Resources
                .Where(r => !r.Retired)
                .OrderBy(r => r.Name)
                .ToListAsync();

            var itemQuery = _db.Items
                .Where(i => !i.Retired && i.Billable);

            if (FilterDepartmentId.HasValue && FilterDepartmentId.Value > 0)
                itemQuery = itemQuery.Where(i => i.DepartmentId == FilterDepartmentId.Value);

            Services = await itemQuery.OrderBy(i => i.Name).ToListAsync();

            var rateQuery = _db.ResourceServiceRates
                .Include(r => r.Resource)
                .Include(r => r.Item)
                .Where(r => !r.Retired);

            if (FilterResourceId.HasValue && FilterResourceId.Value > 0)
                rateQuery = rateQuery.Where(r => r.ResourceId == FilterResourceId.Value);

            Rates = await rateQuery.ToListAsync();
        }
    }
}