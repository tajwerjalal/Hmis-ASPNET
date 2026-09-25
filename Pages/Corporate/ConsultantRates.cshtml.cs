using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Corporate
{
    public class ConsultantRatesModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public ConsultantRatesModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        public List<ConsultantServiceRate> Rates { get; set; } = new();
        public List<SelectListItem> ConsultantList { get; set; } = new();
        public List<SelectListItem> DepartmentList { get; set; } = new();
        public List<Item> Services { get; set; } = new();
        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? FilterConsultantId { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? FilterDepartmentId { get; set; }

        [BindProperty]
        public RateInputModel Rate { get; set; } = new();

        public class RateInputModel
        {
            public long? Id { get; set; }
            public long ConsultantId { get; set; }
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
            if (Rate.ConsultantId <= 0 || Rate.ItemId <= 0)
            {
                ErrorMessage = "Consultant and Service are required.";
                await LoadAsync();
                return Page();
            }

            var existing = Rate.Id.HasValue
                ? await _db.ConsultantServiceRates.FindAsync(Rate.Id.Value)
                : await _db.ConsultantServiceRates
                    .FirstOrDefaultAsync(r => r.ConsultantId == Rate.ConsultantId
                        && r.ItemId == Rate.ItemId
                        && !r.Retired);

            if (existing != null)
            {
                existing.StaffRate = Rate.StaffRate;
                existing.UnitRate = Rate.UnitRate;
                existing.EditedAt = DateTime.Now;
                existing.EditerId = _session.WebUserId;
                SuccessMessage = "Consultant rate updated.";
            }
            else
            {
                _db.ConsultantServiceRates.Add(new ConsultantServiceRate
                {
                    ConsultantId = Rate.ConsultantId,
                    ItemId = Rate.ItemId,
                    StaffRate = Rate.StaffRate,
                    UnitRate = Rate.UnitRate,
                    CreatedAt = DateTime.Now,
                    CreaterId = _session.WebUserId
                });
                SuccessMessage = "Consultant rate saved.";
            }

            await _db.SaveChangesAsync();
            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            var entity = await _db.ConsultantServiceRates.FindAsync(id);
            if (entity != null)
            {
                entity.Retired = true;
                entity.RetireComments = "Retired from Consultant Rates page.";
                entity.EditedAt = DateTime.Now;
                entity.EditerId = _session.WebUserId;
                await _db.SaveChangesAsync();
                SuccessMessage = "Consultant rate retired.";
            }

            await LoadAsync();
            return Page();
        }

        private async Task LoadAsync()
        {
            ConsultantList = await _db.Consultants
                .Include(c => c.Person)
                .Where(c => !c.Retired)
                .OrderBy(c => c.Person != null ? c.Person.Name : "")
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Person != null
                        ? (c.Person.NameWithInitials ?? c.Person.Name ?? ("Consultant #" + c.Id))
                        : ("Consultant #" + c.Id)
                })
                .ToListAsync();

            DepartmentList = await _db.Departments
                .Where(d => !d.Retired)
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .ToListAsync();

            var itemQuery = _db.Items
                .Where(i => !i.Retired && i.Billable);

            if (FilterDepartmentId.HasValue && FilterDepartmentId.Value > 0)
                itemQuery = itemQuery.Where(i => i.DepartmentId == FilterDepartmentId.Value);

            Services = await itemQuery.OrderBy(i => i.Name).ToListAsync();

            var rateQuery = _db.ConsultantServiceRates
                .Include(r => r.Consultant).ThenInclude(c => c.Person)
                .Include(r => r.Item)
                .Where(r => !r.Retired);

            if (FilterConsultantId.HasValue && FilterConsultantId.Value > 0)
                rateQuery = rateQuery.Where(r => r.ConsultantId == FilterConsultantId.Value);

            Rates = await rateQuery.ToListAsync();
        }
    }
}