using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IPasswordService _passwordService;
        private readonly IAppSession _appSession;

        public LoginModel(ApplicationDbContext db, IPasswordService passwordService, IAppSession appSession)
        {
            _db = db;
            _passwordService = passwordService;
            _appSession = appSession;
        }

        [BindProperty]
        public string UserName { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public long? DepartmentId { get; set; }
        public List<Department> Departments { get; set; } = new();
        public string ErrorMessage { get; set; }
        public bool NeedSetup { get; set; }

        public void OnGet(long? departmentId)
        {
            DepartmentId = departmentId;
            var hasUsers = _db.WebUsers.Any(u => !u.Retired);
            var hasInstitutions = _db.Institutions.Any(i => !i.Retired);
            if (!hasUsers || !hasInstitutions)
                NeedSetup = true;

            if (DepartmentId != null)
                Departments = _db.Departments.Where(d => d.InstitutionId == departmentId && !d.Retired).OrderBy(d => d.Name).ToList();
            else
                Departments = _db.Departments.Where(d => !d.Retired).OrderBy(d => d.Name).ToList();
        }

        public async Task<IActionResult> OnPostAsync(long? departmentId)
        {
            if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "User name and password are required.";
                return Page();
            }

            var user = _db.WebUsers
                .Include(u => u.WebUserPerson)
                .Include(u => u.WebUserRole)
                .FirstOrDefault(u => !u.Retired && u.UserName.ToLower() == UserName.Trim().ToLower());

            if (user == null || !_passwordService.VerifyPassword(Password, user.Password))
            {
                ErrorMessage = "Invalid user name or password.";
                return Page();
            }

            if (!System.IO.Directory.Exists("bin"))
            {
                // log
            }

            _appSession.SetUser(user);
            _appSession.SetDepartment(departmentId ?? user.DepartmentId);

            var login = new Logins
            {
                UserName = user.UserName,
                LoginTime = DateTime.Now,
                InstitutionId = user.InstitutionId,
                DepartmentId = departmentId ?? user.DepartmentId,
                WebUserId = user.Id,
                Ipaddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };
            _db.Logins.Add(login);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Index");
        }
    }
}
