using HmisWeb.Data;
using HmisWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Ot.Reports
{
    /// <summary>
    /// OT utilisation - theatre-wise case counts, split of elective / emergency,
    /// completed / cancelled, minutes occupied and occupancy against an 8-hour
    /// (480 min) theatre day. This is the standard utilisation stat a mid-size
    /// hospital reviews weekly.
    /// </summary>
    public class OtUtilizationModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public OtUtilizationModel(ApplicationDbContext db) { _db = db; }

        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; } = DateTime.Today.AddDays(-30);
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; } = DateTime.Today;

        public List<UtilRow> Rows { get; set; } = new();

        public const int TheatreDayMins = 480; // an 8-hour theatre day

        public int TotalCases => Rows.Sum(r => r.Cases);
        public int TotalCompleted => Rows.Sum(r => r.Completed);
        public int TotalCancelled => Rows.Sum(r => r.Cancelled);
        public int TotalElective => Rows.Sum(r => r.Elective);
        public int TotalEmergency => Rows.Sum(r => r.Emergency);
        public int TotalMinutes => Rows.Sum(r => r.Minutes);
        public int TotalAvailable => Rows.Sum(r => r.AvailableMins);
        public int DaysInPeriod { get; private set; }
        public int GrandPct => TotalAvailable > 0 ? (int)Math.Round(100.0 * TotalMinutes / TotalAvailable) : 0;

        public string PeriodText =>
            (FromDate ?? DateTime.MinValue).ToString("dd-MMM-yyyy") + " to " +
            (ToDate ?? DateTime.Today).ToString("dd-MMM-yyyy");

        public async Task OnGetAsync()
        {
            var from = (FromDate ?? DateTime.Today.AddDays(-30)).Date;
            var to = (ToDate ?? DateTime.Today).Date;
            DaysInPeriod = Math.Max(1, (to - from).Days + 1);

            var theatres = await _db.OtTheatres.Where(t => !t.Retired).OrderBy(t => t.Name).ToListAsync();

            var cases = await _db.OtCases
                .Include(c => c.Theatre)
                .Where(c => !c.Retired && c.ScheduledDate >= from && c.ScheduledDate <= to.AddDays(1).AddSeconds(-1))
                .ToListAsync();

            foreach (var t in theatres)
            {
                var mine = cases.Where(c => c.TheatreId == t.Id).ToList();
                var minutes = mine.Where(c => c.ActualMinutes.HasValue).Sum(c => c.ActualMinutes.Value);

                Rows.Add(new UtilRow
                {
                    Theatre = t.Name,
                    TheatreType = t.TheatreType,
                    Active = t.Active,
                    Cases = mine.Count,
                    Elective = mine.Count(c => c.CaseType == "Elective"),
                    Emergency = mine.Count(c => c.CaseType == "Emergency"),
                    Completed = mine.Count(c => c.Status == "Completed"),
                    Cancelled = mine.Count(c => c.Status == "Cancelled"),
                    Postponed = mine.Count(c => c.Status == "Postponed"),
                    Minutes = minutes,
                    AvgMins = mine.Count(c => c.ActualMinutes.HasValue) > 0
                        ? (int)Math.Round((double)minutes / mine.Count(c => c.ActualMinutes.HasValue))
                        : 0,
                    AvailableMins = DaysInPeriod * TheatreDayMins
                });
            }

            // Cases recorded without a theatre (possible if the master was edited later).
            var orphan = cases.Where(c => !c.TheatreId.HasValue || theatres.All(t => t.Id != c.TheatreId.Value)).ToList();
            if (orphan.Count > 0)
            {
                var minutes = orphan.Where(c => c.ActualMinutes.HasValue).Sum(c => c.ActualMinutes.Value);
                Rows.Add(new UtilRow
                {
                    Theatre = "(no theatre assigned)",
                    TheatreType = "",
                    Active = true,
                    Cases = orphan.Count,
                    Elective = orphan.Count(c => c.CaseType == "Elective"),
                    Emergency = orphan.Count(c => c.CaseType == "Emergency"),
                    Completed = orphan.Count(c => c.Status == "Completed"),
                    Cancelled = orphan.Count(c => c.Status == "Cancelled"),
                    Postponed = orphan.Count(c => c.Status == "Postponed"),
                    Minutes = minutes,
                    AvgMins = orphan.Count(c => c.ActualMinutes.HasValue) > 0
                        ? (int)Math.Round((double)minutes / orphan.Count(c => c.ActualMinutes.HasValue))
                        : 0,
                    AvailableMins = 0
                });
            }
        }

        public class UtilRow
        {
            public string Theatre { get; set; } = "";
            public string TheatreType { get; set; } = "";
            public bool Active { get; set; }
            public int Cases { get; set; }
            public int Elective { get; set; }
            public int Emergency { get; set; }
            public int Completed { get; set; }
            public int Cancelled { get; set; }
            public int Postponed { get; set; }
            public int Minutes { get; set; }
            public int AvgMins { get; set; }
            public int AvailableMins { get; set; }
            public int Pct => AvailableMins > 0 ? (int)Math.Round(100.0 * Minutes / AvailableMins) : 0;
        }
    }
}
