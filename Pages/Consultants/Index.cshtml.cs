using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Consultants
{
    /// <summary>
    /// Consultant master data - the legacy register
    /// [HMIS].[dbo].[CONSULT] (201 rows: PREFIX, NAME, ID, TITLE, PHONE,
    /// DIGREE, DEPT, USERID, UDATE, UTIME).
    ///
    /// The screen is the maintenance form of that table. Every legacy column is
    /// editable and the legacy stamp (USERID/UDATE/UTIME) is written on each
    /// save, so the file keeps looking like the VFP master it came from.
    ///
    /// Consultants are not an island any more: the application's own consultant
    /// rows (STAFF with DTYPE='Consultant') already carry the same codes in
    /// STAFF.Code - 200 of the 201 legacy rows are linked that way - and those
    /// rows are what the OPD, OT and billing screens list. A save therefore
    /// writes both sides (the same dual-write idea used by the panel company
    /// register), otherwise a consultant added here would never appear in a
    /// consultant dropdown.
    ///
    /// Rows are never deleted: Retired + RetireComments (and the same fields on
    /// the linked STAFF row) keep the history of every doctor who ever billed.
    ///
    /// Server-side gates: the page requires a login and the
    /// MASTER_CONSULTANT_EDIT privilege (or ALL); the list itself is readable by
    /// any logged-in user.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 22-Oct-2026
    /// Time      : 12:20
    /// Change Log: 22-Oct-2026 12:20 - created; list + popup entry, paging,
    ///             soft-retire with reason, dual write to the app consultant.
    /// </summary>
    public class IndexModel : Shared.PagedPageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public IndexModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        /// <summary>One row of the grid.</summary>
        public class ConsultantRow
        {
            public long Id { get; set; }
            public string Prefix { get; set; }
            public string Name { get; set; }
            public string Code { get; set; }
            public string Title { get; set; }
            public string Phone { get; set; }
            public string Degree { get; set; }
            public string Dept { get; set; }
            /// <summary>Active / Inactive / Expired / Resigned / Retired / Other.</summary>
            public string Status { get; set; }
            public bool Retired { get; set; }
            public string RetireComments { get; set; }
            /// <summary>The app consultant row (STAFF) linked by the same code.</summary>
            public long? StaffId { get; set; }

            // clinic card copied from the linked STAFF row
            public string WorkDays { get; set; }
            public string SessionStartTime { get; set; }
            public string SessionEndTime { get; set; }
            public string EveningStartTime { get; set; }
            public string EveningEndTime { get; set; }
            public double? FirstVisitFee { get; set; }
            public double? SubsequentVisitFee { get; set; }
        }

        public List<ConsultantRow> Items { get; set; } = new();
        public List<SelectListItem> DeptList { get; set; } = new();

        /// <summary>
        /// Distinct speciality / title values already used in the register, so the
        /// popup can offer them instead of making the user retype a speciality.
        /// Retired rows are included: their titles are still valid choices.
        /// </summary>
        public List<string> TitleList { get; set; } = new();

        /// <summary>The lifecycle statuses offered by the popup, in display order.</summary>
        public static readonly string[] Statuses =
        {
            "Active", "Inactive", "Expired", "Resigned", "Retired", "Other"
        };

        /// <summary>Badge colour of a status (Bootstrap class).</summary>
        public static string StatusCss(string status)
        {
            switch ((status ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "active": return "bg-success";
                case "inactive": return "bg-warning text-dark";
                case "expired": return "bg-info text-dark";
                case "resigned": return "bg-dark";
                case "retired": return "bg-secondary";
                default: return "bg-light text-dark border";
            }
        }

        /// <summary>
        /// Maps the posted status onto the list above (case-insensitive); a blank
        /// value means Active, an unknown value is rejected with null.
        /// </summary>
        private static string NormalizeStatus(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Active";

            foreach (var s in Statuses)
            {
                if (string.Equals(s, value.Trim(), StringComparison.OrdinalIgnoreCase))
                    return s;
            }

            return null;
        }

        /// <summary>Search over the code, name, speciality and phone.</summary>
        [BindProperty(SupportsGet = true)]
        public string Q { get; set; }

        /// <summary>Shows the retired consultants together with the live ones.</summary>
        [BindProperty(SupportsGet = true)]
        public bool ShowRetired { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [TempData]
        public string Success { get; set; }

        [TempData]
        public string Error { get; set; }

        /// <summary>Master data may only be changed with the edit privilege.</summary>
        public bool CanEdit => _session.HasPrivilege("MASTER_CONSULTANT_EDIT")
                               || _session.HasPrivilege("ALL");

        /// <summary>Payload of the add / edit popup.</summary>
        public class InputModel
        {
            public long Id { get; set; }
            public string Prefix { get; set; }
            public string Name { get; set; }
            public string Code { get; set; }
            public string Title { get; set; }
            public string Phone { get; set; }
            public string Degree { get; set; }
            public string Dept { get; set; }
            /// <summary>Active / Inactive / Expired / Resigned / Retired / Other.</summary>
            public string Status { get; set; }
            /// <summary>Reason of the status change, required when not Active.</summary>
            public string RetireReason { get; set; }
            /// <summary>Code before the edit, to find the linked STAFF row.</summary>
            public string OriginalCode { get; set; }

            // ---- clinic card (written to the linked STAFF row) ------------
            /// <summary>Consulting days, e.g. MON,TUE,WED.</summary>
            public List<string> WorkDays { get; set; } = new();
            public string SessionStartTime { get; set; }
            public string SessionEndTime { get; set; }
            public string EveningStartTime { get; set; }
            public string EveningEndTime { get; set; }
            public double? FirstVisitFee { get; set; }
            public double? SubsequentVisitFee { get; set; }
        }

        // ------------------------------------------------------------------ get

        public async Task<IActionResult> OnGetAsync()
        {
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");

            await LoadAsync();
            return Page();
        }

        // --------------------------------------------------------------- save

        /// <summary>Creates or updates one consultant (list + popup entry).</summary>
        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");
            if (!CanEdit)
            {
                Error = "You do not have the right to change consultant master data.";
                return RedirectToPage();
            }

            var name = Input.Name?.Trim();
            var code = Input.Code?.Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(code))
            {
                Error = "Consultant name and code are required.";
                return RedirectToPage(new { Q, ShowRetired });
            }

            if ((Input.FirstVisitFee ?? 0) < 0 || (Input.SubsequentVisitFee ?? 0) < 0)
            {
                Error = "Consultation fees cannot be negative.";
                return RedirectToPage(new { Q, ShowRetired });
            }

            var now = DateTime.Now;
            var user = _session.LoggedUser?.UserName ?? "SYSTEM";

            ConsultantMaster entity;
            if (Input.Id > 0)
            {
                entity = await _db.ConsultantMasters.FirstOrDefaultAsync(c => c.RowId == Input.Id);
                if (entity == null)
                {
                    Error = "Consultant not found.";
                    return RedirectToPage(new { Q, ShowRetired });
                }
            }
            else
            {
                entity = new ConsultantMaster { Retired = false };
                _db.ConsultantMasters.Add(entity);
            }

            // the legacy code is the link to the app consultant: a new code must
            // not collide with a live one (two legacy rows already share K04, so
            // an unchanged code is never re-checked)
            var codeChanged = Input.Id == 0
                              || !string.Equals(entity.Code, code, StringComparison.OrdinalIgnoreCase);

            if (codeChanged)
            {
                var clash = await _db.ConsultantMasters
                    .AnyAsync(c => !c.Retired
                                   && c.Code == code
                                   && c.RowId != Input.Id);
                if (clash)
                {
                    Error = $"Consultant code {code} is already used by a live consultant.";
                    return RedirectToPage(new { Q, ShowRetired });
                }
            }

            var originalCode = string.IsNullOrWhiteSpace(Input.OriginalCode)
                ? entity.Code
                : Input.OriginalCode.Trim();

            entity.Prefix = Input.Prefix?.Trim();
            entity.Name = name;
            entity.Code = code;
            entity.Title = Input.Title?.Trim();
            entity.Phone = Input.Phone?.Trim();
            entity.Degree = Input.Degree?.Trim();
            entity.Dept = Input.Dept?.Trim();
            entity.UserId = user;
            entity.UDate = now.ToString("yyyyMMdd");
            entity.UTime = now.ToString("HH:mm");

            // ---- lifecycle status (Active / Inactive / Expired / Resigned /
            //      Retired / Other) chosen in the popup -----------------------
            var status = NormalizeStatus(Input.Status);
            if (status == null)
            {
                Error = "Please choose a valid consultant status.";
                return RedirectToPage(new { Q, ShowRetired });
            }

            if (status == "Active")
            {
                // back on the list: the old reason no longer applies
                entity.Retired = false;
                entity.RetireComments = null;
            }
            else
            {
                var why = Input.RetireReason?.Trim();

                // a reason is mandatory for every status but Active; an existing
                // reason may be kept as-is when the row is only re-saved
                if (string.IsNullOrWhiteSpace(why) && string.IsNullOrWhiteSpace(entity.RetireComments))
                {
                    Error = $"A reason is required when the status is {status}.";
                    return RedirectToPage(new { Q, ShowRetired });
                }

                // every status but Active also sets the retire flag, so the
                // "active only" filters and the app consultant stay correct
                entity.Retired = true;
                if (!string.IsNullOrWhiteSpace(why))
                    entity.RetireComments = why;
            }

            entity.Status = status;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                Error = "The consultant could not be saved (duplicate code or a value too long for its column).";
                return RedirectToPage(new { Q, ShowRetired });
            }

            // ---- dual write: keep the app consultant (STAFF) in step ----------
            await SyncAppConsultantAsync(originalCode, entity, now);

            Success = $"Consultant {code} - {name} saved.";
            return RedirectToPage(new { Q, ShowRetired });
        }

        // ------------------------------------------------------------- retire

        /// <summary>Soft-retires one consultant, with the reason.</summary>
        public async Task<IActionResult> OnPostRetireAsync(long id, string reason)
        {
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");
            if (!CanEdit)
            {
                Error = "You do not have the right to change consultant master data.";
                return RedirectToPage();
            }

            var entity = await _db.ConsultantMasters.FirstOrDefaultAsync(c => c.RowId == id);
            if (entity == null)
            {
                Error = "Consultant not found.";
                return RedirectToPage(new { Q, ShowRetired });
            }

            var why = string.IsNullOrWhiteSpace(reason) ? "Retired from consultant master." : reason.Trim();
            var now = DateTime.Now;

            entity.Retired = true;
            entity.Status = "Retired";
            entity.RetireComments = why;
            entity.UserId = _session.LoggedUser?.UserName ?? "SYSTEM";
            entity.UDate = now.ToString("yyyyMMdd");
            entity.UTime = now.ToString("HH:mm");

            // the app consultant must disappear from the pickers as well
            var staff = await _db.Consultants.FirstOrDefaultAsync(c => c.Code == entity.Code);
            if (staff != null)
            {
                staff.Retired = true;
                staff.RetirerId = _session.WebUserId;
                staff.RetiredAt = now;
                staff.RetireComments = why;
            }

            await _db.SaveChangesAsync();
            Success = $"Consultant {entity.Code} retired.";
            return RedirectToPage(new { Q, ShowRetired });
        }

        /// <summary>Brings a retired consultant back.</summary>
        public async Task<IActionResult> OnPostRestoreAsync(long id)
        {
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");
            if (!CanEdit)
            {
                Error = "You do not have the right to change consultant master data.";
                return RedirectToPage();
            }

            var entity = await _db.ConsultantMasters.FirstOrDefaultAsync(c => c.RowId == id);
            if (entity == null)
            {
                Error = "Consultant not found.";
                return RedirectToPage(new { Q, ShowRetired });
            }

            entity.Retired = false;
            entity.Status = "Active";
            entity.RetireComments = null;
            entity.UserId = _session.LoggedUser?.UserName ?? "SYSTEM";
            entity.UDate = DateTime.Now.ToString("yyyyMMdd");
            entity.UTime = DateTime.Now.ToString("HH:mm");

            var staff = await _db.Consultants.FirstOrDefaultAsync(c => c.Code == entity.Code);
            if (staff != null)
            {
                staff.Retired = false;
                staff.RetireComments = null;
                staff.RetiredAt = null;
                staff.RetirerId = null;
            }

            await _db.SaveChangesAsync();
            Success = $"Consultant {entity.Code} restored.";
            return RedirectToPage(new { Q, ShowRetired });
        }

        // -------------------------------------------------------------- load

        private async Task LoadAsync()
        {
            DeptList = DeptServiceScopes.AllDepts
                .Select(d => new SelectListItem
                {
                    Value = d,
                    Text = $"{d} - {DeptServiceScopes.HospitalDeptName(d)}"
                })
                .ToList();

            // specialities / titles already used anywhere in the register; the popup
            // shows them as a datalist so a known speciality can be picked or a new
            // one still typed
            var titles = await _db.ConsultantMasters.AsNoTracking()
                .Where(c => c.Title != null && c.Title != "")
                .Select(c => c.Title)
                .Distinct()
                .ToListAsync();

            TitleList = titles
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var query = _db.ConsultantMasters.AsNoTracking();

            if (!ShowRetired)
                query = query.Where(c => !c.Retired);

            if (!string.IsNullOrWhiteSpace(Q))
            {
                var term = Q.Trim();
                query = query.Where(c =>
                    (c.Code != null && c.Code.Contains(term))
                    || (c.Name != null && c.Name.Contains(term))
                    || (c.Title != null && c.Title.Contains(term))
                    || (c.Phone != null && c.Phone.Contains(term)));
            }

            TotalCount = await query.CountAsync();
            ClampPaging();

            var page = await ApplyPaging(query
                .OrderBy(c => c.Retired)
                .ThenBy(c => c.Name)
                .ThenBy(c => c.RowId))
                .ToListAsync();

            // the app consultant of each visible row (STAFF.Code = CONSULT.ID)
            var codes = page.Select(c => c.Code)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .ToList();

            var staffRows = await _db.Consultants
                .Where(s => s.Code != null && codes.Contains(s.Code))
                .Select(s => new
                {
                    s.Code,
                    s.Id,
                    s.WorkDays,
                    s.SessionStartTime,
                    s.SessionEndTime,
                    s.EveningStartTime,
                    s.EveningEndTime,
                    s.FirstVisitFee,
                    s.SubsequentVisitFee
                })
                .ToListAsync();

            var staffByCode = new Dictionary<string, (long Id, string WorkDays, string SessionStartTime,
                string SessionEndTime, string EveningStartTime, string EveningEndTime,
                double? FirstVisitFee, double? SubsequentVisitFee)>(StringComparer.OrdinalIgnoreCase);

            foreach (var s in staffRows)
            {
                staffByCode[s.Code] = (s.Id, s.WorkDays, s.SessionStartTime, s.SessionEndTime,
                    s.EveningStartTime, s.EveningEndTime, s.FirstVisitFee, s.SubsequentVisitFee);
            }

            Items = page.Select(c =>
            {
                var card = (Id: 0L, WorkDays: (string)null, SessionStartTime: (string)null,
                    SessionEndTime: (string)null, EveningStartTime: (string)null,
                    EveningEndTime: (string)null, FirstVisitFee: (double?)null,
                    SubsequentVisitFee: (double?)null);
                var hasCard = !string.IsNullOrWhiteSpace(c.Code)
                              && staffByCode.TryGetValue(c.Code, out card);

                return new ConsultantRow
                {
                    Id = c.RowId,
                    Prefix = c.Prefix,
                    Name = c.Name,
                    Code = c.Code,
                    Title = c.Title,
                    Phone = c.Phone,
                    Degree = c.Degree,
                    Dept = c.Dept,
                    Status = string.IsNullOrWhiteSpace(c.Status)
                        ? (c.Retired ? "Retired" : "Active")
                        : c.Status,
                    Retired = c.Retired,
                    RetireComments = c.RetireComments,
                    StaffId = hasCard ? card.Id : (long?)null,
                    WorkDays = hasCard ? card.WorkDays : null,
                    SessionStartTime = hasCard ? card.SessionStartTime : null,
                    SessionEndTime = hasCard ? card.SessionEndTime : null,
                    EveningStartTime = hasCard ? card.EveningStartTime : null,
                    EveningEndTime = hasCard ? card.EveningEndTime : null,
                    FirstVisitFee = hasCard ? card.FirstVisitFee : null,
                    SubsequentVisitFee = hasCard ? card.SubsequentVisitFee : null
                };
            }).ToList();
        }

        /// <summary>
        /// Mirrors the saved legacy row onto the application's consultant row
        /// (STAFF with DTYPE='Consultant'): creates the person + staff pair when
        /// the code is new, otherwise refreshes the name, phone and degrees.
        /// </summary>
        private async Task SyncAppConsultantAsync(string originalCode, ConsultantMaster entity, DateTime now)
        {
            var lookupCode = string.IsNullOrWhiteSpace(originalCode) ? entity.Code : originalCode;

            var workDays = JoinDays(Input.WorkDays);
            var morningStart = NormalizeTime(Input.SessionStartTime);
            var morningEnd = NormalizeTime(Input.SessionEndTime);
            var eveningStart = NormalizeTime(Input.EveningStartTime);
            var eveningEnd = NormalizeTime(Input.EveningEndTime);

            var staff = await _db.Consultants
                .Include(s => s.Person)
                .FirstOrDefaultAsync(s => s.Code == lookupCode);

            if (staff == null)
            {
                var person = new Person
                {
                    Name = entity.Name,
                    Phone = entity.Phone,
                    CreatedAt = now,
                    CreaterId = _session.WebUserId,
                    Retired = false
                };
                _db.Persons.Add(person);
                await _db.SaveChangesAsync();

                _db.Consultants.Add(new Consultant
                {
                    Code = entity.Code,
                    PersonId = person.Id,
                    Degrees = entity.Degree,
                    InstitutionId = _session.LoggedSite?.Id,
                    CreatedAt = now,
                    CreaterId = _session.WebUserId,
                    Retired = entity.Retired,
                    RetireComments = entity.RetireComments,
                    RetiredAt = entity.Retired ? now : (DateTime?)null,
                    RetirerId = entity.Retired ? _session.WebUserId : null,
                    WorkDays = workDays,
                    SessionStartTime = morningStart,
                    SessionEndTime = morningEnd,
                    EveningStartTime = eveningStart,
                    EveningEndTime = eveningEnd,
                    FirstVisitFee = Input.FirstVisitFee,
                    SubsequentVisitFee = Input.SubsequentVisitFee
                });
                await _db.SaveChangesAsync();
                return;
            }

            // keep the app consultant aligned with the legacy master
            staff.Code = entity.Code;
            staff.Degrees = entity.Degree;
            staff.Retired = entity.Retired;
            staff.RetireComments = entity.RetireComments;
            staff.EditedAt = now;
            staff.EditerId = _session.WebUserId;

            // clinic card: days, morning / evening windows and consultation fees
            staff.WorkDays = workDays;
            staff.SessionStartTime = morningStart;
            staff.SessionEndTime = morningEnd;
            staff.EveningStartTime = eveningStart;
            staff.EveningEndTime = eveningEnd;
            staff.FirstVisitFee = Input.FirstVisitFee;
            staff.SubsequentVisitFee = Input.SubsequentVisitFee;

            // the retire stamp belongs to the status change, so it is kept only
            // while the consultant is actually retired
            if (entity.Retired)
            {
                staff.RetiredAt = staff.RetiredAt ?? now;
                staff.RetirerId = _session.WebUserId;
            }
            else
            {
                staff.RetiredAt = null;
                staff.RetirerId = null;
            }

            if (staff.Person != null)
            {
                staff.Person.Name = entity.Name;
                staff.Person.Phone = entity.Phone;
                staff.Person.EditedAt = now;
                staff.Person.EditerId = _session.WebUserId;
            }

            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Turns the checked day boxes into the legacy WorkDays string
        /// (MON,TUE,... in week order). A blank list clears the field.
        /// </summary>
        private static string JoinDays(List<string> days)
        {
            if (days == null || days.Count == 0)
                return null;

            var order = new[] { "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN" };
            var selected = new HashSet<string>(
                days.Where(d => !string.IsNullOrWhiteSpace(d))
                    .Select(d => d.Trim().ToUpperInvariant()));

            var ordered = order.Where(selected.Contains).ToList();
            return ordered.Count == 0 ? null : string.Join(",", ordered);
        }

        /// <summary>Normalises an HH:mm input; anything unparsable is kept as typed.</summary>
        private static string NormalizeTime(string value)
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
