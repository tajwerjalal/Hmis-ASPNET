using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Account
{
    public class SetupModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IPasswordService _passwordService;

        public SetupModel(ApplicationDbContext db, IPasswordService passwordService)
        {
            _db = db;
            _passwordService = passwordService;
        }

        [BindProperty]
        public string InstitutionName { get; set; }
        [BindProperty]
        public string DepartmentName { get; set; }
        [BindProperty]
        public string UserName { get; set; }
        [BindProperty]
        public string Password { get; set; }
        [BindProperty]
        public string ConfirmPassword { get; set; }
        public string ErrorMessage { get; set; }
        public bool AlreadySetup { get; set; }

        public void OnGet()
        {
            if (_db.Institutions.Any(i => !i.Retired) && _db.WebUsers.Any(u => !u.Retired))
                AlreadySetup = true;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(InstitutionName) || string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "All fields are required.";
                return Page();
            }

            if (_db.Institutions.Any(i => !i.Retired) && _db.WebUsers.Any(u => !u.Retired))
            {
                AlreadySetup = true;
                return Page();
            }

            var institution = new Institution
            {
                Name = InstitutionName,
                CreatedAt = DateTime.Now,
                InstitutionType = HmisWeb.Data.Enums.InstitutionType.Hospital
            };
            _db.Institutions.Add(institution);
            await _db.SaveChangesAsync();

            var department = new Department
            {
                Name = string.IsNullOrWhiteSpace(DepartmentName) ? InstitutionName : DepartmentName,
                InstitutionId = institution.Id,
                CreatedAt = DateTime.Now
            };
            _db.Departments.Add(department);
            await _db.SaveChangesAsync();

            var person = new Person
            {
                Name = "System Administrator",
                NameWithInitials = "System Administrator",
                CreatedAt = DateTime.Now
            };
            _db.Persons.Add(person);
            await _db.SaveChangesAsync();

            var role = new WebUserRole
            {
                Name = "Administrator",
                Privileges = "ALL",
                CreatedAt = DateTime.Now
            };
            _db.WebUserRoles.Add(role);
            await _db.SaveChangesAsync();

            var user = new WebUser
            {
                UserName = UserName,
                Password = _passwordService.HashPassword(Password),
                WebUserPersonId = person.Id,
                InstitutionId = institution.Id,
                DepartmentId = department.Id,
                WebUserRoleUserId = role.Id,
                Activated = true,
                CreatedAt = DateTime.Now
            };
            _db.WebUsers.Add(user);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Account/Login");
        }
    }
}
