using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Cashier
{
    public class CounterSetupModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;
        public CounterSetupModel(ApplicationDbContext db, IAppSession session) { _db = db; _session = session; }

        public List<CashCounter> Items { get; set; } = new();
        [BindProperty] public InputModel Input { get; set; } = new();
        public class InputModel
        {
            public string Id { get; set; } = "";
            public string Code { get; set; } = "";
            public string Desc { get; set; } = "";
            public string Status { get; set; } = "A";
        }
        public string ErrorMessage { get; set; } = "";
        public string Success { get; set; } = "";

        public async Task OnGetAsync(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                var c = await _db.CashCounters.FindAsync(id);
                if (c != null)
                {
                    Input = new InputModel { Id = c.CCode, Code = c.CCode, Desc = c.Desc, Status = c.Status };
                }
            }
            Items = await _db.CashCounters.OrderBy(c => c.CCode).ToListAsync();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            ErrorMessage = ""; Success = "";
            if (string.IsNullOrWhiteSpace(Input.Desc)) { ErrorMessage = "Description is required."; await ReloadAsync(); return Page(); }
            var code = (Input.Code ?? "").Trim();
            if (string.IsNullOrEmpty(code)) code = "C" + (_db.CashCounters.Count() + 1).ToString("D2");

            if (!string.IsNullOrEmpty(Input.Id))
            {
                var c = await _db.CashCounters.FindAsync(Input.Id);
                if (c != null)
                {
                    c.Desc = Input.Desc.Trim();
                    c.Status = Input.Status == "A" ? "A" : "I";
                    c.UserId = _session.LoggedUser?.UserName ?? "";
                    c.UDate = DateTime.Today;
                    c.UTime = DateTime.Now.ToString("HH:mm");
                    Success = "Counter updated.";
                }
            }
            else
            {
                if (await _db.CashCounters.AnyAsync(c => c.CCode == code))
                {
                    ErrorMessage = "Code already exists."; await ReloadAsync(); return Page();
                }
                var c = new CashCounter
                {
                    CCode = code,
                    Desc = Input.Desc.Trim(),
                    NextNo = _db.CashCounters.Any() ? _db.CashCounters.Max(x => x.NextNo) + 1 : 1,
                    Status = Input.Status == "A" ? "A" : "I",
                    UserId = _session.LoggedUser?.UserName ?? "",
                    UDate = DateTime.Today,
                    UTime = DateTime.Now.ToString("HH:mm")
                };
                _db.CashCounters.Add(c);
                Success = "Counter added.";
            }
            await _db.SaveChangesAsync();
            Input = new InputModel();
            await ReloadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            var c = await _db.CashCounters.FindAsync(id);
            if (c != null) { _db.CashCounters.Remove(c); await _db.SaveChangesAsync(); Success = "Counter deleted."; }
            await ReloadAsync();
            return RedirectToPage();
        }

        private async Task ReloadAsync() { Items = await _db.CashCounters.OrderBy(c => c.CCode).ToListAsync(); }
    }
}