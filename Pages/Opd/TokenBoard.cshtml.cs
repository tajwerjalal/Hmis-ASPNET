using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Opd
{
    public class TokenBoardModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;

        public TokenBoardModel(ApplicationDbContext db, IAppSession appSession)
        {
            _db = db;
            _appSession = appSession;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? SessionDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? SessionId { get; set; }

        public List<SelectListItem> SessionOptions { get; set; } = new();
        public OpdSession SelectedSession { get; set; }
        public List<TokenRow> Tokens { get; set; } = new();
        public string Message { get; set; }

        public class TokenRow
        {
            public long Id { get; set; }
            public int Number { get; set; }
            public string TokenNumber { get; set; }
            public string PatientName { get; set; }
            public long? Phn { get; set; }
            public TokenStatus Status { get; set; }
            public DateTime? IssuedAt { get; set; }
            public DateTime? CalledAt { get; set; }
        }

        public async Task OnGetAsync()
        {
            if (SessionDate == null) SessionDate = DateTime.Today;
            if (SessionId == null && SessionOptions.Count == 0)
                await LoadOptionsAsync();
            if (SessionId != null)
                await LoadBoardAsync();
        }

        public async Task<IActionResult> OnPostStartSessionAsync(long sessionId)
        {
            var s = await _db.OpdSessions.FirstOrDefaultAsync(x => x.Id == sessionId && !x.Retired);
            if (s != null && s.Status == OpdSessionStatus.Scheduled)
            {
                s.Status = OpdSessionStatus.Active;
                s.EditedAt = DateTime.Now;
                s.EditerId = _appSession.WebUserId;
                await _db.SaveChangesAsync();
            }
            return RedirectToPage("/Opd/TokenBoard", new { SessionId = sessionId });
        }

        public async Task<IActionResult> OnPostCompleteSessionAsync(long sessionId)
        {
            var s = await _db.OpdSessions.FirstOrDefaultAsync(x => x.Id == sessionId && !x.Retired);
            if (s != null && s.Status == OpdSessionStatus.Active)
            {
                s.Status = OpdSessionStatus.Completed;
                s.EditedAt = DateTime.Now;
                s.EditerId = _appSession.WebUserId;
                await _db.SaveChangesAsync();
            }
            return RedirectToPage("/Opd/TokenBoard", new { SessionId = sessionId });
        }

        public async Task<IActionResult> OnPostCallAsync(long tokenId)
        {
            var t = await _db.Tokens.FirstOrDefaultAsync(x => x.Id == tokenId && !x.Retired);
            if (t != null && t.Status == TokenStatus.Waiting)
            {
                t.Status = TokenStatus.InConsultation;
                t.Called = true;
                t.CalledAt = DateTime.Now;
                t.InProgress = true;
                t.StartedAt = DateTime.Now;
                await _db.SaveChangesAsync();
            }
            var sid = t?.SessionId;
            return RedirectToPage("/Opd/TokenBoard", new { SessionId = sid });
        }

        public async Task<IActionResult> OnPostCompleteAsync(long tokenId)
        {
            var t = await _db.Tokens.FirstOrDefaultAsync(x => x.Id == tokenId && !x.Retired);
            if (t != null && (t.Status == TokenStatus.InConsultation || t.Status == TokenStatus.Called))
            {
                t.Status = TokenStatus.Completed;
                t.InProgress = false;
                t.Completed = true;
                t.CompletedAt = DateTime.Now;
                await _db.SaveChangesAsync();
            }
            var sid = t?.SessionId;
            return RedirectToPage("/Opd/TokenBoard", new { SessionId = sid });
        }

        public async Task<IActionResult> OnPostNoShowAsync(long tokenId)
        {
            var t = await _db.Tokens.FirstOrDefaultAsync(x => x.Id == tokenId && !x.Retired);
            if (t != null && t.Status != TokenStatus.Completed && t.Status != TokenStatus.NoShow)
            {
                t.Status = TokenStatus.NoShow;
                t.InProgress = false;
                t.CompletedAt = DateTime.Now;
                await _db.SaveChangesAsync();
            }
            var sid = t?.SessionId;
            return RedirectToPage("/Opd/TokenBoard", new { SessionId = sid });
        }

        public async Task<IActionResult> OnPostRequeueAsync(long tokenId)
        {
            var t = await _db.Tokens.FirstOrDefaultAsync(x => x.Id == tokenId && !x.Retired);
            if (t != null && (t.Status == TokenStatus.NoShow || t.Status == TokenStatus.Cancelled))
            {
                t.Status = TokenStatus.Waiting;
                t.Called = false;
                t.CalledAt = null;
                t.InProgress = false;
                t.Completed = false;
                t.CompletedAt = null;
                await _db.SaveChangesAsync();
            }
            var sid = t?.SessionId;
            return RedirectToPage("/Opd/TokenBoard", new { SessionId = sid });
        }

        private async Task LoadOptionsAsync()
        {
            var day = SessionDate?.Date ?? DateTime.Today;
            SessionOptions = await _db.OpdSessions
                .Include(s => s.Consultant).ThenInclude(c => c.Person)
                .Where(s => !s.Retired && s.SessionDate == day && s.Status != OpdSessionStatus.Cancelled)
                .OrderBy(s => s.FromTime)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = (s.Consultant != null && s.Consultant.Person != null
                            ? (s.Consultant.Person.NameWithInitials ?? s.Consultant.Person.Name ?? "")
                            : "Consultant")
                           + " - Room " + (s.RoomNo ?? "") + " (" + s.FromTime + ")"
                })
                .ToListAsync();
        }

        private async Task LoadBoardAsync()
        {
            await LoadOptionsAsync();

            SelectedSession = await _db.OpdSessions
                .Include(s => s.Consultant).ThenInclude(c => c.Person)
                .Include(s => s.Speciality)
                .FirstOrDefaultAsync(s => s.Id == SessionId && !s.Retired);

            if (SelectedSession == null)
                return;

            var tokens = await _db.Tokens
                .Include(t => t.Patient).ThenInclude(p => p.Person)
                .Where(t => t.SessionId == SessionId && !t.Retired)
                .ToListAsync();

            Tokens = tokens
                .Select(t => new TokenRow
                {
                    Id = t.Id,
                    Number = int.TryParse(t.TokenNumber, out var n) ? n : 0,
                    TokenNumber = t.TokenNumber ?? "-",
                    PatientName = t.Patient?.Person != null
                        ? (t.Patient.Person.NameWithInitials ?? t.Patient.Person.Name ?? "Patient")
                        : "Patient",
                    Phn = t.Patient?.PatientId,
                    Status = t.Status ?? TokenStatus.Waiting,
                    IssuedAt = t.IssuedAt ?? t.CreatedAt,
                    CalledAt = t.CalledAt
                })
                .OrderBy(x => x.Number)
                .ToList();
        }
    }
}