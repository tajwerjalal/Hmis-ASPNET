using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Opd
{
    public class PreBillingModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ICommonService _common;
        private readonly IAppSession _appSession;

        public PreBillingModel(ApplicationDbContext db, ICommonService common, IAppSession appSession)
        {
            _db = db;
            _common = common;
            _appSession = appSession;
        }

        private const string CartKey = OpdCartHelper.PreBillCartKey;
        private const string PatientKey = OpdCartHelper.PreBillPatientKey;

        [BindProperty]
        public string PatientSearch { get; set; }

        [BindProperty]
        public string ItemSearch { get; set; }

        [BindProperty]
        public long? SelectedPatientId { get; set; }

        [BindProperty]
        public long? SelectedItemId { get; set; }

        [BindProperty]
        public long? ItemToRemove { get; set; }

        [BindProperty]
        public List<OpdCartItem> Cart { get; set; } = new();

        [BindProperty]
        public long? ReferringDoctorId { get; set; }

        [BindProperty]
        public long? ReferringInstitutionId { get; set; }

        [BindProperty]
        public long? PaymentSchemeId { get; set; }

        [BindProperty]
        public string Comments { get; set; }

        public Patient SelectedPatient { get; set; }
        public List<Patient> PatientResults { get; set; } = new();
        public List<Item> ItemResults { get; set; } = new();
        public List<OpdCartItem> DisplayCart { get; set; } = new();
        public double Total { get; set; }
        public double DiscountTotal { get; set; }
        public double NetTotal { get; set; }
        public string Message { get; set; }

        public List<SelectListItem> ReferringDoctorList { get; set; } = new();
        public List<SelectListItem> ReferringInstitutionList { get; set; } = new();
        public List<SelectListItem> PaymentSchemeList { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadStateAsync();
        }

        public async Task<IActionResult> OnPostSearchPatientsAsync()
        {
            await LoadStateAsync();
            PatientResults = await _common.SearchPatientsAsync(PatientSearch);
            return Page();
        }

        public async Task<IActionResult> OnPostSelectPatientAsync()
        {
            if (SelectedPatientId == null)
                return RedirectToPage();

            var patient = await _db.Patients
                .Include(p => p.Person)
                .FirstOrDefaultAsync(p => p.Id == SelectedPatientId && !p.Retired);
            if (patient == null)
                return RedirectToPage();

            OpdCartHelper.SavePatientId(HttpContext.Session, PatientKey, patient.Id);
            TempData["OpdMessage"] = "Patient added to the pre-bill.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSearchItemsAsync()
        {
            await LoadStateAsync();
            await SearchItemsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAddItemAsync()
        {
            if (SelectedItemId == null)
                return RedirectToPage();

            var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == SelectedItemId && !i.Retired);
            if (item == null)
                return RedirectToPage();

            var name = string.IsNullOrWhiteSpace(item.PrintName) ? item.Name : item.PrintName;
            var patientId = OpdCartHelper.GetPatientId(HttpContext.Session, PatientKey);
            var companyId = await ResolvePatientCompanyIdAsync(patientId);
            var rate = await GetItemRateAsync(item, companyId);

            var cart = OpdCartHelper.GetCart(HttpContext.Session, CartKey);
            var existing = cart.FirstOrDefault(c => c.ItemId == item.Id);
            if (existing != null)
            {
                existing.Qty += 1;
                existing.Rate = rate;
            }
            else
            {
                cart.Add(new OpdCartItem
                {
                    ItemId = item.Id,
                    Code = item.Code,
                    ItemName = name,
                    Qty = 1,
                    Rate = rate,
                    Discount = 0
                });
            }

            OpdCartHelper.SaveCart(HttpContext.Session, CartKey, cart);
            TempData["OpdMessage"] = $"{name} added to the cart.";
            return RedirectToPage();
        }

        public IActionResult OnPostUpdateCartAsync()
        {
            if (ItemToRemove.HasValue)
            {
                var cart = OpdCartHelper.GetCart(HttpContext.Session, CartKey);
                cart.RemoveAll(c => c.ItemId == ItemToRemove.Value);
                OpdCartHelper.SaveCart(HttpContext.Session, CartKey, cart);
                return RedirectToPage();
            }

            var rows = Cart ?? new List<OpdCartItem>();
            foreach (var r in rows)
            {
                if (r.Qty <= 0) r.Qty = 1;
                if (r.Rate < 0) r.Rate = 0;
                if (r.Discount < 0) r.Discount = 0;
                if (r.Discount > r.Qty * r.Rate) r.Discount = r.Qty * r.Rate;
            }
            rows = rows.Where(r => r.ItemId > 0).ToList();
            OpdCartHelper.SaveCart(HttpContext.Session, CartKey, rows);
            return RedirectToPage();
        }

        public IActionResult OnPostClearAsync()
        {
            OpdCartHelper.Clear(HttpContext.Session, CartKey, PatientKey);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            var patientId = OpdCartHelper.GetPatientId(HttpContext.Session, PatientKey);
            var cart = OpdCartHelper.GetCart(HttpContext.Session, CartKey);
            if (patientId == null)
            {
                TempData["OpdMessage"] = "Please select a patient before saving.";
                return RedirectToPage();
            }
            if (!cart.Any())
            {
                TempData["OpdMessage"] = "Add at least one item before saving.";
                return RedirectToPage();
            }

            var patient = await _db.Patients
                .Include(p => p.Person)
                .FirstOrDefaultAsync(p => p.Id == patientId.Value && !p.Retired);
            if (patient == null)
                return RedirectToPage();

            double total = Math.Round(cart.Sum(c => c.GrossValue), 2);
            double discount = Math.Round(cart.Sum(c => c.Discount), 2);
            double netTotal = Math.Round(total - discount, 2);

            var bill = new Bill
            {
                BillClassType = BillClassType.PreBill,
                BillType = BillType.OpdPreBill,
                PatientId = patient.Id,
                PersonId = patient.PersonId,
                InstitutionId = _appSession.LoggedSite?.Id,
                DepartmentId = _appSession.LoggedDepartment?.Id,
                CreaterId = _appSession.WebUserId,
                CreatedAt = DateTime.Now,
                BillDate = DateTime.Today,
                GrantTotal = total,
                Total = total,
                Discount = discount,
                NetTotal = netTotal,
                ReferredById = ReferringDoctorId,
                ReferredByInstitutionId = ReferringInstitutionId,
                PaymentSchemeId = PaymentSchemeId,
                Comments = Comments,
                Paid = false,
                BillPaymentCompletelySettled = false
            };
            _db.Bills.Add(bill);
            await _db.SaveChangesAsync();

            int serial = 1;
            foreach (var c in cart)
            {
                var gross = Math.Round(c.Qty * c.Rate, 2);
                var lineDiscount = c.Discount > 0 ? Math.Round(c.Discount, 2) : Math.Round(gross - c.NetValue, 2);
                _db.BillItems.Add(new BillItem
                {
                    BillId = bill.Id,
                    ItemId1 = c.ItemId,
                    Qty = c.Qty,
                    Rate = c.Rate,
                    NetRate = c.Rate,
                    GrossValue = gross,
                    Discount = lineDiscount,
                    NetValue = Math.Round(gross - lineDiscount, 2),
                    SearialNo = serial++,
                    CreatedAt = DateTime.Now,
                    CreaterId = _appSession.WebUserId
                });
            }
            await _db.SaveChangesAsync();

            OpdCartHelper.Clear(HttpContext.Session, CartKey, PatientKey);
            TempData["Success"] = $"Pre-bill {bill.Id} saved. Proceed to settlement.";
            return RedirectToPage("/Opd/Settlement", new { billId = bill.Id });
        }

        private async Task LoadStateAsync()
        {
            await LoadLookupsAsync();
            SelectedPatient = await GetSelectedPatientAsync();
            DisplayCart = await BuildDisplayCartAsync();
            Cart = DisplayCart;
            ComputeTotals();
            if (TempData["OpdMessage"] != null)
                Message = TempData["OpdMessage"].ToString();
        }

        private async Task<Patient> GetSelectedPatientAsync()
        {
            var id = OpdCartHelper.GetPatientId(HttpContext.Session, PatientKey);
            if (id == null) return null;
            return await _db.Patients
                .Include(p => p.Person)
                .FirstOrDefaultAsync(p => p.Id == id && !p.Retired);
        }

        private async Task<List<OpdCartItem>> BuildDisplayCartAsync()
        {
            var cart = OpdCartHelper.GetCart(HttpContext.Session, CartKey);
            if (cart.Any())
            {
                var ids = cart.Select(c => c.ItemId).Distinct().ToList();
                var names = await _db.Items
                    .Where(i => ids.Contains(i.Id))
                    .ToDictionaryAsync(i => i.Id, i => string.IsNullOrWhiteSpace(i.PrintName) ? i.Name : i.PrintName);
                foreach (var c in cart)
                {
                    if (names.TryGetValue(c.ItemId, out var n))
                        c.ItemName = n;
                }
            }
            return cart;
        }

        private void ComputeTotals()
        {
            Total = Math.Round(DisplayCart.Sum(c => c.GrossValue), 2);
            DiscountTotal = Math.Round(DisplayCart.Sum(c => c.Discount), 2);
            NetTotal = Math.Round(Total - DiscountTotal, 2);
        }

        private async Task SearchItemsAsync()
        {
            var q = _db.Items.Where(i => !i.Retired && i.Billable
                && (i.ItemType == ItemType.Service || i.ItemType == ItemType.Investigation));

            if (!string.IsNullOrWhiteSpace(ItemSearch))
            {
                var term = ItemSearch.Trim();
                q = q.Where(i => (i.Name != null && i.Name.Contains(term))
                    || (i.Sname != null && i.Sname.Contains(term))
                    || (i.PrintName != null && i.PrintName.Contains(term))
                    || (i.Code != null && i.Code.Contains(term)));
            }

            ItemResults = await q.OrderBy(i => i.Name).Take(50).ToListAsync();
        }

        private async Task<double> GetItemRateAsync(Item item, long? companyId = null)
        {
            return await _common.GetAppliedRateAsync(item, companyId);
        }

        private async Task<long?> ResolvePatientCompanyIdAsync(long? patientId)
        {
            if (patientId == null) return null;
            var personId = await _db.Patients
                .Where(p => p.Id == patientId.Value && !p.Retired)
                .Select(p => p.PersonId)
                .FirstOrDefaultAsync();
            return await _common.GetPatientCompanyIdAsync(personId ?? 0);
        }

        private async Task LoadLookupsAsync()
        {
            ReferringDoctorList = await _db.Doctors
                .Include(d => d.Person)
                .Where(d => !d.Retired)
                .OrderBy(d => d.Person != null ? d.Person.Name : null)
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Person != null
                        ? (d.Person.Surname ?? d.Person.Name ?? ("Doctor #" + d.Id))
                        : ("Doctor #" + d.Id)
                })
                .ToListAsync();

            ReferringInstitutionList = await _db.Institutions
                .Where(i => !i.Retired)
                .OrderBy(i => i.Name)
                .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = i.Name ?? ("Institution #" + i.Id) })
                .ToListAsync();

            PaymentSchemeList = await _db.PaymentSchemes
                .Where(p => !p.Retired)
                .OrderBy(p => p.Name)
                .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Name ?? ("Scheme #" + p.Id) })
                .ToListAsync();
        }
    }
}