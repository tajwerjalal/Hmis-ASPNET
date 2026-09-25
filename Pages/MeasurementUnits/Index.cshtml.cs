using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.MeasurementUnits
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

        public List<MeasurementUnit> Items { get; set; } = new();
        public string ErrorMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public string Name { get; set; }
            public string Sname { get; set; }
            public string Code { get; set; }
            public string Description { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.Name))
            {
                ErrorMessage = "Measurement unit name is required.";
                await LoadAsync();
                return Page();
            }

            var entity = new MeasurementUnit
            {
                Name = Input.Name?.Trim() ?? "",
                Sname = Input.Sname?.Trim() ?? "",
                Code = Input.Code?.Trim() ?? "",
                Description = Input.Description?.Trim() ?? "",
                CreaterId = _session.WebUserId,
                CreatedAt = DateTime.Now,
                Retired = false
            };

            _db.MeasurementUnits.Add(entity);
            await _db.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            var entity = await _db.MeasurementUnits.FindAsync(id);
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
            Items = await _db.MeasurementUnits
                .Where(x => !x.Retired)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }
    }
}