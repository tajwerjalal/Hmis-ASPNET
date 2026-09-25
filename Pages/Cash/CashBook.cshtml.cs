using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Cash
{
    public class CashBookModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;

        public CashBookModel(ApplicationDbContext db, IAppSession appSession)
        {
            _db = db;
            _appSession = appSession;
        }

        [BindProperty]
        public string Description { get; set; }

        [BindProperty]
        public double Amount { get; set; }

        public CashBook CashBook { get; set; }
        public Department Department { get; set; }
        public List<CashBookEntry> Entries { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostDepositAsync()
        {
            return await HandleEntryAsync(true);
        }

        public async Task<IActionResult> OnPostWithdrawAsync()
        {
            return await HandleEntryAsync(false);
        }

        private async Task<IActionResult> HandleEntryAsync(bool isDeposit)
        {
            if (Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Amount must be greater than zero.");
                await LoadAsync();
                return Page();
            }

            await LoadAsync();
            if (CashBook == null) return RedirectToPage("/cash/CashBook");

            var now = DateTime.Now;

            if (isDeposit)
            {
                CashBook.CashBalance += Amount;
                CashBook.TotalCashAdded += Amount;
            }
            else
            {
                CashBook.CashBalance -= Amount;
                CashBook.TotalCashRemoved += Amount;
            }

            _db.CashBookEntries.Add(new CashBookEntry
            {
                CashBookId = CashBook.Id,
                Description = Description,
                CashIn = isDeposit ? Amount : 0,
                CashOut = isDeposit ? 0 : Amount,
                CashBalance = CashBook.CashBalance,
                CreatedAt = now,
                InstitutionId = Department?.InstitutionId,
                DepartmentId = Department?.Id,
                CreaterId = _appSession.WebUserId
            });

            await _db.SaveChangesAsync();
            Entries = await GetEntriesAsync(CashBook.Id);
            return Page();
        }

        private async Task LoadAsync()
        {
            Department = _appSession.LoggedDepartment ?? await _db.Departments.OrderBy(d => d.Id).FirstOrDefaultAsync();

            if (Department == null)
            {
                CashBook = null;
                Entries = new List<CashBookEntry>();
                return;
            }

            CashBook = await _db.CashBooks
                .FirstOrDefaultAsync(cb => cb.DepartmentId == Department.Id && !cb.Retired);

            if (CashBook == null)
            {
                CashBook = new CashBook
                {
                    Name = $"Cash Book - {Department.Name}",
                    CashBalance = 0,
                    TotalCashAdded = 0,
                    TotalCashRemoved = 0,
                    InstitutionId = Department.InstitutionId,
                    DepartmentId = Department.Id,
                    CreaterId = _appSession.WebUserId,
                    CreatedAt = DateTime.Now
                };
                _db.CashBooks.Add(CashBook);
                await _db.SaveChangesAsync();
            }

            Entries = await GetEntriesAsync(CashBook.Id);
        }

        private Task<List<CashBookEntry>> GetEntriesAsync(long cashBookId)
        {
            return _db.CashBookEntries
                .Where(e => e.CashBookId == cashBookId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }
    }
}