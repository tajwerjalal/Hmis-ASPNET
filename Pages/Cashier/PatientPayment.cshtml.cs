using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Cashier
{
    public class PatientPaymentModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;
        public PatientPaymentModel(ApplicationDbContext db, IAppSession session) { _db = db; _session = session; }

        [BindProperty(SupportsGet = true)] public long? RegNo { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; }

        public Patient Patient { get; set; }
        public List<CashHistory> Entries { get; set; } = new();

        public async Task OnGetAsync()
        {
            if (FromDate == null) FromDate = DateTime.Today.AddDays(-30);
            if (ToDate == null) ToDate = DateTime.Today;
            if (RegNo.HasValue)
            {
                Patient = await _db.Patients.FirstOrDefaultAsync(p => p.Phn == RegNo.Value.ToString());
                Entries = await _db.CashHistories
                    .Where(h => h.RegNo == RegNo && h.VouDate >= FromDate && h.VouDate <= ToDate)
                    .OrderBy(h => h.VouDate).ThenBy(h => h.VouNo).ToListAsync();
            }
        }
    }
}