using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Lab
{
    public class NewItemModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public NewItemModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<SelectListItem> CategoryList { get; set; } = new();

        public class InputModel
        {
            public string Name { get; set; } = "";
            public string Code { get; set; } = "";
            public double? Price { get; set; }
            public long? CategoryId { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadCategories();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadCategories();

            if (string.IsNullOrWhiteSpace(Input.Name))
                return Page();

            var item = new Item
            {
                Name = Input.Name.Trim(),
                Code = Input.Code?.Trim() ?? "",
                Total = Input.Price,
                CategoryId = Input.CategoryId,
                ItemType = ItemType.Investigation,
                Billable = true,
                CreatedAt = DateTime.Now,
                CreaterId = _session.WebUserId
            };

            _db.Items.Add(item);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Lab/Investigation");
        }

        private async Task LoadCategories()
        {
            CategoryList = await _db.Categories
                .Where(c => !c.Retired)
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToListAsync();
        }
    }
}
