using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Rooms
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

        public List<Room> Items { get; set; } = new();
        public List<SelectListItem> RoomCategoryIdList { get; set; } = new();
        public List<SelectListItem> DepartmentIdList { get; set; } = new();
        public string ErrorMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public long? RoomCategoryId { get; set; }
            public long? DepartmentId { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.Name))
            {
                ErrorMessage = "Room name is required.";
                await LoadAsync();
                return Page();
            }

            var entity = new Room
            {
                Name = Input.Name?.Trim() ?? "",
                Description = Input.Description?.Trim() ?? "",
                RoomCategoryId = Input.RoomCategoryId,
                DepartmentId = Input.DepartmentId,
                InstitutionId = _session.LoggedSite?.Id,
                CreaterId = _session.WebUserId,
                CreatedAt = DateTime.Now,
                Retired = false
            };

            _db.Rooms.Add(entity);
            await _db.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            var entity = await _db.Rooms.FindAsync(id);
            if (entity != null)
            {
                entity.Retired = true;
                await _db.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        private async Task LoadAsync()
        {
            RoomCategoryIdList = await _db.RoomCategories.Where(x => !x.Retired).OrderBy(x => x.Name).Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name }).ToListAsync();
            DepartmentIdList = await _db.Departments.Where(x => !x.Retired).OrderBy(x => x.Name).Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name }).ToListAsync();
            Items = await _db.Rooms
                .Include(x => x.RoomCategory)
                .Include(x => x.Department)
                .Include(x => x.Institution)
                .Where(x => !x.Retired)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }
    }
}