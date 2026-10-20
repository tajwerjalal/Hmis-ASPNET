using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Dental
{
    public class CompanyChargesModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public CompanyChargesModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        public List<CompanyServiceRate> Items { get; set; } = new();
        public List<SelectListItem> CompanyList { get; set; } = new();
        public List<SelectListItem> ItemList { get; set; } = new();
        public string ErrorMessage { get; set; }
        public string Success { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public long? CompanyId { get; set; }
            public long? ItemId { get; set; }
            public double? Rate { get; set; }
            public double? UrgentRate { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!Input.CompanyId.HasValue || !Input.ItemId.HasValue)
            {
                ErrorMessage = "Company and service are required.";
                await LoadAsync();
                return Page();
            }
            var entity = new CompanyServiceRate
            {
                CompanyId = Input.CompanyId.Value,
                ItemId = Input.ItemId.Value,
                Rate = Input.Rate ?? 0,
                UnitRate = Input.UrgentRate,
                CreaterId = _session.WebUserId,
                CreatedAt = DateTime.Now,
                Retired = false
            };
            _db.CompanyServiceRates.Add(entity);
            await _db.SaveChangesAsync();
            Success = "Company charge saved.";
            Input = new InputModel();
            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            var entity = await _db.CompanyServiceRates.FindAsync(id);
            if (entity != null)
            {
                entity.Retired = true;
                entity.RetireComments = "Retired via Company Charges Setup";
                await _db.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        private async Task LoadAsync()
        {
            CompanyList = await _db.Institutions
                .Where(x => !x.Retired)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name })
                .ToListAsync();
            ItemList = await _db.Items
                .Where(x => !x.Retired && x.Billable)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name + " (" + x.Code + ")" })
                .ToListAsync();
            Items = await _db.CompanyServiceRates
                .Include(x => x.Company)
                .Include(x => x.Item)
                .Where(x => !x.Retired)
                .OrderBy(x => x.Company.Name).ThenBy(x => x.Item.Name)
                .ToListAsync();
        }
    }
}