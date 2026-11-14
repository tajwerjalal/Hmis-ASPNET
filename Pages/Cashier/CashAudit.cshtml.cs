using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Cashier
{
    public class CashAuditModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;
        public CashAuditModel(ApplicationDbContext db, IAppSession session) { _db = db; _session = session; }

        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; }

        public List<CashReceipt> Receipts { get; set; } = new();
        public List<CashPayment> Payments { get; set; } = new();
        public decimal ReceiptTotal { get; set; }
        public decimal PaymentTotal { get; set; }

        public async Task OnGetAsync()
        {
            if (FromDate == null) FromDate = DateTime.Today.AddDays(-30);
            if (ToDate == null) ToDate = DateTime.Today;
            var dept = _session.LoggedDepartment;
            var ccode = dept?.Code ?? "";
            var lcode = (int)(dept?.Id ?? 0);

            Receipts = await _db.CashReceipts
                .Where(r => r.CCode == ccode && r.LCode == lcode && r.TrDate >= FromDate && r.TrDate <= ToDate)
                .OrderBy(r => r.TrDate).ThenBy(r => r.TrNo).ToListAsync();
            Payments = await _db.CashPayments
                .Where(p => p.CCode == ccode && p.LCode == lcode && p.TrDate >= FromDate && p.TrDate <= ToDate)
                .OrderBy(p => p.TrDate).ThenBy(p => p.TrNo).ToListAsync();

            ReceiptTotal = Receipts.Sum(r => r.TotalAmt);
            PaymentTotal = Payments.Sum(p => p.TotalAmt);
        }
    }
}