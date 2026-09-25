using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;

namespace HmisWeb.Pages.Cash
{
    public class TransactionsModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public TransactionsModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        public List<Payment> Payments { get; set; } = new();
        public double TotalPaidValue { get; set; }

        public async Task OnGetAsync()
        {
            FromDate = FromDate ?? DateTime.Today;
            ToDate = ToDate ?? DateTime.Today;

            var from = FromDate.Value.Date;
            var to = ToDate.Value.Date.AddDays(1);

            Payments = await _db.Payments
                .Include(p => p.Bill)
                .Include(p => p.Institution)
                .Include(p => p.Department)
                .Where(p => !p.Retired && p.WrittenAt >= from && p.WrittenAt < to)
                .OrderByDescending(p => p.WrittenAt)
                .Take(50)
                .ToListAsync();

            TotalPaidValue = await _db.Payments
                .Where(p => !p.Retired && p.WrittenAt >= from && p.WrittenAt < to)
                .SumAsync(p => p.PaidValue);
        }
    }
}