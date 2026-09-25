using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Opd
{
    public class EmrModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ICommonService _common;
        private readonly IAppSession _appSession;

        public EmrModel(ApplicationDbContext db, ICommonService common, IAppSession appSession)
        {
            _db = db;
            _common = common;
            _appSession = appSession;
        }

        public const string PatientKey = "EmrPatientId";

        [BindProperty]
        public string PatientSearch { get; set; }

        [BindProperty]
        public long? SelectedPatientId { get; set; }

        public Patient SelectedPatient { get; set; }
        public List<Patient> PatientResults { get; set; } = new();

        // ===== Clinical record =====
        [BindProperty]
        public long? EditId { get; set; }

        [BindProperty]
        public DateTime? VisitDate { get; set; }

        [BindProperty]
        public string VisitTime { get; set; }

        [BindProperty]
        public long? DoctorId { get; set; }

        [BindProperty]
        public long? ReferredById { get; set; }

        [BindProperty]
        public string ChiefComplaint { get; set; }

        [BindProperty]
        public string HistoryOfPresentingComplaint { get; set; }

        [BindProperty]
        public string PastMedicalHistory { get; set; }

        [BindProperty]
        public string Examination { get; set; }

        [BindProperty]
        public string Diagnosis { get; set; }

        [BindProperty]
        public string Investigations { get; set; }

        [BindProperty]
        public List<long> SelectedInvestigationIds { get; set; } = new();

        [BindProperty]
        public string TreatmentPlan { get; set; }

        [BindProperty]
        public string Management { get; set; }

        [BindProperty]
        public string FollowUp { get; set; }

        // ===== Vitals =====
        [BindProperty]
        public double? Weight { get; set; }

        [BindProperty]
        public double? Height { get; set; }

        [BindProperty]
        public double? Bmi { get; set; }

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

        // ===== Lookups / listings =====
        public List<SelectListItem> DoctorList { get; set; } = new();
        public List<SelectListItem> ReferredByList { get; set; } = new();
        public List<SelectListItem> InvestigationList { get; set; } = new();
        public List<OpdVisit> RecentVisits { get; set; } = new();
        public List<Prescription> CurrentPrescriptions { get; set; } = new();
        public List<InvestigationOrder> CurrentInvestigationOrders { get; set; } = new();
        public string Message { get; set; }
        public string SuccessMessage { get; set; }

        public async Task OnGetAsync(long? id)
        {
            await LoadStateAsync(id);
        }

        public async Task<IActionResult> OnPostSearchPatientsAsync()
        {
            await LoadStateAsync(null);
            PatientResults = await _common.SearchPatientsAsync(PatientSearch);
            return Page();
        }

        public async Task<IActionResult> OnPostSelectPatientAsync()
        {
            if (SelectedPatientId == null)
                return RedirectToPage();

            var patient = await _db.Patients
                .Include(p => p.Person)
                .FirstOrDefaultAsync(p => p.Id == SelectedPatientId && !p.Retired);
            if (patient == null)
                return RedirectToPage();

            HttpContext.Session.SetString(PatientKey, patient.Id.ToString());
            TempData["EmrMessage"] = $"Patient selected: {patient.Person?.NameWithInitials ?? patient.Person?.Name ?? ("Patient #" + patient.Id)}";
            return RedirectToPage("/Opd/Emr");
        }

        public IActionResult OnPostStartNewAsync()
        {
            HttpContext.Session.Remove(PatientKey);
            return RedirectToPage("/Opd/Emr");
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            var patientId = GetSelectedPatientId();
            if (patientId == null)
            {
                await LoadLookupsAsync();
                Message = "Please select a patient before saving the EMR record.";
                return Page();
            }

            var patient = await _db.Patients
                .Include(p => p.Person)
                .FirstOrDefaultAsync(p => p.Id == patientId.Value && !p.Retired);
            if (patient == null)
            {
                await LoadLookupsAsync();
                Message = "Selected patient not found.";
                return Page();
            }

            DateTime now = DateTime.Now;
            var encounterDate = VisitDate ?? now.Date;

            OpdVisit visit;
            if (EditId.HasValue)
            {
                visit = await _db.PatientEncounters.OfType<OpdVisit>()
                    .FirstOrDefaultAsync(v => v.Id == EditId.Value && v.PatientId == patient.Id && !v.Retired);
                if (visit == null)
                {
                    await LoadLookupsAsync();
                    Message = "The EMR record to edit was not found.";
                    return Page();
                }
                visit.EditerId = _appSession.WebUserId;
                visit.EditedAt = now;
            }
            else
            {
                visit = new OpdVisit();
                _db.PatientEncounters.Add(visit);
                visit.CreaterId = _appSession.WebUserId;
                visit.CreatedAt = now;
                visit.InstitutionId = _appSession.LoggedSite?.Id;
                visit.DepartmentId = _appSession.LoggedDepartment?.Id;
            }

            visit.PatientId = patient.Id;
            visit.EncounterType = EncounterType.Opd;
            visit.EncounterDate = encounterDate;
            visit.EncounterTime = CombineDateAndTime(encounterDate, VisitTime);
            visit.OpdDoctorId = DoctorId;
            visit.ReferringStaffId = ReferredById;

            visit.ChiefComplaint = ChiefComplaint?.Trim();
            visit.HistoryOfPresentingComplaint = HistoryOfPresentingComplaint?.Trim();
            visit.PastMedicalHistory = PastMedicalHistory?.Trim();
            visit.Examination = Examination?.Trim();
            visit.Diagnosis = Diagnosis?.Trim();
            visit.TreatmentPlan = TreatmentPlan?.Trim();
            visit.Management = Management?.Trim();
            visit.FollowUp = FollowUp?.Trim();

            visit.Investigations = BuildInvestigations();

            visit.Weight = Weight;
            visit.Height = Height;
            visit.Bmi = ComputeBmi(Weight, Height);
            visit.Sbp = Sbp;
            visit.Dbp = Dbp;
            visit.Pr = Pr;
            visit.RespiratoryRate = RespiratoryRate;
            visit.Saturation = Saturation;
            visit.Pfr = Pfr;

            await _db.SaveChangesAsync();

            TempData["EmrSuccess"] = EditId.HasValue
                ? $"EMR record #{visit.Id} updated."
                : $"OPD EMR record #{visit.Id} saved.";
            return RedirectToPage("/Opd/Emr", new { id = visit.Id });
        }

        private string BuildInvestigations()
        {
            var parts = new List<string>();
            if (SelectedInvestigationIds != null && SelectedInvestigationIds.Any())
            {
                var ids = SelectedInvestigationIds.Distinct().ToList();
                var names = _db.Items
                    .Where(i => ids.Contains(i.Id) && !i.Retired)
                    .Select(i => i.PrintName ?? i.Name)
                    .ToList();
                parts.AddRange(names);
            }
            if (!string.IsNullOrWhiteSpace(Investigations))
                parts.Add(Investigations.Trim());
            return string.Join("; ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        }

        private static DateTime? CombineDateAndTime(DateTime? date, string time)
        {
            if (date == null) return null;

            if (TimeSpan.TryParse(time, out var ts))
                return date.Value.Date.Add(ts);

            return date.Value.Date;
        }

        private static double? ComputeBmi(double? weight, double? height)
        {
            if (weight.HasValue && height.HasValue && height.Value > 0)
                return Math.Round(weight.Value / Math.Pow(height.Value / 100.0, 2), 2);
            return null;
        }

        private async Task LoadStateAsync(long? id)
        {
            await LoadLookupsAsync();
            SelectedPatient = await GetSelectedPatientAsync();
            if (SelectedPatient != null)
                RecentVisits = await GetRecentVisitsAsync(SelectedPatient.Id);

            OpdVisit editing = null;
            if (id.HasValue && SelectedPatient != null)
            {
                editing = await _db.PatientEncounters.OfType<OpdVisit>()
                    .Include(v => v.OpdDoctor).ThenInclude(d => d.Person)
                    .FirstOrDefaultAsync(v => v.Id == id.Value && v.PatientId == SelectedPatient.Id && !v.Retired);
            }

            if (editing != null)
            {
                EditId = editing.Id;
                VisitDate = editing.EncounterDate?.Date;
                VisitTime = editing.EncounterTime?.ToString(@"hh\:mm");
                DoctorId = editing.OpdDoctorId;
                ReferredById = editing.ReferringStaffId;
                ChiefComplaint = editing.ChiefComplaint;
                HistoryOfPresentingComplaint = editing.HistoryOfPresentingComplaint;
                PastMedicalHistory = editing.PastMedicalHistory;
                Examination = editing.Examination;
                Diagnosis = editing.Diagnosis;
                Investigations = editing.Investigations;
                TreatmentPlan = editing.TreatmentPlan;
                Management = editing.Management;
                FollowUp = editing.FollowUp;
                Weight = editing.Weight;
                Height = editing.Height;
                Bmi = editing.Bmi;
                Sbp = editing.Sbp;
                Dbp = editing.Dbp;
                Pr = editing.Pr;
                RespiratoryRate = editing.RespiratoryRate;
                Saturation = editing.Saturation;
                Pfr = editing.Pfr;

                CurrentPrescriptions = await _db.Prescriptions
                    .Where(p => p.PatientEncounterId == editing.Id && !p.Retired)
                    .OrderBy(p => p.MedicineName)
                    .ToListAsync();

                CurrentInvestigationOrders = await _db.InvestigationOrders
                    .Where(i => i.PatientEncounterId == editing.Id && !i.Retired)
                    .OrderBy(i => i.InvestigationName)
                    .ToListAsync();

                Message = $"Editing OPD EMR record #{editing.Id}. Saving below will update this record.";
            }

            if (TempData["EmrMessage"] != null)
                Message ??= TempData["EmrMessage"].ToString();
            if (TempData["EmrSuccess"] != null)
                SuccessMessage = TempData["EmrSuccess"].ToString();
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
                .FirstOrDefaultAsync(p => p.Id == id && !p.Retired);
        }

        private async Task<List<OpdVisit>> GetRecentVisitsAsync(long patientId)
        {
            return await _db.PatientEncounters.OfType<OpdVisit>()
                .Include(v => v.OpdDoctor).ThenInclude(d => d.Person)
                .Where(v => v.PatientId == patientId && !v.Retired)
                .OrderByDescending(v => v.EncounterDate)
                .ThenByDescending(v => v.Id)
                .Take(20)
                .ToListAsync();
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
            ReferredByList = DoctorList;

            InvestigationList = await _db.Items
                .Where(i => i.ItemType == ItemType.Investigation && i.Billable && !i.Retired)
                .OrderBy(i => i.Name)
                .Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = (i.Code ?? "") + " - " + (i.PrintName ?? i.Name)
                })
                .ToListAsync();
        }
    }
}