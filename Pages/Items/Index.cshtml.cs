using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Items
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

        public List<Item> Items { get; set; } = new();
        public List<SelectListItem> CategoryIdList { get; set; } = new();
        public List<SelectListItem> MeasurementUnitIdList { get; set; } = new();
        public string ErrorMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public string Name { get; set; }
            public string Code { get; set; }
            public ItemType? ItemType { get; set; }
            public long? CategoryId { get; set; }
            public long? MeasurementUnitId { get; set; }
            public double? VatPercentage { get; set; }
            public bool Billable { get; set; }
            public bool Inactive { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.Name))
            {
                ErrorMessage = "Item name is required.";
                await LoadAsync();
                return Page();
            }

            var entity = new Item
            {
                Name = Input.Name?.Trim() ?? "",
                Code = Input.Code?.Trim() ?? "",
                ItemType = Input.ItemType,
                CategoryId = Input.CategoryId,
                MeasurementUnitId = Input.MeasurementUnitId,
                VatPercentage = Input.VatPercentage ?? 0,
                Billable = Input.Billable,
                Inactive = Input.Inactive,
                InstitutionId = _session.LoggedSite?.Id,
                CreaterId = _session.WebUserId,
                CreatedAt = DateTime.Now,
                Retired = false
            };

            _db.Items.Add(entity);
            await _db.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            var entity = await _db.Items.FindAsync(id);
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
            CategoryIdList = await _db.Categories.Where(x => !x.Retired).OrderBy(x => x.Name).Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name }).ToListAsync();
            MeasurementUnitIdList = await _db.MeasurementUnits.Where(x => !x.Retired).OrderBy(x => x.Name).Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name }).ToListAsync();
            Items = await _db.Items
                .Include(x => x.Category)
                .Include(x => x.MeasurementUnit)
                .Include(x => x.Institution)
                .Where(x => !x.Retired)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }
    }
}