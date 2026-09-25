using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Lab
{
    public class IndentModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ICommonService _common;
        private readonly IAppSession _appSession;

        public IndentModel(ApplicationDbContext db, ICommonService common, IAppSession appSession)
        {
            _db = db;
            _common = common;
            _appSession = appSession;
        }

        private const string CartKey = "LabIndentCart";
        private const string PatientKey = "LabIndentPatientId";

        [BindProperty]
        public string PatientSearch { get; set; }

        [BindProperty]
        public string NewName { get; set; }

        [BindProperty]
        public string NewNameWithInitials { get; set; }

        [BindProperty]
        public string NewNic { get; set; }

        [BindProperty]
        public string NewMobile { get; set; }

        [BindProperty]
        public string NewPhone { get; set; }

        [BindProperty]
        public Sex? NewSex { get; set; }

        [BindProperty]
        public long? SelectedPatientId { get; set; }

        [BindProperty]
        public string ItemSearch { get; set; }

        [BindProperty]
        public long? SelectedItemId { get; set; }

        [BindProperty]
        public long? ItemToRemove { get; set; }

        [BindProperty]
        public List<IndentCartItem> Cart { get; set; } = new();

        [BindProperty]
        public long? SelectedEncounterId { get; set; }

        [BindProperty]
        public long? DepartmentId { get; set; }

        [BindProperty]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        [BindProperty]
        public double TenderedAmount { get; set; }

        [BindProperty]
        public double BillDiscount { get; set; }

        [BindProperty]
        public string Comments { get; set; }

        public Patient SelectedPatient { get; set; }
        public List<Patient> PatientResults { get; set; } = new();
        public List<Item> ItemResults { get; set; } = new();
        public List<IndentCartItem> DisplayCart { get; set; } = new();
        public List<PatientEncounter> ActiveEncounters { get; set; } = new();
        public PatientEncounter SelectedEncounter { get; set; }
        public List<Department> Departments { get; set; } = new();
        public List<Bill> UnpaidBills { get; set; } = new();
        public bool HasUnpaidBills => UnpaidBills.Any();
        public double Total { get; set; }
        public double DiscountTotal { get; set; }
        public double NetTotal { get; set; }
        public double CashBalance { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string Success { get; set; }
        public List<SelectListItem> PaymentMethodList { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadStateAsync();
            if (TempData["IndentSuccess"] != null)
                Success = TempData["IndentSuccess"].ToString();
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

            HttpContext.Session.SetString(PatientKey, patient.Id.ToString());
            return RedirectToPage("/Lab/Indent");
        }

        public IActionResult OnPostStartNewAsync()
        {
            HttpContext.Session.Remove(PatientKey);
            HttpContext.Session.Remove(CartKey);
            return RedirectToPage("/Lab/Indent");
        }

        public async Task<IActionResult> OnPostCreatePatientAsync()
        {
            if (string.IsNullOrWhiteSpace(NewName))
            {
                ErrorMessage = "Patient name is required.";
                return Page();
            }

            var person = new Person
            {
                Name = NewName.Trim(),
                NameWithInitials = string.IsNullOrWhiteSpace(NewNameWithInitials) ? NewName.Trim() : NewNameWithInitials.Trim(),
                Nic = NewNic?.Trim() ?? "",
                Mobile = NewMobile?.Trim() ?? "",
                Phone = NewPhone?.Trim() ?? "",
                Sex = NewSex,
                CreatedAt = DateTime.Now,
                CreaterId = _appSession.WebUserId
            };
            _db.Persons.Add(person);
            await _db.SaveChangesAsync();

            long? maxPatientId = await _db.Patients.MaxAsync(p => (long?)p.PatientId);
            var patient = new Patient
            {
                PersonId = person.Id,
                PatientId = (maxPatientId ?? 0) + 1,
                CreatedAt = DateTime.Now,
                CreaterId = _appSession.WebUserId,
                CreatedInstitutionId = _appSession.LoggedSite?.Id
            };
            _db.Patients.Add(patient);
            await _db.SaveChangesAsync();

            HttpContext.Session.Remove(CartKey);
            HttpContext.Session.SetString(PatientKey, patient.Id.ToString());
            return RedirectToPage("/Lab/Indent");
        }

        public async Task<IActionResult> OnPostSearchItemsAsync()
        {
            await LoadStateAsync();
            await SearchItemsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAddItemAsync()
        {
            await LoadStateAsync();
            if (SelectedItemId == null)
                return Page();

            var departmentId = DepartmentId ?? _appSession.LoggedDepartment?.Id;
            var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == SelectedItemId && !i.Retired && i.Billable
                && (i.ItemType == ItemType.Investigation || i.ItemType == ItemType.Service)
                && (departmentId == null || i.DepartmentId == departmentId));
            if (item == null)
                return Page();

            var patientId = GetSelectedPatientId();
            var companyId = patientId.HasValue
                ? await _common.GetPatientCompanyIdAsync(
                    await _db.Patients.Where(p => p.Id == patientId.Value && !p.Retired).Select(p => p.PersonId).FirstOrDefaultAsync() ?? 0)
                : null;
            var rate = await _common.GetAppliedRateAsync(item, companyId);
            var name = string.IsNullOrWhiteSpace(item.PrintName) ? item.Name : item.PrintName;
            var cart = GetCart();
            var existing = cart.FirstOrDefault(c => c.ItemId == item.Id);
            if (existing != null)
            {
                existing.Qty += 1;
            }
            else
            {
                cart.Add(new IndentCartItem
                {
                    ItemId = item.Id,
                    Code = item.Code,
                    ItemName = name,
                    Qty = 1,
                    Rate = rate,
                    Discount = 0
                });
            }
            SaveCart(cart);
            TempData["IndentMessage"] = $"{name} added to the indent.";
            return RedirectToPage("/Lab/Indent");
        }

        public IActionResult OnPostUpdateCartAsync()
        {
            if (ItemToRemove.HasValue)
            {
                var cart = GetCart();
                cart.RemoveAll(c => c.ItemId == ItemToRemove.Value);
                SaveCart(cart);
                return RedirectToPage("/Lab/Indent");
            }

            var rows = Cart ?? new List<IndentCartItem>();
            foreach (var r in rows)
            {
                if (r.Qty <= 0) r.Qty = 1;
                if (r.Rate < 0) r.Rate = 0;
                if (r.Discount < 0) r.Discount = 0;
                if (r.Discount > r.Qty * r.Rate) r.Discount = r.Qty * r.Rate;
            }
            rows = rows.Where(r => r.ItemId > 0).ToList();
            SaveCart(rows);
            return RedirectToPage("/Lab/Indent");
        }

        public IActionResult OnPostClearAsync()
        {
            HttpContext.Session.Remove(CartKey);
            return RedirectToPage("/Lab/Indent");
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            await LoadLookupsAsync();

            var patientId = GetSelectedPatientId();
            var cart = GetCart();
            if (patientId == null)
            {
                Message = "Please select a patient before saving the indent.";
                return Page();
            }
            if (!cart.Any())
            {
                Message = "Add at least one investigation / service before saving.";
                return Page();
            }

            var patient = await _db.Patients
                .Include(p => p.Person)
                .FirstOrDefaultAsync(p => p.Id == patientId.Value && !p.Retired);
            if (patient == null)
            {
                Message = "Selected patient not found.";
                return Page();
            }

            var encounter = await GetSelectedEncounterAsync(patient.Id);

            double total = Math.Round(cart.Sum(c => c.GrossValue), 2);
            double itemDiscount = Math.Round(cart.Sum(c => c.Discount), 2);
            double maxBillDiscount = Math.Max(total - itemDiscount, 0);
            double billDiscount = Math.Round(Math.Min(Math.Max(BillDiscount, 0), maxBillDiscount), 2);
            double netTotal = Math.Round(total - itemDiscount - billDiscount, 2);
            if (netTotal < 0) netTotal = 0;

            var now = DateTime.Now;
            var userId = _appSession.WebUserId;
            var site = _appSession.LoggedSite;
            var dept = DepartmentId.HasValue
                ? await _db.Departments.FirstOrDefaultAsync(d => d.Id == DepartmentId.Value && !d.Retired)
                : (_appSession.LoggedDepartment ?? await _db.Departments.OrderBy(d => d.Id).FirstOrDefaultAsync());

            bool ipd = encounter != null;
            bool isCredit = PaymentMethod == PaymentMethod.Credit;
            bool settled = !isCredit;
            bool cash = PaymentMethod == PaymentMethod.Cash;

            double tendered = ipd ? 0
                : (settled && cash ? Math.Max(TenderedAmount, netTotal) : 0);
            double cashBalance = ipd || !settled || !cash
                ? 0
                : Math.Round(tendered - netTotal, 2);

            var bill = new Bill
            {
                BillType = ipd ? BillType.InwardServiceBill : BillType.OpdBill,
                BillClassType = ipd ? BillClassType.PreBill : BillClassType.BilledBill,
                PatientId = patient.Id,
                PersonId = patient.PersonId,
                PatientEncounterId = ipd ? encounter.Id : null,
                InstitutionId = site?.Id ?? patient.CreatedInstitutionId,
                DepartmentId = dept?.Id,
                CreaterId = userId,
                CreatedAt = now,
                BillDate = now,
                GrantTotal = total,
                Total = total,
                BillTotal = total,
                Discount = itemDiscount + billDiscount,
                NetTotal = netTotal,
                Balance = ipd ? netTotal : (settled ? 0 : netTotal),
                Paid = ipd ? false : settled,
                PaidAt = ipd || !settled ? (DateTime?)null : now,
                BillPaymentCompletelySettled = settled,
                PaymentMethod = ipd ? null : PaymentMethod,
                TenderedAmount = ipd ? 0 : tendered,
                CashPaid = ipd ? 0 : tendered,
                CashBalance = cashBalance,
                ReferenceNumber = ipd ? encounter.BhtNo : null,
                Comments = "Lab service indent. " + (Comments ?? string.Empty)
            };
            _db.Bills.Add(bill);
            await _db.SaveChangesAsync();

            if (!ipd)
            {
                bill.Payments.Add(new Payment
                {
                    PaymentMethod = PaymentMethod,
                    PaidValue = netTotal,
                    WrittenAt = now,
                    Realized = settled,
                    RealizedAt = settled ? (DateTime?)now : null,
                    InstitutionId = site?.Id ?? patient.CreatedInstitutionId,
                    DepartmentId = dept?.Id,
                    CreatedAt = now
                });
            }

            int serial = 1;
            foreach (var c in cart)
            {
                var gross = Math.Round(c.Qty * c.Rate, 2);
                var lineDiscount = c.Discount > 0 ? Math.Round(c.Discount, 2) : Math.Round(gross - c.NetValue, 2);
                bill.BillItems.Add(new BillItem
                {
                    ItemId1 = c.ItemId,
                    Qty = c.Qty,
                    Rate = c.Rate,
                    NetRate = c.Rate,
                    GrossValue = gross,
                    Discount = lineDiscount,
                    NetValue = Math.Round(gross - lineDiscount, 2),
                    SearialNo = serial++,
                    CreatedAt = now,
                    CreaterId = userId
                });
            }
            await _db.SaveChangesAsync();

            HttpContext.Session.Remove(CartKey);

            TempData["IndentSuccess"] = ipd
                ? $"Lab indent #{bill.Id} saved to IPD bill of #{encounter.Id} ({encounter.BhtNo}). Amount {netTotal:N2} added to the patient account."
                : (settled
                    ? $"Lab bill #{bill.Id} settled via {PaymentMethod}. Cash paid {tendered:N2}, change due {cashBalance:N2}."
                    : $"Lab bill #{bill.Id} saved as CREDIT. Not settled.");

            return RedirectToPage("/Lab/Indent");
        }

        private async Task LoadStateAsync()
        {
            await LoadLookupsAsync();
            SelectedPatient = await GetSelectedPatientAsync();
            DisplayCart = GetCart();
            Cart = DisplayCart;
            if (SelectedPatient != null)
            {
                ActiveEncounters = await GetActiveEncountersAsync(SelectedPatient.Id);
                if (SelectedEncounterId.HasValue && ActiveEncounters.Any(e => e.Id == SelectedEncounterId.Value))
                    SelectedEncounter = ActiveEncounters.First(e => e.Id == SelectedEncounterId.Value);
                else if (ActiveEncounters.Any())
                    SelectedEncounter = ActiveEncounters.First();
                else
                    SelectedEncounter = null;

                UnpaidBills = await _db.Bills
                    .Where(b => b.PatientId == SelectedPatient.Id && !b.Retired && !b.Cancelled && !b.Paid)
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync();
            }
            else
            {
                ActiveEncounters = new();
                SelectedEncounter = null;
                UnpaidBills = new();
            }
            ComputeTotals();
            if (TempData["IndentMessage"] != null)
                Message = TempData["IndentMessage"].ToString();
        }

        private void ComputeTotals()
        {
            Total = Math.Round(DisplayCart.Sum(c => c.GrossValue), 2);
            DiscountTotal = Math.Round(DisplayCart.Sum(c => c.Discount), 2);
            NetTotal = Math.Round(Total - DiscountTotal - Math.Min(Math.Max(BillDiscount, 0), Total - DiscountTotal), 2);
            if (NetTotal < 0) NetTotal = 0;
            var tendered = PaymentMethod == PaymentMethod.Cash ? Math.Max(TenderedAmount, NetTotal) : 0;
            CashBalance = Math.Round(tendered - NetTotal, 2);
        }

        private async Task SearchItemsAsync()
        {
            var term = ItemSearch?.Trim();
            var departmentId = DepartmentId ?? _appSession.LoggedDepartment?.Id;
            var q = _db.Items.Where(i => !i.Retired && i.Billable
                && (i.ItemType == ItemType.Investigation || i.ItemType == ItemType.Service));

            if (departmentId != null)
                q = q.Where(i => i.DepartmentId == departmentId);

            if (!string.IsNullOrWhiteSpace(term))
            {
                q = q.Where(i => (i.Name != null && i.Name.Contains(term))
                    || (i.Sname != null && i.Sname.Contains(term))
                    || (i.PrintName != null && i.PrintName.Contains(term))
                    || (i.Code != null && i.Code.Contains(term)));
            }

            ItemResults = await q.OrderBy(i => i.ItemType).ThenBy(i => i.Name).Take(50).ToListAsync();
        }

        private async Task<PatientEncounter> GetSelectedEncounterAsync(long patientId)
        {
            if (!SelectedEncounterId.HasValue) return null;
            var encounters = await GetActiveEncountersAsync(patientId);
            return encounters.FirstOrDefault(e => e.Id == SelectedEncounterId.Value);
        }

        private async Task<List<PatientEncounter>> GetActiveEncountersAsync(long patientId)
        {
            return await _db.PatientEncounters
                .Where(e => e.PatientId == patientId && !e.Retired
                    && e.EncounterType == EncounterType.Inward && e.DateOfDischarge == null)
                .OrderByDescending(e => e.DateOfAdmission)
                .ToListAsync();
        }

        private long? GetSelectedPatientId()
        {
            var val = HttpContext.Session.GetString(PatientKey);
            return long.TryParse(val, out var id) ? id : null;
        }

        private async Task<Patient> GetSelectedPatientAsync()
        {
            var id = GetSelectedPatientId();
            if (id == null) return null;
            return await _db.Patients
                .Include(p => p.Person)
                .FirstOrDefaultAsync(p => p.Id == id && !p.Retired);
        }

        private List<IndentCartItem> GetCart()
        {
            var raw = HttpContext.Session.GetString(CartKey);
            if (string.IsNullOrWhiteSpace(raw)) return new List<IndentCartItem>();
            try
            {
                return JsonSerializer.Deserialize<List<IndentCartItem>>(raw) ?? new List<IndentCartItem>();
            }
            catch
            {
                return new List<IndentCartItem>();
            }
        }

        private void SaveCart(List<IndentCartItem> cart)
        {
            HttpContext.Session.SetString(CartKey, JsonSerializer.Serialize(cart));
        }

        private async Task LoadLookupsAsync()
        {
            Departments = await _db.Departments
                .Where(d => !d.Retired)
                .OrderBy(d => d.Name)
                .ToListAsync();
            if (DepartmentId == null && _appSession.LoggedDepartment != null)
                DepartmentId = _appSession.LoggedDepartment.Id;

            PaymentMethodList = new List<SelectListItem>
            {
                new SelectListItem { Value = "Cash", Text = "Cash" },
                new SelectListItem { Value = "Card", Text = "Card" },
                new SelectListItem { Value = "Cheque", Text = "Cheque" },
                new SelectListItem { Value = "Credit", Text = "Credit" }
            };
        }

        public class IndentCartItem
        {
            public long ItemId { get; set; }
            public string Code { get; set; }
            public string ItemName { get; set; }
            public int Qty { get; set; }
            public double Rate { get; set; }
            public double Discount { get; set; }
            public double GrossValue => Math.Round(Qty * Rate, 2);
            public double NetValue => Math.Round(GrossValue - Discount, 2);
        }
    }
}