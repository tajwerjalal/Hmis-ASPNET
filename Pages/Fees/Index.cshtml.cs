using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Fees
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public IndexModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        public List<Fee> Items { get; set; } = new();
        public List<SelectListItem> ItemIdList { get; set; } = new();
        public string ErrorMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public string Name { get; set; }
            public FeeType? FeeType { get; set; }
            public double? FeeValue { get; set; }
            public long? ItemId { get; set; }
            public double? VatPercentage { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.Name))
            {
                ErrorMessage = "Fee name is required.";
                await LoadAsync();
                return Page();
            }

            var entity = new Fee
            {
                Name = Input.Name?.Trim() ?? "",
                FeeType = Input.FeeType,
                FeeValue = Input.FeeValue ?? 0,
                ItemId = Input.ItemId,
                VatPercentage = Input.VatPercentage ?? 0,
                InstitutionId = _session.LoggedSite?.Id,
                CreaterId = _session.WebUserId,
                CreatedAt = DateTime.Now,
                Retired = false
            };

            _db.Fees.Add(entity);
            await _db.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            var entity = await _db.Fees.FindAsync(id);
            if (entity != null)
            {
                entity.Retired = true;
                entity.RetiredAt = DateTime.Now;
                await _db.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        private async Task LoadAsync()
        {
            ItemIdList = await _db.Items.Where(x => !x.Retired).OrderBy(x => x.Name).Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name }).ToListAsync();
            Items = await _db.Fees
                .Include(x => x.Item)
                .Include(x => x.Institution)
                .Where(x => !x.Retired)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }
    }
}