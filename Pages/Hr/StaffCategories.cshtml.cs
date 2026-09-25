using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Hr
{
    public class StaffCategoriesModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public StaffCategoriesModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        public List<StaffCategory> Items { get; set; } = new();
        public string ErrorMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public string Name { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.Name))
            {
                ErrorMessage = "Staff category name is required.";
                await LoadAsync();
                return Page();
            }

            var entity = new StaffCategory
            {
                Name = Input.Name?.Trim() ?? "",
                CreaterId = _session.WebUserId,
                CreatedAt = DateTime.Now,
                Retired = false
            };

            _db.StaffCategories.Add(entity);
            await _db.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            var entity = await _db.StaffCategories.FindAsync(id);
            if (entity != null)
            {
                entity.Retired = true;
                await _db.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        private async Task LoadAsync()
        {
            Items = await _db.StaffCategories
                .Where(x => !x.Retired)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }
    }
}