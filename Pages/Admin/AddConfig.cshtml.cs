using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Admin
{
    public class AddConfigModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public AddConfigModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<SelectListItem> InstitutionList { get; set; } = new();

        public class InputModel
        {
            public string Name { get; set; } = "";
            public string OptionValue { get; set; } = "";
            public OptionScope? OptionScope { get; set; }
            public long? InstitutionId { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadInstitutions();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadInstitutions();

            if (string.IsNullOrWhiteSpace(Input.Name))
                return Page();

            var config = new ConfigOption
            {
                Name = Input.Name.Trim(),
                OptionValue = Input.OptionValue?.Trim() ?? "",
                OptionScope = Input.OptionScope,
                InstitutionId = Input.InstitutionId,
                CreatedAt = DateTime.Now,
                CreaterId = _session.WebUserId
            };

            _db.ConfigOptions.Add(config);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Admin/Config");
        }

        private async Task LoadInstitutions()
        {
            InstitutionList = await _db.Institutions
                .Where(i => !i.Retired)
                .OrderBy(i => i.Name)
                .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = i.Name })
                .ToListAsync();
        }
    }
}
