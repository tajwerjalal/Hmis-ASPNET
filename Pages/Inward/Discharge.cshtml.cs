using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using HmisWeb.Data.Enums;

namespace HmisWeb.Pages.Inward
{
    /// <summary>
    /// Patient Discharge - legacy discharge.scx.
    ///
    /// Saves the discharge status, closes the open [PATIENTROOM] stay, frees the
    /// [BED_POS] bed and writes the discharge date back to [ADMISSION] before it
    /// creates the final inward bill.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 22-Oct-2026
    /// Time      : 18:20
    /// Change Log: 22-Oct-2026 - discharge status, bed release and admission
    ///             close added.
    /// </summary>
    public class DischargeModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;

        public DischargeModel(ApplicationDbContext db, IAppSession appSession)
        {
            _db = db;
            _appSession = appSession;
        }

        [BindProperty(SupportsGet = true)]
        public long? EncounterId { get; set; }

        [BindProperty]
        public DateTime? DateOfDischarge { get; set; }

        [BindProperty]
        public string TimeOfDischarge { get; set; }

        [BindProperty]
        public string Comments { get; set; }

        [BindProperty]
        public string DischargeStatus { get; set; }

        [BindProperty]
        public PaymentMethod? PaymentMethod { get; set; } = HmisWeb.Data.Enums.PaymentMethod.Cash;

        [BindProperty]
        public double TenderedAmount { get; set; }

        public PatientEncounter Encounter { get; set; }
        public PatientRoom ActiveRoom { get; set; }
        public bool AlreadyDischarged { get; set; }
        public double ChargesTotal { get; set; }
        public double ChargesDiscount { get; set; }
        public int PendingBillCount { get; set; }
        public string Message { get; set; }
        public double ChangeDue { get; set; }

        public List<SelectListItem> PaymentMethodList { get; set; } = new();
        public List<SelectListItem> DischargeStatusList { get; set; } = new List<SelectListItem>
        {
            new SelectListItem("Recovered", "Recovered"),
            new SelectListItem("Improved", "Improved"),
            new SelectListItem("Transferred", "Transferred"),
            new SelectListItem("LAMA", "LAMA"),
            new SelectListItem("Expired", "Expired"),
            new SelectListItem("Against Advice", "AgainstAdvice")
        };

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadAsync();
            if (Encounter == null) return RedirectToPage("/Inward/PatientList");
            DateOfDischarge = DateTime.Today;
            TimeOfDischarge = DateTime.Now.ToString("HH:mm");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadAsync();
            if (Encounter == null) return RedirectToPage("/Inward/PatientList");

            if (Encounter.DateOfDischarge != null)
            {
                AlreadyDischarged = true;
                return Page();
            }

            var dischargeTime = CombineDateAndTime(DateOfDischarge ?? DateTime.Today, TimeOfDischarge);

            if (Encounter.DateOfAdmission.HasValue && dischargeTime < Encounter.DateOfAdmission.Value)
            {
                Message = $"Discharge time ({dischargeTime:dd MMM yyyy HH:mm}) cannot be before admission time ({Encounter.DateOfAdmission:dd MMM yyyy HH:mm}).";
                return Page();
            }

            var now = DateTime.Now;
            var userId = _appSession.WebUserId;
            var site = _appSession.LoggedSite;
            var dept = _appSession.LoggedDepartment ?? await _db.Departments.OrderBy(d => d.Id).FirstOrDefaultAsync();

            var method = PaymentMethod ?? HmisWeb.Data.Enums.PaymentMethod.Cash;
            bool isCredit = method == HmisWeb.Data.Enums.PaymentMethod.Credit;
            bool settled = !isCredit;

            double tendered = settled ? Math.Max(TenderedAmount, ChargesTotal) : 0;
            double cashBalance = settled ? Math.Round(tendered - ChargesTotal, 2) : 0;

            Encounter.DateOfDischarge = dischargeTime;
            Encounter.TimeOfDischarge = dischargeTime;
            Encounter.Discharged = true;
            Encounter.DischargeStatus = string.IsNullOrWhiteSpace(DischargeStatus) ? Encounter.DischargeStatus : DischargeStatus.Trim();
            Encounter.EditerId = userId;
            Encounter.EditedAt = now;

            var openRooms = await _db.PatientRooms
                .Where(r => r.PatientEncounterId == Encounter.Id && !r.Retired && r.ToTime == null)
                .ToListAsync();
            foreach (var pr in openRooms)
            {
                pr.ToTime = dischargeTime;
                pr.Discharged = true;
                pr.DischargedAt = dischargeTime;
            }

            // free the bed
            if (ActiveRoom?.BedPositionId != null)
            {
                var bed = await _db.BedPositions.FirstOrDefaultAsync(b => b.Id == ActiveRoom.BedPositionId.Value);
                if (bed != null)
                {
                    bed.Status = "Vacant";
                    bed.PatientCode = null;
                    var stamp = DateTime.Now;
                    bed.UserId = _appSession.LoggedUser?.UserName ?? "SYSTEM";
                    bed.UDate = stamp.Date;
                    bed.UTime = stamp.ToString("HH:mm");
                }
            }

            // close the admission
            var admission = await _db.Admissions.FirstOrDefaultAsync(a => a.PatientEncounterId == Encounter.Id && !a.Retired);
            if (admission != null)
            {
                admission.DischargeDate = dischargeTime;
            }

            var bill = new Bill
            {
                BillType = BillType.InwardFinalBill,
                BillClassType = BillClassType.BilledBill,
                PatientId = Encounter.PatientId,
                PatientEncounterId = Encounter.Id,
                PersonId = Encounter.Patient?.PersonId,
                InstitutionId = site?.Id ?? Encounter.InstitutionId,
                DepartmentId = dept?.Id ?? Encounter.DepartmentId,
                GrantTotal = ChargesTotal,
                Total = ChargesTotal,
                BillTotal = ChargesTotal,
                Discount = ChargesDiscount,
                NetTotal = ChargesTotal,
                PaymentMethod = method,
                TenderedAmount = tendered,
                CashPaid = settled ? tendered : 0,
                CashBalance = cashBalance,
                Balance = settled ? 0 : ChargesTotal,
                Paid = settled,
                PaidAt = settled ? (DateTime?)now : null,
                BillPaymentCompletelySettled = settled,
                BillDate = dischargeTime,
                CreatedAt = now,
                CreaterId = userId,
                Comments = Comments,
                ReferenceNumber = Encounter.BhtNo
            };

            if (settled)
            {
                bill.Payments.Add(new Payment
                {
                    PaymentMethod = method,
                    PaidValue = ChargesTotal,
                    WrittenAt = now,
                    Realized = true,
                    RealizedAt = now,
                    InstitutionId = site?.Id ?? Encounter.InstitutionId,
                    DepartmentId = dept?.Id ?? Encounter.DepartmentId,
                    CreatedAt = now
                });
            }

            _db.Bills.Add(bill);
            await _db.SaveChangesAsync();

            Encounter.FinalBillId = bill.Id;
            Encounter.GrantTotal = ChargesTotal;
            Encounter.Discount = ChargesDiscount;
            Encounter.NetTotal = ChargesTotal;
            Encounter.AdjustedTotal = ChargesTotal;
            Encounter.PaymentFinalized = true;

            await _db.SaveChangesAsync();

            TempData["Success"] = settled
                ? $"Patient discharged. Final bill #{bill.Id} settled via {method}."
                : $"Patient discharged. Final bill #{bill.Id} saved on credit.";
            return RedirectToPage("/Inward/PatientList");
        }

        private async Task LoadAsync()
        {
            PaymentMethodList = Enum.GetNames<PaymentMethod>()
                .Select(n => new SelectListItem { Value = n, Text = n })
                .ToList();

            Encounter = await _db.PatientEncounters
                .Include(e => e.Patient).ThenInclude(p => p.Person)
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == EncounterId && !e.Retired);
            if (Encounter == null) return;

            AlreadyDischarged = Encounter.DateOfDischarge != null;

            ActiveRoom = await _db.PatientRooms
                .Include(r => r.BedPosition)
                .Where(r => r.PatientEncounterId == Encounter.Id && !r.Retired && r.ToTime == null)
                .OrderByDescending(r => r.FromTime)
                .FirstOrDefaultAsync();

            var bills = await _db.Bills
                .Where(b => b.PatientEncounterId == Encounter.Id && !b.Retired && b.BillType == BillType.InwardBill)
                .ToListAsync();
            PendingBillCount = bills.Count;
            ChargesDiscount = Math.Round(bills.Sum(b => b.Discount), 2);
            ChargesTotal = Math.Round(bills.Sum(b => b.NetTotal), 2);
        }

        private static DateTime? CombineDateAndTime(DateTime? date, string time)
        {
            if (date == null) return null;

            if (TimeSpan.TryParse(time, out var ts))
                return date.Value.Date.Add(ts);

            return date.Value.Date;
        }
    }
}