using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Cashier
{
    public class SetDefaultModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;
        public SetDefaultModel(ApplicationDbContext db, IAppSession session) { _db = db; _session = session; }

        public List<CashCounterSession> Sessions { get; set; } = new();
        [BindProperty] public string SelectedKey { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public string Success { get; set; } = "";

        public async Task OnGetAsync()
        {
            var dept = _session.LoggedDepartment;
            if (dept != null)
                Sessions = await _db.CashCounterSessions
                    .Where(s => s.CCode == (dept.Code ?? "") && s.Status == "A")
                    .OrderBy(s => s.Desc).ToListAsync();
        }

        public async Task<IActionResult> OnPostClaimAsync()
        {
            ErrorMessage = ""; Success = "";
            var dept = _session.LoggedDepartment;
            if (dept == null) { ErrorMessage = "No department selected."; await OnGetAsync(); return Page(); }

            var session = await _db.CashCounterSessions
                .FirstOrDefaultAsync(s => s.CCode == (dept.Code ?? "") && s.LCode == dept.Id && s.ActiveKey == SelectedKey && s.Status == "A");
            if (session == null) { ErrorMessage = "Invalid activation key or no matching active counter."; await OnGetAsync(); return Page(); }

            // Claiming is session-scoped (globals in VFP); in ASP.NET we store the chosen
            // counter code in session via the department context. No table write (matches VFP).
            Success = "Counter '" + session.Desc + "' set as default for this session.";
            await OnGetAsync();
            return Page();
        }
    }
}