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
    public class SessionsModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;

        public SessionsModel(ApplicationDbContext db, IAppSession appSession)
        {
            _db = db;
            _appSession = appSession;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? SessionDate { get; set; }

        [BindProperty]
        public long? ConsultantId { get; set; }

        [BindProperty]
        public long? SpecialityId { get; set; }

        [BindProperty]
        public string FromTime { get; set; }

        [BindProperty]
        public string ToTime { get; set; }

        [BindProperty]
        public string RoomNo { get; set; }

        [BindProperty]
        public double Fee { get; set; }

        [BindProperty]
        public double HospitalFee { get; set; }

        [BindProperty]
        public int TokenStartingNo { get; set; } = 1;

        [BindProperty]
        public int TokenMax { get; set; } = 50;

        [BindProperty]
        public string Note { get; set; }

        public List<ConsultantOption> ConsultantOptions { get; set; } = new();
        public List<SelectListItem> SpecialityList { get; set; } = new();
        public List<SessionRow> Sessions { get; set; } = new();
        public string Message { get; set; }

        public class ConsultantOption
        {
            public long Id { get; set; }
            public string Text { get; set; }
            public long? SpecialityId { get; set; }
            public double FirstVisitFee { get; set; }
            public double SubsequentVisitFee { get; set; }
            public string SessionStartTime { get; set; }
            public string SessionEndTime { get; set; }
            public string DefaultRoomNo { get; set; }
            public double DefaultHospitalFee { get; set; }
            public string WorkDays { get; set; }
        }

        public class SessionRow
        {
            public long Id { get; set; }
            public string ConsultantName { get; set; }
            public string SpecialityName { get; set; }
            public string RoomNo { get; set; }
            public string FromTime { get; set; }
            public string ToTime { get; set; }
            public double Fee { get; set; }
            public double HospitalFee { get; set; }
            public int TokenStartingNo { get; set; }
            public int TokenMax { get; set; }
            public int IssuedTokens { get; set; }
            public OpdSessionStatus Status { get; set; }
        }

        public async Task OnGetAsync()
        {
            if (SessionDate == null) SessionDate = DateTime.Today;
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (SessionDate == null) SessionDate = DateTime.Today;
            await LoadLookupsAsync();
            await LoadSessionsAsync();

            if (ConsultantId == null)
            {
                Message = "Please select a consultant.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(FromTime))
            {
                Message = "Please enter the session start time. End time is optional.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(RoomNo))
                RoomNo = "OPD";

            var session = new OpdSession
            {
                ConsultantId = ConsultantId,
                SpecialityId = SpecialityId,
                SessionDate = SessionDate.Value.Date,
                FromTime = FromTime.Trim(),
                ToTime = ToTime?.Trim(),
                RoomNo = RoomNo.Trim(),
                Fee = Math.Max(0, Fee),
                HospitalFee = Math.Max(0, HospitalFee),
                TokenStartingNo = Math.Max(1, TokenStartingNo),
                TokenMax = Math.Max(1, TokenMax),
                TokenIncrement = 1,
                Status = OpdSessionStatus.Scheduled,
                Note = Note?.Trim(),
                InstitutionId = _appSession.LoggedSite?.Id,
                DepartmentId = _appSession.LoggedDepartment?.Id,
                CreaterId = _appSession.WebUserId,
                CreatedAt = DateTime.Now
            };

            _db.OpdSessions.Add(session);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Opd/Sessions", new { SessionDate = SessionDate.Value.Date.ToString("yyyy-MM-dd") });
        }

        public async Task<IActionResult> OnPostCreateAndStartAsync()
        {
            if (SessionDate == null) SessionDate = DateTime.Today;
            await LoadLookupsAsync();
            await LoadSessionsAsync();

            if (ConsultantId == null)
            {
                Message = "Please select a consultant.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(FromTime))
            {
                Message = "Please enter the session start time. End time is optional.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(RoomNo))
                RoomNo = "OPD";

            var session = new OpdSession
            {
                ConsultantId = ConsultantId,
                SpecialityId = SpecialityId,
                SessionDate = SessionDate.Value.Date,
                FromTime = FromTime.Trim(),
                ToTime = ToTime?.Trim(),
                RoomNo = RoomNo.Trim(),
                Fee = Math.Max(0, Fee),
                HospitalFee = Math.Max(0, HospitalFee),
                TokenStartingNo = Math.Max(1, TokenStartingNo),
                TokenMax = Math.Max(1, TokenMax),
                TokenIncrement = 1,
                Status = OpdSessionStatus.Active,
                Note = Note?.Trim(),
                InstitutionId = _appSession.LoggedSite?.Id,
                DepartmentId = _appSession.LoggedDepartment?.Id,
                CreaterId = _appSession.WebUserId,
                CreatedAt = DateTime.Now
            };

            _db.OpdSessions.Add(session);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Opd/Sessions", new { SessionDate = SessionDate.Value.Date.ToString("yyyy-MM-dd") });
        }

        public async Task<IActionResult> OnPostStartAsync(long id)
        {
            var s = await _db.OpdSessions.FirstOrDefaultAsync(x => x.Id == id && !x.Retired);
            if (s != null)
            {
                s.Status = OpdSessionStatus.Active;
                s.EditedAt = DateTime.Now;
                s.EditerId = _appSession.WebUserId;
                await _db.SaveChangesAsync();
            }
            return RedirectToPage("/Opd/Sessions", new { SessionDate = SessionDate?.Date.ToString("yyyy-MM-dd") });
        }

        public async Task<IActionResult> OnPostCompleteAsync(long id)
        {
            var s = await _db.OpdSessions.FirstOrDefaultAsync(x => x.Id == id && !x.Retired);
            if (s != null)
            {
                s.Status = OpdSessionStatus.Completed;
                s.EditedAt = DateTime.Now;
                s.EditerId = _appSession.WebUserId;
                await _db.SaveChangesAsync();
            }
            return RedirectToPage("/Opd/Sessions", new { SessionDate = SessionDate?.Date.ToString("yyyy-MM-dd") });
        }

        public async Task<IActionResult> OnPostCancelAsync(long id)
        {
            var s = await _db.OpdSessions.FirstOrDefaultAsync(x => x.Id == id && !x.Retired);
            if (s != null)
            {
                s.Status = OpdSessionStatus.Cancelled;
                s.EditedAt = DateTime.Now;
                s.EditerId = _appSession.WebUserId;
                await _db.SaveChangesAsync();
            }
            return RedirectToPage("/Opd/Sessions", new { SessionDate = SessionDate?.Date.ToString("yyyy-MM-dd") });
        }

        private async Task LoadAsync()
        {
            await LoadLookupsAsync();
            await LoadSessionsAsync();
        }

        private async Task LoadLookupsAsync()
        {
            ConsultantOptions = await _db.Consultants
                .Include(c => c.Person)
                .Where(c => !c.Retired)
                .OrderBy(c => c.Person != null ? c.Person.Name : "")
                .Select(c => new ConsultantOption
                {
                    Id = c.Id,
                    Text = c.Person != null
                        ? (c.Person.NameWithInitials ?? c.Person.Name ?? ("Consultant #" + c.Id))
                        : ("Consultant #" + c.Id),
                    SpecialityId = c.SpecialityId,
                    FirstVisitFee = c.FirstVisitFee,
                    SubsequentVisitFee = c.SubsequentVisitFee,
                    SessionStartTime = c.SessionStartTime ?? "",
                    SessionEndTime = c.SessionEndTime ?? "",
                    DefaultRoomNo = c.DefaultRoomNo ?? "",
                    DefaultHospitalFee = c.DefaultHospitalFee,
                    WorkDays = c.WorkDays ?? ""
                })
                .ToListAsync();

            SpecialityList = await _db.Specialities
                .Where(x => !x.Retired)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name })
                .ToListAsync();
        }

        private async Task LoadSessionsAsync()
        {
            var day = SessionDate?.Date ?? DateTime.Today;

            var counts = await _db.Tokens
                .Where(t => !t.Retired && t.SessionId != null)
                .GroupBy(t => t.SessionId)
                .Select(g => new { SessionId = g.Key, Count = g.Count() })
                .ToListAsync();

            Sessions = await _db.OpdSessions
                .Include(s => s.Consultant).ThenInclude(c => c.Person)
                .Include(s => s.Speciality)
                .Where(s => !s.Retired && s.SessionDate == day)
                .OrderBy(s => s.FromTime)
                .Select(s => new SessionRow
                {
                    Id = s.Id,
                    ConsultantName = s.Consultant != null && s.Consultant.Person != null
                        ? (s.Consultant.Person.NameWithInitials ?? s.Consultant.Person.Name ?? "")
                        : "Unassigned",
                    SpecialityName = s.Speciality != null ? s.Speciality.Name : "",
                    RoomNo = s.RoomNo,
                    FromTime = s.FromTime,
                    ToTime = s.ToTime,
                    Fee = s.Fee,
                    HospitalFee = s.HospitalFee,
                    TokenStartingNo = s.TokenStartingNo,
                    TokenMax = s.TokenMax,
                    Status = s.Status
                })
                .ToListAsync();

            foreach (var s in Sessions)
            {
                var c = counts.FirstOrDefault(x => x.SessionId == s.Id);
                s.IssuedTokens = c?.Count ?? 0;
            }
        }
    }
}