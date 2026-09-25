using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Admin
{
    public class AddUserModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;
        private readonly IPasswordService _passwordService;

        public AddUserModel(ApplicationDbContext db, IAppSession session, IPasswordService passwordService)
        {
            _db = db;
            _session = session;
            _passwordService = passwordService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<SelectListItem> InstitutionList { get; set; } = new();
        public List<SelectListItem> DepartmentList { get; set; } = new();
        public List<SelectListItem> RoleList { get; set; } = new();

        public class InputModel
        {
            public string UserName { get; set; } = "";
            public string Password { get; set; } = "";
            public string RealUserName { get; set; } = "";
            public long? InstitutionId { get; set; }
            public long? DepartmentId { get; set; }
            public long? WebUserRoleUserId { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadDropdowns();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadDropdowns();

            if (string.IsNullOrWhiteSpace(Input.UserName) || string.IsNullOrWhiteSpace(Input.Password))
                return Page();

            var person = new Person
            {
                Name = Input.RealUserName?.Trim() ?? Input.UserName.Trim(),
                NameWithInitials = Input.RealUserName?.Trim() ?? Input.UserName.Trim(),
                CreatedAt = DateTime.Now,
                CreaterId = _session.WebUserId
            };
            _db.Persons.Add(person);
            await _db.SaveChangesAsync();

            var webUser = new WebUser
            {
                UserName = Input.UserName.Trim(),
                Password = _passwordService.HashPassword(Input.Password),
                RealUserName = Input.RealUserName?.Trim() ?? "",
                WebUserPersonId = person.Id,
                InstitutionId = Input.InstitutionId,
                DepartmentId = Input.DepartmentId,
                WebUserRoleUserId = Input.WebUserRoleUserId,
                Activated = true,
                ActivatedAt = DateTime.Now,
                CreatedAt = DateTime.Now,
                CreaterId = _session.WebUserId
            };
            _db.WebUsers.Add(webUser);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Admin/Users");
        }

        private async Task LoadDropdowns()
        {
            InstitutionList = await _db.Institutions
                .Where(i => !i.Retired)
                .OrderBy(i => i.Name)
                .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = i.Name })
                .ToListAsync();

            DepartmentList = await _db.Departments
                .Where(d => !d.Retired)
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .ToListAsync();

            RoleList = await _db.WebUserRoles
                .Where(r => !r.Retired)
                .OrderBy(r => r.Name)
                .Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.Name })
                .ToListAsync();
        }
    }
}
