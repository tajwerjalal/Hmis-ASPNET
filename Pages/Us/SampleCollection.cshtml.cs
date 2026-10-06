using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Us
{
    public class SampleCollectionModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ICommonService _common;
        private readonly IAppSession _appSession;

        public SampleCollectionModel(ApplicationDbContext db, ICommonService common, IAppSession appSession)
        {
            _db = db;
            _common = common;
            _appSession = appSession;
        }

        private const string PatientKey = "UsCollectionPatientId";

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
        public long? SelectedEncounterId { get; set; }

        [BindProperty]
        public List<CollectionRow> SampleCollections { get; set; } = new();

        [BindProperty]
        public string CollectionNotes { get; set; }

        public Patient SelectedPatient { get; set; }
        public List<Patient> PatientResults { get; set; } = new();
        public List<UsOrder> PendingOrders { get; set; } = new();
        public List<PatientEncounter> ActiveEncounters { get; set; } = new();
        public PatientEncounter SelectedEncounter { get; set; }
        public List<PendingScanRow> PendingScans { get; set; } = new();
        public int UnpaidPendingCount => PendingScans.Count(r => !r.IsPaid);
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string Success { get; set; }

        public async Task OnGetAsync()
        {
            await LoadStateAsync();
            if (TempData["UsCollectionSuccess"] != null)
                Success = TempData["UsCollectionSuccess"].ToString();
        }

        public async Task<IActionResult> OnPostSearchPatientsAsync()
        {
            PatientResults = await _common.SearchPatientsAsync(PatientSearch);
            await LoadPendingScansAsync();
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
            return RedirectToPage("/Us/SampleCollection");
        }

        public IActionResult OnPostStartNewAsync()
        {
            HttpContext.Session.Remove(PatientKey);
            return RedirectToPage("/Us/SampleCollection");
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

            HttpContext.Session.SetString(PatientKey, patient.Id.ToString());
            return RedirectToPage("/Us/SampleCollection");
        }

        public async Task<IActionResult> OnPostGenerateBarcodesAsync()
        {
            // LoadStateAsync rebuilds the rows from the database, so the posted rows have to
            // be captured before it runs or the user's input is lost.
            var posted = SampleCollections ?? new List<CollectionRow>();

            await LoadStateAsync();

            if (SelectedPatient == null)
            {
                ErrorMessage = "Please select a patient first.";
                SampleCollections = posted;
                return Page();
            }

            foreach (var row in posted.Where(r => r.OrderId > 0 && string.IsNullOrWhiteSpace(r.Barcode)))
            {
                row.Barcode = GenerateBarcode(row.OrderId);
            }

            SampleCollections = posted;
            Message = "Barcodes generated.";
            return Page();
        }

        public async Task<IActionResult> OnPostSaveCollectionsAsync()
        {
            var posted = SampleCollections ?? new List<CollectionRow>();

            await LoadStateAsync();

            var patientId = GetSelectedPatientId();
            if (patientId == null)
            {
                ErrorMessage = "Please select a patient first.";
                SampleCollections = posted;
                return Page();
            }

            if (!posted.Any(x => x.Collect))
            {
                ErrorMessage = "No scans selected as collected.";
                SampleCollections = posted;
                return Page();
            }

            var now = DateTime.Now;
            var userId = _appSession.WebUserId;
            var saved = 0;

            foreach (var input in posted.Where(x => x.Collect && x.OrderId > 0))
            {
                // Guard against posting an order that belongs to a different patient.
                var order = await _db.UsOrders
                    .FirstOrDefaultAsync(o => o.Id == input.OrderId && o.PatientId == patientId && !o.Retired);

                if (order == null) continue;
                if (order.SampleCollected) continue;

                order.SampleCollected = true;
                order.SampleCollectedAt = now;
                order.SampleCollectedById = userId;
                order.SampleType = string.IsNullOrWhiteSpace(input.SampleType) ? null : input.SampleType.Trim();
                order.Barcode = string.IsNullOrWhiteSpace(input.Barcode) ? GenerateBarcode(order.Id) : input.Barcode.Trim();
                order.BarcodeLabel = order.Barcode;
                order.CollectorId = userId;
                order.CollectionDateTime = input.CollectionDateTime ?? now;
                order.CollectionNotes = string.IsNullOrWhiteSpace(input.CollectionNotes)
                    ? (CollectionNotes ?? string.Empty)
                    : input.CollectionNotes.Trim();
                order.EditerId = userId;
                order.EditedAt = now;

                saved++;
            }

            if (saved == 0)
            {
                ErrorMessage = "No pending scans were updated. They may already be marked collected.";
                SampleCollections = posted;
                return Page();
            }

            await _db.SaveChangesAsync();

            TempData["UsCollectionSuccess"] = $"{saved} scan(s) marked as collected.";
            return RedirectToPage("/Us/SampleCollection");
        }

        private async Task LoadStateAsync()
        {
            SelectedPatient = await GetSelectedPatientAsync();

            if (SelectedPatient == null)
            {
                PendingOrders = new();
                SampleCollections = new();
                ActiveEncounters = new();
                SelectedEncounter = null;
            }
            else
            {
                ActiveEncounters = await GetActiveEncountersAsync(SelectedPatient.Id);
                if (SelectedEncounterId.HasValue && ActiveEncounters.Any(e => e.Id == SelectedEncounterId.Value))
                    SelectedEncounter = ActiveEncounters.First(e => e.Id == SelectedEncounterId.Value);
                else if (ActiveEncounters.Any())
                    SelectedEncounter = ActiveEncounters.First();
                else
                    SelectedEncounter = null;

                var query = _db.UsOrders
                    .Include(o => o.UsService)
                    .Where(o => o.PatientId == SelectedPatient.Id && !o.Retired && !o.SampleCollected);

                if (SelectedEncounter != null)
                    query = query.Where(o => o.PatientEncounterId == SelectedEncounter.Id);
                else
                    query = query.Where(o => o.PatientEncounterId == null);

                PendingOrders = await query
                    .OrderByDescending(o => o.CreatedAt)
                    .ThenBy(o => o.OrderName)
                    .ToListAsync();

                SampleCollections = BuildRows(PendingOrders);
            }

            await LoadPendingScansAsync();

            if (TempData["UsCollectionMessage"] != null)
                Message = TempData["UsCollectionMessage"].ToString();
        }

        // Queue of scans waiting to be collected, listed automatically so the user does not
        // have to search: indent -> cashier payment -> scan collection entry -> report entry.
        private async Task LoadPendingScansAsync()
        {
            var rows = await (
                from o in _db.UsOrders.AsNoTracking()
                join p in _db.Patients.AsNoTracking() on o.PatientId equals p.Id
                join per in _db.Persons.AsNoTracking() on p.PersonId equals per.Id
                join b in _db.Bills.AsNoTracking() on o.BillId equals b.Id into bills
                from b in bills.DefaultIfEmpty()
                where !o.Retired && !o.SampleCollected && !p.Retired && !per.Retired
                select new { o, p, per, b }
            ).ToListAsync();

            PendingScans = rows
                .Select(x => new PendingScanRow
                {
                    OrderId = x.o.Id,
                    PatientDbId = x.p.Id,
                    PatientId = x.p.PatientId,
                    Phn = x.p.Phn,
                    PatientName = x.per.Name,
                    Sex = x.per.Sex,
                    Dob = x.per.Dob,
                    UsRequired = x.o.OrderName,
                    BillId = x.o.BillId,
                    BillNetTotal = x.b?.NetTotal ?? 0,
                    IsPaid = IsBillSettled(x.b),
                    WaitingSince = x.o.CreatedAt
                })
                // Unpaid work surfaces last so the ready-to-scan rows are at the top.
                .OrderBy(r => r.IsPaid ? 1 : 0)
                .ThenByDescending(r => r.WaitingSince)
                .ToList();
        }

        private static bool IsBillSettled(Bill bill)
        {
            if (bill == null) return false;
            if (bill.Cancelled || bill.Retired) return false;
            return bill.Paid || bill.BillPaymentCompletelySettled || bill.Balance <= 0;
        }

        private static List<CollectionRow> BuildRows(List<UsOrder> orders)
        {
            var now = DateTime.Now;
            return orders.Select(o => new CollectionRow
            {
                OrderId = o.Id,
                ServiceName = o.OrderName,
                Code = o.UsService?.Code,
                Instructions = o.Instructions,
                Qty = o.Qty,
                BillNo = o.BillId?.ToString(),
                SampleType = o.SampleType,
                Barcode = o.Barcode,
                CollectionDateTime = o.CollectionDateTime ?? now,
                CollectionNotes = o.CollectionNotes
            }).ToList();
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

        private static string GenerateBarcode(long orderId)
        {
            return $"USC{DateTime.Now:yyyyMMdd}{orderId:D6}";
        }

        public class CollectionRow
        {
            public long OrderId { get; set; }
            public string ServiceName { get; set; }
            public string Code { get; set; }
            public string Instructions { get; set; }
            public int Qty { get; set; } = 1;
            public string BillNo { get; set; }
            public bool Collect { get; set; }
            public string SampleType { get; set; }
            public string Barcode { get; set; }
            public DateTime? CollectionDateTime { get; set; }
            public string CollectionNotes { get; set; }
        }

        public class PendingScanRow
        {
            public long OrderId { get; set; }
            public long PatientDbId { get; set; }
            public long? PatientId { get; set; }
            public string Phn { get; set; }
            public string PatientName { get; set; }
            public Sex? Sex { get; set; }
            public DateTime? Dob { get; set; }
            public string UsRequired { get; set; }
            public long? BillId { get; set; }
            public double BillNetTotal { get; set; }
            public bool IsPaid { get; set; }
            public DateTime? WaitingSince { get; set; }

            public string AgeString
            {
                get
                {
                    if (Dob == null) return "";
                    var age = DateTime.Now - Dob.Value;
                    var years = age.Days / 365;
                    var months = (age.Days % 365) / 30;
                    var days = (age.Days % 365) % 30;
                    if (years > 0) return $"{years}y {months}m {days}d";
                    if (months > 0) return $"{months}m {days}d";
                    return $"{days}d";
                }
            }
        }
    }
}
