using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using HmisWeb.Data;
using HmisWeb.Services;

namespace HmisWeb.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;

        public LogoutModel(ApplicationDbContext db, IAppSession appSession)
        {
            _db = db;
            _appSession = appSession;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _appSession.WebUserId;
            var login = _db.Logins
                .Where(l => l.WebUserId == userId && !l.LoggedOut)
                .OrderByDescending(l => l.LoginTime)
                .FirstOrDefault();
            if (login != null)
            {
                login.LogoutTime = DateTime.Now;
                login.LoggedOut = true;
                await _db.SaveChangesAsync();
            }
            _appSession.Clear();
            await HttpContext.SignOutAsync("HmisAuth");
            return RedirectToPage("/Account/Login");
        }
    }
}