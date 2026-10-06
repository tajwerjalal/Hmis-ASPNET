using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Us
{
    public class IndentModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ICommonService _common;
        private readonly IAppSession _appSession;
        private readonly IUsBillingMirrorService _mirrors;

        public IndentModel(ApplicationDbContext db, ICommonService common, IAppSession appSession,
            IUsBillingMirrorService mirrors)
        {
            _db = db;
            _common = common;
            _appSession = appSession;
            _mirrors = mirrors;
        }

        private const string CartKey = "UsIndentCart";
        private const string PatientKey = "UsIndentPatientId";

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
        public string ServiceSearch { get; set; }

        [BindProperty]
        public long? SelectedServiceId { get; set; }

        [BindProperty]
        public long? ItemToRemove { get; set; }

        [BindProperty]
        public List<IndentCartItem> Cart { get; set; } = new();

        [BindProperty]
        public long? SelectedEncounterId { get; set; }

        [BindProperty]
        public string Comments { get; set; }

        public Patient SelectedPatient { get; set; }
        public List<Patient> PatientResults { get; set; } = new();
        public List<UsService> ServiceResults { get; set; } = new();
        public List<IndentCartItem> DisplayCart { get; set; } = new();
        public List<PatientEncounter> ActiveEncounters { get; set; } = new();
        public PatientEncounter SelectedEncounter { get; set; }
        public List<Bill> UnpaidBills { get; set; } = new();
        public bool HasUnpaidBills => UnpaidBills.Any();
        public double Total { get; set; }
        public double DiscountTotal { get; set; }
        public double NetTotal { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string Success { get; set; }

        public async Task OnGetAsync()
        {
            await LoadStateAsync();
            if (TempData["IndentSuccess"] != null)
                Success = TempData["IndentSuccess"].ToString();
            if (TempData["IndentMessage"] != null)
                Message = TempData["IndentMessage"].ToString();
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
                .FirstOrDefaultAsync(p => p.Id == SelectedPatientId.Value && !p.Retired);
            if (patient == null)
                return RedirectToPage();

            HttpContext.Session.SetString(PatientKey, patient.Id.ToString());
            return RedirectToPage("/Us/Indent");
        }

        public IActionResult OnPostStartNewAsync()
        {
            HttpContext.Session.Remove(PatientKey);
            HttpContext.Session.Remove(CartKey);
            return RedirectToPage("/Us/Indent");
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
            return RedirectToPage("/Us/Indent");
        }

        public async Task<IActionResult> OnPostSearchServicesAsync()
        {
            await LoadStateAsync();
            await SearchServicesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAddServiceAsync()
        {
            await LoadStateAsync();
            if (SelectedServiceId == null)
                return Page();

            var departmentId = _appSession.LoggedDepartment?.Id;
            var service = await _db.UsServices.FirstOrDefaultAsync(s => s.Id == SelectedServiceId.Value && !s.Retired && s.Active
                && (departmentId == null || s.DepartmentId == departmentId));
            if (service == null)
                return Page();

            var patientId = GetSelectedPatientId();
            var companyId = patientId.HasValue
                ? await _common.GetPatientCompanyIdAsync(
                    await _db.Patients.Where(p => p.Id == patientId.Value && !p.Retired).Select(p => p.PersonId).FirstOrDefaultAsync() ?? 0)
                : null;
            var rate = await _common.GetAppliedRateAsync(service, companyId);
            var name = string.IsNullOrWhiteSpace(service.PrintName) ? service.Name : service.PrintName;
            var cart = GetCart();
            var existing = cart.FirstOrDefault(c => c.ItemId == service.Id);
            if (existing != null)
            {
                existing.Qty += 1;
            }
            else
            {
                cart.Add(new IndentCartItem
                {
                    ItemId = service.Id,
                    Code = service.Code,
                    ItemName = name,
                    Qty = 1,
                    Rate = rate,
                    Discount = 0
                });
            }
            SaveCart(cart);
            TempData["IndentMessage"] = $"{name} added to the indent.";
            return RedirectToPage("/Us/Indent");
        }

        public IActionResult OnPostUpdateCartAsync()
        {
            if (ItemToRemove.HasValue)
            {
                var cart = GetCart();
                cart.RemoveAll(c => c.ItemId == ItemToRemove.Value);
                SaveCart(cart);
                return RedirectToPage("/Us/Indent");
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
            return RedirectToPage("/Us/Indent");
        }

        public IActionResult OnPostClearAsync()
        {
            HttpContext.Session.Remove(CartKey);
            return RedirectToPage("/Us/Indent");
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
                Message = "Add at least one ultrasound service before saving.";
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
            double netTotal = Math.Round(total - itemDiscount, 2);
            if (netTotal < 0) netTotal = 0;

            var now = DateTime.Now;
            var userId = _appSession.WebUserId;
            var site = _appSession.LoggedSite;
            var dept = _appSession.LoggedDepartment ?? await _db.Departments
                .Where(d => !d.Retired && d.DepartmentType == DepartmentType.Laboratory)
                .FirstOrDefaultAsync();

            bool ipd = encounter != null;

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
                Discount = itemDiscount,
                NetTotal = netTotal,
                Balance = netTotal,
                Paid = false,
                BillPaymentCompletelySettled = false,
                PaymentMethod = null,
                TenderedAmount = 0,
                CashPaid = 0,
                CashBalance = 0,
                ReferenceNumber = ipd ? encounter.BhtNo : null,
                Comments = "Ultra-Sound service indent. " + (Comments ?? string.Empty)
            };

            // BILLITEM.ItemId1 is FK-bound to ITEM, not to USSERVICE. Resolve the billing
            // item for each service before creating anything, and report a missing mirror
            // as a readable message instead of a raw FK violation.
            Dictionary<long, long> billingItemIds;
            try
            {
                billingItemIds = await ResolveBillingItemIdsAsync(cart);
            }
            catch (InvalidOperationException ex)
            {
                Message = ex.Message;
                return Page();
            }

            _db.Bills.Add(bill);
            await _db.SaveChangesAsync();

            int serial = 1;
            foreach (var c in cart)
            {
                var gross = Math.Round(c.Qty * c.Rate, 2);
                var lineDiscount = c.Discount > 0 ? Math.Round(c.Discount, 2) : Math.Round(gross - c.NetValue, 2);
                bill.BillItems.Add(new BillItem
                {
                    ItemId1 = billingItemIds[c.ItemId],
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

            await CreateUsOrdersAsync(bill, cart, patient, encounter, now, userId, site?.Id);

            TempData["IndentSuccess"] = ipd
                ? $"US Indent #{bill.Id} saved to IPD bill of #{encounter.Id} ({encounter.BhtNo}). Amount {netTotal:N2} added to patient account."
                : $"US Bill #{bill.Id} created as UNPAID. Patient must pay at Cashier. Amount due: {netTotal:N2}.";

            return RedirectToPage("/Us/Indent");
        }

        /// <summary>
        /// Maps each USSERVICE id in the cart to its mirrored ITEM row. Services are mirrored
        /// as INV-US-{code}; a missing mirror is created on the spot so setup stays the single
        /// source of truth instead of the user hitting a raw FK violation.
        /// </summary>
        private async Task<Dictionary<long, long>> ResolveBillingItemIdsAsync(List<IndentCartItem> cart)
        {
            var serviceIds = cart.Select(c => c.ItemId).Where(id => id > 0).Distinct().ToList();

            var services = await _db.UsServices
                .Where(s => serviceIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Code, s.Name })
                .ToDictionaryAsync(s => s.Id);

            await _mirrors.EnsureMirrorsAsync(serviceIds);

            var codes = services.Values
                .Where(s => !string.IsNullOrWhiteSpace(s.Code))
                .Select(s => "INV-US-" + s.Code)
                .ToList();

            var items = await _db.Items
                .Where(i => codes.Contains(i.Code) && !i.Retired)
                .Select(i => new { i.Id, i.Code })
                .ToDictionaryAsync(i => i.Code, i => i.Id);

            var map = new Dictionary<long, long>();
            var missing = new List<string>();

            foreach (var id in serviceIds)
            {
                if (!services.TryGetValue(id, out var service)) continue;

                var code = string.IsNullOrWhiteSpace(service.Code) ? null : "INV-US-" + service.Code;
                if (code != null && items.TryGetValue(code, out var itemId))
                {
                    map[id] = itemId;
                }
                else
                {
                    missing.Add($"{service.Code ?? service.Name} (service #{id})");
                }
            }

            if (missing.Count > 0)
            {
                throw new InvalidOperationException(
                    "These ultrasound services cannot be billed yet: "
                    + string.Join(", ", missing)
                    + ". Give the service a code in Ultrasound Setup, then activate it again to create its item.");
            }

            return map;
        }

        /// <summary>
        /// Billing alone leaves the scan room with nothing to work from, so every indented
        /// line also becomes an ultrasound order that Scan Collection Entry can pick up.
        /// </summary>
        private async Task CreateUsOrdersAsync(Bill bill, List<IndentCartItem> cart, Patient patient,
            PatientEncounter encounter, DateTime now, long? userId, long? institutionId)
        {
            var serviceIds = cart.Select(c => c.ItemId).Where(id => id > 0).Distinct().ToList();
            if (serviceIds.Count == 0)
                return;

            var services = await _db.UsServices
                .Where(s => serviceIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id);

            foreach (var c in cart)
            {
                if (c.ItemId <= 0) continue;

                services.TryGetValue(c.ItemId, out var service);

                _db.UsOrders.Add(new UsOrder
                {
                    PatientId = patient.Id,
                    PatientEncounterId = encounter?.Id,
                    UsServiceId = c.ItemId,
                    InstitutionId = institutionId,
                    DepartmentId = service?.DepartmentId,
                    BillId = bill.Id,
                    Qty = c.Qty,
                    OrderName = string.IsNullOrWhiteSpace(c.ItemName) ? (service?.Name ?? c.ItemName) : c.ItemName,
                    Instructions = service?.Requirements ?? string.Empty,
                    Ordered = true,
                    SampleCollected = false,
                    Completed = false,
                    Retired = false,
                    CreaterId = userId,
                    CreatedAt = now
                });
            }

            await _db.SaveChangesAsync();
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
            NetTotal = Math.Round(Total - DiscountTotal, 2);
            if (NetTotal < 0) NetTotal = 0;
        }

        private async Task SearchServicesAsync()
        {
            var term = ServiceSearch?.Trim();
            var departmentId = _appSession.LoggedDepartment?.Id;
            var q = _db.UsServices.Where(s => !s.Retired && s.Active && s.Billable);

            if (departmentId != null)
                q = q.Where(s => s.DepartmentId == departmentId);

            if (!string.IsNullOrWhiteSpace(term))
            {
                q = q.Where(s => (s.Name != null && s.Name.Contains(term))
                    || (s.Sname != null && s.Sname.Contains(term))
                    || (s.PrintName != null && s.PrintName.Contains(term))
                    || (s.Code != null && s.Code.Contains(term)));
            }

            ServiceResults = await q.OrderBy(s => s.Name).Take(50).ToListAsync();
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
                .FirstOrDefaultAsync(p => p.Id == id.Value && !p.Retired);
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
            // Ensure we have a US department
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