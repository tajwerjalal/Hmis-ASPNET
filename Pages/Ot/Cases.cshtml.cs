using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Ot
{
    /// <summary>
    /// OT day board - the theatre-wise list of cases for one date, the way a
    /// mid-size hospital runs its operating theatres. Shows every case with its
    /// slot, surgeon, status and actual times, with quick actions to start a case,
    /// record the operation (opens the case sheet) or cancel it, plus the day's
    /// counters.
    /// </summary>
    public class CasesModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public CasesModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? Date { get; set; } = DateTime.Today;

        [BindProperty(SupportsGet = true)]
        public long? TheatreId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } = "";

        public List<CaseRow> Rows { get; set; } = new();
        public List<SelectListItem> TheatreList { get; set; } = new();

        public string Success { get; set; } = "";
        public string ErrorMessage { get; set; } = "";

        public int Total => Rows.Count;
        public int ScheduledCount => Rows.Count(r => r.Status == "Scheduled");
        public int InTheatreCount => Rows.Count(r => r.Status == "In Theatre");
        public int CompletedCount => Rows.Count(r => r.Status == "Completed");
        public int CancelledCount => Rows.Count(r => r.Status == "Cancelled");
        public int PostponedCount => Rows.Count(r => r.Status == "Postponed");
        public int EmergencyCount => Rows.Count(r => r.CaseType == "Emergency");
        public int? TotalMinutes
        {
            get
            {
                var mins = Rows.Where(r => r.Minutes.HasValue).Select(r => r.Minutes.Value).ToList();
                return mins.Count > 0 ? mins.Sum() : (int?)null;
            }
        }

        public static string StatusesRequired =>
            "Scheduled,In Theatre,Completed,Cancelled,Postponed";

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostStartAsync(long id)
        {
            var c = await _db.OtCases.FindAsync(id);
            if (c != null && !c.Retired)
            {
                if (c.Status == "Scheduled" || c.Status == "Postponed")
                {
                    c.Status = "In Theatre";
                    if (!c.ActualStart.HasValue) c.ActualStart = DateTime.Now;
                    c.EditerId = _session.WebUserId;
                    c.EditedAt = DateTime.Now;
                    await _db.SaveChangesAsync();
                    Success = "Case " + c.CaseNo + " started.";
                }
                else
                {
                    ErrorMessage = "Case " + c.CaseNo + " is already '" + c.Status + "'.";
                }
                if (c != null) Date ??= c.ScheduledDate.Date;
            }
            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostCancelAsync(long id, string reason)
        {
            var c = await _db.OtCases.FindAsync(id);
            if (c != null && !c.Retired)
            {
                if (c.Status == "Completed")
                {
                    ErrorMessage = "A completed case cannot be cancelled - edit the case instead.";
                }
                else
                {
                    c.Status = "Cancelled";
                    c.CancelReason = string.IsNullOrWhiteSpace(reason) ? "Cancelled from the OT day board" : reason.Trim();
                    c.ActualStart = null;
                    c.ActualEnd = null;
                    c.EditerId = _session.WebUserId;
                    c.EditedAt = DateTime.Now;
                    await _db.SaveChangesAsync();
                    Success = "Case " + c.CaseNo + " cancelled.";
                    Date ??= c.ScheduledDate.Date;
                }
            }
            await LoadAsync();
            return Page();
        }

        private async Task LoadAsync()
        {
            var day = (Date ?? DateTime.Today).Date;
            Date = day;

            TheatreList = (await _db.OtTheatres
                    .Where(t => !t.Retired)
                    .OrderBy(t => t.Name)
                    .ToListAsync())
                .Select(t => new SelectListItem
                {
                    Value = t.Id.ToString(),
                    Text = t.Name,
                    Selected = t.Id == TheatreId
                }).ToList();

            var q = _db.OtCases
                .Include(c => c.Patient).ThenInclude(p => p.Person)
                .Include(c => c.Theatre)
                .Include(c => c.Surgeon).ThenInclude(s => s.Person)
                .Include(c => c.ProcedureItem)
                .Where(c => !c.Retired && c.ScheduledDate >= day && c.ScheduledDate < day.AddDays(1));

            if (TheatreId.HasValue)
                q = q.Where(c => c.TheatreId == TheatreId.Value);

            if (!string.IsNullOrWhiteSpace(StatusFilter))
                q = q.Where(c => c.Status == StatusFilter);

            var cases = await q.OrderBy(c => c.SlotTime).ThenBy(c => c.CaseNo).ToListAsync();

            Rows = cases.Select(c => new CaseRow
            {
                Id = c.Id,
                CaseNo = c.CaseNo,
                SlotTime = c.SlotTime,
                DurationMins = c.DurationMins,
                CaseType = c.CaseType,
                Status = c.Status,
                PatientId = c.Patient?.PatientId?.ToString() ?? c.Patient?.Phn ?? "",
                PatientName = c.Patient?.Person?.NameWithInitials ?? c.Patient?.Person?.Name ?? "",
                TheatreName = c.Theatre?.Name ?? "",
                TheatreType = c.Theatre?.TheatreType ?? "",
                SurgeonName = c.Surgeon?.Person?.Name ?? "",
                AnesthesiaPlanned = c.AnesthesiaPlanned,
                Procedure = !string.IsNullOrWhiteSpace(c.ProcedurePerformed) ? c.ProcedurePerformed : c.ProcedurePlanned,
                HasProcedureItem = c.ProcedureItemId.HasValue,
                ActualStart = c.ActualStart,
                ActualEnd = c.ActualEnd,
                Minutes = c.ActualMinutes,
                Outcome = c.Outcome,
                CancelReason = c.CancelReason
            }).ToList();
        }

        public class CaseRow
        {
            public long Id { get; set; }
            public string CaseNo { get; set; } = "";
            public string SlotTime { get; set; } = "";
            public int DurationMins { get; set; }
            public string CaseType { get; set; } = "";
            public string Status { get; set; } = "";
            public string PatientId { get; set; } = "";
            public string PatientName { get; set; } = "";
            public string TheatreName { get; set; } = "";
            public string TheatreType { get; set; } = "";
            public string SurgeonName { get; set; } = "";
            public string AnesthesiaPlanned { get; set; } = "";
            public string Procedure { get; set; } = "";
            public bool HasProcedureItem { get; set; }
            public DateTime? ActualStart { get; set; }
            public DateTime? ActualEnd { get; set; }
            public int? Minutes { get; set; }
            public string Outcome { get; set; } = "";
            public string CancelReason { get; set; } = "";
        }
    }
}
