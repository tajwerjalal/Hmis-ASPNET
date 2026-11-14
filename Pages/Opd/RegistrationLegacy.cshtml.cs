using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Models.Legacy;
using HmisWeb.Services;

namespace HmisWeb.Pages.Opd
{
    /// <summary>
    /// Legacy-style OPD Registration form (Phase 1 of Option A).
    /// Mirrors the legacy Visual FoxPro form <c>OPDREG.SCX</c> (menukey rg0110,
    /// findform opdregf): one REGISTER row per visit, PCODE is the permanent
    /// patient code, REGNO is the visit number.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 23-Nov-2026
    /// Time      : 09:00
    /// Change Log: 23-Nov-2026 - Phase 1 legacy OPD registration form created.
    /// </summary>
    public class RegistrationLegacyModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ICommonService _common;
        private readonly IAppSession _appSession;

        public RegistrationLegacyModel(ApplicationDbContext db, ICommonService common, IAppSession appSession)
        {
            _db = db;
            _common = common;
            _appSession = appSession;
        }

        private string UserLabel => _appSession.LoggedUser?.UserName
            ?? _appSession.LoggedUser?.RealUserName
            ?? _appSession.WebUserId?.ToString()
            ?? "system";

        // ---- Visit / registration fields (legacy REGISTER) ----
        [BindProperty] public long? RegNo { get; set; }
        [BindProperty] public DateTime? RegDate { get; set; }
        [BindProperty] public string RegTime { get; set; }
        [BindProperty] public string PCode { get; set; }

        // ---- Patient fields (legacy PATIENT) ----
        [BindProperty] public string PatientName { get; set; }
        [BindProperty] public int? Age { get; set; }
        [BindProperty] public string AType { get; set; } = "Y";
        [BindProperty] public DateTime? NewDob { get; set; }
        [BindProperty] public string Sex { get; set; }
        [BindProperty] public string Nic { get; set; }
        [BindProperty] public string Address { get; set; }
        [BindProperty] public string Phone1 { get; set; }
        [BindProperty] public string Phone2 { get; set; }
        [BindProperty] public string Phone3 { get; set; }

        // ---- Registration details ----
        [BindProperty] public string Diagnosis { get; set; }
        [BindProperty] public string ConCode { get; set; }
        [BindProperty] public string Cid { get; set; }
        [BindProperty] public string ECode { get; set; }
        [BindProperty] public string ERelation { get; set; }
        [BindProperty] public string RefNo { get; set; }
        [BindProperty] public DateTime? RefValid { get; set; }
        [BindProperty] public string CardNo { get; set; }

        // ---- Audit ----
        [BindProperty] public string CUserId { get; set; }
        [BindProperty] public string CDate { get; set; }
        [BindProperty] public string CTime { get; set; }
        [BindProperty] public string UserId { get; set; }
        [BindProperty] public string UDate { get; set; }
        [BindProperty] public string UTime { get; set; }

        // ---- Display-only (resolved from lookups) ----
        public string ConName { get; set; }
        public string CompName { get; set; }
        public string EName { get; set; }
        public string EDesg { get; set; }

        // ---- State ----
        public bool ChgMode { get; set; }
        public bool EditMode { get; set; }
        public bool ChkList { get; set; }
        public bool ChkTop { get; set; }
        public bool ChkBottom { get; set; }
        public long OldRegNo { get; set; }
        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }
        public string ValidMsg { get; set; }
        public bool ShowNewPatientModal { get; set; }

        [BindProperty]
        public string NewPCode { get; set; }

        public bool ShowPatientLookup { get; set; }

        public List<Patient> PatientSearchResults { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool NewPatient { get; set; }

        // ---- Lookups ----
        public List<SelectListItem> ConsultantList { get; set; } = new();
        public List<SelectListItem> CompanyList { get; set; } = new();
        public List<SelectListItem> EmployeeList { get; set; } = new();
        public List<SelectListItem> SexList { get; set; } = new();
        public List<SelectListItem> AgeUnitList { get; set; } = new();

        // ---- List tab ----
        public List<LegacyPatientRow> ListRows { get; set; } = new();

        // ---- Search ----
        [BindProperty] public string SearchTerm { get; set; }
        [BindProperty] public string PatientSearch { get; set; }

        public async Task OnGetAsync(long? regNo)
        {
            await LoadLookupsAsync();
            NewPCode = await _common.GeneratePatientCodeAsync(DateTime.Today);
            if (NewPatient) ShowNewPatientModal = true;
            if (regNo.HasValue)
            {
                await LoadRecordAsync(regNo.Value);
            }
            else
            {
                // point to first record
                var first = await _db.LegacyRegisters
                    .Where(r => r.Type == "O")
                    .OrderBy(r => r.RegNo)
                    .Select(r => r.RegNo)
                    .FirstOrDefaultAsync();
                if (first != null && long.TryParse(first, out var fn)) await LoadRecordAsync(fn);
            }
        }

        private async Task LoadRecordAsync(long regNo)
        {
            var reg = await _db.LegacyRegisters
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Type == "O" && r.RegNo == regNo.ToString());
            if (reg == null) { ErrorMessage = "Record not found."; return; }

            RegNo = long.TryParse(reg.RegNo, out var rn) ? rn : (long?)null;
            RegDate = reg.RDate;
            RegTime = reg.RTime;
            PCode = reg.PCode;

            // Patient
            if (!string.IsNullOrWhiteSpace(reg.PCode))
            {
                var pat = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Code == reg.PCode);
                if (pat != null)
                {
                    PatientName = pat.LegacyName;
                    Age = int.TryParse(pat.LegacyAge, out var a) ? a : (int?)null;
                    AType = string.IsNullOrWhiteSpace(pat.AType) ? "Y" : pat.AType;
                    Sex = pat.LegacySex;
                    Nic = pat.Nic;
                    Address = pat.Address;
                    Phone1 = pat.Phone1;
                    Phone2 = pat.Phone2;
                    Phone3 = pat.Phone3;
                }
            }
            Diagnosis = reg.Diagnose;
            ConCode = reg.ConCode;
            Cid = reg.Cid;
            ECode = reg.ECode;
            ERelation = reg.ERelation;
            RefNo = reg.RefNo;
            RefValid = DateTime.TryParse(reg.RefValid, out var rv) ? rv : (DateTime?)null;
            CardNo = reg.CardNo;

            CUserId = reg.CUserId;
            CDate = reg.CDate;
            CTime = reg.CTime;
            UserId = reg.UserId;
            UDate = reg.UDate;
            UTime = reg.UTime;

            // Resolve display names
            if (!string.IsNullOrWhiteSpace(ConCode))
                ConName = await _db.ConsultantMasters.AsNoTracking()
                    .Where(c => !c.Retired && c.Code == ConCode)
                    .Select(c => c.Name).FirstOrDefaultAsync();
            if (!string.IsNullOrWhiteSpace(Cid))
                CompName = await _db.CompanyMasters.AsNoTracking()
                    .Where(c => c.Id == Cid).Select(c => c.Name).FirstOrDefaultAsync();
            if (!string.IsNullOrWhiteSpace(Cid) && !string.IsNullOrWhiteSpace(ECode))
                EName = await _db.Database.SqlQueryRaw<string>(
                    "SELECT TOP 1 NAME FROM EMPLOYEE WHERE CID = {0} AND CODE = {1}", Cid, ECode)
                    .FirstOrDefaultAsync();
            if (!string.IsNullOrWhiteSpace(Cid) && !string.IsNullOrWhiteSpace(ECode))
                EDesg = await _db.Database.SqlQueryRaw<string>(
                    "SELECT TOP 1 DESG FROM EMPLOYEE WHERE CID = {0} AND CODE = {1}", Cid, ECode)
                    .FirstOrDefaultAsync();

            // navigation state
            var regNos = await _db.LegacyRegisters
                .Where(r => r.Type == "O").OrderBy(r => r.RegNo)
                .Select(r => r.RegNo).ToListAsync();
            var idx = regNos.IndexOf(reg.RegNo);
            ChkTop = idx <= 0;
            ChkBottom = idx >= regNos.Count - 1;
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            await LoadLookupsAsync();
            ChgMode = true;
            EditMode = false;
            OldRegNo = RegNo ?? 0;

            // newrecord
            var last = await _db.LegacyRegisters
                .Where(r => r.Type == "O")
                .OrderByDescending(r => r.RegNo)
                .Select(r => r.RegNo).FirstOrDefaultAsync();
            long next = 1;
            if (long.TryParse(last, out var ln)) next = ln + 1;
            RegNo = next;
            RegDate = DateTime.Today;
            RegTime = DateTime.Now.ToString("HH:mm");
            CUserId = UserLabel;
            CDate = DateTime.Today.ToString("dd/MM/yyyy");
            CTime = DateTime.Now.ToString("HH:mm");
            UserId = UserLabel;
            UDate = DateTime.Today.ToString("dd/MM/yyyy");
            UTime = DateTime.Now.ToString("HH:mm");

            // clear patient fields
            PCode = null; PatientName = null; Age = null; AType = "Y"; Sex = null; Nic = null;
            Address = null; Phone1 = null; Phone2 = null; Phone3 = null;
            Diagnosis = null; ConCode = null; Cid = null; ECode = null; ERelation = null;
            RefNo = null; RefValid = null; CardNo = null;
            ConName = null; CompName = null; EName = null; EDesg = null;

            return Page();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            await LoadLookupsAsync();
            if (RegNo == null) { ErrorMessage = "No record selected."; return Page(); }
            ChgMode = true;
            EditMode = true;
            OldRegNo = RegNo.Value;
            UserId = UserLabel;
            UDate = DateTime.Today.ToString("dd/MM/yyyy");
            UTime = DateTime.Now.ToString("HH:mm");
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            await LoadLookupsAsync();
            if (RegNo == null) { ErrorMessage = "No record selected."; return Page(); }

            var reg = await _db.LegacyRegisters.FirstOrDefaultAsync(r => r.Type == "O" && r.RegNo == RegNo.Value.ToString());
            if (reg == null) { ErrorMessage = "Record not found."; return Page(); }

            // billgen check
            if (reg.BillGen == true)
            {
                ErrorMessage = "Cannot delete a billed registration.";
                await LoadRecordAsync(RegNo.Value);
                return Page();
            }

            _db.LegacyRegisters.Remove(reg);
            await _db.SaveChangesAsync();

            SuccessMessage = "Record deleted.";
            // move to next/prev
            var next = await _db.LegacyRegisters.Where(r => r.Type == "O" && r.RegNo != RegNo.Value.ToString())
                .OrderBy(r => r.RegNo).Select(r => r.RegNo).FirstOrDefaultAsync();
            if (next != null && long.TryParse(next, out var nn)) await LoadRecordAsync(nn);
            else { RegNo = null; }
            ChgMode = false; EditMode = false;
            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            await LoadLookupsAsync();
            // validrec
            if (RegNo == null) { ValidMsg = "Invalid Registration No."; ErrorMessage = ValidMsg; return Page(); }
            if (string.IsNullOrWhiteSpace(PCode)) { ValidMsg = "Patient Code must be entered"; ErrorMessage = ValidMsg; return Page(); }

            // duplicate check (chgmode && !editmode)
            // (simplified: skip complex duplicate-regen; rely on PK)

            var regNoStr = RegNo.Value.ToString();
            RegisterLegacy reg;
            if (EditMode)
            {
                reg = await _db.LegacyRegisters.FirstOrDefaultAsync(r => r.Type == "O" && r.RegNo == regNoStr);
                if (reg == null) { ErrorMessage = "Record not found."; return Page(); }
            }
            else
            {
                reg = new RegisterLegacy { Type = "O", RegNo = regNoStr };
                _db.LegacyRegisters.Add(reg);
            }

            reg.RDate = RegDate;
            reg.RTime = RegTime;
            reg.PCode = PCode;
            reg.Diagnose = Diagnosis;
            reg.ConCode = ConCode;
            reg.Cid = Cid;
            reg.ECode = ECode;
            reg.ERelation = ERelation;
            reg.RefNo = RefNo;
            reg.RefValid = RefValid?.ToString("yyyy-MM-dd");
            reg.CardNo = CardNo;
            reg.CUserId = CUserId;
            reg.CDate = CDate;
            reg.CTime = CTime;
            reg.UserId = UserId;
            reg.UDate = UDate;
            reg.UTime = UTime;
            reg.BillGen = false;

            // sync patient master (legacy columns)
            if (!string.IsNullOrWhiteSpace(PCode))
            {
                var pat = await _db.Patients.FirstOrDefaultAsync(p => p.Code == PCode);
                if (pat != null)
                {
                    pat.LegacyName = PatientName;
                    pat.LegacyAge = Age?.ToString();
                    pat.AType = string.IsNullOrWhiteSpace(AType) ? "Y" : AType;
                    pat.LegacySex = Sex;
                    pat.Nic = Nic;
                    pat.Address = Address;
                    pat.Phone1 = Phone1;
                    pat.Phone2 = Phone2;
                    pat.Phone3 = Phone3;
                    pat.RDate = RegDate?.ToString("yyyyMMdd");
                    // keep RegDate if empty
                    if (string.IsNullOrWhiteSpace(pat.RegDate)) pat.RegDate = RegDate?.ToString("yyyyMMdd");
                }
            }

            await _db.SaveChangesAsync();

            ChgMode = false; EditMode = false;
            SuccessMessage = "Record saved.";
            await LoadRecordAsync(RegNo.Value);
            await LoadLookupsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostRevertAsync()
        {
            await LoadLookupsAsync();
            if (OldRegNo > 0) await LoadRecordAsync(OldRegNo);
            else if (RegNo.HasValue) await LoadRecordAsync(RegNo.Value);
            ChgMode = false; EditMode = false;
            return Page();
        }

        public async Task<IActionResult> OnPostTopAsync()
        {
            await LoadLookupsAsync();
            var first = await _db.LegacyRegisters.Where(r => r.Type == "O").OrderBy(r => r.RegNo).Select(r => r.RegNo).FirstOrDefaultAsync();
            if (first != null && long.TryParse(first, out var n)) await LoadRecordAsync(n);
            return Page();
        }

        public async Task<IActionResult> OnPostPrevAsync()
        {
            await LoadLookupsAsync();
            if (RegNo == null) return Page();
            var prev = await _db.LegacyRegisters.Where(r => r.Type == "O" && r.RegNo.CompareTo(RegNo.Value.ToString()) < 0)
                .OrderByDescending(r => r.RegNo).Select(r => r.RegNo).FirstOrDefaultAsync();
            if (prev != null && long.TryParse(prev, out var n)) await LoadRecordAsync(n);
            return Page();
        }

        public async Task<IActionResult> OnPostNextAsync()
        {
            await LoadLookupsAsync();
            if (RegNo == null) return Page();
            var next = await _db.LegacyRegisters.Where(r => r.Type == "O" && r.RegNo.CompareTo(RegNo.Value.ToString()) > 0)
                .OrderBy(r => r.RegNo).Select(r => r.RegNo).FirstOrDefaultAsync();
            if (next != null && long.TryParse(next, out var n)) await LoadRecordAsync(n);
            return Page();
        }

        public async Task<IActionResult> OnPostBottomAsync()
        {
            await LoadLookupsAsync();
            var last = await _db.LegacyRegisters.Where(r => r.Type == "O").OrderByDescending(r => r.RegNo).Select(r => r.RegNo).FirstOrDefaultAsync();
            if (last != null && long.TryParse(last, out var n)) await LoadRecordAsync(n);
            return Page();
        }

        public async Task<IActionResult> OnPostSearchPatientAsync()
        {
            await LoadLookupsAsync();
            if (string.IsNullOrWhiteSpace(PatientSearch)) return Page();
            var pat = await _db.Patients.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Code == PatientSearch.Trim() || p.Phn == PatientSearch.Trim());
            if (pat != null)
            {
                PCode = pat.Code;
                PatientName = pat.LegacyName;
                Age = int.TryParse(pat.LegacyAge, out var a) ? a : (int?)null;
                AType = string.IsNullOrWhiteSpace(pat.AType) ? "Y" : pat.AType;
                Sex = pat.LegacySex;
                Nic = pat.Nic;
                Address = pat.Address;
                Phone1 = pat.Phone1;
                Phone2 = pat.Phone2;
                Phone3 = pat.Phone3;
            }
            else ErrorMessage = "Patient not found.";
            return Page();
        }

        public async Task<IActionResult> OnPostCreatePatientAsync()
        {
            await LoadLookupsAsync();
            if (string.IsNullOrWhiteSpace(PatientName))
            {
                ErrorMessage = "Patient name is required.";
                return Page();
            }
            if (NewDob.HasValue)
            {
                var age = DateTime.Today.Year - NewDob.Value.Year;
                if (NewDob.Value.Date > DateTime.Today.AddYears(-age)) age--;
                if (age < 0) age = 0;
                if (!Age.HasValue) Age = age;
                if (string.IsNullOrWhiteSpace(AType)) AType = "Y";
            }
            var now = DateTime.Now;
            var person = new Person { Name = PatientName, Nic = Nic, Sex = Enum.TryParse<Sex>(Sex, out var sx) ? sx : (Sex?)null, Address = Address, Phone = Phone1, Mobile = Phone2, Dob = NewDob, CreatedAt = now, CreaterId = _appSession.WebUserId };
            var patient = new Patient { Code = string.IsNullOrWhiteSpace(NewPCode) ? null : NewPCode, AType = string.IsNullOrWhiteSpace(AType) ? "Y" : AType, LegacyAge = Age?.ToString(), CreatedAt = now, CreaterId = _appSession.WebUserId };
            patient = await _common.CreatePatientWithLegacyCodeAsync(person, patient, now, _appSession.WebUserId);
            patient = await _common.CreatePatientWithLegacyCodeAsync(person, patient, now, _appSession.WebUserId);
            PCode = patient.Code;
            SuccessMessage = $"Patient created. Code {patient.Code}";
            NewPCode = await _common.GeneratePatientCodeAsync(DateTime.Today);

            if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                var isInwardReturn = ReturnUrl.Contains("/inward", StringComparison.OrdinalIgnoreCase);
                var isOpdReturn = ReturnUrl.Contains("/opd", StringComparison.OrdinalIgnoreCase);
                long? visitRegNo = null;
                if (!isInwardReturn && !isOpdReturn)
                {
                    visitRegNo = await _common.CreateRegisterVisitAsync(new RegisterVisitRequest
                    {
                        Type = "O",
                        PCode = patient.Code,
                        PatientAppId = patient.Id,
                        VisitDate = now.Date,
                        VisitTime = now.ToString("HH:mm"),
                        CUserId = UserLabel,
                        UserId = UserLabel
                    });
                }
                TempData["LabSampleNotice"] = visitRegNo.HasValue
                    ? $"Patient '{person.Name}' added and selected. Patient Code {patient.Code}, Reg No {visitRegNo}."
                    : $"Patient '{person.Name}' added and selected. Patient Code {patient.Code}.";
                var separator = ReturnUrl.Contains('?') ? "&" : "?";
                return Redirect($"{ReturnUrl}{separator}selectPatientId={patient.Id}");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostOpenPatientLookupAsync()
        {
            await LoadLookupsAsync();
            ShowPatientLookup = true;
            PatientSearchResults = await _db.Patients.AsNoTracking()
                .OrderBy(p => p.Code)
                .Take(50)
                .ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSearchPatientLookupAsync(string term)
        {
            await LoadLookupsAsync();
            ShowPatientLookup = true;
            var q = _db.Patients.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(term))
            {
                var t = term.Trim();
                q = q.Where(p => p.Code == t || (p.LegacyName != null && p.LegacyName.Contains(t)) || p.Phn == t || p.Phone1 == t || p.Phone2 == t);
            }
            PatientSearchResults = await q.OrderBy(p => p.Code).Take(50).ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSelectPatientLookupAsync(long patientId)
        {
            await LoadLookupsAsync();
            var pat = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == patientId);
            if (pat != null)
            {
                PCode = pat.Code;
                PatientName = pat.LegacyName;
                Age = int.TryParse(pat.LegacyAge, out var a) ? a : (int?)null;
                AType = string.IsNullOrWhiteSpace(pat.AType) ? "Y" : pat.AType;
                Sex = pat.LegacySex;
                Nic = pat.Nic;
                Address = pat.Address;
                Phone1 = pat.Phone1;
                Phone2 = pat.Phone2;
                Phone3 = pat.Phone3;
            }
            ShowPatientLookup = false;
            return Page();
        }
        public async Task<IActionResult> OnPostFindAsync()
        {
            await LoadLookupsAsync();
            // search by regno
            if (long.TryParse(SearchTerm, out var rn))
            {
                var reg = await _db.LegacyRegisters.AsNoTracking().FirstOrDefaultAsync(r => r.Type == "O" && r.RegNo == rn.ToString());
                if (reg != null && long.TryParse(reg.RegNo, out var n)) { await LoadRecordAsync(n); return Page(); }
            }
            // search by pcode
            var byPcode = await _db.LegacyRegisters.AsNoTracking().FirstOrDefaultAsync(r => r.Type == "O" && r.PCode == SearchTerm.Trim());
            if (byPcode != null && long.TryParse(byPcode.RegNo, out var n2)) { await LoadRecordAsync(n2); return Page(); }
            ErrorMessage = "Record not found.";
            return Page();
        }

        public async Task<IActionResult> OnPostLoadListAsync()
        {
            await LoadLookupsAsync();
            ChkList = true;
            var rows = await _db.LegacyRegisters.AsNoTracking()
                .Where(r => r.Type == "O")
                .OrderByDescending(r => r.RegNo)
                .Take(200)
                .ToListAsync();
            var result = new List<LegacyPatientRow>();
            foreach (var r in rows)
            {
                var pat = string.IsNullOrWhiteSpace(r.PCode) ? null : await _db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Code == r.PCode);
                result.Add(new LegacyPatientRow
                {
                    RegNo = r.RegNo,
                    VisitRegNo = r.RegNo,
                    PCode = r.PCode,
                    PatientCode = r.PCode,
                    PatientAppId = pat?.Id ?? 0,
                    Name = pat?.LegacyName,
                    Age = pat?.LegacyAge,
                    Sex = pat?.LegacySex,
                    Phone = pat?.Phone1,
                    RDate = r.RDate,
                    RTime = r.RTime
                });
            }
            ListRows = result;
            return Page();
        }

        private async Task LoadLookupsAsync()
        {
            ConsultantList = await _db.ConsultantMasters.AsNoTracking()
                .Where(c => !c.Retired).OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Code, Text = c.Name ?? "" })
                .ToListAsync();
            CompanyList = await _db.CompanyMasters.AsNoTracking()
                .Where(c => c.Status != "I").OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id, Text = c.Name ?? "" })
                .ToListAsync();
            // employees from legacy EMPLOYEE table
            try
            {
                var rows = await _db.Database.SqlQueryRaw<EmployeeLookupRow>(
                    "SELECT TOP 500 CODE, NAME, DESG FROM EMPLOYEE ORDER BY NAME").AsNoTracking().ToListAsync();
                EmployeeList = rows.Where(r => !string.IsNullOrWhiteSpace(r.CODE))
                    .Select(r => new SelectListItem { Value = (r.CODE ?? "").Trim(), Text = $"{(r.CODE ?? "").Trim()} - {(r.NAME ?? "").Trim()}" })
                    .DistinctBy(x => x.Value).ToList();
            }
            catch { EmployeeList = new List<SelectListItem>(); }

            SexList = new List<SelectListItem>
            {
                new SelectListItem { Value = "M", Text = "Male" },
                new SelectListItem { Value = "F", Text = "Female" }
            };
            AgeUnitList = new List<SelectListItem>
            {
                new SelectListItem { Value = "Y", Text = "Years" },
                new SelectListItem { Value = "M", Text = "Months" },
                new SelectListItem { Value = "W", Text = "Weeks" },
                new SelectListItem { Value = "D", Text = "Days" },
                new SelectListItem { Value = "H", Text = "Hours" }
            };
        }

        public class EmployeeLookupRow
        {
            public string CODE { get; set; }
            public string NAME { get; set; }
            public string DESG { get; set; }
        }
    }
}
