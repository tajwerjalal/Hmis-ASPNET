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
    public class TestResultEntryModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ICommonService _common;
        private readonly IAppSession _appSession;

        public TestResultEntryModel(ApplicationDbContext db, ICommonService common, IAppSession appSession)
        {
            _db = db;
            _common = common;
            _appSession = appSession;
        }

        private const string PatientKey = "LabTestResultPatientId";

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

        [BindProperty(SupportsGet = true)]
        public long? PatientId { get; set; }

        [BindProperty]
        public List<TestResultInput> TestResults { get; set; } = new();

        [BindProperty]
        public long? SelectedEncounterId { get; set; }

        public Patient SelectedPatient { get; set; }
        public List<Patient> PatientResults { get; set; } = new();
        public List<InvestigationOrder> PendingTests { get; set; } = new();
        public List<InvestigationOrder> PendingIndents { get; set; } = new(); // Sample collected, report pending
        public List<PatientEncounter> ActiveEncounters { get; set; } = new();
        public PatientEncounter SelectedEncounter { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string Success { get; set; }

        public async Task OnGetAsync()
        {
            // If PatientId is provided via route, auto-select that patient
            if (PatientId.HasValue)
            {
                var patient = await _db.Patients
                    .Include(p => p.Person)
                    .FirstOrDefaultAsync(p => p.Id == PatientId.Value && !p.Retired);
                if (patient != null)
                {
                    HttpContext.Session.SetString(PatientKey, patient.Id.ToString());
                }
            }

            await LoadStateAsync();
            if (TempData["TestResultSuccess"] != null)
                Success = TempData["TestResultSuccess"].ToString();
            if (TempData["TestResultMessage"] != null)
                Message = TempData["TestResultMessage"].ToString();
        }

        public async Task<IActionResult> OnPostSearchPatientsAsync()
        {
            await LoadStateAsync();
            
            if (!string.IsNullOrWhiteSpace(PatientSearch))
            {
                // Search patients with pending investigation orders awaiting result entry
                var term = PatientSearch.Trim().ToLower();
                bool isNumeric = long.TryParse(term, out var searchId);
                
                var patientIdsWithPendingResults = await _db.InvestigationOrders
                    .Where(o => !o.Retired && o.Ordered && o.SampleCollected && !o.Completed)
                    .Select(o => o.PatientId)
                    .Distinct()
                    .ToListAsync();

                var query = _db.Patients
                    .Include(p => p.Person)
                    .Where(p => !p.Retired && patientIdsWithPendingResults.Contains(p.Id));

                if (isNumeric)
                {
                    query = query.Where(p => p.Id == searchId || p.PatientId == searchId || 
                        (p.Person != null && (p.Person.Mobile != null && p.Person.Mobile.Contains(term)) ||
                        (p.Person.Nic != null && p.Person.Nic.Contains(term))));
                }
                else
                {
                    query = query.Where(p => p.Person != null && 
                        ((p.Person.Name != null && p.Person.Name.ToLower().Contains(term)) ||
                        (p.Person.NameWithInitials != null && p.Person.NameWithInitials.ToLower().Contains(term)) ||
                        (p.Person.Mobile != null && p.Person.Mobile.Contains(term)) ||
                        (p.Person.Nic != null && p.Person.Nic.ToLower().Contains(term))));
                }

                var patients = await query.Take(50).ToListAsync();
                
                // Add pending test count to each patient
                foreach (var p in patients)
                {
                    p.PendingTestCount = await _db.InvestigationOrders
                        .CountAsync(o => o.PatientId == p.Id && !o.Retired && o.Ordered && o.SampleCollected && !o.Completed);
                }

                PatientResults = patients;
            }
            else
            {
                PatientResults = new();
            }
            
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
            return RedirectToPage("/Lab/TestResultEntry");
        }

        public IActionResult OnPostStartNewAsync()
        {
            HttpContext.Session.Remove(PatientKey);
            return RedirectToPage("/Lab/TestResultEntry");
        }

        public async Task<IActionResult> OnPostSelectIndentAsync(long indentId)
        {
            var order = await _db.InvestigationOrders
                .Include(o => o.Patient)
                .FirstOrDefaultAsync(o => o.Id == indentId && !o.Retired);
            
            if (order != null)
            {
                HttpContext.Session.SetString(PatientKey, order.PatientId.ToString());
            }
            return RedirectToPage("/Lab/TestResultEntry");
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
            return RedirectToPage("/Lab/TestResultEntry");
        }

        public async Task<IActionResult> OnPostSaveResultsAsync()
        {
            await LoadStateAsync();

            var patientId = GetSelectedPatientId();
            if (patientId == null)
            {
                ErrorMessage = "Please select a patient first.";
                return Page();
            }

            if (TestResults == null || !TestResults.Any())
            {
                ErrorMessage = "No test results to save.";
                return Page();
            }

            var now = DateTime.Now;
            var userId = _appSession.WebUserId;

            foreach (var input in TestResults.Where(x => x.OrderId > 0))
            {
                var order = await _db.InvestigationOrders
                    .FirstOrDefaultAsync(o => o.Id == input.OrderId && !o.Retired);
                if (order == null) continue;

                order.Result = input.Result ?? "";
                order.ResultNotes = input.ResultNotes ?? "";
                order.Completed = input.Completed;
                order.CompletedAt = input.Completed ? now : null;
            }

            await _db.SaveChangesAsync();

            TempData["TestResultSuccess"] = "Test results saved successfully.";
            return RedirectToPage("/Lab/TestResultEntry");
        }

        private async Task LoadStateAsync()
        {
            SelectedPatient = await GetSelectedPatientAsync();
            
            // Always load pending indents (sample collected, report not generated)
            await LoadPendingIndentsAsync();

            if (SelectedPatient != null)
            {
                ActiveEncounters = await GetActiveEncountersAsync(SelectedPatient.Id);
                if (SelectedEncounterId.HasValue && ActiveEncounters.Any(e => e.Id == SelectedEncounterId.Value))
                    SelectedEncounter = ActiveEncounters.First(e => e.Id == SelectedEncounterId.Value);
                else if (ActiveEncounters.Any())
                    SelectedEncounter = ActiveEncounters.First();
                else
                    SelectedEncounter = null;

                await LoadPendingTestsAsync();
            }
            else
            {
                ActiveEncounters = new();
                SelectedEncounter = null;
                PendingTests = new();
            }
        }

        private async Task LoadPendingIndentsAsync()
        {
            // All investigation orders where sample is collected but report not generated
            PendingIndents = await _db.InvestigationOrders
                .Include(o => o.Item)
                .Include(o => o.Patient).ThenInclude(p => p.Person)
                .Include(o => o.PatientEncounter)
                .Where(o => !o.Retired && o.Ordered && o.SampleCollected && !o.Completed)
                .OrderByDescending(o => o.SampleCollectedAt)
                .ToListAsync();
        }

        private async Task LoadPendingTestsAsync()
        {
            var patientId = GetSelectedPatientId();
            if (patientId == null)
            {
                PendingTests = new();
                return;
            }

            var query = _db.InvestigationOrders
                .Include(o => o.Item)
                .Where(o => o.PatientId == patientId && !o.Retired && !o.Completed);

            if (SelectedEncounterId.HasValue)
                query = query.Where(o => o.PatientEncounterId == SelectedEncounterId.Value);

            PendingTests = await query.OrderBy(o => o.CreatedAt).ToListAsync();

            // Initialize TestResults with pending tests
            TestResults = PendingTests.Select(o => new TestResultInput
            {
                OrderId = o.Id,
                InvestigationName = o.InvestigationName,
                ItemName = o.Item?.Name ?? "",
                Result = o.Result,
                ResultNotes = o.ResultNotes,
                Completed = o.Completed
            }).ToList();
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

        private async Task<List<PatientEncounter>> GetActiveEncountersAsync(long patientId)
        {
            return await _db.PatientEncounters
                .Where(e => e.PatientId == patientId && !e.Retired
                    && e.EncounterType == EncounterType.Inward && e.DateOfDischarge == null)
                .OrderByDescending(e => e.DateOfAdmission)
                .ToListAsync();
        }

        public class TestResultInput
        {
            public long OrderId { get; set; }
            public string InvestigationName { get; set; }
            public string ItemName { get; set; }
            public string Result { get; set; }
            public string ResultNotes { get; set; }
            public bool Completed { get; set; }
        }
    }
}