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
    /// <summary>
    /// OPD consultant sessions.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 23-Oct-2026
    /// Time      : 10:30
    /// Change Log: 23-Oct-2026 - the consultant lookup coalesces the nullable
    ///             [STAFF] fee columns so a consultant without a rate card no
    ///             longer fails materialisation with "Nullable object must have
    ///             a value".
    /// </summary>
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

        /// <summary>
        /// Session proposals for the selected day, built from each consultant's
        /// working days and morning / evening window. The user ticks the rows
        /// that are actually running and creates them in one go.
        /// </summary>
        public List<AutoSlot> AutoSlots { get; set; } = new();

        /// <summary>Number of scheduled sessions created automatically on this page load.</summary>
        public int AutoCreatedCount { get; set; }

        public string Message { get; set; }

        [TempData]
        public string Success { get; set; }

        public class ConsultantOption
        {
            public long Id { get; set; }
            public string Text { get; set; }
            public long? SpecialityId { get; set; }
            public string SpecialityName { get; set; }
            public long? InstitutionId { get; set; }
            public long? DepartmentId { get; set; }
            public double FirstVisitFee { get; set; }
            public double SubsequentVisitFee { get; set; }
            public string SessionStartTime { get; set; }
            public string SessionEndTime { get; set; }
            public string EveningStartTime { get; set; }
            public string EveningEndTime { get; set; }
            public string DefaultRoomNo { get; set; }
            public double DefaultHospitalFee { get; set; }
            public string WorkDays { get; set; }
        }

        /// <summary>One proposed session row in the auto-fill panel.</summary>
        public class AutoSlot
        {
            public string Key { get; set; }
            public long ConsultantId { get; set; }
            public string ConsultantName { get; set; }
            public long? SpecialityId { get; set; }
            public string SpecialityName { get; set; }
            public long? InstitutionId { get; set; }
            public long? DepartmentId { get; set; }
            public string Period { get; set; }
            public string FromTime { get; set; }
            public string ToTime { get; set; }
            public string RoomNo { get; set; }
            public double Fee { get; set; }
            public double HospitalFee { get; set; }
            public int TokenStartingNo { get; set; } = 1;
            public int TokenMax { get; set; } = 50;
            public string WorkDays { get; set; }
            public bool ScheduledToday { get; set; }
            public bool DaysKnown { get; set; }
            /// <summary>0 = scheduled today, 1 = no working days set, 2 = not scheduled today.</summary>
            public int GroupOrder { get; set; }
            public string GroupTitle { get; set; }
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
            await LoadAsync(autoCreateScheduled: false);

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
            await LoadAsync(autoCreateScheduled: false);

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

        /// <summary>Starts every Scheduled session of the day in one click.</summary>
        public async Task<IActionResult> OnPostStartAllAsync()
        {
            if (SessionDate == null) SessionDate = DateTime.Today;
            var day = SessionDate.Value.Date;

            var sessions = await _db.OpdSessions
                .Where(s => !s.Retired && s.SessionDate == day
                            && s.Status == OpdSessionStatus.Scheduled)
                .ToListAsync();

            var now = DateTime.Now;
            foreach (var s in sessions)
            {
                s.Status = OpdSessionStatus.Active;
                s.EditedAt = now;
                s.EditerId = _appSession.WebUserId;
            }

            if (sessions.Count > 0)
                await _db.SaveChangesAsync();

            Success = sessions.Count == 0
                ? "There are no scheduled sessions to start."
                : $"{sessions.Count} scheduled session(s) started for {day:dd MMM yyyy}.";

            return RedirectToPage("/Opd/Sessions",
                new { SessionDate = day.ToString("yyyy-MM-dd") });
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

        // ------------------------------------------------------- auto-fill

        /// <summary>
        /// Creates every ticked proposal from the auto-fill panel. Only rows that
        /// still exist in the freshly rebuilt proposal list are created, so a
        /// session is never duplicated and a stale browser form cannot invent one.
        /// </summary>
        public async Task<IActionResult> OnPostAutoCreateAsync(string[] selected, bool startNow)
        {
            if (SessionDate == null) SessionDate = DateTime.Today;
            var day = SessionDate.Value.Date;

            await LoadLookupsAsync();

            var wanted = new HashSet<string>(
                selected ?? Array.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            if (wanted.Count == 0)
            {
                Message = "Tick at least one proposed session, then create.";
                await LoadSessionsAsync();
                AutoSlots = await BuildAutoSlotsAsync(day);
                return Page();
            }

            var proposals = await BuildAutoSlotsAsync(day);
            var chosen = proposals.Where(p => wanted.Contains(p.Key)).ToList();

            if (chosen.Count == 0)
            {
                Message = "The ticked sessions are already created for this day.";
                await LoadSessionsAsync();
                AutoSlots = proposals;
                return Page();
            }

            var now = DateTime.Now;

            foreach (var slot in chosen)
            {
                _db.OpdSessions.Add(new OpdSession
                {
                    ConsultantId = slot.ConsultantId,
                    SpecialityId = slot.SpecialityId,
                    SessionDate = day,
                    FromTime = slot.FromTime,
                    ToTime = string.IsNullOrWhiteSpace(slot.ToTime) ? null : slot.ToTime,
                    RoomNo = slot.RoomNo,
                    Fee = Math.Max(0, slot.Fee),
                    HospitalFee = Math.Max(0, slot.HospitalFee),
                    TokenStartingNo = 1,
                    TokenMax = 50,
                    TokenIncrement = 1,
                    Status = startNow ? OpdSessionStatus.Active : OpdSessionStatus.Scheduled,
                    Note = "Auto-filled from consultant schedule",
                    InstitutionId = slot.InstitutionId ?? _appSession.LoggedSite?.Id,
                    DepartmentId = slot.DepartmentId ?? _appSession.LoggedDepartment?.Id,
                    CreaterId = _appSession.WebUserId,
                    CreatedAt = now
                });
            }

            await _db.SaveChangesAsync();

            Success = $"{chosen.Count} session(s) created for {day:dd MMM yyyy}"
                      + (startNow ? " and started." : ".");

            return RedirectToPage("/Opd/Sessions",
                new { SessionDate = day.ToString("yyyy-MM-dd") });
        }

        private async Task LoadAsync(bool autoCreateScheduled = true)
        {
            await LoadLookupsAsync();
            var day = SessionDate?.Date ?? DateTime.Today;

            if (autoCreateScheduled)
                AutoCreatedCount = await EnsureScheduledSessionsAsync(day);

            await LoadSessionsAsync();
            AutoSlots = await BuildAutoSlotsAsync(day);
        }

        /// <summary>
        /// Opens today's (or a future day's) scheduled sessions automatically from
        /// each consultant's working days and clinic window. Only consultants whose
        /// WorkDays contains the weekday are created; today's desk therefore finds
        /// the roster already on the sessions list and in registration.
        /// </summary>
        private async Task<int> EnsureScheduledSessionsAsync(DateTime day)
        {
            // never back-create sessions for a past date
            if (day.Date < DateTime.Today)
                return 0;

            var proposals = await BuildAutoSlotsAsync(day);
            var scheduled = proposals.Where(p => p.ScheduledToday).ToList();
            if (scheduled.Count == 0)
                return 0;

            var now = DateTime.Now;

            foreach (var slot in scheduled)
            {
                _db.OpdSessions.Add(new OpdSession
                {
                    ConsultantId = slot.ConsultantId,
                    SpecialityId = slot.SpecialityId,
                    SessionDate = day.Date,
                    FromTime = slot.FromTime,
                    ToTime = string.IsNullOrWhiteSpace(slot.ToTime) ? null : slot.ToTime,
                    RoomNo = slot.RoomNo,
                    Fee = Math.Max(0, slot.Fee),
                    HospitalFee = Math.Max(0, slot.HospitalFee),
                    TokenStartingNo = 1,
                    TokenMax = 50,
                    TokenIncrement = 1,
                    Status = OpdSessionStatus.Scheduled,
                    Note = "Auto-created from consultant schedule",
                    InstitutionId = slot.InstitutionId ?? _appSession.LoggedSite?.Id,
                    DepartmentId = slot.DepartmentId ?? _appSession.LoggedDepartment?.Id,
                    CreaterId = _appSession.WebUserId,
                    CreatedAt = now
                });
            }

            await _db.SaveChangesAsync();
            return scheduled.Count;
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
                    SpecialityName = c.Speciality != null ? c.Speciality.Name : "",
                    InstitutionId = c.InstitutionId,
                    DepartmentId = c.DepartmentId,
                    FirstVisitFee = c.FirstVisitFee ?? 0,
                    SubsequentVisitFee = c.SubsequentVisitFee ?? 0,
                    SessionStartTime = c.SessionStartTime ?? "",
                    SessionEndTime = c.SessionEndTime ?? "",
                    EveningStartTime = c.EveningStartTime ?? "",
                    EveningEndTime = c.EveningEndTime ?? "",
                    DefaultRoomNo = c.DefaultRoomNo ?? "",
                    DefaultHospitalFee = c.DefaultHospitalFee ?? 0,
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

        /// <summary>
        /// Builds the auto-fill roster for one day: every live consultant gets a
        /// morning row (08:00-12:00 when no schedule is configured) and an evening
        /// row when an evening window exists. Consultants whose WorkDays contains
        /// the weekday are grouped first and pre-ticked in the view.
        /// </summary>
        private async Task<List<AutoSlot>> BuildAutoSlotsAsync(DateTime day)
        {
            var dayCode = day.DayOfWeek.ToString().Substring(0, 3).ToUpperInvariant();

            var existing = await _db.OpdSessions.AsNoTracking()
                .Where(s => !s.Retired && s.SessionDate == day && s.ConsultantId != null)
                .Select(s => new { s.ConsultantId, s.FromTime })
                .ToListAsync();

            var slots = new List<AutoSlot>();

            foreach (var c in ConsultantOptions)
            {
                var days = (c.WorkDays ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(d => d.Trim().ToUpperInvariant())
                    .Where(d => d.Length > 0)
                    .ToList();

                var daysKnown = days.Count > 0;
                var scheduledToday = days.Contains(dayCode);

                var groupOrder = scheduledToday ? 0 : (daysKnown ? 2 : 1);
                var groupTitle = scheduledToday
                    ? "Scheduled today"
                    : (daysKnown ? "Not scheduled today" : "Working days not set");

                var morningStart = CleanClock(c.SessionStartTime);
                var morningEnd = CleanClock(c.SessionEndTime);
                var eveningStart = CleanClock(c.EveningStartTime);
                var eveningEnd = CleanClock(c.EveningEndTime);

                if (morningStart != null || morningEnd != null)
                {
                    AddSlot("Morning", morningStart ?? "08:00", morningEnd ?? "");
                }
                else if (eveningStart == null && eveningEnd == null)
                {
                    // no clinic window configured: offer the standard morning slot
                    // so an unscheduled consultant can still be added in one tick
                    AddSlot("Morning", "08:00", "12:00");
                }

                if (eveningStart != null || eveningEnd != null)
                {
                    AddSlot("Evening", eveningStart ?? "16:00", eveningEnd ?? "");
                }

                void AddSlot(string period, string from, string to)
                {
                    // never propose the same consultant / start time twice on a day
                    if (existing.Any(e => e.ConsultantId == c.Id
                                          && string.Equals(
                                              CleanClock(e.FromTime), from,
                                              StringComparison.OrdinalIgnoreCase)))
                    {
                        return;
                    }

                    slots.Add(new AutoSlot
                    {
                        Key = $"{c.Id}|{from}|{period}",
                        ConsultantId = c.Id,
                        ConsultantName = c.Text,
                        SpecialityId = c.SpecialityId,
                        SpecialityName = c.SpecialityName,
                        InstitutionId = c.InstitutionId,
                        DepartmentId = c.DepartmentId,
                        Period = period,
                        FromTime = from,
                        ToTime = to,
                        RoomNo = string.IsNullOrWhiteSpace(c.DefaultRoomNo)
                            ? "OPD"
                            : c.DefaultRoomNo.Trim(),
                        Fee = Math.Max(0, c.FirstVisitFee),
                        HospitalFee = Math.Max(0, c.DefaultHospitalFee),
                        TokenStartingNo = 1,
                        TokenMax = 50,
                        WorkDays = c.WorkDays,
                        ScheduledToday = scheduledToday,
                        DaysKnown = daysKnown,
                        GroupOrder = groupOrder,
                        GroupTitle = groupTitle
                    });
                }
            }

            return slots
                .OrderBy(s => s.GroupOrder)
                .ThenBy(s => s.ConsultantName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(s => s.FromTime)
                .ToList();
        }

        /// <summary>Trims a clock value and normalises it to HH:mm when possible.</summary>
        private static string CleanClock(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var text = value.Trim();
            return TimeSpan.TryParse(text, out var time)
                ? time.ToString(@"hh\:mm")
                : text;
        }
    }
}