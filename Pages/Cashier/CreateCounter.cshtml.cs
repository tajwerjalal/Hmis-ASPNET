using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Cashier
{
    public class CreateCounterModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;
        public CreateCounterModel(ApplicationDbContext db, IAppSession session) { _db = db; _session = session; }

        public List<CashCounterSession> Sessions { get; set; } = new();
        public List<CashCounter> Counters { get; set; } = new();

        [BindProperty] public InputModel Input { get; set; } = new();
        public class InputModel
        {
            public string CCode { get; set; } = "";
            public string Desc { get; set; } = "";
            public string ActiveKey { get; set; } = "";
        }

        public string ErrorMessage { get; set; } = "";
        public string Success { get; set; } = "";

        public async Task OnGetAsync()
        {
            var dept = _session.LoggedDepartment;
            Counters = await _db.CashCounters.Where(c => c.Status == "A").OrderBy(c => c.CCode).ToListAsync();
            if (dept != null)
                Sessions = await _db.CashCounterSessions
                    .Where(s => s.CCode == (dept.Code ?? "")).OrderByDescending(s => s.ActiveDt).Take(50).ToListAsync();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            ErrorMessage = ""; Success = "";
            var dept = _session.LoggedDepartment;
            if (dept == null) { ErrorMessage = "No department selected."; await ReloadAsync(); return Page(); }
            if (string.IsNullOrWhiteSpace(Input.Desc)) { ErrorMessage = "Description required."; await ReloadAsync(); return Page(); }
            if (string.IsNullOrWhiteSpace(Input.ActiveKey)) { ErrorMessage = "Activation key required."; await ReloadAsync(); return Page(); }

            var session = new CashCounterSession
            {
                CCode = string.IsNullOrWhiteSpace(Input.CCode) ? (Counters.FirstOrDefault()?.CCode ?? "C1") : Input.CCode,
                LCode = dept.Id,
                Desc = Input.Desc.Trim(),
                ActiveDt = DateTime.Today,
                ActiveTm = DateTime.Now.ToString("HH:mm"),
                ActiveBy = _session.LoggedUser?.UserName ?? "",
                ActiveKey = Input.ActiveKey,
                Status = "A",
                NetCash = 0
            };
            _db.CashCounterSessions.Add(session);
            await _db.SaveChangesAsync();
            Success = "Counter session created.";
            Input = new InputModel();
            await ReloadAsync();
            return Page();
        }

        private async Task ReloadAsync()
        {
            var dept = _session.LoggedDepartment;
            Counters = await _db.CashCounters.Where(c => c.Status == "A").OrderBy(c => c.CCode).ToListAsync();
            if (dept != null)
                Sessions = await _db.CashCounterSessions
                    .Where(s => s.CCode == (dept.Code ?? "")).OrderByDescending(s => s.ActiveDt).Take(50).ToListAsync();
        }
    }
}