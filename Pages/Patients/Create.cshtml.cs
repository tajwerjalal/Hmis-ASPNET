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

        public CreateModel(ApplicationDbContext db, IAppSession appSession)
        {
            _db = db;
            _appSession = appSession;
        }

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

        [BindProperty]
        public long? PatientId { get; set; }

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

                await _db.SaveChangesAsync();
                return RedirectToPage("/patients/Details", new { id = patient.Id });
            }

            var person = new Person { CreatedAt = now, CreaterId = _appSession.WebUserId };
            ApplyPerson(person, now);
            _db.Persons.Add(person);
            await _db.SaveChangesAsync();

            long? phnId = PatientId;
            if (phnId == null || phnId <= 0)
            {
                long? maxId = await _db.Patients.MaxAsync(p => (long?)p.PatientId);
                phnId = (maxId ?? 0) + 1;
            }

            var newPatient = new Patient
            {
                PersonId = person.Id,
                Phn = Phn,
                PatientId = phnId,
                CreatedAt = now,
                CreaterId = _appSession.WebUserId,
                CreatedInstitutionId = _appSession.LoggedSite?.Id
            };
            _db.Patients.Add(newPatient);
            await _db.SaveChangesAsync();

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