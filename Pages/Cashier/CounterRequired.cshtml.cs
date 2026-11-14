using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HmisWeb.Pages.Cashier
{
    /// <summary>
    /// Landing page shown when a cashier data-entry screen is opened without an
    /// open cash counter for the signed-in user.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 23-Oct-2026
    /// Time      : 12:35
    /// Change Log: 23-Oct-2026 - created with the open-counter guard.
    /// </summary>
    public class CounterRequiredModel : PageModel
    {
        private readonly IAppSession _session;

        public CounterRequiredModel(IAppSession session)
        {
            _session = session;
        }

        public IActionResult OnGet()
        {
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");

            return Page();
        }
    }
}
