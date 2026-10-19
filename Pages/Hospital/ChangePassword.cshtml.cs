using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Hospital
{
    public class ChangePasswordModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;
        private readonly IPasswordService _passwordService;

        public ChangePasswordModel(ApplicationDbContext db, IAppSession appSession, IPasswordService passwordService)
        {
            _db = db;
            _appSession = appSession;
            _passwordService = passwordService;
        }

        [BindProperty] public string CurrentPassword { get; set; }
        [BindProperty] public string NewPassword { get; set; }
        [BindProperty] public string ConfirmPassword { get; set; }

        public string Message { get; set; }
        public string ErrorMessage { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword != ConfirmPassword)
            {
                ErrorMessage = "New password and confirmation do not match.";
                return Page();
            }
            var userId = _appSession.WebUserId;
            if (userId == null)
            {
                ErrorMessage = "Not logged in.";
                return Page();
            }
            var user = await _db.WebUsers.FirstOrDefaultAsync(u => u.Id == userId.Value);
            if (user == null)
            {
                ErrorMessage = "User not found.";
                return Page();
            }
            if (!_passwordService.VerifyPassword(CurrentPassword ?? "", user.Password))
            {
                ErrorMessage = "Current password is incorrect.";
                return Page();
            }
            user.Password = _passwordService.HashPassword(NewPassword);
            await _db.SaveChangesAsync();
            Message = "Password changed successfully.";
            return Page();
        }
    }
}