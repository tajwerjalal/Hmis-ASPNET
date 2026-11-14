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
    /// <summary>
    /// Inward Admission - IPD registration (legacy ipdreg.scx).
    ///
    /// The stay is registered against [BED_POS] / [WARD] (the empty [ROOM]
    /// master is no longer used), the first bed assignment is written to
    /// [BEDTRANSFER] and the current stay to [PATIENTROOM]. The panel-company
    /// data of the legacy register (employee, relation, card, reference, bed
    /// entitlement) is stored on [PATIENTENCOUNTER].
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 22-Oct-2026
    /// Time      : 17:40
    /// Change Log: 22-Oct-2026 - bed handling moved to BED_POS, company /
    ///             employee / diagnosis fields added.
    /// </summary>
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
        public long? BedId { get; set; }

        [BindProperty]
        public string Diagnosis { get; set; }

        [BindProperty]
        public string Caste { get; set; }

        [BindProperty]
        public string EmployeeCode { get; set; }

        [BindProperty]
        public string EmployeeName { get; set; }

        [BindProperty]
        public string EmployeeRelation { get; set; }

        [BindProperty]
        public DateTime? RefValid { get; set; }

        [BindProperty]
        public string CardNo { get; set; }

        [BindProperty]
        public string BedEntitlement { get; set; }

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

        /// <summary>
        /// Legacy PATIENT/REGISTER values for the selected patient. Code is the
        /// permanent PCODE / MRNo and RegNo is the latest visit registration.
        /// </summary>
        public HmisWeb.Models.Legacy.PatientLegacyInfo LegacyPatient { get; set; }

        private string UserLabel => _appSession.LoggedUser?.UserName
            ?? _appSession.LoggedUser?.RealUserName
            ?? _appSession.WebUserId?.ToString()
            ?? "system";
        public List<SelectListItem> WardList { get; set; } = new();
        public List<SelectListItem> BedList { get; set; } = new();

        public List<SelectListItem> DoctorList { get; set; } = new();
        public List<SelectListItem> CreditCompanyList { get; set; } = new();
        public List<SelectListItem> RelationList { get; set; } = new();
        public List<SelectListItem> AdmissionTypeList { get; set; } = new();
        public List<SelectListItem> PaymentMethodList { get; set; } = new();

        public string Message { get; set; }

        public async Task OnGetAsync(long? patientId, long? selectPatientId, string searchTerm)
        {
            SearchTerm = searchTerm;
            // The patient registration form returns selectPatientId, while the
            // admission search links use patientId. Accept both.
            SelectedPatientId = selectPatientId ?? patientId;
            DateOfAdmission = DateTime.Today;
            await LoadAsync(SelectedPatientId, searchTerm);
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

            // the patient is not re-loaded by the binder, so fetch it here for
            // the bed occupancy marker (PCODE) and the name checks
            SelectedPatient = await _db.Patients
                .Include(p => p.Person)
                .FirstOrDefaultAsync(p => p.Id == SelectedPatientId.Value && !p.Retired);

            if (SelectedPatient == null)
            {
                ModelState.AddModelError("SelectedPatientId", "Please select a patient.");
                await LoadAsync(null, SearchTerm);
                return Page();
            }

            var admissionDateTime = CombineDateAndTime(DateOfAdmission, TimeOfAdmission) ?? now;
            var bed = BedId.HasValue
                ? await _db.BedPositions.FirstOrDefaultAsync(b => b.Id == BedId.Value && !b.Retired)
                : null;

            var guardianCategoryName = GuardianRelationshipToPatientId.HasValue
                ? await _db.Categories
                    .Where(c => c.Id == GuardianRelationshipToPatientId.Value)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync()
                : null;
            // Legacy REGISTER.GRELATION stores a single-character relation code
            // (ipdreg.scx cbogrelation: Son/S, Daughter/D, Wife/W, Mother/M,
            // Father/F, Husband/B, Self/s, Nephew/N, Uncle/U). Keep the modern
            // Category link for the encounter, but persist the legacy code so
            // IpdBillReportService.RelationWord keeps working.
            var guardianRelation = MapGuardianRelationToLegacyCode(guardianCategoryName);

            // Keep legacy PATIENT.CASTE in step (ipdreg.scx txtcast). Only
            // overwrite when a value is supplied so an existing caste is preserved.
            if (!string.IsNullOrWhiteSpace(Caste))
                SelectedPatient.Caste = Caste.Trim();

            // REGISTER.REGNO is the visit identity (and the IPD BHT). PCODE stays
            // the patient's permanent code. The legacy admission fields are
            // written to REGISTER, not to a separate patient table.
            await _commonService.SyncPatientLegacyAsync(SelectedPatient, userId);
            var regNo = await _commonService.CreateRegisterVisitAsync(new RegisterVisitRequest
            {
                Type = "I",
                PCode = SelectedPatient.Code,
                PatientAppId = SelectedPatient.Id,
                VisitDate = admissionDateTime.Date,
                VisitTime = admissionDateTime.ToString("HH:mm"),
                Diagnosis = Diagnosis,
                CompanyId = CreditCompanyId,
                ConsultantId = ReferringConsultantId ?? OpdDoctorId,
                EmployeeCode = EmployeeCode,
                EmployeeRelation = EmployeeRelation,
                ReferenceNo = ReferenceNo,
                RefValid = RefValid,
                CardNo = CardNo,
                GuardianName = GuardianName,
                GuardianRelation = guardianRelation,
                BedNo = bed?.BedCode,
                BedEntitlement = BedEntitlement,
                CUserId = UserLabel,
                UserId = UserLabel,
                CStat = (Claimable || CreditCompanyId.HasValue) ? "A" : null
            });

            var bht = regNo.ToString();

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
                Diagnosis = string.IsNullOrWhiteSpace(Diagnosis) ? null : Diagnosis.Trim(),
                EmployeeCode = string.IsNullOrWhiteSpace(EmployeeCode) ? null : EmployeeCode.Trim(),
                EmployeeName = string.IsNullOrWhiteSpace(EmployeeName) ? null : EmployeeName.Trim(),
                EmployeeRelation = string.IsNullOrWhiteSpace(EmployeeRelation) ? null : EmployeeRelation.Trim(),
                RefValid = RefValid,
                CardNo = string.IsNullOrWhiteSpace(CardNo) ? null : CardNo.Trim(),
                BedEntitlement = string.IsNullOrWhiteSpace(BedEntitlement) ? null : BedEntitlement.Trim(),
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

            if (BedId.HasValue)
            {
                if (bed != null)
                {
                    bed.Status = "Occupied";
                    // PAT_CD in the legacy bed master is the permanent PCODE.
                    bed.PatientCode = SelectedPatient?.Code ?? SelectedPatientId?.ToString();
                    var nowStamp = DateTime.Now;
                    bed.UserId = _appSession.LoggedUser?.UserName ?? "SYSTEM";
                    bed.UDate = nowStamp.Date;
                    bed.UTime = nowStamp.ToString("HH:mm");

                    var ward = await _db.Wards.FirstOrDefaultAsync(w => w.Code == bed.WardCode && !w.Retired);

                    var patientRoom = new PatientRoom
                    {
                        BedPositionId = bed.Id,
                        WardId = ward?.Id,
                        PatientEncounterId = encounter.Id,
                        AdmissionId = admission.Id,
                        FromTime = admissionDateTime,
                        CreaterId = userId,
                        CreatedAt = now,
                        Retired = false
                    };
                    _db.PatientRooms.Add(patientRoom);

                    // initial bed assignment history
                    var initialTransfer = new BedTransfer
                    {
                        Type = "I",
                        RegNo = bht,
                        PatientEncounterId = encounter.Id,
                        AdmissionId = admission.Id,
                        FromBedPositionId = null,
                        ToBedPositionId = bed.Id,
                        FromBedCode = null,
                        ToBedCode = bed.BedCode,
                        TransferDate = admissionDateTime,
                        CreaterId = userId,
                        CreatedAt = now,
                        Retired = false,
                        UserId = _appSession.LoggedUser?.UserName ?? "SYSTEM",
                        UDate = now.Date,
                        UTime = now.ToString("HH:mm")
                    };
                    _db.BedTransfers.Add(initialTransfer);
                }
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Patient admitted. BHT: {bht}";
            return RedirectToPage("/Inward/PatientList");
        }

        private static string MapGuardianRelationToLegacyCode(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName)) return null;
            var key = categoryName.Trim().ToLowerInvariant();
            return key switch
            {
                "son" => "S",
                "daughter" => "D",
                "wife" => "W",
                "mother" => "M",
                "father" => "F",
                "husband" => "B",
                "self" => "s",
                "nephew" => "N",
                "uncle" => "U",
                _ => categoryName.Trim().Length <= 1 ? categoryName.Trim() : null
            };
        }

        private async Task LoadAsync(long? patientId, string searchTerm)
        {
            await LoadLookupsAsync();

            WardList = await _db.Wards.AsNoTracking()
                .Where(w => !w.Retired)
                .OrderBy(w => w.Code)
                .Select(w => new SelectListItem { Value = w.Code, Text = $"{w.Code} - {w.Name}" })
                .ToListAsync();

            BedList = await _db.BedPositions.AsNoTracking()
                .Where(b => !b.Retired && b.Status == "Vacant")
                .OrderBy(b => b.WardCode).ThenBy(b => b.BedCode)
                .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = $"{b.BedCode} ({b.WardCode})" })
                .ToListAsync();

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

                if (SelectedPatient != null)
                    LegacyPatient = await _commonService.GetPatientLegacyInfoAsync(SelectedPatient);
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