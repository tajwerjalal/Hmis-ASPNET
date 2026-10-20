using System.Globalization;
using System.Text;
using HmisWeb.Data;
using HmisWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Ot.Reports
{
    /// <summary>
    /// OT Register - the standard theatre register every mid-size hospital keeps:
    /// all cases in a period with patient, theatre, surgeon, anaesthesia, actual
    /// times, outcome and status. Filterable by theatre / status, printable and
    /// exportable to CSV.
    /// </summary>
    public class OtRegisterModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public OtRegisterModel(ApplicationDbContext db) { _db = db; }

        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; } = DateTime.Today.AddDays(-30);
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; } = DateTime.Today;
        [BindProperty(SupportsGet = true)] public long? TheatreId { get; set; }
        [BindProperty(SupportsGet = true)] public string Status { get; set; } = "";
        [BindProperty(SupportsGet = true)] public string CaseType { get; set; } = "";

        public List<RegisterRow> Rows { get; set; } = new();
        public List<SelectListItem> TheatreList { get; set; } = new();

        public int CompletedCount => Rows.Count(r => r.Status == "Completed");
        public int CancelledCount => Rows.Count(r => r.Status == "Cancelled");
        public int EmergencyCount => Rows.Count(r => r.CaseType == "Emergency");
        public int? TotalMinutes
        {
            get
            {
                var m = Rows.Where(r => r.Minutes.HasValue).Select(r => r.Minutes.Value).ToList();
                return m.Count > 0 ? m.Sum() : (int?)null;
            }
        }

        public string PeriodText =>
            (FromDate ?? DateTime.MinValue).ToString("dd-MMM-yyyy") + " to " +
            (ToDate ?? DateTime.Today).ToString("dd-MMM-yyyy");

        public async Task OnGetAsync()
        {
            TheatreList = (await _db.OtTheatres.Where(t => !t.Retired).OrderBy(t => t.Name).ToListAsync())
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name, Selected = t.Id == TheatreId })
                .ToList();

            var from = (FromDate ?? DateTime.Today.AddDays(-30)).Date;
            var to = (ToDate ?? DateTime.Today).Date;

            var q = _db.OtCases
                .Include(c => c.Patient).ThenInclude(p => p.Person)
                .Include(c => c.Theatre)
                .Include(c => c.Surgeon).ThenInclude(s => s.Person)
                .Include(c => c.Anesthetist).ThenInclude(s => s.Person)
                .Include(c => c.ProcedureItem)
                .Where(c => !c.Retired && c.ScheduledDate >= from && c.ScheduledDate <= to.AddDays(1).AddSeconds(-1));

            if (TheatreId.HasValue) q = q.Where(c => c.TheatreId == TheatreId.Value);
            if (!string.IsNullOrWhiteSpace(Status)) q = q.Where(c => c.Status == Status);
            if (!string.IsNullOrWhiteSpace(CaseType)) q = q.Where(c => c.CaseType == CaseType);

            var cases = await q.OrderBy(c => c.ScheduledDate).ThenBy(c => c.SlotTime).ToListAsync();

            Rows = cases.Select(c => new RegisterRow
            {
                Id = c.Id,
                CaseNo = c.CaseNo,
                Date = c.ScheduledDate,
                SlotTime = c.SlotTime,
                CaseType = c.CaseType,
                Status = c.Status,
                Phn = c.Patient?.PatientId?.ToString() ?? c.Patient?.Phn ?? "",
                PatientName = c.Patient?.Person?.NameWithInitials ?? c.Patient?.Person?.Name ?? "",
                Theatre = c.Theatre?.Name ?? "",
                Surgeon = c.Surgeon?.Person?.Name ?? "",
                Anesthetist = c.Anesthetist?.Person?.Name ?? "",
                Procedure = !string.IsNullOrWhiteSpace(c.ProcedurePerformed) ? c.ProcedurePerformed : c.ProcedurePlanned,
                Anesthesia = !string.IsNullOrWhiteSpace(c.AnesthesiaGiven) ? c.AnesthesiaGiven : c.AnesthesiaPlanned,
                ActualStart = c.ActualStart,
                ActualEnd = c.ActualEnd,
                Minutes = c.ActualMinutes,
                Outcome = c.Outcome,
                BillNo = c.BillNo,
                CancelReason = c.CancelReason
            }).ToList();
        }

        public async Task<IActionResult> OnGetExportAsync()
        {
            await OnGetAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Case No,Date,Slot,PHN,Patient,Theatre,Surgeon,Anaesthetist,Type,Procedure,Anaesthesia,Start,End,Minutes,Status,Outcome,Bill No,Cancel Reason");
            foreach (var r in Rows)
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    Csv(r.CaseNo),
                    r.Date.ToString("yyyy-MM-dd"),
                    Csv(r.SlotTime),
                    Csv(r.Phn),
                    Csv(r.PatientName),
                    Csv(r.Theatre),
                    Csv(r.Surgeon),
                    Csv(r.Anesthetist),
                    Csv(r.CaseType),
                    Csv(r.Procedure),
                    Csv(r.Anesthesia),
                    r.ActualStart?.ToString("yyyy-MM-dd HH:mm") ?? "",
                    r.ActualEnd?.ToString("yyyy-MM-dd HH:mm") ?? "",
                    r.Minutes?.ToString() ?? "",
                    Csv(r.Status),
                    Csv(r.Outcome),
                    Csv(r.BillNo),
                    Csv(r.CancelReason)
                }));
            }

            return File(new UTF8Encoding(false).GetBytes(sb.ToString()), "text/csv", "OTRegister.csv");
        }

        private static string Csv(string v)
        {
            if (string.IsNullOrEmpty(v)) return "";
            return v.Contains(',') || v.Contains('"')
                ? "\"" + v.Replace("\"", "\"\"") + "\""
                : v;
        }

        public class RegisterRow
        {
            public long Id { get; set; }
            public string CaseNo { get; set; } = "";
            public DateTime Date { get; set; }
            public string SlotTime { get; set; } = "";
            public string CaseType { get; set; } = "";
            public string Status { get; set; } = "";
            public string Phn { get; set; } = "";
            public string PatientName { get; set; } = "";
            public string Theatre { get; set; } = "";
            public string Surgeon { get; set; } = "";
            public string Anesthetist { get; set; } = "";
            public string Procedure { get; set; } = "";
            public string Anesthesia { get; set; } = "";
            public DateTime? ActualStart { get; set; }
            public DateTime? ActualEnd { get; set; }
            public int? Minutes { get; set; }
            public string Outcome { get; set; } = "";
            public string BillNo { get; set; } = "";
            public string CancelReason { get; set; } = "";
        }
    }
}
