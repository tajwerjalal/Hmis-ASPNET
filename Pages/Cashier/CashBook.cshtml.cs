using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Cashier
{
    public class CashBookModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;
        public CashBookModel(ApplicationDbContext db, IAppSession session) { _db = db; _session = session; }

        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; }
        [BindProperty(SupportsGet = true)] public string TypeFilter { get; set; } = "B"; // R/P/B

        public List<CashHistory> Entries { get; set; } = new();
        public decimal ReceiptTotal { get; set; }
        public decimal PaymentTotal { get; set; }

        public async Task OnGetAsync()
        {
            if (FromDate == null) FromDate = DateTime.Today.AddDays(-30);
            if (ToDate == null) ToDate = DateTime.Today;
            var dept = _session.LoggedDepartment;
            var ccode = dept?.Code ?? "";

            var q = _db.CashHistories
                .Include(h => h.Details)
                .Where(h => h.VouDate >= FromDate && h.VouDate <= ToDate);
            if (TypeFilter == "R") q = q.Where(h => h.TrType == "R");
            else if (TypeFilter == "P") q = q.Where(h => h.TrType == "P");

            Entries = await q.OrderBy(h => h.VouDate).ThenBy(h => h.VouNo).ToListAsync();
            ReceiptTotal = Entries.Where(e => e.TrType == "R").Sum(e => e.TotalAmt);
            PaymentTotal = Entries.Where(e => e.TrType == "P").Sum(e => e.TotalAmt);
        }
    }
}