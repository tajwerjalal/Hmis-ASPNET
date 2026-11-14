using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Models.Legacy;
using HmisWeb.Services;

namespace HmisWeb.Pages.Shared
{
    /// <summary>
    /// One shared indenting process for every department that bills from the
    /// canonical [SERVICES] master. This is the Ultrasound flow (/us/indent),
    /// lifted into a base class so X-ray, Dental, Operation Theatre and
    /// Consultancy &amp; Procedures get exactly the same behaviour instead of
    /// four drifting copies.
    ///
    /// What the process does
    ///   1. Find / register a patient (session-held selection).
    ///   2. Detect whether the patient is currently admitted (active IPD
    ///      encounter with no discharge date). If so every line is posted to
    ///      the patient account; otherwise the bill is created UNPAID for the
    ///      cashier.
    ///   3. Add services from a server-searched popup (SERVICES filtered to the
    ///      owning DEPT code), with panel / company rate resolution.
    ///   4. Save one [BILL] header + [BILLITEM] lines, then let the module add
    ///      its own worklist rows (X-ray orders, ultrasound orders).
    ///
    /// Why it exists
    ///   * The departments were ported from separate VFP programs and each had
    ///     a slightly different indent screen. The departments agreed
    ///     (22-Oct-2026) that all services come from [SERVICES] and that the
    ///     indenting process should be identical everywhere.
    ///
    /// Business rules / validations
    ///   * Patient required; at least one service line required.
    ///   * A service can appear only once - adding it again bumps quantity.
    ///   * Rates are resolved server-side (COMPANYSERVICERATE, then SRATE); the
    ///     browser may edit qty / rate / discount but Save re-clamps them so a
    ///     posted discount can never exceed the line charge.
    ///   * Cart edits merge back onto the session cart so server-owned fields
    ///     (Code, ItemName, FromDeptService) are never lost.
    ///   * Bill lines store the SERVICES code in BILLITEM.ItemId; the existing
    ///     collection / audit / posting screens resolve the service through the
    ///     BILLITEM.ItemId1 -> ITEM mirror when present.
    ///   * FromDeptService lines are recorded against BILLITEM.DeptId = the
    ///     module's legacy DEPT code.
    ///
    /// Parameters / return values
    ///   * Handlers return the page (validation / modal) or redirect back to the
    ///     module route after a successful state change.
    ///   * OnPostSaveAsync returns a redirect with a TempData success message
    ///     containing the bill number and the amount due / posted.
    ///
    /// Derived pages supply the DEPT code, labels, session keys, route and an
    /// optional order-creation hook.
    ///
    /// Developer : AI Assistant
    /// Created   : 27-Oct-2026 11:05
    /// Change Log: 27-Oct-2026 11:05 - created by lifting the Ultrasound
    ///             indenting process so X-ray, Dental, OT and CNP can share it.
    /// </summary>
    public abstract class DepartmentIndentPageModel : PageModel
    {
        protected readonly ApplicationDbContext Db;
        protected readonly ICommonService Common;
        protected readonly IAppSession AppSession;

        protected DepartmentIndentPageModel(ApplicationDbContext db, ICommonService common, IAppSession appSession)
        {
            Db = db;
            Common = common;
            AppSession = appSession;
        }

        // ------------------------------------------------------------------
        // Module identity supplied by the derived page model.
        // ------------------------------------------------------------------

        /// <summary>Legacy SERVICES.DEPT code owned by this module (01, 02, 03, 04, 06, 11).</summary>
        public abstract string ServiceDept { get; }

        /// <summary>Friendly module name used in headings, buttons and bill remarks.</summary>
        public abstract string ModuleName { get; }

        /// <summary>Short badge text shown next to a service (US, XR, DNT, OT, CNP).</summary>
        public abstract string ModuleShortName { get; }

        /// <summary>Page route used for redirects, e.g. "/Xr/Indent".</summary>
        public abstract string RoutePath { get; }

        /// <summary>Invoice/indent printout route to open for cashier payment, e.g. "/Xr/Reports/IndentPrintout".</summary>
        public virtual string InvoicePrintRoute => null;

        /// <summary>Session key holding the selected patient id.</summary>
        public abstract string PatientSessionKey { get; }

        /// <summary>Session key holding the JSON indent cart.</summary>
        public abstract string CartSessionKey { get; }

        /// <summary>Lower-case module name used inside helper sentences.</summary>
        public virtual string ModuleLowerName => ModuleName.ToLowerInvariant();

        /// <summary>Prefix written into BILL.Comments so reports can identify the module.</summary>
        protected virtual string BillCommentPrefix => $"{ModuleName} service indent. ";

        /// <summary>
        /// Hook for modules that also need a worklist row per line (X-ray and
        /// ultrasound orders). Departments without an order table leave it
        /// as a no-op.
        /// </summary>
        protected virtual Task CreateModuleOrdersAsync(Bill bill, List<IndentCartItem> cart, Patient patient,
            PatientEncounter encounter, DateTime now, long? userId, long? institutionId)
            => Task.CompletedTask;

        // ------------------------------------------------------------------
        // Bound form state.
        // ------------------------------------------------------------------

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

        [BindProperty(SupportsGet = true)]
        public bool OpenServiceModal { get; set; }

        [BindProperty]
        public long? SelectedServiceId { get; set; }

        [BindProperty]
        public long? ItemToRemove { get; set; }

        [BindProperty]
        public List<IndentCartItem> Cart { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public long? SelectedEncounterId { get; set; }

        [BindProperty]
        public string Comments { get; set; }

        // ------------------------------------------------------------------
        // View state.
        // ------------------------------------------------------------------

        public Patient SelectedPatient { get; set; }
        public PatientLegacyInfo LegacyPatient { get; set; }
        public List<Patient> PatientResults { get; set; } = new();
        public List<DeptService> ServiceResults { get; set; } = new();
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

        // ------------------------------------------------------------------
        // Handlers.
        // ------------------------------------------------------------------

        public async Task OnGetAsync()
        {
            await LoadStateAsync();
            if (OpenServiceModal)
                await SearchServicesAsync();
            if (TempData["IndentSuccess"] != null)
                Success = TempData["IndentSuccess"].ToString();
            if (TempData["IndentMessage"] != null)
                Message = TempData["IndentMessage"].ToString();
        }

        public async Task<IActionResult> OnPostSearchPatientsAsync()
        {
            await LoadStateAsync();
            PatientResults = await Common.SearchPatientsAsync(PatientSearch);
            return Page();
        }

        public async Task<IActionResult> OnPostSelectPatientAsync()
        {
            if (SelectedPatientId == null)
                return RedirectToPage(RoutePath);

            var patient = await Db.Patients
                .Include(p => p.Person)
                .FirstOrDefaultAsync(p => p.Id == SelectedPatientId.Value && !p.Retired);
            if (patient == null)
                return RedirectToPage(RoutePath);

            HttpContext.Session.SetString(PatientSessionKey, patient.Id.ToString());
            HttpContext.Session.Remove(CartSessionKey);
            return RedirectToPage(RoutePath);
        }

        public IActionResult OnPostStartNewAsync()
        {
            HttpContext.Session.Remove(PatientSessionKey);
            HttpContext.Session.Remove(CartSessionKey);
            return RedirectToPage(RoutePath);
        }

        /// <summary>Re-selects the same patient without clearing their indent.</summary>
        public async Task<IActionResult> OnPostSwitchPatientAsync()
        {
            if (SelectedPatientId == null)
                return RedirectToPage(RoutePath);

            var patient = await Db.Patients
                .Include(p => p.Person)
                .FirstOrDefaultAsync(p => p.Id == SelectedPatientId.Value && !p.Retired);
            if (patient == null)
                return RedirectToPage(RoutePath);

            HttpContext.Session.SetString(PatientSessionKey, patient.Id.ToString());
            HttpContext.Session.Remove(CartSessionKey);
            return RedirectToPage(RoutePath);
        }

        /// <summary>Keeps the chosen IPD encounter without clearing the patient or cart.</summary>
        public IActionResult OnPostSelectEncounterAsync()
        {
            return RedirectToPage(RoutePath, new { SelectedEncounterId });
        }

        public async Task<IActionResult> OnPostCreatePatientAsync()
        {
            if (string.IsNullOrWhiteSpace(NewName))
            {
                await LoadStateAsync();
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
                CreaterId = AppSession.WebUserId
            };
            Db.Persons.Add(person);
            await Db.SaveChangesAsync();

            long? maxPatientId = await Db.Patients.MaxAsync(p => (long?)p.PatientId);
            var patient = new Patient
            {
                PersonId = person.Id,
                PatientId = (maxPatientId ?? 0) + 1,
                CreatedAt = DateTime.Now,
                CreaterId = AppSession.WebUserId,
                CreatedInstitutionId = AppSession.LoggedSite?.Id
            };
            Db.Patients.Add(patient);
            await Db.SaveChangesAsync();

            HttpContext.Session.Remove(CartSessionKey);
            HttpContext.Session.SetString(PatientSessionKey, patient.Id.ToString());
            return RedirectToPage(RoutePath);
        }

        public async Task<IActionResult> OnPostSearchServicesAsync()
        {
            await LoadStateAsync();
            await SearchServicesAsync();
            OpenServiceModal = true;
            return Page();
        }

        public async Task<IActionResult> OnGetSearchServicesAsync()
        {
            await LoadStateAsync();
            await SearchServicesAsync();
            OpenServiceModal = true;
            return Page();
        }

        public async Task<IActionResult> OnPostAddServiceAsync()
        {
            await LoadStateAsync();
            if (SelectedServiceId == null)
                return Page();

            // Only rows owned by this module can be billed here, even if the
            // browser posts another department's service id.
            var deptService = await Db.DeptServices
                .FirstOrDefaultAsync(s => s.Id == SelectedServiceId.Value
                    && s.Dept == ServiceDept && !s.Retired && s.Billable && s.Active);
            if (deptService == null)
            {
                OpenServiceModal = true;
                ErrorMessage = "The selected service is not part of this department's catalogue.";
                return Page();
            }

            // Panel / company rate card first, then the SERVICES SRATE - the
            // same resolution the Laboratory indent uses, so a company patient
            // is never charged the walk-in price.
            var patient = await GetSelectedPatientAsync();
            var companyId = patient != null
                ? await Common.GetPatientCompanyIdAsync(patient.PersonId ?? 0)
                : null;
            var appliedRate = await Common.GetAppliedRateAsync(deptService, companyId);

            var cart = GetCart();
            var existing = cart.FirstOrDefault(c => c.ItemId == deptService.Id && c.FromDeptService);
            if (existing != null)
            {
                existing.Qty += 1;
            }
            else
            {
                cart.Add(new IndentCartItem
                {
                    ItemId = deptService.Id,
                    Code = deptService.Code,
                    ItemName = deptService.Name,
                    Qty = 1,
                    Rate = appliedRate,
                    Discount = 0,
                    FromDeptService = true
                });
            }
            SaveCart(cart);
            TempData["IndentMessage"] = $"{deptService.Name} added to the indent.";
            return RedirectToPage(RoutePath, new { OpenServiceModal = true });
        }

        public IActionResult OnPostUpdateCartAsync()
        {
            if (ItemToRemove.HasValue)
            {
                var cart = GetCart();
                cart.RemoveAll(c => c.ItemId == ItemToRemove.Value);
                SaveCart(cart);
                return RedirectToPage(RoutePath);
            }

            var rows = Cart ?? new List<IndentCartItem>();

            // The cart grid only posts the editable columns. Merge the posted
            // rows back onto the session cart so server-owned fields (Code,
            // ItemName, FromDeptService) survive a qty / rate / discount edit.
            // Losing FromDeptService would make the save write a SERVICES id
            // into BILLITEM.ItemId1 as if it were an ITEM id.
            var sessionCart = GetCart().GroupBy(c => c.ItemId).ToDictionary(g => g.Key, g => g.First());
            foreach (var r in rows)
            {
                if (sessionCart.TryGetValue(r.ItemId, out var original))
                {
                    if (string.IsNullOrWhiteSpace(r.Code)) r.Code = original.Code;
                    if (string.IsNullOrWhiteSpace(r.ItemName)) r.ItemName = original.ItemName;
                    r.FromDeptService = r.FromDeptService || original.FromDeptService;
                }
            }

            foreach (var r in rows)
            {
                if (r.Qty <= 0) r.Qty = 1;
                if (r.Rate < 0) r.Rate = 0;
                if (r.Discount < 0) r.Discount = 0;
                if (r.Discount > r.Qty * r.Rate) r.Discount = r.Qty * r.Rate;
            }
            rows = rows.Where(r => r.ItemId > 0).ToList();
            SaveCart(rows);
            return RedirectToPage(RoutePath);
        }

        public IActionResult OnPostClearAsync()
        {
            HttpContext.Session.Remove(CartSessionKey);
            return RedirectToPage(RoutePath);
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            // Full state is needed so a validation failure re-renders the
            // patient + cart instead of dropping back to an empty search step.
            await LoadStateAsync();

            var patientId = GetSelectedPatientId();
            var cart = GetCart();
            if (patientId == null)
            {
                Message = "Please select a patient before saving the indent.";
                return Page();
            }
            if (!cart.Any())
            {
                Message = $"Add at least one {ModuleLowerName} service before saving.";
                return Page();
            }

            var patient = await Db.Patients
                .Include(p => p.Person)
                .FirstOrDefaultAsync(p => p.Id == patientId.Value && !p.Retired);
            if (patient == null)
            {
                Message = "Selected patient not found.";
                return Page();
            }

            var encounter = await GetSelectedEncounterAsync(patient.Id);

            // Re-clamp before the money is written: the browser can post any
            // qty / rate / discount combination, and a discount above the line
            // charge would otherwise create a negative bill line.
            foreach (var c in cart)
            {
                if (c.Qty <= 0) c.Qty = 1;
                if (c.Rate < 0) c.Rate = 0;
                if (c.Discount < 0) c.Discount = 0;
                if (c.Discount > c.Qty * c.Rate) c.Discount = c.Qty * c.Rate;
            }

            double total = Math.Round(cart.Sum(c => c.GrossValue), 2);
            double itemDiscount = Math.Round(cart.Sum(c => c.Discount), 2);
            double netTotal = Math.Round(total - itemDiscount, 2);
            if (netTotal < 0) netTotal = 0;

            if (total <= 0)
            {
                Message = "Indent total must be greater than zero.";
                return Page();
            }

            var now = DateTime.Now;
            var userId = AppSession.WebUserId;
            var site = AppSession.LoggedSite;
            var dept = AppSession.LoggedDepartment ?? await Db.Departments
                .Where(d => !d.Retired && d.Code == ServiceDept)
                .FirstOrDefaultAsync();

            bool ipd = encounter != null;

            var bill = new Bill
            {
                BillType = ipd ? BillType.InwardServiceBill : BillType.OpdBill,
                BillTypeAtomic = ipd ? BillTypeAtomic.InwardBill : BillTypeAtomic.OpdBill,
                BillClassType = ipd ? BillClassType.PreBill : BillClassType.BilledBill,
                PatientId = patient.Id,
                PersonId = patient.PersonId,
                PatientEncounterId = ipd ? encounter.Id : null,
                InstitutionId = site?.Id ?? patient.CreatedInstitutionId,
                DepartmentId = dept?.Id,
                CreaterId = userId,
                CreatedAt = now,
                BillDate = now,
                BillTime = now,
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
                Comments = BillCommentPrefix + (Comments ?? string.Empty)
            };

            Db.Bills.Add(bill);
            await Db.SaveChangesAsync();

            bill.InvoiceNumber = $"IN-{bill.Id:D6}";
            bill.LocalNumber = $"IN-{bill.Id:D6}";

            // Existing department reports (collection / audit / company summary)
            // read the billing side through BILLITEM.ItemId1 -> ITEM, so keep the
            // INV-xx-{code} mirror link when it exists. The billing itself does
            // not depend on the mirror; a missing row simply leaves ItemId1 null.
            var mirrorPrefix = DeptBillingMirrorService.PrefixFor(ServiceDept);
            var mirrorCodes = cart
                .Where(c => c.FromDeptService && !string.IsNullOrWhiteSpace(c.Code))
                .Select(c => mirrorPrefix + c.Code.Trim())
                .Distinct()
                .ToList();
            var mirrorIds = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            if (mirrorCodes.Count > 0)
            {
                var mirrorRows = await Db.Items
                    .Where(i => mirrorCodes.Contains(i.Code))
                    .Select(i => new { i.Id, i.Code })
                    .ToListAsync();
                foreach (var row in mirrorRows)
                {
                    if (!string.IsNullOrWhiteSpace(row.Code) && !mirrorIds.ContainsKey(row.Code))
                        mirrorIds[row.Code] = row.Id;
                }
            }

            int serial = 1;
            foreach (var c in cart)
            {
                var gross = Math.Round(c.Qty * c.Rate, 2);
                var lineDiscount = c.Discount > 0 ? Math.Round(c.Discount, 2) : Math.Round(gross - c.NetValue, 2);

                var billItem = new BillItem
                {
                    Qty = c.Qty,
                    Rate = c.Rate,
                    NetRate = c.Rate,
                    GrossValue = gross,
                    Discount = lineDiscount,
                    NetValue = Math.Round(gross - lineDiscount, 2),
                    Descreption = c.ItemName,
                    DeptId = ServiceDept,
                    SearialNo = serial++,
                    PatientEncounterId = ipd ? encounter.Id : null,
                    CreatedAt = now,
                    CreaterId = userId
                };

                if (c.FromDeptService)
                {
                    // SERVICES row - keep the billing mirror link so the existing
                    // collection / audit / posting screens can resolve the service,
                    // and store the department service code in BILLITEM.ItemId.
                    var mirrorCode = mirrorPrefix + (c.Code ?? string.Empty).Trim();
                    billItem.ItemId1 = mirrorIds.TryGetValue(mirrorCode, out var mirrorId) ? mirrorId : null;
                    billItem.ItemId = c.Code;
                }
                else
                {
                    billItem.ItemId1 = c.ItemId;
                    billItem.ItemId = c.Code;
                }

                bill.BillItems.Add(billItem);
            }
            await Db.SaveChangesAsync();

            HttpContext.Session.Remove(CartSessionKey);

            await CreateModuleOrdersAsync(bill, cart, patient, encounter, now, userId, site?.Id);

            TempData["IndentSuccess"] = ipd
                ? $"{ModuleName} indent #{bill.Id} saved to IPD bill of #{encounter.Id} ({encounter.BhtNo}). Amount {netTotal:N2} added to patient account."
                : $"{ModuleName} bill #{bill.Id} created as UNPAID. Patient must pay at Cashier. Amount due: {netTotal:N2}.";
            TempData["IndentBillId"] = bill.Id.ToString();
            TempData["IndentIsIPD"] = ipd ? "1" : "0";
            TempData["IndentPrintUrl"] = !string.IsNullOrWhiteSpace(InvoicePrintRoute)
                ? InvoicePrintRoute + "?IndentNo=" + bill.Id + "&print=1" : null;

            return RedirectToPage(RoutePath);
        }

        // ------------------------------------------------------------------
        // Helpers.
        // ------------------------------------------------------------------

        private async Task LoadStateAsync()
        {
            SelectedPatient = await GetSelectedPatientAsync();
            if (SelectedPatient != null)
            {
                await Common.EnrichLegacyNamesAsync(new[] { SelectedPatient });
                LegacyPatient = await Common.GetPatientLegacyInfoAsync(SelectedPatient);
            }
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

                UnpaidBills = await Db.Bills
                    .Where(b => b.PatientId == SelectedPatient.Id && !b.Retired && !b.Cancelled && !b.Paid)
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync();
            }
            else
            {
                ActiveEncounters = new();
                SelectedEncounter = null;
                UnpaidBills = new();
                LegacyPatient = null;
            }
            ComputeTotals();
            if (TempData["IndentMessage"] != null)
                Message = TempData["IndentMessage"].ToString();

            // Preload the department bank so the add-service popup always has
            // content before the user types anything.
            ServiceResults = await Db.DeptServices
                .Where(s => s.Dept == ServiceDept && !s.Retired && s.Billable && s.Active)
                .OrderBy(s => s.Name)
                .Take(100)
                .ToListAsync();
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

            var q = Db.DeptServices.Where(s => s.Dept == ServiceDept && !s.Retired && s.Billable && s.Active);

            if (!string.IsNullOrWhiteSpace(term))
            {
                q = q.Where(s => (s.Name != null && s.Name.Contains(term))
                    || (s.Sname != null && s.Sname.Contains(term))
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
            return await Db.PatientEncounters
                .Where(e => e.PatientId == patientId && !e.Retired
                    && e.EncounterType == EncounterType.Inward && e.DateOfDischarge == null)
                .OrderByDescending(e => e.DateOfAdmission)
                .ToListAsync();
        }

        private long? GetSelectedPatientId()
        {
            var val = HttpContext.Session.GetString(PatientSessionKey);
            return long.TryParse(val, out var id) ? id : null;
        }

        private async Task<Patient> GetSelectedPatientAsync()
        {
            var id = GetSelectedPatientId();
            if (id == null) return null;
            return await Db.Patients
                .Include(p => p.Person)
                .FirstOrDefaultAsync(p => p.Id == id.Value && !p.Retired);
        }

        private List<IndentCartItem> GetCart()
        {
            var raw = HttpContext.Session.GetString(CartSessionKey);
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
            HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
        }

        /// <summary>One line of the session-held indent cart.</summary>
        public class IndentCartItem
        {
            public long ItemId { get; set; }
            public string Code { get; set; }
            public string ItemName { get; set; }
            public int Qty { get; set; }
            public double Rate { get; set; }
            public double Discount { get; set; }

            /// <summary>True when the line comes from SERVICES (DeptService) rather than a mirrored ITEM row.</summary>
            public bool FromDeptService { get; set; }

            public double GrossValue => Math.Round(Qty * Rate, 2);
            public double NetValue => Math.Round(GrossValue - Discount, 2);
        }
    }
}
