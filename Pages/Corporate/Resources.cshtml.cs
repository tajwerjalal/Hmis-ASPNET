using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Corporate
{
    public class ResourcesModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public ResourcesModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        public List<Resource> Resources { get; set; } = new();
        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }

        [BindProperty]
        public long EditId { get; set; }

        [BindProperty]
        public string Name { get; set; }

        [BindProperty]
        public string Code { get; set; }

        [BindProperty]
        public bool IsActive { get; set; } = true;

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Resource name is required.";
                await LoadAsync();
                return Page();
            }

            var nextCode = await GenerateCodeAsync();

            _db.Resources.Add(new Resource
            {
                Name = Name.Trim(),
                Code = string.IsNullOrWhiteSpace(Code) ? nextCode : Code.Trim(),
                IsActive = IsActive,
                CreatedAt = DateTime.Now,
                CreaterId = _session.WebUserId
            });
            await _db.SaveChangesAsync();

            SuccessMessage = $"Resource \"{Name.Trim()}\" created.";
            Name = Code = "";
            IsActive = true;
            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            var resource = await _db.Resources.FirstOrDefaultAsync(r => r.Id == EditId && !r.Retired);
            if (resource == null)
            {
                ErrorMessage = "Resource not found.";
                await LoadAsync();
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Resource name is required.";
                await LoadAsync();
                return Page();
            }

            resource.Name = Name.Trim();
            resource.Code = Code?.Trim();
            resource.IsActive = IsActive;
            resource.EditedAt = DateTime.Now;
            resource.EditerId = _session.WebUserId;

            await _db.SaveChangesAsync();

            SuccessMessage = $"Resource \"{resource.Name}\" updated.";
            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostRetireAsync(long id)
        {
            var resource = await _db.Resources.FirstOrDefaultAsync(r => r.Id == id && !r.Retired);
            if (resource != null)
            {
                resource.Retired = true;
                resource.RetiredAt = DateTime.Now;
                resource.RetirerId = _session.WebUserId;
                resource.RetireComments = "Retired from Resources page.";
                await _db.SaveChangesAsync();
            }

            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostRestoreAsync(long id)
        {
            var resource = await _db.Resources.FirstOrDefaultAsync(r => r.Id == id && r.Retired);
            if (resource != null)
            {
                resource.Retired = false;
                resource.RetiredAt = null;
                resource.RetirerId = null;
                resource.RetireComments = null;
                await _db.SaveChangesAsync();
            }

            await LoadAsync();
            return Page();
        }

        private async Task<string> GenerateCodeAsync()
        {
            var last = await _db.Resources
                .Where(r => r.Code != null && r.Code.StartsWith("R"))
                .OrderByDescending(r => r.Code)
                .Select(r => r.Code)
                .FirstOrDefaultAsync();

            if (last != null && int.TryParse(last.Substring(1), out var lastNum))
                return $"R{(lastNum + 1):D2}";

            return "R01";
        }

        private async Task LoadAsync()
        {
            Resources = await _db.Resources
                .OrderBy(r => r.Name)
                .ToListAsync();
        }
    }
}