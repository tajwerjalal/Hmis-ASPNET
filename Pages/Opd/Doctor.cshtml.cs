using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Opd
{
    public class DoctorModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;

        public DoctorModel(ApplicationDbContext db, IAppSession appSession)
        {
            _db = db;
            _appSession = appSession;
        }

        public List<OpdSession> Sessions { get; set; } = new();
        public List<PatientQueueItem> PatientQueue { get; set; } = new();
        public long? SelectedSessionId { get; set; }
        public PatientEncounter? CurrentEncounter { get; set; }
        public List<Models.Prescription> CurrentPrescriptions { get; set; } = new();
        public List<Models.InvestigationOrder> CurrentInvestigations { get; set; } = new();
        public List<Models.FollowUp> CurrentFollowUpList { get; set; } = new();
        public Models.FollowUp? CurrentFollowUp { get; set; }
        public List<SymptomItem> CurrentSymptoms { get; set; } = new();
        public List<DiagnosisItem> CurrentDiagnoses { get; set; } = new();
        public List<Doctor> Doctors { get; set; } = new();
        public string? SuccessMessage { get; set; }

        public async Task OnGetAsync(long? sessionId)
        {
            if (TempData["Success"] != null) SuccessMessage = TempData["Success"]!.ToString();

            var siteId = _appSession.LoggedSite?.Id;
            Sessions = await _db.OpdSessions
                .Include(s => s.Consultant)
                    .ThenInclude(c => c.Person)
                .Where(s => s.InstitutionId == siteId && s.SessionDate == DateTime.Today)
                .OrderBy(s => s.FromTime)
                .ToListAsync();

            Doctors = await _db.Doctors
                .Include(d => d.Person)
                .Where(d => !d.Retired)
                .ToListAsync();

            if (sessionId.HasValue)
            {
                SelectedSessionId = sessionId;
                await LoadPatientQueueAsync(sessionId.Value);
            }
        }

        private async Task LoadPatientQueueAsync(long sessionId)
        {
            var tokens = await _db.Tokens
                .Include(t => t.PatientEncounter).ThenInclude(e => e.Patient).ThenInclude(p => p.Person)
                .Where(t => t.SessionId == sessionId && !t.Retired)
                .OrderBy(t => t.TokenNumber)
                .ToListAsync();

            PatientQueue = tokens.Select(t => new PatientQueueItem
                {
                    TokenId = t.Id,
                    PatientId = t.PatientEncounter?.PatientId ?? 0,
                    EncounterId = t.PatientEncounterId ?? 0,
                    PatientName = t.PatientEncounter?.Patient?.Person?.NameWithInitials ?? t.PatientEncounter?.Patient?.Person?.Name ?? "",
                    Phn = t.PatientEncounter?.Patient?.Phn ?? "",
                    TokenNo = int.TryParse(t.TokenNumber, out var tn) ? tn : 0,
                    Status = t.Status?.ToString() ?? "Waiting",
                    ChiefComplaint = t.PatientEncounter?.ChiefComplaint ?? "",
                    Age = t.PatientEncounter?.Patient?.Person?.GetAgeString() ?? ""
                }).ToList();
        }

        public async Task<IActionResult> OnPostCallPatientAsync([FromBody] CallPatientRequest req)
        {
            var token = await _db.Tokens.FindAsync(req.TokenId);
            if (token == null) return new JsonResult(new { success = false, message = "Token not found" });

            token.Status = TokenStatus.Called;
            token.CalledAt = DateTime.Now;
            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostFinalizeAsync([FromBody] FinalizeRequest req)
        {
            var encounter = await _db.PatientEncounters.FindAsync(req.EncounterId);
            if (encounter == null) return new JsonResult(new { success = false, message = "Encounter not found" });

            var token = await _db.Tokens.FirstOrDefaultAsync(t => t.PatientEncounterId == req.EncounterId);
            if (token != null)
            {
                token.Status = TokenStatus.Completed;
                token.CompletedAt = DateTime.Now;
            }

            encounter.EditedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Consultation finalized.";
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostSaveClinicalNotesAsync([FromBody] ClinicalNotesRequest req)
        {
            var enc = await _db.PatientEncounters.FindAsync(req.EncounterId);
            if (enc == null) return new JsonResult(new { success = false, message = "Encounter not found" });

            enc.ChiefComplaint = req.ChiefComplaint;
            enc.HistoryOfPresentingComplaint = req.Hpc;
            enc.PastMedicalHistory = req.Pmh;
            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostSaveVitalsAsync([FromBody] VitalsRequest req)
        {
            var enc = await _db.PatientEncounters.FindAsync(req.EncounterId);
            if (enc == null) return new JsonResult(new { success = false, message = "Encounter not found" });

            enc.Weight = ParseDouble(req.Weight);
            enc.Height = ParseDouble(req.Height);
            enc.Sbp = ParseInt(req.BpSys);
            enc.Dbp = ParseInt(req.BpDia);
            enc.Pr = ParseInt(req.Pulse);
            enc.RespiratoryRate = ParseInt(req.Resp);
            enc.Saturation = ParseInt(req.Spo2);
            enc.Pfr = ParseInt(req.Temp);
            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostSaveExaminationAsync([FromBody] ExaminationRequest req)
        {
            var enc = await _db.PatientEncounters.FindAsync(req.EncounterId);
            if (enc == null) return new JsonResult(new { success = false, message = "Encounter not found" });

            enc.Examination = req.Examination;
            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostSaveNotesAsync([FromBody] NotesRequest req)
        {
            var enc = await _db.PatientEncounters.FindAsync(req.EncounterId);
            if (enc == null) return new JsonResult(new { success = false, message = "Encounter not found" });

            enc.TreatmentPlan = req.TreatmentPlan;
            enc.Management = req.Management;
            enc.EncounterComments = req.Notes;
            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostAddSymptomAsync([FromBody] SymptomRequest req)
        {
            var enc = await _db.PatientEncounters.FindAsync(req.EncounterId);
            if (enc == null) return new JsonResult(new { success = false, message = "Encounter not found" });

            var symptom = new SymptomItem
            {
                Id = DateTime.Now.Ticks,
                PatientEncounterId = req.EncounterId,
                Description = req.Description,
                Severity = req.Severity
            };
            CurrentSymptoms.Add(symptom);

            var notes = enc.EncounterComments ?? "";
            notes += $"[Symptom: {req.Description} (Severity {req.Severity})] ";
            enc.EncounterComments = notes;
            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostRemoveSymptomAsync([FromBody] RemoveRequest req)
        {
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostAddDiagnosisAsync([FromBody] DiagnosisRequest req)
        {
            var enc = await _db.PatientEncounters.FindAsync(req.EncounterId);
            if (enc == null) return new JsonResult(new { success = false, message = "Encounter not found" });

            var existing = enc.Diagnosis ?? "";
            var prefix = req.DiagnosisType == "Primary" ? "[Primary] " : req.DiagnosisType == "Secondary" ? "[Secondary] " : "[Differential] ";
            existing += $"{prefix}{req.DiagnosisName}; ";
            enc.Diagnosis = existing;
            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostRemoveDiagnosisAsync([FromBody] RemoveRequest req)
        {
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostAddPrescriptionAsync([FromBody] PrescriptionRequest req)
        {
            var enc = await _db.PatientEncounters.FindAsync(req.EncounterId);
            if (enc == null) return new JsonResult(new { success = false, message = "Encounter not found" });

            var rx = new Models.Prescription
            {
                PatientEncounterId = req.EncounterId,
                PatientId = req.PatientId,
                MedicineName = req.MedicineName,
                Dose = req.Dose,
                Frequency = req.Frequency,
                Duration = req.Duration,
                Route = req.Route,
                Instructions = req.Instructions,
                Qty = int.TryParse(req.Qty, out var q) ? q : 1,
                RequiredAction = req.RequiredAction,
                DoctorId = null,
                InstitutionId = _appSession.LoggedSite?.Id,
                CreaterId = _appSession.WebUserId,
                CreatedAt = DateTime.Now
            };

            _db.Prescriptions.Add(rx);
            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostRemovePrescriptionAsync([FromBody] RemoveRequest req)
        {
            var rx = await _db.Prescriptions.FindAsync(req.Id);
            if (rx != null)
            {
                rx.Retired = true;
                rx.RetiredAt = DateTime.Now;
                await _db.SaveChangesAsync();
            }
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostAddInvestigationAsync([FromBody] InvestigationRequest req)
        {
            var enc = await _db.PatientEncounters.FindAsync(req.EncounterId);
            if (enc == null) return new JsonResult(new { success = false, message = "Encounter not found" });

            var inv = new Models.InvestigationOrder
            {
                PatientEncounterId = req.EncounterId,
                PatientId = req.PatientId,
                InvestigationName = req.InvestigationName,
                Description = req.Description,
                Precautions = req.Precautions,
                DueDate = DateTime.Now,
                Ordered = true,
                InstitutionId = _appSession.LoggedSite?.Id,
                CreaterId = _appSession.WebUserId,
                CreatedAt = DateTime.Now
            };

            _db.InvestigationOrders.Add(inv);
            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostRemoveInvestigationAsync([FromBody] RemoveRequest req)
        {
            var inv = await _db.InvestigationOrders.FindAsync(req.Id);
            if (inv != null)
            {
                inv.Retired = true;
                inv.RetiredAt = DateTime.Now;
                await _db.SaveChangesAsync();
            }
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostSaveFollowUpAsync([FromBody] FollowUpRequest req)
        {
            var enc = await _db.PatientEncounters.FindAsync(req.EncounterId);
            if (enc == null) return new JsonResult(new { success = false, message = "Encounter not found" });

            var fu = new Models.FollowUp
            {
                PatientEncounterId = req.EncounterId,
                PatientId = req.PatientId,
                NextVisitDate = DateTime.TryParse(req.NextDate, out var d) ? d : null,
                TreatmentPlan = req.Plan ?? "",
                DoctorId = null,
                RecommendedDoctorId = long.TryParse(req.DoctorId, out var docId) ? docId : null,
                InstitutionId = _appSession.LoggedSite?.Id,
                CreaterId = _appSession.WebUserId,
                CreatedAt = DateTime.Now
            };

            _db.FollowUps.Add(fu);
            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        private static double? ParseDouble(string? val) => double.TryParse(val, out var d) ? d : null;
        private static int? ParseInt(string? val) => int.TryParse(val, out var i) ? i : null;

        public class PatientQueueItem
        {
            public long TokenId { get; set; }
            public long PatientId { get; set; }
            public long EncounterId { get; set; }
            public string PatientName { get; set; } = "";
            public string Phn { get; set; } = "";
            public int TokenNo { get; set; }
            public string Status { get; set; } = "";
            public string ChiefComplaint { get; set; } = "";
            public string Age { get; set; } = "";
        }

        public class SymptomItem
        {
            public long Id { get; set; }
            public long PatientEncounterId { get; set; }
            public string Description { get; set; } = "";
            public int Severity { get; set; }
        }

        public class DiagnosisItem
        {
            public long Id { get; set; }
            public string DiagnosisName { get; set; } = "";
            public string DiagnosisType { get; set; } = "";
            public string Notes { get; set; } = "";
        }

        public class CallPatientRequest { public long TokenId { get; set; } }
        public class FinalizeRequest { public long EncounterId { get; set; } }
        public class ClinicalNotesRequest { public long EncounterId { get; set; } public string? ChiefComplaint { get; set; } public string? Hpc { get; set; } public string? Pmh { get; set; } }
        public class VitalsRequest { public long EncounterId { get; set; } public string? Weight { get; set; } public string? Height { get; set; } public string? BpSys { get; set; } public string? BpDia { get; set; } public string? Pulse { get; set; } public string? Resp { get; set; } public string? Spo2 { get; set; } public string? Temp { get; set; } }
        public class ExaminationRequest { public long EncounterId { get; set; } public string? Examination { get; set; } }
        public class NotesRequest { public long EncounterId { get; set; } public string? TreatmentPlan { get; set; } public string? Management { get; set; } public string? Notes { get; set; } }
        public class SymptomRequest { public long EncounterId { get; set; } public long PatientId { get; set; } public string Description { get; set; } = ""; public int Severity { get; set; } }
        public class DiagnosisRequest { public long EncounterId { get; set; } public long PatientId { get; set; } public string DiagnosisName { get; set; } = ""; public string DiagnosisType { get; set; } = ""; }
        public class PrescriptionRequest { public long EncounterId { get; set; } public long PatientId { get; set; } public string MedicineName { get; set; } = ""; public string? Dose { get; set; } public string? Frequency { get; set; } public string? Duration { get; set; } public string? Route { get; set; } public string? Instructions { get; set; } public string? Qty { get; set; } public bool RequiredAction { get; set; } }
        public class InvestigationRequest { public long EncounterId { get; set; } public long PatientId { get; set; } public string InvestigationName { get; set; } = ""; public string? Description { get; set; } public string? Precautions { get; set; } }
        public class FollowUpRequest { public long EncounterId { get; set; } public long PatientId { get; set; } public string? NextDate { get; set; } public string? DoctorId { get; set; } public string? Plan { get; set; } }
        public class RemoveRequest { public long Id { get; set; } }
    }
}
