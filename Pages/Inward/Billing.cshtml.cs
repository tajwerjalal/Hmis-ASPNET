using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using HmisWeb.Data.Enums;

namespace HmisWeb.Pages.Inward
{
    public class BillingModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;
        private readonly ICommonService _common;

        public BillingModel(ApplicationDbContext db, IAppSession appSession, ICommonService common)
        {
            _db = db;
            _appSession = appSession;
            _common = common;
        }

        [BindProperty(SupportsGet = true)]
        public long? EncounterId { get; set; }

        // Per-encounter cart mode
        [BindProperty]
        public string SearchTerm { get; set; }

        [BindProperty]
        public long? AddItemId { get; set; }

        [BindProperty]
        public string Comments { get; set; }

        public PatientEncounter Encounter { get; set; }
        public List<Item> ItemResults { get; set; } = new();
        public List<CartItem> Cart { get; set; } = new();
        public double CartTotal => Cart.Sum(c => c.Rate * c.Qty);
        public List<Room> Rooms { get; set; } = new();
        public List<Bill> PendingBills { get; set; } = new();
        public double PendingTotal { get; set; }

        // Batch (GENBILLI) mode
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
        public bool BatchMode => EncounterId == null;

        public async Task<IActionResult> OnGetAsync()
        {
            if (BatchMode)
            {
                await LoadCompaniesAsync();
                return Page();
            }

            await LoadEncounterAsync();
            if (Encounter == null) return RedirectToPage("/Inward/PatientList");
            Cart = GetCart();
            return Page();
        }

        public async Task<IActionResult> OnPostSearchAsync()
        {
            await LoadEncounterAsync();
            if (Encounter == null) return RedirectToPage("/Inward/PatientList");
            Cart = GetCart();
            await SearchItemsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            await LoadEncounterAsync();
            if (Encounter == null) return RedirectToPage("/Inward/PatientList");
            Cart = GetCart();

            if (AddItemId != null)
            {
                var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == AddItemId && !i.Retired
                    && (i.ItemType == ItemType.Service || i.ItemType == ItemType.Medicine || i.ItemType == ItemType.Investigation));
                if (item != null)
                {
                    var rate = await _common.GetAppliedRateAsync(item,
                        Encounter?.CreditCompanyId,
                        Encounter?.OpdDoctorId,
                        null);
                    var existing = Cart.FirstOrDefault(c => c.ItemId == item.Id);
                    if (existing != null)
                        existing.Qty += 1;
                    else
                        Cart.Add(new CartItem { ItemId = item.Id, ItemName = item.Name, Rate = rate, Qty = 1 });
                    SetCart(Cart);
                }
            }
            await SearchItemsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateQtyAsync(long itemId, int qty)
        {
            await LoadEncounterAsync();
            if (Encounter == null) return RedirectToPage("/Inward/PatientList");
            Cart = GetCart();
            var line = Cart.FirstOrDefault(c => c.ItemId == itemId);
            if (line != null)
            {
                line.Qty = qty > 0 ? qty : 1;
                SetCart(Cart);
            }
            return Page();
        }

        public async Task<IActionResult> OnPostRemoveAsync(long itemId)
        {
            await LoadEncounterAsync();
            if (Encounter == null) return RedirectToPage("/Inward/PatientList");
            Cart = GetCart();
            Cart.RemoveAll(c => c.ItemId == itemId);
            SetCart(Cart);
            return Page();
        }

        public async Task<IActionResult> OnPostSaveBillAsync()
        {
            await LoadEncounterAsync();
            if (Encounter == null) return RedirectToPage("/Inward/PatientList");
            Cart = GetCart();

            if (!Cart.Any())
            {
                await SearchItemsAsync();
                ModelState.AddModelError("", "Add at least one item to the bill.");
                return Page();
            }

            var now = DateTime.Now;
            var userId = _appSession.WebUserId;
            var site = _appSession.LoggedSite;
            var dept = _appSession.LoggedDepartment ?? await _db.Departments.OrderBy(d => d.Id).FirstOrDefaultAsync();

            var bill = new Bill
            {
                BillType = BillType.InwardBill,
                BillClassType = BillClassType.PreBill,
                PatientEncounterId = Encounter.Id,
                PatientId = Encounter.PatientId,
                PersonId = Encounter.Patient?.PersonId,
                InstitutionId = site?.Id ?? Encounter.InstitutionId,
                DepartmentId = dept?.Id ?? Encounter.DepartmentId,
                Total = CartTotal,
                NetTotal = CartTotal,
                BillTotal = CartTotal,
                BillDate = now,
                CreatedAt = now,
                Paid = false,
                CreaterId = userId,
                Comments = Comments
            };
            _db.Bills.Add(bill);
            await _db.SaveChangesAsync();

            int serial = 1;
            foreach (var line in Cart)
            {
                var value = line.Rate * line.Qty;
                _db.BillItems.Add(new BillItem
                {
                    BillId = bill.Id,
                    ItemId = line.ItemId.ToString(),
                    ItemId1 = line.ItemId,
                    Qty = line.Qty,
                    Rate = line.Rate,
                    NetRate = line.Rate,
                    NetValue = value,
                    GrossValue = value,
                    CreatedAt = now,
                    SearialNo = serial++
                });
            }
            await _db.SaveChangesAsync();

            SetCart(new List<CartItem>());
            TempData["Success"] = $"Inward bill #{bill.Id} saved.";
            return RedirectToPage("/Inward/PatientList");
        }

        public void OnPostClearCart()
        {
            SetCart(new List<CartItem>());
        }

        // ----- Batch (GENBILLI) handlers -----

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

            TempData["Success"] = $"Bills successfully generated ({generated}).";
            await LoadRowsAsync();
            return Page();
        }

        private async Task<bool> GenerateBillAsync(long encounterId)
        {
            var encounter = await _db.PatientEncounters
                .Include(e => e.Patient).ThenInclude(p => p.Person)
                .FirstOrDefaultAsync(e => e.Id == encounterId && !e.Retired
                    && e.EncounterType == EncounterType.Inward);
            if (encounter == null) return false;

            var slips = await _db.Bills
                .Where(b => b.PatientEncounterId == encounterId && !b.Retired && !b.Cancelled
                    && b.BillClosed == false && b.BilledBillId == null
                    && (b.BillClassType == BillClassType.PreBill || b.Balance > 0)
                    && b.BillType != BillType.InwardFinalBill)
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
                BillType = BillType.InwardFinalBill,
                BillClassType = BillClassType.BilledBill,
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
                ReferenceNumber = encounter.BhtNo
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
                        Qty = it.Qty,
                        Rate = it.Rate,
                        NetRate = it.NetRate,
                        GrossValue = it.GrossValue,
                        Discount = it.Discount,
                        NetValue = it.NetValue,
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

            encounter.FinalBillId = bill.Id;
            encounter.GrantTotal = grantTotal;
            encounter.Discount = discount;
            encounter.NetTotal = netTotal;
            encounter.PaymentFinalized = !isCredit;
            if (isCredit && encounter.PaymentMethod == null)
                encounter.PaymentMethod = PaymentMethod.Credit;

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
                    && b.BillType != BillType.InwardFinalBill
                    && b.PatientEncounterId != null
                    && b.PatientEncounter.EncounterType == EncounterType.Inward
                    && b.PatientEncounter.DateOfDischarge >= FromDate
                    && b.PatientEncounter.DateOfDischarge <= ToDate)
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
                    RegNo = encounter.BhtNo ?? encounter.Id.ToString(),
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

        private async Task LoadEncounterAsync()
        {
            Encounter = await _db.PatientEncounters
                .Include(e => e.Patient).ThenInclude(p => p.Person)
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == EncounterId && !e.Retired
                    && e.EncounterType == EncounterType.Inward && e.DateOfDischarge == null);
            Rooms = await _db.Rooms.Where(r => !r.Retired).OrderBy(r => r.Name).ToListAsync();
            if (Encounter != null)
            {
                PendingBills = await _db.Bills
                    .Where(b => b.PatientEncounterId == Encounter.Id && !b.Paid && b.Retired == false)
                    .OrderByDescending(b => b.Id)
                    .ToListAsync();
                PendingTotal = PendingBills.Sum(b => b.Balance);
            }
        }

        private async Task SearchItemsAsync()
        {
            var term = SearchTerm?.Trim();
            if (string.IsNullOrWhiteSpace(term)) return;

            ItemResults = await _db.Items
                .Where(i => !i.Retired
                    && (i.ItemType == ItemType.Service || i.ItemType == ItemType.Medicine || i.ItemType == ItemType.Investigation)
                    && (i.Name.Contains(term) || (i.Sname != null && i.Sname.Contains(term))))
                .OrderBy(i => i.Name)
                .Take(20)
                .ToListAsync();
        }

        private string CartKey => "InwardBillingCart_" + (EncounterId?.ToString() ?? "0");

        private List<CartItem> GetCart()
        {
            var raw = HttpContext.Session.GetString(CartKey);
            if (string.IsNullOrWhiteSpace(raw)) return new List<CartItem>();
            try
            {
                return JsonSerializer.Deserialize<List<CartItem>>(raw) ?? new List<CartItem>();
            }
            catch
            {
                return new List<CartItem>();
            }
        }

        private void SetCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString(CartKey, JsonSerializer.Serialize(cart));
        }

        public class CartItem
        {
            public long ItemId { get; set; }
            public string ItemName { get; set; }
            public double Rate { get; set; }
            public int Qty { get; set; }
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