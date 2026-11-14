using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf;

namespace HmisWeb.Pages.Opd
{
    /// <summary>
    /// OPD Registration - legacy opdreg.scx.
    ///
    /// Session / token based outpatient registration. The diagnosis entered on
    /// the form is copied to the created [PATIENTENCOUNTER] so the consultation
    /// starts with the presenting diagnosis.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 22-Oct-2026
    /// Time      : 18:45
    /// Change Log: 22-Oct-2026 - diagnosis field added to the encounter.
    /// </summary>
    public class RegistrationModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ICommonService _common;
        private readonly IAppSession _appSession;

        public RegistrationModel(ApplicationDbContext db, ICommonService common, IAppSession appSession)
        {
            _db = db;
            _common = common;
            _appSession = appSession;
        }

        private const string SessionKey = "RegSessionId";
        private const string PatientKey = "RegPatientId";

        [BindProperty(SupportsGet = true)]
        public DateTime? SessionDate { get; set; }

        [BindProperty]
        public long? SelectedSessionId { get; set; }

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
        public string NewAge { get; set; }

        [BindProperty]
        public string NewAgeType { get; set; } = "Y";

        [BindProperty]
        public string NewAddress { get; set; }

        [BindProperty]
        public Sex? NewSex { get; set; }

        [BindProperty]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        [BindProperty]
        public double TenderedAmount { get; set; }

        [BindProperty]
        public string Diagnosis { get; set; }

        [BindProperty]
        public string RefNo { get; set; }

        [BindProperty]
        public DateTime? RefValid { get; set; }

        [BindProperty]
        public string CardNo { get; set; }

        [BindProperty]
        public string EmployeeCode { get; set; }

        [BindProperty]
        public string EmployeeRelation { get; set; }

        [BindProperty]
        public string PatientType { get; set; } = "Self";

        [BindProperty]
        public long? PanelCompanyId { get; set; }

        /// <summary>
        /// Optional local URL to return to after the patient is created or the
        /// OPD registration is saved. The lab screens use this so a new patient
        /// added from the lab "+" button comes back already selected on the lab
        /// indent form.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; }

        /// <summary>
        /// Opens the Add New Patient modal immediately. Used by the lab quick-add
        /// flow so the operator lands directly on the patient form.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public bool NewPatient { get; set; }

        /// <summary>
        /// When checked (default) the registration only raises the OPD bill and
        /// the patient pays it at the Cashier. Uncheck to collect the fee here,
        /// which preserves the old counter-collection behaviour.
        /// </summary>
        [BindProperty]
        public bool PayAtCashier { get; set; } = true;

        public List<SelectListItem> CompanyList { get; set; } = new();
        public List<SelectListItem> EmployeeList { get; set; } = new();
        public List<SessionCard> Sessions { get; set; } = new();
        public OpdSession SelectedSession { get; set; }

        /// <summary>
        /// When the selected day has no sessions, the most recently created
        /// session day - offered as a one-click link so the desk can find the
        /// consultants that were set up on OPD Sessions.
        /// </summary>
        public DateTime? LatestSessionDate { get; set; }
        public Patient SelectedPatient { get; set; }

        /// <summary>
        /// Legacy PATIENT/REGISTER values for the selected patient: CODE is the
        /// permanent MRNo (PCODE) and RegNo is the latest visit registration.
        /// </summary>
        public HmisWeb.Models.Legacy.PatientLegacyInfo LegacyPatient { get; set; }

        public List<Patient> PatientResults { get; set; } = new();

        /// <summary>
        /// Search results joined from REGISTER and PATIENT. Shows the visit REGNO
        /// and RDATE alongside the permanent PCODE and patient demographics.
        /// </summary>
        public List<HmisWeb.Models.Legacy.LegacyPatientRow> LegacyPatientResults { get; set; } = new();
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Re-opens the Add New Patient modal after a server-side validation
        /// failure so the operator does not lose the values they typed.
        /// </summary>
        public bool ShowNewPatientModal { get; set; }
        public string SuccessMessage { get; set; }
        public ReceiptModel Receipt { get; set; }
        public double AppliedFee { get; private set; }
        public bool IsFirstVisit { get; private set; }

        public class SessionCard
        {
            public long Id { get; set; }
            public long? ConsultantId { get; set; }
            public string ConsultantName { get; set; }
            public string SpecialityName { get; set; }
            public string RoomNo { get; set; }
            public string FromTime { get; set; }
            public string ToTime { get; set; }
            public string Mobile { get; set; }
            public double Fee { get; set; }
            public double HospitalFee { get; set; }
            public int IssuedTokens { get; set; }
            public int TokenMax { get; set; }
            public int NextToken { get; set; }
            public OpdSessionStatus Status { get; set; }

            /// <summary>
            /// Clinic period inferred from the start time. Sessions do not store a
            /// period, so this keeps the grouped consultant list scannable:
            /// Morning before noon, Afternoon before 5 pm, Evening after that.
            /// </summary>
            public string Period
            {
                get
                {
                    if (TimeSpan.TryParse(FromTime, out var t))
                    {
                        if (t.TotalHours < 12) return "Morning";
                        if (t.TotalHours < 17) return "Afternoon";
                        return "Evening";
                    }

                    return "Session";
                }
            }
        }

        public class ReceiptModel
        {
            public long BillId { get; set; }
            public string HospitalName { get; set; }
            public string HospitalNo { get; set; }
            public DateTime ReceiptAt { get; set; }
            public string PatientName { get; set; }
            public long? PatientPhn { get; set; }
            public string PatientCode { get; set; }
            public string VisitRegNo { get; set; }
            public string TokenNumber { get; set; }
            public long SessionId { get; set; }
            public string ConsultantName { get; set; }
            public string RoomNo { get; set; }
            public double Fee { get; set; }
            public double HospitalFee { get; set; }
            public double Total { get; set; }
            public double Tendered { get; set; }
            public double Balance { get; set; }
            public string PaymentMethod { get; set; }
            public string Cashier { get; set; }
            public string PatientType { get; set; }
            public bool Paid { get; set; } = true;
            public double AmountDue { get; set; }
        }

        public class EmployeeLookupRow
        {
            public string CODE { get; set; }
            public string NAME { get; set; }
            public string DESG { get; set; }
        }

        public long? CurrentSessionId
        {
            get => long.TryParse(HttpContext.Session.GetString(SessionKey), out var v) ? v : null;
        }

        private string UserLabel => _appSession.LoggedUser?.UserName
            ?? _appSession.LoggedUser?.RealUserName
            ?? _appSession.WebUserId?.ToString()
            ?? "system";

        public async Task OnGetAsync(long? sessionId)
        {
            // arriving from the OPD Sessions "Register" action: select that session
            // and use its date, so the consultant list matches the session page
            if (sessionId != null)
            {
                var chosen = await _db.OpdSessions.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == sessionId && !s.Retired
                        && (s.Status == OpdSessionStatus.Scheduled
                            || s.Status == OpdSessionStatus.Active));

                if (chosen != null)
                {
                    SessionDate = chosen.SessionDate.Date;
                    HttpContext.Session.SetString(SessionKey, chosen.Id.ToString());
                    HttpContext.Session.Remove(PatientKey);
                }
            }

            if (SessionDate == null) SessionDate = DateTime.Today;
            SuccessMessage = TempData["RegSuccess"] as string;
            await LoadStateAsync();

            // Quick-add from the lab screens: open the patient form straight away.
            if (NewPatient) ShowNewPatientModal = true;
        }

        public async Task<IActionResult> OnPostSelectSessionAsync(long sessionId)
        {
            if (SessionDate == null) SessionDate = DateTime.Today;
            var s = await _db.OpdSessions.FirstOrDefaultAsync(x => x.Id == sessionId && !x.Retired);
            if (s != null && (s.Status == OpdSessionStatus.Scheduled || s.Status == OpdSessionStatus.Active))
            {
                HttpContext.Session.SetString(SessionKey, s.Id.ToString());
                HttpContext.Session.Remove(PatientKey);
            }
            return RedirectToPage("/Opd/Registration", new
            {
                SessionDate = SessionDate.Value.Date.ToString("yyyy-MM-dd"),
                returnUrl = string.IsNullOrWhiteSpace(ReturnUrl) ? null : ReturnUrl
            });
        }

        public async Task<IActionResult> OnPostSearchPatientAsync()
        {
            if (SessionDate == null) SessionDate = DateTime.Today;
            await LoadStateAsync();

            if (!string.IsNullOrWhiteSpace(PatientSearch))
            {
                // Registration-based search: one row per visit with REGISTER.REGNO,
                // REGISTER.RDATE and REGISTER.RTIME, joined to PATIENT on
                // REGISTER.PCODE = PATIENT.CODE.
                LegacyPatientResults = await _common.SearchLegacyPatientsAsync(PatientSearch.Trim());
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSelectPatientAsync(long patientId)
        {
            if (SessionDate == null) SessionDate = DateTime.Today;
            var p = await _db.Patients.AnyAsync(x => x.Id == patientId && !x.Retired);
            if (p)
                HttpContext.Session.SetString(PatientKey, patientId.ToString());
            return RedirectToPage("/Opd/Registration", new
            {
                SessionDate = SessionDate.Value.Date.ToString("yyyy-MM-dd"),
                returnUrl = string.IsNullOrWhiteSpace(ReturnUrl) ? null : ReturnUrl
            });
        }

        public async Task<IActionResult> OnPostCreatePatientAsync()
        {
            if (SessionDate == null) SessionDate = DateTime.Today;
            await LoadStateAsync();

            if (string.IsNullOrWhiteSpace(NewName))
            {
                ErrorMessage = "Patient name is required.";
                ShowNewPatientModal = true;
                return Page();
            }

            // A lab quick-add does not need an OPD consultant session - it only
            // needs the patient master record. The normal OPD flow still requires
            // a session before a patient can be registered.
            if (CurrentSessionId == null && string.IsNullOrWhiteSpace(ReturnUrl))
            {
                ErrorMessage = "Select a consultant session first.";
                ShowNewPatientModal = true;
                return Page();
            }

            var now = DateTime.Now;
            var person = new Person
            {
                Name = NewName.Trim(),
                NameWithInitials = string.IsNullOrWhiteSpace(NewNameWithInitials) ? NewName.Trim() : NewNameWithInitials.Trim(),
                Nic = NewNic?.Trim() ?? "",
                Mobile = NewMobile?.Trim() ?? "",
                Phone = NewPhone?.Trim() ?? "",
                Address = string.IsNullOrWhiteSpace(NewAddress) ? null : NewAddress.Trim(),
                Sex = NewSex,
                CreatedAt = now,
                CreaterId = _appSession.WebUserId
            };

            var patient = new Patient
            {
                AType = string.IsNullOrWhiteSpace(NewAgeType) ? "Y" : NewAgeType,
                LegacyAge = string.IsNullOrWhiteSpace(NewAge) ? null : NewAge.Trim(),
                CreatedAt = now,
                CreaterId = _appSession.WebUserId,
                CreatedInstitutionId = _appSession.LoggedSite?.Id
            };

            // Issues the permanent PATIENT.CODE (PCODE / MRNo) and fills the
            // legacy patient columns used by opdreg.scx.
            patient = await _common.CreatePatientWithLegacyCodeAsync(
                person, patient, SessionDate ?? DateTime.Today, _appSession.WebUserId);

            // Coming from the lab "+" button: hand the new patient straight back
            // to the lab screen so its indent form is filled with this patient.
            if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                // The lab quick-add has no OPD Register step, so the visit
                // REGISTER row (and its REGNO) is issued here. The patient's
                // PCODE never changes; a new REGNO is issued on the next visit.
                var regNo = await _common.CreateRegisterVisitAsync(new RegisterVisitRequest
                {
                    Type = "O",
                    PCode = patient.Code,
                    PatientAppId = patient.Id,
                    VisitDate = DateTime.Today,
                    VisitTime = now.ToString("HH:mm"),
                    CUserId = UserLabel,
                    UserId = UserLabel
                });

                TempData["LabSampleNotice"] =
                    $"Patient '{person.Name}' added and selected. Patient Code {patient.Code}, Reg No {regNo}.";
                var separator = ReturnUrl.Contains('?') ? "&" : "?";
                return Redirect($"{ReturnUrl}{separator}selectPatientId={patient.Id}");
            }

            HttpContext.Session.SetString(PatientKey, patient.Id.ToString());
            TempData["RegSuccess"] = $"Patient '{person.Name}' added and selected. Patient Code {patient.Code}. Continue with the payment below.";
            return RedirectToPage("/Opd/Registration", new { SessionDate = SessionDate.Value.Date.ToString("yyyy-MM-dd") });
        }

        public async Task<IActionResult> OnPostRegisterAsync()
        {
            if (SessionDate == null) SessionDate = DateTime.Today;
            await LoadStateAsync();

            var sessionId = CurrentSessionId;
            var patientId = long.TryParse(HttpContext.Session.GetString(PatientKey), out var pid) ? pid : (long?)null;

            if (sessionId == null || patientId == null)
            {
                ErrorMessage = "Select a consultant session and a patient to register.";
                return Page();
            }

            var session = await _db.OpdSessions
                .Include(s => s.Consultant).ThenInclude(c => c.Person)
                .Include(s => s.Speciality)
                .FirstOrDefaultAsync(s => s.Id == sessionId && !s.Retired);
            if (session == null || session.Status == OpdSessionStatus.Cancelled)
            {
                ErrorMessage = "The selected session is unavailable.";
                return Page();
            }

            var patient = await _db.Patients.Include(p => p.Person).FirstOrDefaultAsync(p => p.Id == patientId && !p.Retired);
            if (patient == null)
            {
                ErrorMessage = "The selected patient was not found.";
                return Page();
            }

            var issued = await _db.Tokens.CountAsync(t => t.SessionId == session.Id && !t.Retired);
            var nextNo = session.TokenStartingNo + issued;
            if (nextNo > session.TokenMax)
            {
                ErrorMessage = $"Session is full ({session.TokenMax} tokens). Please book with another consultant.";
                return Page();
            }

            var alreadyRegistered = await _db.PatientEncounters
                .AnyAsync(e => !e.Retired && e.PatientId == patient.Id
                    && e.OpdDoctorId == session.ConsultantId
                    && e.EncounterDate == session.SessionDate);
            if (alreadyRegistered)
            {
                ErrorMessage = $"This patient is already registered with {session.Consultant?.Person?.NameWithInitials ?? "this consultant"} today.";
                return Page();
            }

            var now = DateTime.Now;
            double fee = AppliedFee > 0 ? AppliedFee : session.Fee;
            double hosp = session.HospitalFee;
            double total = fee + hosp;
            double tendered = PaymentMethod == PaymentMethod.Cash ? Math.Max(total, TenderedAmount) : total;

            if (PatientType == "Gratis") total = 0;
            if (PatientType == "Panel" && PanelCompanyId.HasValue)
            {
                var companyRate = await _db.CompanyServiceRates
                    .FirstOrDefaultAsync(r => r.CompanyId == PanelCompanyId && !r.Retired);
                if (companyRate != null)
                {
                    fee = companyRate.Rate > 0 ? (double)companyRate.Rate : fee;
                    total = fee + hosp;
                }
            }

            // 1. Legacy visit row. REGISTER.REGNO is the visit identity and
            //    PATIENT.CODE (PCODE) is the permanent patient MRNo. Billing and
            //    the lab screens use REGNO, so this row is created before the
            //    modern encounter/bill/token rows.
            await _common.SyncPatientLegacyAsync(patient, _appSession.WebUserId);
            var visitRegNo = await _common.CreateRegisterVisitAsync(new RegisterVisitRequest
            {
                Type = "O",
                PCode = patient.Code,
                PatientAppId = patient.Id,
                VisitDate = session.SessionDate,
                VisitTime = now.ToString("HH:mm"),
                Diagnosis = Diagnosis,
                CompanyId = PanelCompanyId,
                ConsultantId = session.ConsultantId,
                EmployeeCode = EmployeeCode,
                EmployeeRelation = EmployeeRelation,
                ReferenceNo = RefNo,
                RefValid = RefValid,
                CardNo = CardNo,
                CUserId = UserLabel,
                UserId = UserLabel,
                CStat = PatientType == "Panel" ? "A" : null
            });

            // 2. Clinical encounter
            // A zero-value registration has nothing to collect, so it is treated
            // as settled. Otherwise PayAtCashier leaves the bill OPEN for the
            // Cashier module (Cash Receipts unpaid list).
            bool billOnly = PayAtCashier && total > 0.001;

            var encounter = new OpdVisit
            {
                PatientId = patient.Id,
                EncounterType = EncounterType.Opd,
                EncounterDate = session.SessionDate,
                EncounterTime = Combine(session.SessionDate, session.FromTime),
                OpdDoctorId = session.ConsultantId,
                DepartmentId = session.DepartmentId ?? _appSession.LoggedDepartment?.Id,
                InstitutionId = session.InstitutionId ?? _appSession.LoggedSite?.Id,
                Name = patient.Person?.Name,
                EncounterComments = $"OPD consultation - {session.Consultant?.Person?.NameWithInitials ?? session.Consultant?.Person?.Name}",
                Diagnosis = string.IsNullOrWhiteSpace(Diagnosis) ? null : Diagnosis.Trim(),
                BhtNo = visitRegNo.ToString(),
                BhtLong = visitRegNo.ToString(),
                CreatedAt = now,
                CreaterId = _appSession.WebUserId
            };
            _db.PatientEncounters.Add(encounter);
            await _db.SaveChangesAsync();

            // 2. Bill (consultant fee tracked separately as ProfessionalFee)
            var tokenNo = nextNo.ToString("D3");
            var bill = new Bill
            {
                BillClassType = BillClassType.BilledBill,
                BillType = BillType.ChannelBill,
                BillTypeAtomic = BillTypeAtomic.ChannelBill,
                PaymentMethod = billOnly ? (PaymentMethod?)null : PaymentMethod,
                PatientId = patient.Id,
                PersonId = patient.PersonId,
                StaffId = session.ConsultantId,
                InstitutionId = session.InstitutionId ?? _appSession.LoggedSite?.Id,
                DepartmentId = session.DepartmentId ?? _appSession.LoggedDepartment?.Id,
                Total = total,
                NetTotal = total,
                BillTotal = total,
                GrantTotal = total,
                HospitalFee = hosp,
                TotalHospitalFee = hosp,
                ProfessionalFee = fee,
                TotalStaffFee = fee,
                PaidAmount = billOnly ? 0 : total,
                Paid = !billOnly,
                PaidAt = billOnly ? (DateTime?)null : now,
                BillPaymentCompletelySettled = !billOnly,
                Balance = billOnly ? total : 0,
                TenderedAmount = billOnly ? 0 : tendered,
                CashPaid = billOnly ? (double?)null : (PaymentMethod == PaymentMethod.Cash ? tendered : 0),
                CashBalance = billOnly ? (double?)null : (PaymentMethod == PaymentMethod.Cash ? tendered - total : 0),
                BillDate = session.SessionDate,
                BillTime = now,
                AppointmentAt = session.SessionDate.Date + Time( session.FromTime),
                SessionId = session.Id.ToString(),
                LocalNumber = tokenNo,
                Comments = "OPD consultation",
                PatientEncounterId = encounter.Id,
                CreatedAt = now,
                CreaterId = _appSession.WebUserId
            };
            _db.Bills.Add(bill);
            await _db.SaveChangesAsync();

            var billItem = new BillItem
            {
                BillId = bill.Id,
                ItemId1 = null,
                Qty = 1,
                Rate = total,
                NetRate = total,
                GrossValue = total,
                Discount = 0,
                NetValue = total,
                SearialNo = 1,
                Priority = Priority.Routine,
                BillItemStatus = BillItemStatus.Active,
                PatientEncounterId = encounter.Id,
                SessionId = session.Id.ToString(),
                SessionDate = session.SessionDate
            };
            _db.BillItems.Add(billItem);
            await _db.SaveChangesAsync();

            var payment = new Payment
            {
                BillId = bill.Id,
                PaymentMethod = PaymentMethod,
                PaidValue = total,
                Realized = true,
                RealizedAt = now,
                WrittenAt = now,
                CreatedAt = now,
                InstitutionId = session.InstitutionId ?? _appSession.LoggedSite?.Id,
                DepartmentId = session.DepartmentId ?? _appSession.LoggedDepartment?.Id,
                CreaterId = _appSession.WebUserId,
                Comments = $"OPD token {tokenNo}"
            };
            if (!billOnly)
            {
                _db.Payments.Add(payment);
                await _db.SaveChangesAsync();
            }

            var token = new Token
            {
                TokenId = $"OPD{session.SessionDate:yyyyMMdd}-{session.Id}-{tokenNo}",
                TokenNumber = tokenNo,
                TokenDate = session.SessionDate,
                TokenAt = now,
                IssuedAt = now,
                TokenType = TokenType.OPDTOKEN,
                PatientId = patient.Id,
                StaffId = session.ConsultantId,
                DoctorId = session.ConsultantId,
                DepartmentId = session.DepartmentId ?? _appSession.LoggedDepartment?.Id,
                InstitutionId = session.InstitutionId ?? _appSession.LoggedSite?.Id,
                BillId = bill.Id,
                PatientEncounterId = encounter.Id,
                SessionId = session.Id,
                CreatedById = _appSession.WebUserId,
                CreatedAt = now,
                Status = TokenStatus.Waiting
            };
            _db.Tokens.Add(token);
            await _db.SaveChangesAsync();

            // Build receipt
            Receipt = new ReceiptModel
            {
                BillId = bill.Id,
                HospitalName = _appSession.LoggedSite?.PrintingName ?? _appSession.LoggedSite?.Name ?? "HMIS Hospital",
                HospitalNo = _appSession.LoggedSite?.RegNo ?? _appSession.LoggedSite?.Code ?? "",
                ReceiptAt = now,
                PatientName = patient.Person?.NameWithInitials ?? patient.Person?.Name ?? "Patient",
                PatientPhn = patient.PatientId,
                PatientCode = patient.Code,
                VisitRegNo = visitRegNo.ToString(),
                TokenNumber = tokenNo,
                SessionId = session.Id,
                ConsultantName = session.Consultant?.Person?.NameWithInitials ?? session.Consultant?.Person?.Name ?? "Consultant",
                RoomNo = session.RoomNo,
                Fee = fee,
                HospitalFee = hosp,
                Total = total,
                Tendered = billOnly ? 0 : tendered,
                Balance = billOnly ? 0 : tendered - total,
                PaymentMethod = billOnly ? "Pay at Cashier" : PaymentMethod.ToString(),
                Paid = !billOnly,
                AmountDue = billOnly ? total : 0,
                Cashier = _appSession.LoggedUser?.WebUserPerson?.NameWithInitials
                    ?? _appSession.LoggedUser?.RealUserName
                    ?? _appSession.LoggedUser?.UserName
                    ?? "Cashier",
                PatientType = PatientType
            };

            HttpContext.Session.SetString("LastReceiptBillId", bill.Id.ToString());

            // Clear patient so cashier can register the next patient
            HttpContext.Session.Remove(PatientKey);

            // If this registration was launched from a lab screen, return to the
            // lab indent with the registered patient already selected.
            if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                TempData["LabSampleNotice"] = $"Patient registered. Patient Code {patient.Code}, Reg No {visitRegNo}. Continue with the lab tests.";
                var separator = ReturnUrl.Contains('?') ? "&" : "?";
                return Redirect($"{ReturnUrl}{separator}selectPatientId={patient.Id}");
            }

            return Page();
        }

        public IActionResult OnPostResetAsync()
        {
            HttpContext.Session.Remove(SessionKey);
            HttpContext.Session.Remove(PatientKey);
            return RedirectToPage("/Opd/Registration", new
            {
                returnUrl = string.IsNullOrWhiteSpace(ReturnUrl) ? null : ReturnUrl
            });
        }

        public async Task<IActionResult> OnGetPdfAsync(long? id)
        {
            if (!id.HasValue)
            {
                var stored = HttpContext.Session.GetString("LastReceiptBillId");
                if (stored != null) id = long.Parse(stored);
            }
            if (!id.HasValue) return NotFound();

            var r = await BuildReceiptForPdfAsync(id.Value);
            if (r == null) return NotFound();

            var bytes = BuildReceiptPdf(r);
            return File(bytes, "application/pdf", $"OPD-Receipt-{r.TokenNumber}.pdf");
        }

        private async Task<ReceiptModel?> BuildReceiptForPdfAsync(long billId)
        {
            var bill = await _db.Bills
                .Include(b => b.Patient).ThenInclude(p => p.Person)
                .Include(b => b.Staff).ThenInclude(s => s.Person)
                .FirstOrDefaultAsync(b => b.Id == billId && !b.Retired);
            if (bill == null) return null;

            var legacy = bill.Patient != null ? await _common.GetPatientLegacyInfoAsync(bill.Patient) : null;

            return new ReceiptModel
            {
                BillId = bill.Id,
                HospitalName = _appSession.LoggedSite?.PrintingName ?? _appSession.LoggedSite?.Name ?? "HMIS Hospital",
                HospitalNo = _appSession.LoggedSite?.RegNo ?? _appSession.LoggedSite?.Code ?? "",
                ReceiptAt = bill.CreatedAt ?? DateTime.Now,
                PatientName = bill.Patient?.Person?.NameWithInitials ?? bill.Patient?.Person?.Name ?? "Patient",
                PatientPhn = bill.Patient?.PatientId,
                PatientCode = legacy?.Code ?? bill.Patient?.Code,
                VisitRegNo = legacy?.RegNo,
                TokenNumber = bill.LocalNumber ?? bill.Id.ToString("D3"),
                ConsultantName = bill.Staff?.Person?.NameWithInitials ?? bill.Staff?.Person?.Name ?? "-",
                RoomNo = "-",
                Fee = bill.ProfessionalFee,
                HospitalFee = bill.HospitalFee,
                Total = bill.GrantTotal,
                Tendered = bill.TenderedAmount > 0 ? bill.TenderedAmount : bill.PaidAmount,
                Balance = bill.CashBalance ?? 0,
                PaymentMethod = bill.PaymentMethod?.ToString() ?? "Cash",
                Cashier = _appSession.LoggedUser?.WebUserPerson?.NameWithInitials
                    ?? _appSession.LoggedUser?.RealUserName
                    ?? "Cashier",
                PatientType = "Self"
            };
        }

        private byte[] BuildReceiptPdf(ReceiptModel r)
        {
            HmisFontResolver.Install();
            const double PW = 595.28;
            const double PH = 841.89;
            const double margin = 30;
            double ticketH = (PH - margin * 2 - 14) / 2;
            double secondTop = margin + ticketH + 14;

            using var ms = new MemoryStream();
            using (var doc = new PdfDocument())
            {
                var page = doc.AddPage();
                page.Width = PW;
                page.Height = PH;
                using (var g = XGraphics.FromPdfPage(page))
                {
                    DrawRegistrationTicket(g, r, "OFFICE COPY", margin, margin, PW - margin * 2, ticketH);
                    g.DrawLine(new XPen(XColor.FromArgb(140, 140, 140), 1), margin, secondTop - 8, PW - margin, secondTop - 8);
                    DrawRegistrationTicket(g, r, "PATIENT COPY", margin, secondTop, PW - margin * 2, ticketH);
                }
                doc.Save(ms, false);
            }
            return ms.ToArray();
        }

        private void DrawRegistrationTicket(XGraphics g, ReceiptModel r, string badge, double x, double y, double w, double h)
        {
            var pen = new XPen(XColor.FromArgb(70, 70, 70), 1);
            g.DrawRectangle(pen, XBrushes.White, x, y, w, h);

            double cx = x + 8;
            double cw = w - 16;
            double ty = y + 6;

            var fName = new XFont("Arial", 15, XFontStyle.Bold);
            var fMid = new XFont("Arial", 10.5, XFontStyle.Bold);
            var fSmall = new XFont("Arial", 9);
            var fFoot = new XFont("Arial", 8.5);
            var fHdr = new XFont("Arial", 9, XFontStyle.Bold);
            var fToken = new XFont("Arial", 22, XFontStyle.Bold);

            g.DrawString(badge, fSmall, XBrushes.Gray, new XRect(x + w - 68, ty, 60, 13), XStringFormats.Center);
            g.DrawString(r.HospitalName, fName, XBrushes.Black, new XRect(cx, ty, cw, 22), XStringFormats.Center); ty += 21;
            g.DrawString(r.HospitalNo, fSmall, XBrushes.Black, new XRect(cx, ty, cw, 14), XStringFormats.Center); ty += 14;
            g.DrawString("OPD CONSULTATION - PAYMENT & TOKEN RECEIPT", fMid, XBrushes.Black, new XRect(cx, ty, cw, 16), XStringFormats.Center); ty += 17;
            g.DrawString($"Receipt No: {r.BillId}    {r.ReceiptAt:dd MMM yyyy HH:mm}", fSmall, XBrushes.DarkGray, new XRect(cx, ty, cw, 14), XStringFormats.Center); ty += 16;
            g.DrawLine(pen, cx, ty, x + w - 8, ty); ty += 10;

            ty = Row(g, fSmall, "Patient", r.PatientName, cx, ty, cw);
            ty = Row(g, fSmall, "Patient Code", r.PatientCode ?? "-", cx, ty, cw);
            ty = Row(g, fSmall, "Reg No", r.VisitRegNo ?? "-", cx, ty, cw);
            ty = Row(g, fSmall, "PHN", r.PatientPhn?.ToString() ?? "-", cx, ty, cw);
            ty = Row(g, fSmall, "Patient Type", r.PatientType ?? "Self", cx, ty, cw);
            ty += 3;

            g.DrawLine(pen, cx, ty, x + w - 8, ty); ty += 8;

            g.DrawString("TOKEN", fSmall, XBrushes.Gray, new XRect(cx, ty, cw, 12), XStringFormats.Center); ty += 12;
            g.DrawString(r.TokenNumber, fToken, XBrushes.Black, new XRect(cx, ty, cw, 30), XStringFormats.Center); ty += 30;

            g.DrawLine(pen, cx, ty, x + w - 8, ty); ty += 8;

            ty = Row(g, fSmall, "Consultant", r.ConsultantName, cx, ty, cw);
            ty = Row(g, fSmall, "Room", r.RoomNo, cx, ty, cw);
            ty = Row(g, fSmall, "Payment", r.PaymentMethod, cx, ty, cw);
            ty += 5;

            g.DrawLine(pen, cx, ty, x + w - 8, ty); ty += 6;

            g.DrawString("Description", fHdr, XBrushes.Black, new XRect(cx, ty, cw * 0.6, 15), XStringFormats.CenterLeft);
            g.DrawString("Amount (Rs)", fHdr, XBrushes.Black, new XRect(cx + cw * 0.6, ty, cw * 0.4, 15), XStringFormats.CenterRight);
            ty += 16;
            g.DrawLine(pen, cx, ty, x + w - 8, ty); ty += 4;

            ty = Row(g, fSmall, "Consultation fee", r.Fee.ToString("N2"), cx, ty, cw);
            ty = Row(g, fSmall, "Hospital charges", r.HospitalFee.ToString("N2"), cx, ty, cw);
            ty += 3;
            g.DrawLine(pen, cx, ty, x + w - 8, ty); ty += 5;
            g.DrawString("Total paid", fMid, XBrushes.Black, new XRect(cx, ty, cw * 0.6, 17), XStringFormats.CenterLeft);
            g.DrawString("Rs " + r.Total.ToString("N2"), fMid, XBrushes.Black, new XRect(cx + cw * 0.6, ty, cw * 0.4, 17), XStringFormats.CenterRight);
            ty += 19;

            if (r.PaymentMethod == "Cash")
            {
                ty = Row(g, fSmall, "Tendered", r.Tendered.ToString("N2"), cx, ty, cw);
                ty = Row(g, fSmall, "Balance", r.Balance.ToString("N2"), cx, ty, cw);
            }

            double fy = y + h - 26;
            g.DrawString($"Cashier: {r.Cashier}  |  Thank you", fFoot, XBrushes.DarkGray, new XRect(cx, fy, cw, 13), XStringFormats.Center);
        }

        private static double Row(XGraphics g, XFont f, string label, string value, double x, double y, double w)
        {
            g.DrawString(label, f, XBrushes.Black, new XRect(x, y, w * 0.42, 15), XStringFormats.CenterLeft);
            g.DrawString(value ?? "-", f, XBrushes.Black, new XRect(x + w * 0.44, y, w * 0.56, 15), XStringFormats.CenterLeft);
            return y + 15;
        }

        private async Task LoadLookupsAsync()
        {
            CompanyList = await _db.Institutions
                .Where(i => !i.Retired && i.InstitutionType == InstitutionType.InsuranceCompany)
                .OrderBy(i => i.Name)
                .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = i.Name ?? "" })
                .ToListAsync();

            // Legacy employee lookup (opdreg.scx txtecode): code + name + designation,
            // filtered to non-retired rows, ordered by name. Capped to keep the
            // dropdown usable; the operator can still type a code via the text box.
            try
            {
                var rows = await _db.Database
                    .SqlQueryRaw<EmployeeLookupRow>("SELECT TOP 500 CODE, NAME, DESG FROM EMPLOYEE ORDER BY NAME")
                    .AsNoTracking()
                    .ToListAsync();
                EmployeeList = rows
                    .Where(r => !string.IsNullOrWhiteSpace(r.CODE))
                    .Select(r => new SelectListItem
                    {
                        Value = (r.CODE ?? "").Trim(),
                        Text = $"{(r.CODE ?? "").Trim()} - {(r.NAME ?? "").Trim()}" + (string.IsNullOrWhiteSpace(r.DESG) ? "" : $" ({r.DESG.Trim()})")
                    })
                    .DistinctBy(x => x.Value)
                    .ToList();
            }
            catch
            {
                EmployeeList = new List<SelectListItem>();
            }
        }

        private async Task LoadStateAsync()
        {
            await LoadLookupsAsync();
            var day = SessionDate?.Date ?? DateTime.Today;

            var counts = await _db.Tokens
                .Where(t => !t.Retired && t.SessionId != null)
                .GroupBy(t => t.SessionId)
                .Select(g => new { SessionId = g.Key, Count = g.Count() })
                .ToListAsync();

            Sessions = await _db.OpdSessions
                .Include(s => s.Consultant).ThenInclude(c => c.Person)
                .Include(s => s.Speciality)
                .Where(s => !s.Retired && s.SessionDate == day &&
                    (s.Status == OpdSessionStatus.Scheduled || s.Status == OpdSessionStatus.Active))
                .OrderBy(s => s.FromTime)
                .Select(s => new SessionCard
                {
                    Id = s.Id,
                    ConsultantId = s.ConsultantId,
                    ConsultantName = s.Consultant != null && s.Consultant.Person != null
                        ? (s.Consultant.Person.NameWithInitials ?? s.Consultant.Person.Name ?? "")
                        : "Unassigned",
                    SpecialityName = s.Speciality != null ? s.Speciality.Name : "",
                    RoomNo = s.RoomNo,
                    FromTime = s.FromTime,
                    ToTime = s.ToTime,
                    Mobile = s.Consultant != null && s.Consultant.Person != null ? s.Consultant.Person.Mobile : "",
                    Fee = s.Fee,
                    HospitalFee = s.HospitalFee,
                    TokenMax = s.TokenMax,
                    Status = s.Status
                })
                .ToListAsync();

            foreach (var s in Sessions)
            {
                var c = counts.FirstOrDefault(x => x.SessionId == s.Id);
                s.IssuedTokens = c?.Count ?? 0;
                s.NextToken = s.IssuedTokens + 1;
            }

            if (!Sessions.Any())
            {
                var latest = await _db.OpdSessions.AsNoTracking()
                    .Where(s => !s.Retired
                        && (s.Status == OpdSessionStatus.Scheduled
                            || s.Status == OpdSessionStatus.Active))
                    .OrderByDescending(s => s.Id)
                    .Select(s => (DateTime?)s.SessionDate)
                    .FirstOrDefaultAsync();

                if (latest != null && latest.Value.Date != day)
                    LatestSessionDate = latest.Value.Date;
            }

            var sid = CurrentSessionId;
            if (sid != null)
            {
                // a session selected on another day must not stay selected after the
                // user changes the date - the consultant list would no longer match
                SelectedSession = await _db.OpdSessions
                    .Include(s => s.Consultant).ThenInclude(c => c.Person)
                    .Include(s => s.Speciality)
                    .FirstOrDefaultAsync(s => s.Id == sid && !s.Retired
                        && s.SessionDate == day
                        && (s.Status == OpdSessionStatus.Scheduled
                            || s.Status == OpdSessionStatus.Active));

                if (SelectedSession == null)
                    HttpContext.Session.Remove(SessionKey);
            }

            if (long.TryParse(HttpContext.Session.GetString(PatientKey), out var pid))
            {
                SelectedPatient = await _db.Patients.Include(p => p.Person).FirstOrDefaultAsync(p => p.Id == pid && !p.Retired);
                if (SelectedPatient != null)
                {
                    PatientResults = new List<Patient> { SelectedPatient };
                    LegacyPatient = await _common.GetPatientLegacyInfoAsync(SelectedPatient);
                }
            }

            await ComputeAppliedFeeAsync();
        }

        private async Task ComputeAppliedFeeAsync()
        {
            AppliedFee = 0;
            if (SelectedSession == null || SelectedPatient == null) return;

            var priorVisits = await _db.PatientEncounters
                .CountAsync(e => !e.Retired && e.PatientId == SelectedPatient.Id
                    && e.OpdDoctorId == SelectedSession.ConsultantId
                    && e.EncounterDate < DateTime.Today);
            IsFirstVisit = priorVisits == 0;

            var consultant = SelectedSession.Consultant as Consultant;
            var firstFee = consultant?.FirstVisitFee ?? 0;
            var subFee = consultant?.SubsequentVisitFee ?? 0;
            var chosen = IsFirstVisit
                ? (firstFee > 0 ? firstFee : SelectedSession.Fee)
                : (subFee > 0 ? subFee : SelectedSession.Fee);
            AppliedFee = chosen > 0 ? chosen : SelectedSession.Fee;
        }

        private static DateTime? Combine(DateTime date, string time)
        {
            if (string.IsNullOrWhiteSpace(time)) return null;
            if (TimeSpan.TryParse(time, out var ts)) return date.Date.Add(ts);
            return date.Date;
        }

        private static TimeSpan Time(string time) => TimeSpan.TryParse(time, out var ts) ? ts : TimeSpan.Zero;
    }
}