using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HmisWeb.Pages.Admin
{
    public class RoleCreateModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public RoleCreateModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string AvailablePrivileges { get; set; } = "";

        public class InputModel
        {
            public string Name { get; set; } = "";
            public string Privileges { get; set; } = "";
        }

        public void OnGet()
        {
            AvailablePrivileges = string.Join(", ", Enum.GetNames(typeof(Data.Enums.Privileges)));
        }

        public IActionResult OnPost()
        {
            if (string.IsNullOrWhiteSpace(Input.Name))
                return Page();

            var role = new WebUserRole
            {
                Name = Input.Name.Trim(),
                Privileges = Input.Privileges?.Trim() ?? "",
                CreatedAt = DateTime.Now,
                CreaterId = _session.WebUserId
            };

            _db.WebUserRoles.Add(role);
            _db.SaveChanges();

            return RedirectToPage("/Admin/Roles");
        }
    }
}
