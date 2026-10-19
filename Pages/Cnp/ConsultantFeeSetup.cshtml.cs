using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Cnp
{
    public class ConsultantFeeSetupModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public ConsultantFeeSetupModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        public List<ConsultantServiceRate> Items { get; set; } = new();
        public List<SelectListItem> ConsultantList { get; set; } = new();
        public List<SelectListItem> ItemList { get; set; } = new();
        public string ErrorMessage { get; set; }
        public string Success { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public long? ConsultantId { get; set; }
            public long? ItemId { get; set; }
            public double? StaffRate { get; set; }
            public double? UnitRate { get; set; }
        }

        public async Task OnGetAsync() => await LoadAsync();

        public async Task<IActionResult> OnPostAsync()
        {
            if (!Input.ConsultantId.HasValue || !Input.ItemId.HasValue)
            {
                ErrorMessage = "Consultant and service are required.";
                await LoadAsync();
                return Page();
            }
            var entity = new ConsultantServiceRate
            {
                ConsultantId = Input.ConsultantId.Value,
                ItemId = Input.ItemId.Value,
                StaffRate = Input.StaffRate ?? 0,
                UnitRate = Input.UnitRate ?? 0,
                CreaterId = _session.WebUserId,
                CreatedAt = DateTime.Now,
                Retired = false
            };
            _db.ConsultantServiceRates.Add(entity);
            await _db.SaveChangesAsync();
            Success = "Consultant fee saved.";
            Input = new InputModel();
            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            var entity = await _db.ConsultantServiceRates.FindAsync(id);
            if (entity != null)
            {
                entity.Retired = true;
                entity.RetireComments = "Retired via Consultant Fee Setup";
                await _db.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        private async Task LoadAsync()
        {
            ConsultantList = await _db.Consultants.Where(x => !x.Retired).OrderBy(x => x.Person.Name)
                .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Person.Name }).ToListAsync();
            ItemList = await _db.Items.Where(x => !x.Retired && x.Billable).OrderBy(x => x.Name)
                .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name + " (" + x.Code + ")" }).ToListAsync();
            Items = await _db.ConsultantServiceRates
                .Include(x => x.Consultant).Include(x => x.Item)
                .Where(x => !x.Retired)
                .OrderBy(x => x.Consultant.Person.Name).ThenBy(x => x.Item.Name)
                .ToListAsync();
        }
    }
}