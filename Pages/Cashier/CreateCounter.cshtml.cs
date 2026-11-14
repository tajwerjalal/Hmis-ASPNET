using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Cashier
{
    /// <summary>
    /// Opens a cash counter for the signed-in user (legacy VFP Cashier login
    /// screen). Creates/uses a CCOUNTER master row, closes any counter the user
    /// still has open in the department, and stores "CCode|LCode" in session so
    /// the CashierPageModel guard resolves the current counter.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 23-Oct-2026
    /// Time      : 12:35
    /// Change Log: 23-Oct-2026 - open-counter workflow aligned with the guard.
    ///             23-Oct-2026 - activation key now requires a matching
    ///             confirmation key before the counter is opened.
    ///             23-Oct-2026 - login gate added and the department / line is
    ///             now chosen on the form and persisted into session, so the
    ///             open-counter guard resolves the same department.
    /// </summary>
    public class CreateCounterModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;
        public CreateCounterModel(ApplicationDbContext db, IAppSession session) { _db = db; _session = session; }

        public List<CashCounterSession> Sessions { get; set; } = new();
        public List<CashCounter> Counters { get; set; } = new();
        public List<Department> Departments { get; set; } = new();

        [BindProperty] public InputModel Input { get; set; } = new();
        public class InputModel
        {
            public long? LCode { get; set; }
            public string CCode { get; set; } = "";
            public string Desc { get; set; } = "";
            public string ActiveKey { get; set; } = "";
            public string ConfirmActiveKey { get; set; } = "";
        }

        public string ErrorMessage { get; set; } = "";
        public string Success { get; set; } = "";

        public async Task<IActionResult> OnGetAsync()
        {
            // The cashier counter lives in the server-side session; a stale
            // session (for example after an app restart) must go back to login
            // instead of failing later with "No department selected.".
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");

            Departments = await _db.Departments.Where(d => !d.Retired).OrderBy(d => d.Name).ToListAsync();
            if (Input.LCode == null)
            {
                var dept = _session.LoggedDepartment;
                Input.LCode = dept?.Id ?? _session.LoggedUser?.DepartmentId ?? Departments.FirstOrDefault()?.Id;
            }
            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            ErrorMessage = ""; Success = "";
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");

            // The department / line is chosen on the form. It falls back to the
            // login department and then to the user's own department.
            var departmentId = Input.LCode ?? _session.LoggedDepartment?.Id ?? _session.LoggedUser?.DepartmentId;
            var department = departmentId == null
                ? null
                : await _db.Departments.FirstOrDefaultAsync(d => d.Id == departmentId.Value && !d.Retired);
            if (department == null)
            {
                ErrorMessage = "Select the department / line for this counter.";
                await LoadAsync();
                return Page();
            }

            // Keep the session department in step with the counter, otherwise the
            // open-counter guard (which keys on the login department) would not
            // see the counter we just opened.
            _session.SetDepartment(department.Id);

            if (string.IsNullOrWhiteSpace(Input.Desc)) { ErrorMessage = "Description required."; await LoadAsync(); return Page(); }
            if (string.IsNullOrWhiteSpace(Input.ActiveKey)) { ErrorMessage = "Activation key required."; await LoadAsync(); return Page(); }
            if (!string.Equals((Input.ActiveKey ?? "").Trim(), (Input.ConfirmActiveKey ?? "").Trim(), StringComparison.Ordinal))
            {
                ErrorMessage = "Activation key and confirmation key do not match.";
                await LoadAsync();
                return Page();
            }
            var activeKey = Input.ActiveKey.Trim();

            var userName = _session.LoggedUser?.UserName ?? "";
            var now = DateTime.Now;
            var deptId = (int)department.Id;
            var counterCode = string.IsNullOrWhiteSpace(Input.CCode) ? (Counters.FirstOrDefault()?.CCode ?? "C1") : Input.CCode.Trim();

            // The counter master row must exist before an open session can point at it.
            if (!await _db.CashCounters.AnyAsync(c => c.CCode == counterCode))
            {
                _db.CashCounters.Add(new CashCounter
                {
                    CCode = counterCode,
                    Desc = "Default Cash Counter",
                    NextNo = 1,
                    Status = "A",
                    UserId = userName,
                    UDate = DateTime.Today,
                    UTime = now.ToString("HH:mm")
                });
            }

            // One open counter per user / department: close any *other* counter
            // this user still has open (the target counter is re-activated below).
            var stale = await _db.CashCounterSessions
                .Where(s => s.Status == "A" && s.LCode == deptId && s.ActiveBy == userName && s.CCode != counterCode)
                .ToListAsync();
            foreach (var old in stale)
            {
                old.Status = "C";
                old.CloseDt = DateTime.Today;
                old.CloseTm = now.ToString("HH:mm");
                old.CloseBy = userName;
            }

            // CASHCNT is keyed by (CCODE, LCODE), so opening a counter that was
            // closed earlier re-activates its row instead of inserting a duplicate.
            var session = await _db.CashCounterSessions
                .FirstOrDefaultAsync(s => s.CCode == counterCode && s.LCode == deptId);
            if (session == null)
            {
                session = new CashCounterSession
                {
                    CCode = counterCode,
                    LCode = deptId,
                    NetCash = 0
                };
                _db.CashCounterSessions.Add(session);
            }

            session.Desc = Input.Desc.Trim();
            session.ActiveDt = DateTime.Today;
            session.ActiveTm = now.ToString("HH:mm");
            session.ActiveBy = userName;
            session.ActiveKey = activeKey;
            session.Status = "A";
            session.CloseDt = null;
            session.CloseTm = "";
            session.CloseBy = "";
            session.Vno = "";
            await _db.SaveChangesAsync();

            HttpContext.Session.SetString(CashierPageModel.CounterSessionKey, session.CCode + "|" + session.LCode);
            Success = "Counter session created and set as the open counter for this user.";
            Input = new InputModel { LCode = department.Id };
            await LoadAsync();
            return Page();
        }

        /// <summary>
        /// Loads the department / line list, the counter master rows and the
        /// recent counter sessions for the currently chosen department.
        /// </summary>
        private async Task LoadAsync()
        {
            Departments = await _db.Departments.Where(d => !d.Retired).OrderBy(d => d.Name).ToListAsync();
            Counters = await _db.CashCounters.Where(c => c.Status == "A").OrderBy(c => c.CCode).ToListAsync();

            var deptId = Input.LCode ?? _session.LoggedDepartment?.Id;
            if (deptId != null)
                Sessions = await _db.CashCounterSessions
                    .Where(s => s.LCode == (int)deptId.Value).OrderByDescending(s => s.ActiveDt).Take(50).ToListAsync();
        }
    }
}