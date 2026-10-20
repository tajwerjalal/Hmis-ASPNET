using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Opd
{
    /// <summary>
    /// GENERATE OPD BILL (BATCH) - /opd/BillingBatch.
    /// New-schema batch generator that turns open OPD PreBill slips
    /// (new-schema Bills) into BilledBills per patient encounter.
    /// Moved out of the route /opd/Billing on 20-Oct-2026 to make room for the
    /// legacy Outdoor Patient (OPD Bill) screen (BS0101 port); re-point any
    /// old book marks to /opd/BillingBatch.
    ///
    /// Developer: Tajwer Jalal | Date: 20-Oct-2026 | Time: 16:40
    /// </summary>
    public class BillingBatchModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ICommonService _common;
        private readonly IAppSession _appSession;

        public BillingBatchModel(ApplicationDbContext db, ICommonService common, IAppSession appSession)
        {
            _db = db;
            _common = common;
            _appSession = appSession;
        }

        [BindProperty]
        public DateTime BillDate { get; set; } = DateTime.Today;

        [BindProperty]
        public DateTime FromDate { get; set; } = DateTime.Today;

        [BindProperty]
        public DateTime ToDate { get; set; } = DateTime.Today;

        [BindProperty]
        public bool FilterByCompany { get; set; }

        [BindProperty]
        public long? CompanyId { get; set; }

        [BindProperty]
        public List<long> SelectedIds { get; set; } = new();

        public List<BillingRow> Rows { get; set; } = new();
        public List<SelectListItem> CompanyList { get; set; } = new();
        public string Message { get; set; }
        public int GeneratedCount { get; set; }

        public async Task OnGetAsync()
        {
            await LoadCompaniesAsync();
        }

        public async Task<IActionResult> OnPostUpdateListAsync()
        {
            await LoadCompaniesAsync();
            await LoadRowsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostGenerateAsync()
        {
            await LoadCompaniesAsync();

            if (SelectedIds == null || !SelectedIds.Any())
            {
                Message = "Select at least one patient to generate bills.";
                await LoadRowsAsync();
                return Page();
            }

            int generated = 0;
            foreach (var encounterId in SelectedIds)
            {
                if (await GenerateBillAsync(encounterId))
                    generated++;
            }

            GeneratedCount = generated;
            TempData["Success"] = $"Bills successfully generated ({generated}).";
            await LoadRowsAsync();
            return Page();
        }

        private async Task<bool> GenerateBillAsync(long encounterId)
        {
            var encounter = await _db.PatientEncounters
                .Include(e => e.Patient).ThenInclude(p => p.Person)
                .FirstOrDefaultAsync(e => e.Id == encounterId && !e.Retired
                    && e.EncounterType == EncounterType.Opd);
            if (encounter == null) return false;

            var slips = await _db.Bills
                .Where(b => b.PatientEncounterId == encounterId && !b.Retired && !b.Cancelled
                    && b.BillClosed == false && b.BilledBillId == null
                    && (b.BillClassType == BillClassType.PreBill || b.Balance > 0))
                .Include(b => b.BillItems)
                .ToListAsync();
            if (!slips.Any()) return false;

            double grantTotal = Math.Round(slips.Sum(b => b.Total), 2);
            double total = Math.Round(slips.Sum(b => b.Total), 2);
            double discount = Math.Round(slips.Sum(b => b.Discount), 2);
            double netTotal = Math.Round(slips.Sum(b => b.NetTotal), 2);

            var companyId = encounter.CreditCompanyId
                ?? await _common.GetPatientCompanyIdAsync(encounter.Patient?.PersonId ?? 0);
            bool isCredit = companyId != null;

            var now = DateTime.Now;
            var site = _appSession.LoggedSite;
            var dept = _appSession.LoggedDepartment ?? await _db.Departments.OrderBy(d => d.Id).FirstOrDefaultAsync();
            var userId = _appSession.WebUserId;

            var bill = new Bill
            {
                BillClassType = BillClassType.BilledBill,
                BillType = BillType.OpdBill,
                PatientEncounterId = encounter.Id,
                PatientId = encounter.PatientId,
                PersonId = encounter.Patient?.PersonId,
                CreditCompanyId = companyId,
                InstitutionId = site?.Id ?? encounter.InstitutionId,
                DepartmentId = dept?.Id ?? encounter.DepartmentId,
                GrantTotal = grantTotal,
                Total = total,
                BillTotal = total,
                Discount = discount,
                NetTotal = netTotal,
                Balance = isCredit ? netTotal : 0,
                PaymentMethod = isCredit ? PaymentMethod.Credit : PaymentMethod.Cash,
                Paid = !isCredit,
                PaidAt = isCredit ? (DateTime?)null : now,
                BillPaymentCompletelySettled = !isCredit,
                BillDate = BillDate,
                CreatedAt = now,
                CreaterId = userId,
                ReferenceNumber = encounter.Id.ToString()
            };
            _db.Bills.Add(bill);
            await _db.SaveChangesAsync();

            int serial = 1;
            foreach (var s in slips)
            {
                foreach (var it in s.BillItems.Where(i => !i.Retired))
                {
                    _db.BillItems.Add(new BillItem
                    {
                        BillId = bill.Id,
                        ItemId1 = it.ItemId1,
                        ItemId = it.ItemId,
                        Descreption = it.Descreption,
                        Qty = it.Qty,
                        Rate = it.Rate,
                        NetRate = it.NetRate,
                        GrossValue = it.GrossValue,
                        Discount = it.Discount,
                        NetValue = it.NetValue,
                        SessionDate = it.SessionDate ?? s.BillDate,
                        SearialNo = serial++,
                        CreatedAt = now,
                        CreaterId = userId
                    });
                }

                s.BilledBillId = bill.Id;
                s.BillClosed = true;
            }

            if (!isCredit)
            {
                _db.Payments.Add(new Payment
                {
                    BillId = bill.Id,
                    PaymentMethod = PaymentMethod.Cash,
                    PaidValue = netTotal,
                    WrittenAt = now,
                    Realized = true,
                    RealizedAt = now,
                    InstitutionId = bill.InstitutionId,
                    DepartmentId = bill.DepartmentId,
                    CreatedAt = now
                });
            }

            await _db.SaveChangesAsync();

            encounter.GrantTotal = grantTotal;
            encounter.NetTotal = netTotal;
            await _db.SaveChangesAsync();

            return true;
        }

        private async Task LoadRowsAsync()
        {
            Rows = new List<BillingRow>();

            var slips = await _db.Bills
                .Where(b => !b.Retired && !b.Cancelled && !b.Refunded
                    && b.BillClosed == false && b.BilledBillId == null
                    && (b.BillClassType == BillClassType.PreBill || b.Balance > 0)
                    && b.PatientEncounterId != null
                    && b.PatientEncounter.EncounterType == EncounterType.Opd
                    && (b.BillDate ?? b.CreatedAt) >= FromDate
                    && (b.BillDate ?? b.CreatedAt) <= ToDate)
                .Include(b => b.PatientEncounter).ThenInclude(e => e.Patient).ThenInclude(p => p.Person)
                .Include(b => b.PatientEncounter).ThenInclude(e => e.CreditCompany)
                .OrderBy(b => b.PatientEncounterId)
                .ToListAsync();

            var groups = slips.GroupBy(b => b.PatientEncounterId.Value);
            foreach (var g in groups)
            {
                var first = g.First();
                var encounter = first.PatientEncounter;
                long? companyId = encounter.CreditCompanyId
                    ?? await _common.GetPatientCompanyIdAsync(encounter.Patient?.PersonId ?? 0);
                if (companyId == null) companyId = encounter.CreditCompanyId;

                if (FilterByCompany && CompanyId.HasValue && companyId != CompanyId.Value)
                    continue;

                var companyName = encounter.CreditCompany?.Name;
                if (string.IsNullOrEmpty(companyName) && companyId.HasValue)
                {
                    var company = await _db.Institutions.FirstOrDefaultAsync(i => i.Id == companyId.Value);
                    companyName = company?.Name;
                }

                Rows.Add(new BillingRow
                {
                    EncounterId = encounter.Id,
                    RegNo = encounter.Id.ToString(),
                    PatientName = encounter.Patient?.Person?.NameWithInitials
                        ?? encounter.Patient?.Person?.Name
                        ?? ("Patient #" + encounter.PatientId),
                    CompanyId = companyId,
                    CompanyName = companyName ?? "-",
                    NetAmount = Math.Round(g.Sum(b => b.NetTotal), 2),
                    Selected = true
                });
            }
        }

        private async Task LoadCompaniesAsync()
        {
            var rateCompanies = await _db.CompanyServiceRates
                .Where(c => !c.Retired)
                .Select(c => c.CompanyId)
                .ToListAsync();
            var employeeCompanies = await _db.CompanyEmployees
                .Where(e => !e.Retired)
                .Select(e => e.CompanyId)
                .ToListAsync();
            var ids = rateCompanies.Concat(employeeCompanies).Distinct().ToList();

            List<Institution> companies;
            if (ids.Any())
            {
                companies = await _db.Institutions
                    .Where(i => !i.Retired && ids.Contains(i.Id))
                    .OrderBy(i => i.Name)
                    .ToListAsync();
            }
            else
            {
                companies = await _db.Institutions
                    .Where(i => !i.Retired)
                    .OrderBy(i => i.Name)
                    .ToListAsync();
            }

            CompanyList = companies
                .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = i.Name ?? ("Institution #" + i.Id) })
                .ToList();
        }

        public class BillingRow
        {
            public long EncounterId { get; set; }
            public string RegNo { get; set; }
            public string PatientName { get; set; }
            public long? CompanyId { get; set; }
            public string CompanyName { get; set; }
            public double NetAmount { get; set; }
            public bool Selected { get; set; }
        }
    }
}