using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Hr
{
    public class DesignationsModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public DesignationsModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        public List<Designation> Items { get; set; } = new();
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
                ErrorMessage = "Designation name is required.";
                await LoadAsync();
                return Page();
            }

            var entity = new Designation
            {
                Name = Input.Name?.Trim() ?? "",
                CreaterId = _session.WebUserId,
                CreatedAt = DateTime.Now,
                Retired = false
            };

            _db.Designations.Add(entity);
            await _db.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            var entity = await _db.Designations.FindAsync(id);
            if (entity != null)
            {
                entity.Retired = true;
                await _db.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        private async Task LoadAsync()
        {
            Items = await _db.Designations
                .Where(x => !x.Retired)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }
    }
}