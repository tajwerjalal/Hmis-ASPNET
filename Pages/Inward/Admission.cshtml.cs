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
    public class AdmissionModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;
        private readonly ICommonService _commonService;

        public AdmissionModel(ApplicationDbContext db, IAppSession appSession, ICommonService commonService)
        {
            _db = db;
            _appSession = appSession;
            _commonService = commonService;
        }

        [BindProperty]
        public string SearchTerm { get; set; }

        [BindProperty]
        public long? SelectedPatientId { get; set; }

        [BindProperty]
        public DateTime? DateOfAdmission { get; set; }

        [BindProperty]
        public string TimeOfAdmission { get; set; }

        [BindProperty]
        public long? RoomId { get; set; }

        [BindProperty]
        public string Comments { get; set; }

        [BindProperty]
        public AdmissionType? AdmissionType { get; set; }

        [BindProperty]
        public string BhtNo { get; set; }

        [BindProperty]
        public long? ReferringConsultantId { get; set; }

        [BindProperty]
        public long? OpdDoctorId { get; set; }

        [BindProperty]
        public PaymentMethod? PaymentMethod { get; set; } = HmisWeb.Data.Enums.PaymentMethod.Cash;

        [BindProperty]
        public long? CreditCompanyId { get; set; }

        [BindProperty]
        public double? CreditLimit { get; set; }

        [BindProperty]
        public string ReferenceNo { get; set; }

        [BindProperty]
        public string PolicyNo { get; set; }

        [BindProperty]
        public bool Claimable { get; set; }

        [BindProperty]
        public string GuardianName { get; set; }

        [BindProperty]
        public string GuardianNic { get; set; }

        [BindProperty]
        public string GuardianMobile { get; set; }

        [BindProperty]
        public long? GuardianRelationshipToPatientId { get; set; }

        [BindProperty]
        public double? Weight { get; set; }

        [BindProperty]
        public double? Height { get; set; }

        [BindProperty]
        public int? Sbp { get; set; }

        [BindProperty]
        public int? Dbp { get; set; }

        [BindProperty]
        public int? Pr { get; set; }

        [BindProperty]
        public int? RespiratoryRate { get; set; }

        [BindProperty]
        public int? Saturation { get; set; }

        [BindProperty]
        public int? Pfr { get; set; }

        public List<Patient> SearchResults { get; set; } = new();
        public Patient SelectedPatient { get; set; }
        public List<Room> Rooms { get; set; } = new();

        public List<SelectListItem> DoctorList { get; set; } = new();
        public List<SelectListItem> CreditCompanyList { get; set; } = new();
        public List<SelectListItem> RelationList { get; set; } = new();
        public List<SelectListItem> AdmissionTypeList { get; set; } = new();
        public List<SelectListItem> PaymentMethodList { get; set; } = new();

        public string Message { get; set; }

        public async Task OnGetAsync(long? patientId, string searchTerm)
        {
            SearchTerm = searchTerm;
            SelectedPatientId = patientId;
            DateOfAdmission = DateTime.Today;
            await LoadAsync(patientId, searchTerm);
        }

        public async Task<IActionResult> OnPostSearchAsync()
        {
            DateOfAdmission = DateOfAdmission ?? DateTime.Today;
            await LoadAsync(SelectedPatientId, SearchTerm);
            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            DateOfAdmission = DateOfAdmission ?? DateTime.Today;

            if (SelectedPatientId == null)
            {
                ModelState.AddModelError("SelectedPatientId", "Please select a patient.");
                await LoadAsync(null, SearchTerm);
                return Page();
            }

            var now = DateTime.Now;
            var userId = _appSession.WebUserId;
            var site = _appSession.LoggedSite;
            var dept = _appSession.LoggedDepartment ?? await _db.Departments.OrderBy(d => d.Id).FirstOrDefaultAsync();

            var admissionDateTime = CombineDateAndTime(DateOfAdmission, TimeOfAdmission);
            var bht = string.IsNullOrWhiteSpace(BhtNo) ? await GenerateBhtAsync() : BhtNo.Trim();

            var encounter = new PatientEncounter
            {
                PatientId = SelectedPatientId.Value,
                EncounterType = EncounterType.Inward,
                DateOfAdmission = admissionDateTime,
                TimeOfAdmission = admissionDateTime,
                BhtNo = bht,
                BhtLong = bht,
                AdmissionType = AdmissionType,
                ReferringConsultantId = ReferringConsultantId,
                OpdDoctorId = OpdDoctorId,
                PaymentMethod = PaymentMethod,
                CreditCompanyId = CreditCompanyId,
                CreditLimit = CreditLimit,
                ReferenceNo = ReferenceNo,
                PolicyNo = PolicyNo,
                Claimable = Claimable,
                Weight = Weight,
                Height = Height,
                Bmi = Weight.HasValue && Height.HasValue && Height.Value > 0
                    ? Math.Round(Weight.Value / Math.Pow(Height.Value / 100.0, 2), 2)
                    : (double?)null,
                Sbp = Sbp,
                Dbp = Dbp,
                Pr = Pr,
                RespiratoryRate = RespiratoryRate,
                Saturation = Saturation,
                Pfr = Pfr,
                EncounterComments = Comments,
                InstitutionId = site?.Id ?? dept?.InstitutionId,
                DepartmentId = dept?.Id,
                CreaterId = userId,
                CreatedAt = now
            };
            _db.PatientEncounters.Add(encounter);
            await _db.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(GuardianName))
            {
                var guardian = new Person
                {
                    Name = GuardianName.Trim(),
                    NameWithInitials = GuardianName.Trim(),
                    Nic = GuardianNic,
                    Mobile = GuardianMobile,
                    CreatedAt = now,
                    CreaterId = userId
                };
                _db.Persons.Add(guardian);
                await _db.SaveChangesAsync();

                encounter.GuardianId = guardian.Id;
                encounter.GuardianRelationshipToPatientId = GuardianRelationshipToPatientId;
            }

            var admission = new Admission
            {
                PatientEncounterId = encounter.Id,
                AdmissionDate = admissionDateTime,
                CreaterId = userId,
                CreatedAt = now
            };
            _db.Admissions.Add(admission);
            await _db.SaveChangesAsync();

            if (RoomId != null)
            {
                var patientRoom = new PatientRoom
                {
                    RoomId = RoomId.Value,
                    PatientEncounterId = encounter.Id,
                    AdmissionId = admission.Id,
                    FromTime = now,
                    CreaterId = userId,
                    CreatedAt = now
                };
                _db.PatientRooms.Add(patientRoom);
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Patient admitted. BHT: {bht}";
            return RedirectToPage("/Inward/PatientList");
        }

        private async Task LoadAsync(long? patientId, string searchTerm)
        {
            await LoadLookupsAsync();

            Rooms = await _db.Rooms.Where(r => !r.Retired).OrderBy(r => r.Name).ToListAsync();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                SearchResults = await _commonService.SearchPatientsAsync(searchTerm);
            }

            SelectedPatientId = patientId;
            if (patientId != null)
            {
                SelectedPatient = await _db.Patients
                    .Include(p => p.Person)
                    .FirstOrDefaultAsync(p => p.Id == patientId && !p.Retired);
            }

            if (TempData["Success"] != null)
                Message = TempData["Success"].ToString();
        }

        private async Task LoadLookupsAsync()
        {
            DoctorList = await _db.Doctors
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

            CreditCompanyList = await _db.Institutions
                .Where(i => !i.Retired)
                .OrderBy(i => i.Name)
                .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = i.Name ?? ("Institution #" + i.Id) })
                .ToListAsync();

            RelationList = await _db.Categories
                .OfType<Relation>()
                .Where(r => !r.Retired)
                .OrderBy(r => r.Name)
                .Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.Name })
                .ToListAsync();

            AdmissionTypeList = new List<SelectListItem>
            {
                new SelectListItem("Admission", "Admission"),
                new SelectListItem("Day Case", "DayCase"),
                new SelectListItem("Emergency", "Emergency"),
                new SelectListItem("Elective", "Elective")
            };

            PaymentMethodList = new List<SelectListItem>
            {
                new SelectListItem("Cash", "Cash"),
                new SelectListItem("Credit", "Credit"),
                new SelectListItem("Patient Deposit", "PatientDeposit"),
                new SelectListItem("Voucher", "Voucher"),
                new SelectListItem("Staff", "Staff")
            };
        }

        private async Task<string> GenerateBhtAsync()
        {
            const string prefix = "BHT";
            var last = await _db.PatientEncounters
                .Where(e => e.BhtNo != null && e.BhtNo.StartsWith(prefix))
                .OrderByDescending(e => e.Id)
                .Select(e => e.BhtNo)
                .FirstOrDefaultAsync();

            int seq = 1;
            if (!string.IsNullOrWhiteSpace(last) && int.TryParse(last.Substring(prefix.Length), out var parsed))
                seq = parsed + 1;

            return prefix + seq.ToString("D6");
        }

        private static DateTime? CombineDateAndTime(DateTime? date, string time)
        {
            if (date == null) return null;

            if (TimeSpan.TryParse(time, out var ts))
                return date.Value.Date.Add(ts);

            return date.Value.Date;
        }
    }
}