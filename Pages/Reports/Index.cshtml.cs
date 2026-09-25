using HmisWeb.Data;
using HmisWeb.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Reports
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public int TotalPatients { get; set; }
        public int TotalStaff { get; set; }
        public int Inpatients { get; set; }
        public int TodayBills { get; set; }
        public double TodaySales { get; set; }
        public double TodayCash { get; set; }
        public List<TopItem> TopItems { get; set; } = new();
        public List<BillSummary> RecentBills { get; set; } = new();

        public class TopItem { public string ItemName { get; set; } public double Qty { get; set; } public double Total { get; set; } }
        public class BillSummary { public string BillNo { get; set; } public string Patient { get; set; } public string BillType { get; set; } public double Total { get; set; } public double NetTotal { get; set; } public DateTime? BillDate { get; set; } }

        public async Task OnGetAsync()
        {
            var today = DateTime.Today;
            TotalPatients = await _db.Patients.CountAsync(p => !p.Retired);
            TotalStaff = await _db.Staffs.CountAsync(s => !s.Retired);
            Inpatients = await _db.PatientEncounters.CountAsync(e => !e.Retired && e.DateOfDischarge == null);

            var bills = await _db.Bills
                .Include(b => b.Patient).ThenInclude(p => p.Person)
                .Where(b => !b.Retired && !b.Cancelled && b.BillDate != null && b.BillDate.Value.Date == today)
                .ToListAsync();

            TodayBills = bills.Count;
            TodaySales = bills.Sum(b => b.NetTotal);
            TodayCash = await _db.Payments
                .Where(p => !p.Retired && p.CreatedAt != null && p.CreatedAt.Value.Date == today)
                .SumAsync(p => p.PaidValue);

            var billItems = await _db.BillItems
                .Include(bi => bi.Item)
                .Include(bi => bi.Bill)
                .Where(bi => !bi.Retired && bi.Item != null &&
                             bi.Bill != null && !bi.Bill.Retired && !bi.Bill.Cancelled &&
                             bi.Bill.BillDate != null && bi.Bill.BillDate.Value.Date == today)
                .ToListAsync();

            TopItems = billItems
                .GroupBy(bi => bi.Item.Name)
                .Select(g => new TopItem { ItemName = g.Key ?? "Unknown", Qty = g.Sum(x => x.Qty ?? 0), Total = g.Sum(x => x.NetValue) })
                .OrderByDescending(x => x.Total)
                .Take(10)
                .ToList();

            RecentBills = bills
                .OrderByDescending(b => b.BillDate)
                .Take(20)
                .Select(b => new BillSummary
                {
                    BillNo = "B-" + b.Id,
                    Patient = b.Patient?.Person?.Name ?? "Walk-In",
                    BillType = b.BillType?.ToString() ?? "-",
                    Total = b.Total,
                    NetTotal = b.NetTotal,
                    BillDate = b.BillDate
                })
                .ToList();
        }
    }
}