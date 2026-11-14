using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Hr
{
    /// <summary>
    /// Consultant master list.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 23-Oct-2026
    /// Time      : 10:30
    /// Change Log: 23-Oct-2026 - list projection coalesces the nullable
    ///             [STAFF] fee columns so consultants without a rate card
    ///             display as zero instead of throwing.
    /// </summary>
    public class ConsultantsModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public ConsultantsModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        [BindProperty(SupportsGet = true)]
        public string Q { get; set; }

        [BindProperty]
        public long? RetireId { get; set; }

        public List<ConsultantRow> Consultants { get; set; } = new();
        public string Message { get; set; }

        public class ConsultantRow
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public string SpecialityName { get; set; }
            public string Mobile { get; set; }
            public double FirstVisitFee { get; set; }
            public double SubsequentVisitFee { get; set; }
            public string Degrees { get; set; }
            public string SltRegNo { get; set; }
            public string Code { get; set; }
            public int SessionCount { get; set; }
            public string WorkDays { get; set; }
            public string SessionStartTime { get; set; }
            public string SessionEndTime { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostRetireAsync(long id)
        {
            var c = await _db.Staffs.FirstOrDefaultAsync(s => s.Id == id && !s.Retired);
            if (c != null)
            {
                c.Retired = true;
                c.RetiredAt = DateTime.Now;
                c.RetirerId = _session.WebUserId;
                await _db.SaveChangesAsync();
            }
            return RedirectToPage("/Hr/Consultants", new { Q });
        }

        private async Task LoadAsync()
        {
            var query = _db.Consultants
                .Include(c => c.Person)
                .Include(c => c.Speciality)
                .Where(c => !c.Retired);

            if (!string.IsNullOrWhiteSpace(Q))
            {
                var q = Q.Trim();
                query = query.Where(c =>
                    (c.Person != null && (c.Person.Name.Contains(q) || c.Person.NameWithInitials.Contains(q) || c.Person.Mobile.Contains(q)))
                    || (c.Speciality != null && c.Speciality.Name.Contains(q)));
            }

            Consultants = await query
                .OrderBy(c => c.Person != null ? c.Person.Name : "")
                .Select(c => new ConsultantRow
                {
                    Id = c.Id,
                    Name = c.Person != null
                        ? (c.Person.NameWithInitials ?? c.Person.Name ?? ("Consultant #" + c.Id))
                        : ("Consultant #" + c.Id),
                    SpecialityName = c.Speciality != null ? c.Speciality.Name : "",
                    Mobile = c.Person != null ? c.Person.Mobile : "",
                    FirstVisitFee = c.FirstVisitFee ?? 0,
                    SubsequentVisitFee = c.SubsequentVisitFee ?? 0,
                    Degrees = c.Degrees ?? "",
                    SltRegNo = c.SltRegNo ?? "",
                    Code = c.Code ?? "",
                    WorkDays = c.WorkDays ?? "",
                    SessionStartTime = c.SessionStartTime ?? "",
                    SessionEndTime = c.SessionEndTime ?? ""
                })
                .ToListAsync();

            var ids = Consultants.Select(c => c.Id).ToList();
            if (ids.Any())
            {
                var counts = await _db.OpdSessions
                    .Where(s => s.ConsultantId != null && ids.Contains(s.ConsultantId.Value))
                    .GroupBy(s => s.ConsultantId.Value)
                    .Select(g => new { g.Key, Count = g.Count() })
                    .ToListAsync();
                foreach (var c in Consultants)
                {
                    var cc = counts.FirstOrDefault(x => x.Key == c.Id);
                    c.SessionCount = cc?.Count ?? 0;
                }
            }
        }
    }
}