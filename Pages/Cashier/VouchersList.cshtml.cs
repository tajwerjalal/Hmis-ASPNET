using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Cashier
{
    public class VouchersListModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;
        public VouchersListModel(ApplicationDbContext db, IAppSession session) { _db = db; _session = session; }

        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; }

        public List<CashHistory> Vouchers { get; set; } = new();

        public async Task OnGetAsync()
        {
            if (FromDate == null) FromDate = DateTime.Today.AddDays(-30);
            if (ToDate == null) ToDate = DateTime.Today;
            Vouchers = await _db.CashHistories
                .Where(h => h.VouDate >= FromDate && h.VouDate <= ToDate)
                .OrderBy(h => h.VouDate).ThenBy(h => h.VouNo).ToListAsync();
        }
    }
}