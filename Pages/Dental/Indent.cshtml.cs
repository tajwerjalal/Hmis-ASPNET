using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Dental
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

        private const string CartKey = "DentalIndentCart";
        private const string PatientKey = "DentalIndentPatientId";

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
        public string Comments { get; set; }

        public Patient SelectedPatient { get; set; }
        public List<Patient> PatientResults { get; set; } = new();
        public List<Item> ItemResults { get; set; } = new();
        public List<IndentCartItem> DisplayCart { get; set; } = new();
        public List<PatientEncounter> Visits { get; set; } = new();
        public PatientEncounter SelectedVisit { get; set; }
        public List<Department> Departments { get; set; } = new();
        public List<IndentCard> PatientIndents { get; set; } = new();
        public bool HasUnpaidIndents => PatientIndents.Any(i => !i.Paid);
        public double Total { get; set; }
        public string Message { get; set; }
        public string Success { get; set; }
        public List<SelectListItem> MethodNames { get; set; } = new();

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
            return RedirectToPage("/Cashier/Indent");
        }

        public IActionResult OnPostStartNewAsync()
        {
            HttpContext.Session.Remove(PatientKey);
            HttpContext.Session.Remove(CartKey);
            return RedirectToPage("/Cashier/Indent");
        }

        public async Task<IActionResult> OnPostCreatePatientAsync()
        {
            if (string.IsNullOrWhiteSpace(NewName))
            {
                Message = "Patient name is required.";
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
            return RedirectToPage("/Cashier/Indent");
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
            var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == SelectedItemId.Value && !i.Retired && i.Billable
                && (i.ItemType == ItemType.Investigation || i.ItemType == ItemType.Service)
                && (departmentId == null || i.DepartmentId == departmentId));
            if (item == null)
                return Page();

            var patient = await GetSelectedPatientAsync();
            var companyId = patient != null
                ? await _common.GetPatientCompanyIdAsync(patient.PersonId ?? 0)
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
                    Rate = rate
                });
            }
            SaveCart(cart);
            TempData["IndentMessage"] = $"{name} added to the indent.";
            return RedirectToPage("/Cashier/Indent");
        }

        public IActionResult OnPostUpdateCartAsync()
        {
            if (ItemToRemove.HasValue)
            {
                var cart = GetCart();
                cart.RemoveAll(c => c.ItemId == ItemToRemove.Value);
                SaveCart(cart);
                return RedirectToPage("/Cashier/Indent");
            }

            var rows = Cart ?? new List<IndentCartItem>();
            foreach (var r in rows)
            {
                if (r.Qty <= 0) r.Qty = 1;
                if (r.Rate < 0) r.Rate = 0;
            }
            rows = rows.Where(r => r.ItemId > 0).ToList();
            SaveCart(rows);
            return RedirectToPage("/Cashier/Indent");
        }

        public IActionResult OnPostClearAsync()
        {
            HttpContext.Session.Remove(CartKey);
            return RedirectToPage("/Cashier/Indent");
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
                Message = "Add at least one procedure / test before saving.";
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

            var now = DateTime.Now;
            var userId = _appSession.WebUserId;
            var site = _appSession.LoggedSite;
            var dept = DepartmentId.HasValue
                ? await _db.Departments.FirstOrDefaultAsync(d => d.Id == DepartmentId.Value && !d.Retired)
                : (_appSession.LoggedDepartment ?? await _db.Departments.OrderBy(d => d.Id).FirstOrDefaultAsync());

            // Visit: every indent is linked to a unique visit number (PatientEncounter.Id).
            PatientEncounter encounter = null;
            if (SelectedEncounterId.HasValue)
                encounter = await _db.PatientEncounters.FirstOrDefaultAsync(e => e.Id == SelectedEncounterId.Value && e.PatientId == patient.Id && !e.Retired);
            if (encounter == null)
            {
                encounter = new OpdVisit
                {
                    PatientId = patient.Id,
                    EncounterType = EncounterType.Opd,
                    EncounterDate = now,
                    EncounterTime = now,
                    Name = patient.Person?.Name,
                    DepartmentId = dept?.Id,
                    InstitutionId = site?.Id,
                    EncounterComments = "Visit raised for department indent",
                    CreatedAt = now,
                    CreaterId = userId
                };
                _db.PatientEncounters.Add(encounter);
                await _db.SaveChangesAsync();
            }

            double total = Math.Round(cart.Sum(c => c.Qty * c.Rate), 2);
            if (total <= 0)
            {
                Message = "Indent total must be greater than zero.";
                return Page();
            }

            var bill = new Bill
            {
                BillType = BillType.OpdBill,
                BillTypeAtomic = BillTypeAtomic.OpdBill,
                BillClassType = BillClassType.BilledBill,
                PatientId = patient.Id,
                PersonId = patient.PersonId,
                PatientEncounterId = encounter.Id,
                DepartmentId = dept?.Id,
                InstitutionId = site?.Id ?? patient.CreatedInstitutionId,
                CreaterId = userId,
                CreatedAt = now,
                BillDate = now,
                BillTime = now,
                GrantTotal = total,
                Total = total,
                BillTotal = total,
                NetTotal = total,
                Balance = total,
                Paid = false,
                BillPaymentCompletelySettled = false,
                Comments = "Department indent. " + (Comments ?? string.Empty)
            };
            _db.Bills.Add(bill);
            await _db.SaveChangesAsync();

            bill.InvoiceNumber = $"IN-{bill.Id:D6}";
            bill.LocalNumber = $"IN-{bill.Id:D6}";

            int serial = 1;
            foreach (var c in cart)
            {
                bill.BillItems.Add(new BillItem
                {
                    ItemId1 = c.ItemId,
                    Qty = c.Qty,
                    Rate = c.Rate,
                    NetRate = c.Rate,
                    GrossValue = Math.Round(c.Qty * c.Rate, 2),
                    Discount = 0,
                    NetValue = Math.Round(c.Qty * c.Rate, 2),
                    SearialNo = serial++,
                    PatientEncounterId = encounter.Id,
                    CreatedAt = now,
                    CreaterId = userId
                });
            }
            await _db.SaveChangesAsync();

            HttpContext.Session.Remove(CartKey);

            TempData["IndentSuccess"] = $"Indent #{bill.Id} ({bill.InvoiceNumber}) raised for Visit #{encounter.Id}. Amount {total:N2} now appears in the Cashier to collect payment.";
            return RedirectToPage("/Cashier/Indent");
        }

        private async Task LoadStateAsync()
        {
            await LoadLookupsAsync();
            SelectedPatient = await GetSelectedPatientAsync();
            DisplayCart = GetCart();
            Cart = DisplayCart;
            if (SelectedPatient != null)
            {
                Visits = await _db.PatientEncounters
                    .Where(e => e.PatientId == SelectedPatient.Id && !e.Retired)
                    .OrderByDescending(e => e.EncounterDate)
                    .ToListAsync();

                if (SelectedEncounterId.HasValue && Visits.Any(e => e.Id == SelectedEncounterId.Value))
                    SelectedVisit = Visits.First(e => e.Id == SelectedEncounterId.Value);
                else if (Visits.Any())
                    SelectedVisit = Visits.First();

                if (SelectedEncounterId == null && SelectedVisit != null)
                    SelectedEncounterId = SelectedVisit.Id;

                PatientIndents = await _db.Bills
                    .Where(b => b.PatientId == SelectedPatient.Id && !b.Retired && !b.Cancelled)
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(20)
                    .Select(b => new IndentCard
                    {
                        Id = b.Id,
                        InvoiceNumber = b.InvoiceNumber,
                        VisitId = b.PatientEncounterId,
                        DepartmentName = b.Department != null ? b.Department.Name : "",
                        Amount = b.NetTotal,
                        Paid = b.Paid,
                        PaidAt = b.PaidAt,
                        PaymentMethod = b.PaymentMethod
                    })
                    .ToListAsync();
            }
            else
            {
                Visits = new();
                SelectedVisit = null;
                PatientIndents = new();
            }
            Total = Math.Round(DisplayCart.Sum(c => c.Qty * c.Rate), 2);

            if (TempData["IndentMessage"] != null)
                Message = TempData["IndentMessage"].ToString();
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
            Departments = await _db.Departments
                .Where(d => !d.Retired)
                .OrderBy(d => d.Name)
                .ToListAsync();
            if (DepartmentId == null && _appSession.LoggedDepartment != null)
                DepartmentId = _appSession.LoggedDepartment.Id;
        }

        public class IndentCartItem
        {
            public long ItemId { get; set; }
            public string Code { get; set; }
            public string ItemName { get; set; }
            public int Qty { get; set; }
            public double Rate { get; set; }
        }

        public class IndentCard
        {
            public long Id { get; set; }
            public string InvoiceNumber { get; set; }
            public long? VisitId { get; set; }
            public string DepartmentName { get; set; }
            public double Amount { get; set; }
            public bool Paid { get; set; }
            public DateTime? PaidAt { get; set; }
            public PaymentMethod? PaymentMethod { get; set; }
        }
    }
}