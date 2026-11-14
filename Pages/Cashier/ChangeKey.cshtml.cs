using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Cashier
{
    /// <summary>
    /// Changes the activation key of an open cash counter.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 23-Oct-2026
    /// Time      : 12:35
    /// Change Log: 23-Oct-2026 - counter lookup now uses the department line
    ///             code instead of the department code string.
    /// </summary>
    public class ChangeKeyModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;
        public ChangeKeyModel(ApplicationDbContext db, IAppSession session) { _db = db; _session = session; }

        public List<CashCounterSession> Sessions { get; set; } = new();
        [BindProperty] public InputModel Input { get; set; } = new();
        public class InputModel
        {
            public string CCode { get; set; } = "";
            public int? LCode { get; set; }
            public string OldKey { get; set; } = "";
            public string NewKey { get; set; } = "";
        }
        public string ErrorMessage { get; set; } = "";
        public string Success { get; set; } = "";

        public async Task OnGetAsync()
        {
            var dept = _session.LoggedDepartment;
            if (dept != null)
                Sessions = await _db.CashCounterSessions
                    .Where(s => s.LCode == dept.Id && s.Status == "A").OrderBy(s => s.Desc).ToListAsync();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            ErrorMessage = ""; Success = "";
            var dept = _session.LoggedDepartment;
            var query = _db.CashCounterSessions.Where(s => s.Status == "A");
            if (!string.IsNullOrWhiteSpace(Input.CCode))
                query = query.Where(s => s.CCode == Input.CCode.Trim());
            if (Input.LCode.HasValue)
                query = query.Where(s => s.LCode == Input.LCode.Value);
            else if (dept != null)
                query = query.Where(s => s.LCode == dept.Id);

            var session = await query.FirstOrDefaultAsync();
            if (session == null) { ErrorMessage = "Counter session not found."; await OnGetAsync(); return Page(); }
            if (session.ActiveKey != Input.OldKey) { ErrorMessage = "Old key does not match."; await OnGetAsync(); return Page(); }
            if (string.IsNullOrWhiteSpace(Input.NewKey)) { ErrorMessage = "New key required."; await OnGetAsync(); return Page(); }

            session.ActiveKey = Input.NewKey.Trim().ToUpper();
            await _db.SaveChangesAsync();
            Success = "Activation key changed.";
            Input = new InputModel();
            await OnGetAsync();
            return Page();
        }
    }
}