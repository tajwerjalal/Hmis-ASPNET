using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Pharmacy
{
    public class StockRecord
    {
        public string ItemName { get; set; } = "";
        public string BatchNo { get; set; } = "";
        public double Qty { get; set; }
        public double FreeQty { get; set; }
        public double PurchasePrice { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string DepartmentName { get; set; } = "";
    }

    public class StockModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;
        private readonly ICommonService _commonService;

        public StockModel(ApplicationDbContext db, IAppSession appSession, ICommonService commonService)
        {
            _db = db;
            _appSession = appSession;
            _commonService = commonService;
        }

        [BindProperty]
        public long? SelectedDepartmentId { get; set; }

        public string SearchTerm { get; set; }

        public List<Department> Departments { get; set; } = new();
        public List<StockRecord> StockRecords { get; set; } = new();

        public async Task OnGetAsync()
        {
            Departments = await _commonService.GetDepartmentsAsync();
            SelectedDepartmentId = _appSession.LoggedDepartment?.Id;
            if (SelectedDepartmentId != null)
                await LoadStockAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Departments = await _commonService.GetDepartmentsAsync();
            await LoadStockAsync();
            return Page();
        }

        private async Task LoadStockAsync()
        {
            if (SelectedDepartmentId == null) return;

            var query = _db.Stocks
                .Include(s => s.Item)
                .Include(s => s.ItemBatch)
                .Include(s => s.Department)
                .Where(s => s.DepartmentId == SelectedDepartmentId && !s.Retired && s.Item != null);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(s => s.Item != null &&
                    (s.Item.Name.ToLower().Contains(term) || s.Item.PrintName.ToLower().Contains(term)
                        || s.Item.Code.ToLower().Contains(term)));
            }

            StockRecords = await query
                .OrderBy(s => s.Item!.Name)
                .Select(s => new StockRecord
                {
                    ItemName = s.Item != null ? (s.Item.PrintName ?? s.Item.Name) : "",
                    BatchNo = s.ItemBatch != null ? s.ItemBatch.BatchNo ?? "" : "",
                    Qty = s.StockQty ?? 0,
                    FreeQty = s.StockFreeQty ?? 0,
                    PurchasePrice = s.LastPurchaseUnitPrice ?? 0,
                    ExpiryDate = s.ItemBatch != null ? s.ItemBatch.DateOfExpiry : null,
                    DepartmentName = s.Department != null ? s.Department.Name : ""
                })
                .ToListAsync();
        }
    }
}
