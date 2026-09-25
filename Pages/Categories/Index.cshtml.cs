using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Categories
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

        public List<Category> Items { get; set; } = new();
        public List<SelectListItem> ParentCategoryIdList { get; set; } = new();
        public string ErrorMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public string Name { get; set; }
            public string Code { get; set; }
            public SymanticType? SymanticType { get; set; }
            public long? ParentCategoryId { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.Name))
            {
                ErrorMessage = "Category name is required.";
                await LoadAsync();
                return Page();
            }

            var entity = new Category
            {
                Name = Input.Name?.Trim() ?? "",
                Code = Input.Code?.Trim() ?? "",
                SymanticType = Input.SymanticType,
                ParentCategoryId = Input.ParentCategoryId,
                InstitutionId = _session.LoggedSite?.Id,
                CreaterId = _session.WebUserId,
                CreatedAt = DateTime.Now,
                Retired = false
            };

            _db.Categories.Add(entity);
            await _db.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            var entity = await _db.Categories.FindAsync(id);
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
            ParentCategoryIdList = await _db.Categories.Where(x => !x.Retired).OrderBy(x => x.Name).Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name }).ToListAsync();
            Items = await _db.Categories
                .Include(x => x.Institution)
                .Where(x => !x.Retired)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }
    }
}