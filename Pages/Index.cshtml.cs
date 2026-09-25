using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;

        public IndexModel(ApplicationDbContext db, IAppSession appSession)
        {
            _db = db;
            _appSession = appSession;
        }

        public WebUser LoggedUser => _appSession.LoggedUser;
        public string InstitutionName { get; set; }
        public string DepartmentName { get; set; }
        public int PatientCount { get; set; }
        public int ActiveInpatientCount { get; set; }
        public int TodayBillCount { get; set; }
        public double TodaySalesValue { get; set; }
        public int PharmacyItemCount { get; set; }
        public List<Bill> RecentBills { get; set; } = new();
        public List<string> ModuleLinks { get; set; } = new();

        public async Task OnGetAsync()
        {
            var user = LoggedUser;
            var site = _appSession.LoggedSite;
            var dept = _appSession.LoggedDepartment;

            InstitutionName = site?.Name ?? user?.Institution?.Name ?? "Hospital";
            DepartmentName = dept?.Name ?? user?.Department?.Name ?? "Main";

            PatientCount = await _db.Patients.CountAsync(p => !p.Retired);
            ActiveInpatientCount = await _db.PatientEncounters.CountAsync(e => !e.Retired && e.DateOfDischarge == null && e.EncounterType == HmisWeb.Data.Enums.EncounterType.Inward);
            TodayBillCount = await _db.Bills.CountAsync(b => !b.Retired && b.BillDate == DateTime.Today);
            TodaySalesValue = await _db.Bills
                .Where(b => !b.Retired && b.BillDate == DateTime.Today)
                .SumAsync(b => b.NetTotal);
            PharmacyItemCount = await _db.Items.CountAsync(i => !i.Retired && i.ItemType == HmisWeb.Data.Enums.ItemType.Medicine);

            RecentBills = await _db.Bills
                .Include(b => b.Patient).ThenInclude(p => p.Person)
                .Where(b => !b.Retired)
                .OrderByDescending(b => b.CreatedAt)
                .Take(8)
                .ToListAsync();

            BuildModules(user);
        }

        private void BuildModules(WebUser user)
        {
            var has = _appSession.HasPrivilege;
            ModuleLinks.Add("/opd/PreBilling:OPD Billing:bi-receipt");
            ModuleLinks.Add("/opd/Settlement:OPD Settlement:bi-check2-square");
            ModuleLinks.Add("/inward/Admission:Inward Admission:bi-hospital");
            ModuleLinks.Add("/pharmacy/Sale:Pharmacy POS:bi-bag-plus");
            ModuleLinks.Add("/pharmacy/Stock:Pharmacy Stock:bi-boxes");
            ModuleLinks.Add("/lab/Investigation:Investigations:bi-droplet");
            ModuleLinks.Add("/channel/Booking:Channel Booking:bi-calendar-check");
            ModuleLinks.Add("/patients/Index:Patient Registration:bi-people");
            ModuleLinks.Add("/hr/Staff:Staff Management:bi-person-badge");
            ModuleLinks.Add("/cash/CashBook:Cash Management:bi-wallet2");
        }
    }
}