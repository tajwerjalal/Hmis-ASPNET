using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Cashier
{
    /// <summary>
    /// Set Default Cash Counter: pick one of the active counters for the
    /// department and confirm its activation key to make it the current user's
    /// open counter (legacy VFP "set default counter" screen).
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 23-Oct-2026
    /// Time      : 12:35
    /// Change Log: 23-Oct-2026 - claim now assigns the counter to the user and
    ///             stores it in session for the open-counter guard.
    ///             23-Oct-2026 - active counter is now chosen from a list and
    ///             the activation key must match the selected counter.
    /// </summary>
    public class SetDefaultModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;
        public SetDefaultModel(ApplicationDbContext db, IAppSession session) { _db = db; _session = session; }

        public List<CashCounterSession> Sessions { get; set; } = new();
        public Department CurrentDepartment { get; set; }

        /// <summary>Selected counter as "CCode|LCode".</summary>
        [BindProperty] public string SelectedCounter { get; set; } = "";
        [BindProperty] public string SelectedKey { get; set; } = "";

        public string ErrorMessage { get; set; } = "";
        public string Success { get; set; } = "";

        public async Task<IActionResult> OnGetAsync()
        {
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");

            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostClaimAsync()
        {
            ErrorMessage = ""; Success = "";
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");

            await LoadAsync();
            var dept = CurrentDepartment;
            if (dept == null) { ErrorMessage = "No department selected."; return Page(); }

            // Resolve the counter the user picked from the list.
            CashCounterSession counter = null;
            var parts = (SelectedCounter ?? "").Split('|');
            if (parts.Length == 2 && int.TryParse(parts[1], out var lineCode))
            {
                var counterCode = parts[0];
                counter = await _db.CashCounterSessions
                    .FirstOrDefaultAsync(s => s.CCode == counterCode && s.LCode == lineCode && s.Status == "A");
            }

            // Backwards compatible fallback: match the activation key inside the
            // department when no counter row was explicitly chosen.
            if (counter == null)
            {
                counter = await _db.CashCounterSessions
                    .FirstOrDefaultAsync(s => s.LCode == (int)dept.Id && s.Status == "A" && s.ActiveKey == SelectedKey);
            }

            if (counter == null)
            {
                ErrorMessage = "Select an active counter.";
                return Page();
            }

            if (!string.Equals((counter.ActiveKey ?? "").Trim(), (SelectedKey ?? "").Trim(), StringComparison.Ordinal))
            {
                ErrorMessage = "Invalid activation key for the selected counter.";
                return Page();
            }

            // Claiming the counter makes it the current user's open counter.
            counter.ActiveBy = _session.LoggedUser?.UserName ?? counter.ActiveBy;
            counter.ActiveDt = DateTime.Today;
            counter.ActiveTm = DateTime.Now.ToString("HH:mm");
            counter.Status = "A";
            await _db.SaveChangesAsync();

            HttpContext.Session.SetString(CashierPageModel.CounterSessionKey, counter.CCode + "|" + counter.LCode);
            Success = "Counter '" + counter.Desc + "' (" + counter.CCode + ") is now open for this user.";

            await LoadAsync();
            return Page();
        }

        /// <summary>
        /// Loads the department (falling back to the user's own department when
        /// the login session lost it) and the active counters for it.
        /// </summary>
        private async Task LoadAsync()
        {
            var dept = _session.LoggedDepartment;
            if (dept == null)
            {
                var fallbackId = _session.LoggedUser?.DepartmentId
                    ?? await _db.Departments.Where(d => !d.Retired).OrderBy(d => d.Id)
                            .Select(d => (long?)d.Id).FirstOrDefaultAsync();
                if (fallbackId != null)
                {
                    _session.SetDepartment(fallbackId.Value);
                    dept = _session.LoggedDepartment;
                }
            }

            CurrentDepartment = dept;
            if (dept != null)
                Sessions = await _db.CashCounterSessions
                    .Where(s => s.LCode == (int)dept.Id && s.Status == "A")
                    .OrderBy(s => s.Desc).ToListAsync();
        }
    }
}
