using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
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

        /// <summary>
        /// Keep the user signed in on this device. When set, the auth cookie is
        /// written as a persistent cookie and the in-memory session is rebuilt
        /// from it after an app restart.
        /// </summary>
        [BindProperty]
        public bool RememberMe { get; set; }

        /// <summary>
        /// Local URL to return to after a successful sign-in. Populated by the
        /// authentication middleware when an anonymous request is redirected.
        /// </summary>
        [BindProperty]
        public string ReturnUrl { get; set; }

        public long? DepartmentId { get; set; }
        public List<Department> Departments { get; set; } = new();
        public string ErrorMessage { get; set; }
        public bool NeedSetup { get; set; }

        public IActionResult OnGet(string returnUrl = null, long? departmentId = null)
        {
            // Already signed in (session or a valid "Remember me" cookie).
            if (_appSession.IsLoggedIn)
                return RedirectToPage("/Index");

            ReturnUrl = returnUrl;
            DepartmentId = departmentId;

            if (string.IsNullOrWhiteSpace(UserName))
                UserName = Request.Cookies["HmisLastUser"];

            var hasUsers = _db.WebUsers.Any(u => !u.Retired);
            var hasInstitutions = _db.Institutions.Any(i => !i.Retired);
            if (!hasUsers || !hasInstitutions)
                NeedSetup = true;

            LoadDepartments();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long? departmentId)
        {
            DepartmentId = departmentId;

            if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "User name and password are required.";
                LoadDepartments();
                return Page();
            }

            var user = _db.WebUsers
                .Include(u => u.WebUserPerson)
                .Include(u => u.WebUserRole)
                .FirstOrDefault(u => !u.Retired && u.UserName.ToLower() == UserName.Trim().ToLower());

            if (user == null || !_passwordService.VerifyPassword(Password, user.Password))
            {
                ErrorMessage = "Invalid user name or password.";
                LoadDepartments();
                return Page();
            }

            if (!System.IO.Directory.Exists("bin"))
            {
                // log
            }

            var resolvedDepartmentId = departmentId ?? user.DepartmentId;
            _appSession.SetUser(user);
            _appSession.SetDepartment(resolvedDepartmentId);

            // A real auth cookie lets the user stay signed in after the
            // in-memory session is recycled, and gives the browser password
            // manager a proper sign-in to remember.
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
                new Claim("DepartmentId", resolvedDepartmentId?.ToString() ?? "")
            };
            var identity = new ClaimsIdentity(claims, "HmisAuth");
            await HttpContext.SignInAsync(
                "HmisAuth",
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = RememberMe,
                    ExpiresUtc = RememberMe
                        ? DateTimeOffset.UtcNow.AddDays(30)
                        : DateTimeOffset.UtcNow.AddMinutes(90),
                    AllowRefresh = true
                });

            Response.Cookies.Append("HmisLastUser", user.UserName ?? "", new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddDays(90),
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps
            });

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

            if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                return LocalRedirect(ReturnUrl);

            return RedirectToPage("/Index");
        }

        private void LoadDepartments()
        {
            Departments = _db.Departments
                .Where(d => !d.Retired)
                .OrderBy(d => d.Name)
                .ToList();
        }
    }
}
