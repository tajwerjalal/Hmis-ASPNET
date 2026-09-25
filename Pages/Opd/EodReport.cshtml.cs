using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Opd
{
    public class EodReportModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public EodReportModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? SessionDate { get; set; }

        public DateTime ViewDate => (SessionDate ?? DateTime.Today).Date;
        public List<EodRow> Rows { get; set; } = new();
        public TotalsModel Totals { get; set; } = new();

        public class EodRow
        {
            public long SessionId { get; set; }
            public string ConsultantName { get; set; }
            public string SpecialityName { get; set; }
            public string RoomNo { get; set; }
            public string FromTime { get; set; }
            public int TokensIssued { get; set; }
            public int Waiting { get; set; }
            public int Consulting { get; set; }
            public int Completed { get; set; }
            public int NoShow { get; set; }
            public double HospitalFee { get; set; }
            public double ProfessionalFee { get; set; }
            public double Total { get; set; }
            public double HandedOver { get; set; }
            public bool Settled => HandedOver >= ProfessionalFee;
        }

        public class TotalsModel
        {
            public int TokensIssued { get; set; }
            public int Completed { get; set; }
            public int NoShow { get; set; }
            public double HospitalFee { get; set; }
            public double ProfessionalFee { get; set; }
            public double Total { get; set; }
            public double HandedOver { get; set; }
        }

        public async Task OnGetAsync()
        {
            if (SessionDate == null) SessionDate = ViewDate;

            var sessions = await _db.OpdSessions
                .Include(s => s.Consultant).ThenInclude(c => c.Person)
                .Include(s => s.Speciality)
                .Where(s => !s.Retired && s.SessionDate == ViewDate && s.Status != OpdSessionStatus.Cancelled)
                .OrderBy(s => s.FromTime)
                .ToListAsync();

            if (!sessions.Any()) return;

            var sessionIds = sessions.Select(s => s.Id).ToHashSet();

            var bills = await _db.Bills
                .Where(b => !b.Retired && b.BillType == BillType.ChannelBill && b.SessionId != null)
                .ToListAsync();
            bills = bills.Where(b => long.TryParse(b.SessionId, out var sid) && sessionIds.Contains(sid)).ToList();

            var tokenStatuses = await _db.Tokens
                .Where(t => !t.Retired && t.SessionId != null && sessionIds.Contains(t.SessionId.Value))
                .Select(t => new { t.SessionId, t.Status })
                .ToListAsync();

            foreach (var s in sessions)
            {
                var sBills = bills.Where(b => long.TryParse(b.SessionId, out var sid) && sid == s.Id).ToList();
                var sTokens = tokenStatuses.Where(t => t.SessionId == s.Id).ToList();

                var row = new EodRow
                {
                    SessionId = s.Id,
                    ConsultantName = s.Consultant?.Person != null
                        ? (s.Consultant.Person.NameWithInitials ?? s.Consultant.Person.Name ?? "Consultant")
                        : "Consultant",
                    SpecialityName = s.Speciality?.Name ?? "General",
                    RoomNo = s.RoomNo,
                    FromTime = s.FromTime,
                    TokensIssued = sTokens.Count,
                    Waiting = sTokens.Count(t => t.Status == TokenStatus.Waiting),
                    Consulting = sTokens.Count(t => t.Status == TokenStatus.InConsultation || t.Status == TokenStatus.Called),
                    Completed = sTokens.Count(t => t.Status == TokenStatus.Completed),
                    NoShow = sTokens.Count(t => t.Status == TokenStatus.NoShow || t.Status == TokenStatus.Cancelled),
                    HospitalFee = sBills.Sum(b => b.HospitalFee),
                    ProfessionalFee = sBills.Sum(b => b.ProfessionalFee),
                    HandedOver = sBills.Where(b => b.ConsultantSettlementId != null).Sum(b => b.ProfessionalFee)
                };
                row.Total = row.HospitalFee + row.ProfessionalFee;
                Rows.Add(row);
            }

            Totals.TokensIssued = Rows.Sum(r => r.TokensIssued);
            Totals.Completed = Rows.Sum(r => r.Completed);
            Totals.NoShow = Rows.Sum(r => r.NoShow);
            Totals.HospitalFee = Rows.Sum(r => r.HospitalFee);
            Totals.ProfessionalFee = Rows.Sum(r => r.ProfessionalFee);
            Totals.Total = Rows.Sum(r => r.Total);
            Totals.HandedOver = Rows.Sum(r => r.HandedOver);
        }
    }
}