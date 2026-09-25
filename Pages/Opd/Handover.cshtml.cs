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
    public class HandoverModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;

        public HandoverModel(ApplicationDbContext db, IAppSession appSession)
        {
            _db = db;
            _appSession = appSession;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? SessionDate { get; set; }

        [BindProperty]
        public long? ConsultantId { get; set; }

        [BindProperty]
        public long? ReceivedById { get; set; }

        public DateTime ViewDate => (SessionDate ?? DateTime.Today).Date;
        public List<PendingGroup> PendingGroups { get; set; } = new();
        public List<SelectListItem> DoctorOptions { get; set; } = new();
        public List<SelectListItem> ReceiverOptions { get; set; } = new();
        public List<SettlementHistory> History { get; set; } = new();
        public string ErrorMessage { get; set; }
        public SettlementReceiptModel Receipt { get; set; }

        public class PendingGroup
        {
            public long ConsultantId { get; set; }
            public string ConsultantName { get; set; }
            public int BillCount { get; set; }
            public int TokenCount { get; set; }
            public double ProfessionalFee { get; set; }
            public double HospitalFee { get; set; }
            public double Total => ProfessionalFee + HospitalFee;
        }

        public class SettlementHistory
        {
            public long Id { get; set; }
            public string ConsultantName { get; set; }
            public string ReceivedByName { get; set; }
            public int BillCount { get; set; }
            public int TokenCount { get; set; }
            public double AmountHandedOver { get; set; }
            public double AmountHospital { get; set; }
            public double AmountTotal { get; set; }
            public DateTime? ReceivedAt { get; set; }
            public string PaymentMethod { get; set; }
        }

        public class SettlementReceiptModel
        {
            public long SettlementId { get; set; }
            public string HospitalName { get; set; }
            public DateTime ReceivedAt { get; set; }
            public string ConsultantName { get; set; }
            public string ReceivedByName { get; set; }
            public int BillCount { get; set; }
            public int TokenCount { get; set; }
            public double AmountHospital { get; set; }
            public double AmountHandedOver { get; set; }
            public double AmountTotal { get; set; }
            public string PaymentMethod { get; set; }
            public string ReferenceNo { get; set; }
        }

        public async Task OnGetAsync()
        {
            if (SessionDate == null) SessionDate = ViewDate;
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (SessionDate == null) SessionDate = ViewDate;

            if (ConsultantId == null || ReceivedById == null)
            {
                ErrorMessage = "Select both the consultant and the receiving staff member.";
                await LoadAsync();
                return Page();
            }

            var day = ViewDate;
            var bills = await PendingBillsAsync(day, ConsultantId.Value);
            if (!bills.Any())
            {
                ErrorMessage = "There are no pending collections to hand over for this consultant.";
                await LoadAsync();
                return Page();
            }

            var billIds = bills.Select(b => b.Id).ToList();
            var tokenCount = await _db.Tokens
                .CountAsync(t => !t.Retired && t.BillId != null && billIds.Contains(t.BillId.Value));

            var settlement = new ConsultantSettlement
            {
                ConsultantId = ConsultantId,
                SettlementDate = day,
                TokenCount = tokenCount,
                BillCount = bills.Count,
                AmountHandedOver = bills.Sum(b => b.ProfessionalFee),
                AmountHospital = bills.Sum(b => b.HospitalFee),
                AmountTotal = bills.Sum(b => b.ProfessionalFee + b.HospitalFee),
                PaymentMethod = PaymentMethod.Cash,
                ReceivedById = ReceivedById,
                ReceivedAt = DateTime.Now,
                CreaterId = _appSession.WebUserId,
                CreatedAt = DateTime.Now,
                Note = $"EOD handover for {day:dd MMM yyyy}"
            };
            _db.ConsultantSettlements.Add(settlement);
            await _db.SaveChangesAsync();

            foreach (var b in bills)
                b.ConsultantSettlementId = settlement.Id;
            await _db.SaveChangesAsync();

            var consultant = await _db.Staffs.Include(s => s.Person).FirstOrDefaultAsync(s => s.Id == ConsultantId);
            var receiver = await _db.Staffs.Include(s => s.Person).FirstOrDefaultAsync(s => s.Id == ReceivedById);

            Receipt = new SettlementReceiptModel
            {
                SettlementId = settlement.Id,
                HospitalName = _appSession.LoggedSite?.PrintingName ?? _appSession.LoggedSite?.Name ?? "HMIS Hospital",
                ReceivedAt = settlement.ReceivedAt.Value,
                ConsultantName = consultant?.Person != null
                    ? (consultant.Person.NameWithInitials ?? consultant.Person.Name ?? "Consultant")
                    : "Consultant",
                ReceivedByName = receiver?.Person != null
                    ? (receiver.Person.NameWithInitials ?? receiver.Person.Name ?? "Staff")
                    : "Staff",
                BillCount = settlement.BillCount,
                TokenCount = settlement.TokenCount,
                AmountHospital = settlement.AmountHospital,
                AmountHandedOver = settlement.AmountHandedOver,
                AmountTotal = settlement.AmountTotal,
                PaymentMethod = settlement.PaymentMethod?.ToString() ?? "Cash",
                ReferenceNo = $"HNDR-{settlement.Id:D5}"
            };

            await LoadAsync();
            return Page();
        }

        private async Task LoadAsync()
        {
            await LoadConsultantOptionsAsync();

            var day = ViewDate;
            var sessions = await _db.OpdSessions
                .Include(s => s.Consultant).ThenInclude(c => c.Person)
                .Where(s => !s.Retired && s.SessionDate == day && s.ConsultantId != null && s.Status != OpdSessionStatus.Cancelled)
                .OrderBy(s => s.FromTime)
                .ToListAsync();

            var consultants = sessions
                .Where(s => s.ConsultantId != null)
                .GroupBy(s => s.ConsultantId.Value)
                .Select(g => g.First())
                .ToList();

            foreach (var s in consultants)
            {
                var bills = await PendingBillsAsync(day, s.ConsultantId.Value);
                if (!bills.Any()) continue;

                var billIds = bills.Select(b => b.Id).ToList();
                var tokenCount = await _db.Tokens
                    .CountAsync(t => !t.Retired && t.BillId != null && billIds.Contains(t.BillId.Value));

                PendingGroups.Add(new PendingGroup
                {
                    ConsultantId = s.ConsultantId.Value,
                    ConsultantName = s.Consultant.Person != null
                        ? (s.Consultant.Person.NameWithInitials ?? s.Consultant.Person.Name ?? "Consultant")
                        : "Consultant",
                    BillCount = bills.Count,
                    TokenCount = tokenCount,
                    ProfessionalFee = bills.Sum(b => b.ProfessionalFee),
                    HospitalFee = bills.Sum(b => b.HospitalFee)
                });
            }

            var settlements = await _db.ConsultantSettlements
                .Include(x => x.Consultant).ThenInclude(c => c.Person)
                .Include(x => x.ReceivedBy).ThenInclude(c => c.Person)
                .Where(x => x.SettlementDate == day && !x.Retired)
                .OrderByDescending(x => x.ReceivedAt)
                .ToListAsync();

            History = settlements.Select(x => new SettlementHistory
            {
                Id = x.Id,
                ConsultantName = x.Consultant?.Person != null
                    ? (x.Consultant.Person.NameWithInitials ?? x.Consultant.Person.Name ?? "Consultant")
                    : "Consultant",
                ReceivedByName = x.ReceivedBy?.Person != null
                    ? (x.ReceivedBy.Person.NameWithInitials ?? x.ReceivedBy.Person.Name ?? "Staff")
                    : "Staff",
                BillCount = x.BillCount,
                TokenCount = x.TokenCount,
                AmountHandedOver = x.AmountHandedOver,
                AmountHospital = x.AmountHospital,
                AmountTotal = x.AmountTotal,
                ReceivedAt = x.ReceivedAt,
                PaymentMethod = x.PaymentMethod?.ToString() ?? "Cash"
            }).ToList();

            ReceiverOptions = DoctorOptions;
        }

        private async Task<List<Bill>> PendingBillsAsync(DateTime day, long consultantId)
        {
            var bills = await _db.Bills
                .Where(b => !b.Retired && b.BillType == BillType.ChannelBill
                    && b.StaffId == consultantId && b.BillDate == day
                    && b.ProfessionalFee > 0 && b.ConsultantSettlementId == null)
                .ToListAsync();
            return bills;
        }

        private async Task LoadConsultantOptionsAsync()
        {
            var consultants = await _db.Consultants
                .Include(s => s.Person)
                .Where(s => !s.Retired)
                .OrderBy(s => s.Person.Name)
                .Select(s => new
                {
                    s.Id,
                    Name = s.Person.NameWithInitials ?? s.Person.Name ?? "Consultant"
                })
                .ToListAsync();

            var currentStaffId = _appSession.LoggedUser?.StaffId;
            var current = await _db.Staffs
                .Include(s => s.Person)
                .Where(s => !s.Retired && s.Id == currentStaffId)
                .Select(s => new { s.Id, Name = s.Person.NameWithInitials ?? s.Person.Name })
                .FirstOrDefaultAsync();

            DoctorOptions = consultants.Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .ToList();

            if (current != null && !DoctorOptions.Any(d => d.Value == current.Id.ToString()))
                DoctorOptions.Add(new SelectListItem { Value = current.Id.ToString(), Text = current.Name });
        }
    }
}