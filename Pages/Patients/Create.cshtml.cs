using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Patients
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;
        private readonly ICommonService _common;

        public CreateModel(ApplicationDbContext db, IAppSession appSession, ICommonService common)
        {
            _db = db;
            _appSession = appSession;
            _common = common;
        }

        private string UserLabel => _appSession.LoggedUser?.UserName
            ?? _appSession.LoggedUser?.RealUserName
            ?? _appSession.WebUserId?.ToString()
            ?? "system";

        [BindProperty]
        public long? Id { get; set; }

        [BindProperty]
        public Title? Title { get; set; }

        [BindProperty]
        public string PersonName { get; set; }

        [BindProperty]
        public string NameWithInitials { get; set; }

        [BindProperty]
        public string Surname { get; set; }

        [BindProperty]
        public DateTime? Dob { get; set; }

        [BindProperty]
        public Sex? Sex { get; set; }

        [BindProperty]
        public string Nic { get; set; }

        [BindProperty]
        public string Mobile { get; set; }

        [BindProperty]
        public string Phone { get; set; }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Address { get; set; }

        [BindProperty]
        public long? AreaId { get; set; }

        [BindProperty]
        public long? BloodGroupId { get; set; }

        [BindProperty]
        public long? CivilStatusId { get; set; }

        [BindProperty]
        public long? RaceId { get; set; }

        [BindProperty]
        public long? ReligionId { get; set; }

        [BindProperty]
        public long? OccupationId { get; set; }

        [BindProperty]
        public long? MembershipSchemeId { get; set; }

        [BindProperty]
        public bool Foreigner { get; set; }

        [BindProperty]
        public string Phn { get; set; }

        /// <summary>
        /// Latest visit registration details (from REGISTER) for the patient being
        /// edited. Displayed read-only so the patient master screen mirrors the
        /// legacy opdreg.scx registration block (Reg No / Reg Date / Reg Time).
        /// </summary>
        public string LatestRegNo { get; set; }
        public DateTime? LatestRegDate { get; set; }
        public string LatestRegTime { get; set; }

        [BindProperty]
        public long? PatientId { get; set; }

        /// <summary>
        /// Optional local URL to return to after a new patient is saved. Used by
        /// the lab screens so the new patient is selected on the lab indent form.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; }

        public bool IsEdit => Id != null;
        public string ErrorMessage { get; set; }

        public List<SelectListItem> TitleList { get; set; } = new();
        public List<SelectListItem> SexList { get; set; } = new();
        public List<SelectListItem> BloodGroupList { get; set; } = new();
        public List<SelectListItem> CivilStatusList { get; set; } = new();
        public List<SelectListItem> RaceList { get; set; } = new();
        public List<SelectListItem> ReligionList { get; set; } = new();
        public List<SelectListItem> OccupationList { get; set; } = new();
        public List<SelectListItem> AreaList { get; set; } = new();
        public List<SelectListItem> MembershipSchemeList { get; set; } = new();

        public async Task OnGetAsync(long? id)
        {
            await LoadLookupsAsync();

            if (id == null) return;

            var patient = await _db.Patients.Include(p => p.Person).FirstOrDefaultAsync(p => p.Id == id);
            if (patient == null) return;

            Id = patient.Id;
            Title = patient.Person?.Title;
            PersonName = patient.Person?.Name;
            NameWithInitials = patient.Person?.NameWithInitials;
            Surname = patient.Person?.Surname;
            Dob = patient.Person?.Dob;
            Sex = patient.Person?.Sex;
            Nic = patient.Person?.Nic;
            Mobile = patient.Person?.Mobile;
            Phone = patient.Person?.Phone;
            Email = patient.Person?.Email;
            Address = patient.Person?.Address;
            AreaId = patient.Person?.AreaId;
            BloodGroupId = patient.Person?.BloodGroupId;
            CivilStatusId = patient.Person?.CivilStatusId;
            RaceId = patient.Person?.RaceId;
            ReligionId = patient.Person?.ReligionId;
            OccupationId = patient.Person?.OccupationId;
            MembershipSchemeId = patient.Person?.MembershipSchemeId;
            Foreigner = patient.Person?.Foreigner ?? false;
            Phn = patient.Phn;
            PatientId = patient.PatientId;

            if (!string.IsNullOrWhiteSpace(patient.Code))
            {
                var latest = await _db.LegacyRegisters
                    .AsNoTracking()
                    .Where(r => r.PCode == patient.Code)
                    .OrderByDescending(r => r.RegNo)
                    .FirstOrDefaultAsync();
                if (latest != null)
                {
                    LatestRegNo = latest.RegNo;
                    LatestRegDate = latest.RDate;
                    LatestRegTime = latest.RTime;
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadLookupsAsync();

            if (string.IsNullOrWhiteSpace(PersonName))
            {
                ErrorMessage = "Full name is required.";
                if (Id != null) await LoadForEditAsync(Id);
                return Page();
            }

            var now = DateTime.Now;

            if (Id != null)
            {
                var patient = await _db.Patients.Include(p => p.Person).FirstOrDefaultAsync(p => p.Id == Id);
                if (patient == null || patient.Person == null)
                    return RedirectToPage("/patients/Index");

                ApplyPerson(patient.Person, now);
                patient.Phn = Phn;
                if (PatientId != null && PatientId > 0)
                    patient.PatientId = PatientId;
                patient.EditedAt = now;
                patient.EditerId = _appSession.WebUserId;

                // Keep the legacy PATIENT columns (NAME/SEX/AGE/ADDRESS/...) in
                // step with PERSON. This also issues a PCODE for imported patients
                // that do not have one yet.
                await _common.SyncPatientLegacyAsync(patient, _appSession.WebUserId, overwrite: true);
                return RedirectToPage("/patients/Details", new { id = patient.Id });
            }

            var person = new Person { CreatedAt = now, CreaterId = _appSession.WebUserId };
            ApplyPerson(person, now);

            var isInwardReturn = !string.IsNullOrWhiteSpace(ReturnUrl)
                && ReturnUrl.Contains("/inward", StringComparison.OrdinalIgnoreCase);
            var isOpdReturn = !string.IsNullOrWhiteSpace(ReturnUrl)
                && ReturnUrl.Contains("/opd", StringComparison.OrdinalIgnoreCase);

            var newPatient = new Patient
            {
                Phn = Phn,
                PatientId = this.PatientId,
                AType = "Y",
                CreatedAt = now,
                CreaterId = _appSession.WebUserId,
                CreatedInstitutionId = _appSession.LoggedSite?.Id
            };

            // Issues the permanent PATIENT.CODE (PCODE / MRNo) and fills the
            // legacy patient columns used by opdreg.scx / ipdreg.scx.
            newPatient = await _common.CreatePatientWithLegacyCodeAsync(
                person, newPatient, now, _appSession.WebUserId);

            // Coming from the lab/department screens: return with the new patient
            // selected so the indent form is ready. That visit also needs a
            // REGISTER row; OPD registration and IPD admission create their own.
            if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                long? visitRegNo = null;
                if (!isInwardReturn && !isOpdReturn)
                {
                    visitRegNo = await _common.CreateRegisterVisitAsync(new RegisterVisitRequest
                    {
                        Type = "O",
                        PCode = newPatient.Code,
                        PatientAppId = newPatient.Id,
                        VisitDate = now.Date,
                        VisitTime = now.ToString("HH:mm"),
                        CUserId = UserLabel,
                        UserId = UserLabel
                    });
                }

                TempData["LabSampleNotice"] = visitRegNo.HasValue
                    ? $"Patient '{PersonName.Trim()}' added and selected. Patient Code {newPatient.Code}, Reg No {visitRegNo}."
                    : $"Patient '{PersonName.Trim()}' added and selected. Patient Code {newPatient.Code}.";
                var separator = ReturnUrl.Contains('?') ? "&" : "?";
                return Redirect($"{ReturnUrl}{separator}selectPatientId={newPatient.Id}");
            }

            return RedirectToPage("/patients/Details", new { id = newPatient.Id });
        }

        private void ApplyPerson(Person p, DateTime now)
        {
            p.Title = Title;
            p.Name = PersonName.Trim();
            p.FullName = PersonName.Trim();
            p.NameWithInitials = string.IsNullOrWhiteSpace(NameWithInitials) ? PersonName.Trim() : NameWithInitials.Trim();
            p.Surname = Surname;
            p.Dob = Dob;
            p.Sex = Sex;
            p.Nic = Nic;
            p.Mobile = Mobile;
            p.Phone = Phone;
            p.Email = Email;
            p.Address = Address;
            p.AreaId = AreaId;
            p.BloodGroupId = BloodGroupId;
            p.CivilStatusId = CivilStatusId;
            p.RaceId = RaceId;
            p.ReligionId = ReligionId;
            p.OccupationId = OccupationId;
            p.MembershipSchemeId = MembershipSchemeId;
            p.Foreigner = Foreigner;
            p.EditedAt = now;
            p.EditerId = _appSession.WebUserId;
        }

        private async Task LoadForEditAsync(long? id)
        {
            if (id == null) return;
            var patient = await _db.Patients.Include(p => p.Person).FirstOrDefaultAsync(p => p.Id == id);
            if (patient == null) return;
            Id = patient.Id;
            Title = patient.Person?.Title;
            PersonName = patient.Person?.Name;
            NameWithInitials = patient.Person?.NameWithInitials;
            Surname = patient.Person?.Surname;
            Dob = patient.Person?.Dob;
            Sex = patient.Person?.Sex;
            Nic = patient.Person?.Nic;
            Mobile = patient.Person?.Mobile;
            Phone = patient.Person?.Phone;
            Email = patient.Person?.Email;
            Address = patient.Person?.Address;
            AreaId = patient.Person?.AreaId;
            BloodGroupId = patient.Person?.BloodGroupId;
            CivilStatusId = patient.Person?.CivilStatusId;
            RaceId = patient.Person?.RaceId;
            ReligionId = patient.Person?.ReligionId;
            OccupationId = patient.Person?.OccupationId;
            MembershipSchemeId = patient.Person?.MembershipSchemeId;
            Foreigner = patient.Person?.Foreigner ?? false;
            Phn = patient.Phn;
            PatientId = patient.PatientId;

            if (!string.IsNullOrWhiteSpace(patient.Code))
            {
                var latest = await _db.LegacyRegisters
                    .AsNoTracking()
                    .Where(r => r.PCode == patient.Code)
                    .OrderByDescending(r => r.RegNo)
                    .FirstOrDefaultAsync();
                if (latest != null)
                {
                    LatestRegNo = latest.RegNo;
                    LatestRegDate = latest.RDate;
                    LatestRegTime = latest.RTime;
                }
            }
        }

        private async Task LoadLookupsAsync()
        {
            TitleList = Enum.GetNames<Title>().Select(t => new SelectListItem { Value = t, Text = t }).ToList();
            SexList = Enum.GetNames<Sex>().Select(s => new SelectListItem { Value = s, Text = s }).ToList();

            BloodGroupList = await GetItemsByCodePrefixAsync("BG-");
            CivilStatusList = await GetItemsByCodePrefixAsync("CS-");
            RaceList = await GetItemsByCodePrefixAsync("RA-");
            ReligionList = await GetItemsByCodePrefixAsync("RL-");
            OccupationList = await GetItemsByCodePrefixAsync("OC-");

            AreaList = await _db.Areas
                .Where(a => !a.Retired)
                .OrderBy(a => a.Name)
                .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.Name })
                .ToListAsync();

            MembershipSchemeList = await _db.MembershipSchemes
                .Where(m => !m.Retired)
                .OrderBy(m => m.Name)
                .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Name })
                .ToListAsync();
        }

        private Task<List<SelectListItem>> GetItemsByCodePrefixAsync(string prefix) =>
            _db.Items
                .Where(i => !i.Retired && i.Code != null && i.Code.StartsWith(prefix))
                .OrderBy(i => i.Name)
                .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = i.Name })
                .ToListAsync();
    }
}