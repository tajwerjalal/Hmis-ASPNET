using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Models.Pharmacy;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Ot
{
    /// <summary>
    /// OT case sheet: books a patient into a theatre (who / which theatre / when),
    /// records the operation once done (actual times, procedure performed, findings,
    /// anaesthesia, ASA grade, outcome, post-op order) and lists the consumables /
    /// implants used. Billing is deliberately NOT done here - chargeable services
    /// continue to flow through the existing department indent / cashier pages, this
    /// page only keeps the bill number as a reference.
    /// </summary>
    public class CaseEntryModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;
        private readonly ICommonService _common;

        public CaseEntryModel(ApplicationDbContext db, IAppSession session, ICommonService common)
        {
            _db = db;
            _session = session;
            _common = common;
        }

        [BindProperty(SupportsGet = true)]
        public long? Id { get; set; }
        public bool IsNew => !Id.HasValue || Id.Value == 0;

        public OtCase Case { get; set; } = new();

        public List<SelectListItem> TheatreList { get; set; } = new();
        public List<SelectListItem> SurgeonList { get; set; } = new();
        public List<SelectListItem> AnesthetistList { get; set; } = new();
        public List<SelectListItem> ProcedureList { get; set; } = new();

        public List<OtCaseItem> CaseItems { get; set; } = new();

        /// <summary>Stock items offered on the consumables line (pharmacy master).</summary>
        public List<ConsumableOption> ConsumableList { get; set; } = new();

        public string ErrorMessage { get; set; } = "";
        public string Success { get; set; } = "";

        // Patient search (only used while booking a new case / changing patient)
        [BindProperty]
        public string PatientSearch { get; set; } = "";
        public List<Patient> PatientResults { get; set; } = new();

        // Consumables "add line" inputs
        [BindProperty] public int? AddItemId { get; set; }
        [BindProperty] public string AddItemName { get; set; } = "";
        [BindProperty] public decimal AddQty { get; set; } = 1;
        [BindProperty] public decimal AddRate { get; set; }
        [BindProperty] public bool AddChargeable { get; set; } = true;
        [BindProperty] public string AddRemarks { get; set; } = "";

        public class ConsumableOption
        {
            public int Id { get; set; }
            public string Text { get; set; } = "";
            public decimal Rate { get; set; }
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public long? PatientId { get; set; }
            public long? TheatreId { get; set; }
            public long? SurgeonId { get; set; }
            public long? AnesthetistId { get; set; }
            public long? ProcedureItemId { get; set; }
            public string ProcedurePlanned { get; set; } = "";
            public string CaseType { get; set; } = "Elective";
            public DateTime ScheduledDate { get; set; } = DateTime.Today;
            public string SlotTime { get; set; } = "";
            public int DurationMins { get; set; }
            public string Status { get; set; } = "Scheduled";
            public string AnesthesiaPlanned { get; set; } = "None";
            public string AnesthesiaGiven { get; set; } = "";
            public string AsaGrade { get; set; } = "";
            public string Fitness { get; set; } = "Not Done";
            public DateTime? ActualStart { get; set; }
            public DateTime? ActualEnd { get; set; }
            public string ProcedurePerformed { get; set; } = "";
            public string Findings { get; set; } = "";
            public string Outcome { get; set; } = "";
            public string Complications { get; set; } = "";
            public string PostOpInstructions { get; set; } = "";
            public string CancelReason { get; set; } = "";
            public string BillNo { get; set; } = "";
            public string Remarks { get; set; } = "";
        }

        public static readonly string[] Statuses = { "Scheduled", "In Theatre", "Completed", "Cancelled", "Postponed" };
        public static readonly string[] CaseTypes = { "Elective", "Emergency" };
        public static readonly string[] Anesthetics = { "None", "Local", "Sedation", "GA", "Spinal", "Epidural", "Regional", "Combined" };
        public static readonly string[] AsaGrades = { "", "I", "II", "III", "IV", "V", "VI" };
        public static readonly string[] FitnessStates = { "Not Done", "Fit", "Fit With Caution", "Unfit" };
        public static readonly string[] Outcomes = { "", "Uneventful", "Complication", "Death" };

        public async Task OnGetAsync(long? id)
        {
            Id = id;
            await LoadListsAsync();

            if (Id.HasValue && Id.Value > 0)
            {
                var c = await _db.OtCases
                    .Include(x => x.Patient).ThenInclude(p => p.Person)
                    .FirstOrDefaultAsync(x => x.Id == Id.Value && !x.Retired);
                if (c == null)
                {
                    ErrorMessage = "OT case not found.";
                    return;
                }
                Case = c;
                Input = FromEntity(c);
            }

            await LoadItemsAsync();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            Success = "";

            if (Input.PatientId == null)
            {
                ErrorMessage = "Select the patient for this OT case.";
                await LoadListsAsync();
                await ReloadCaseAsync();
                return Page();
            }

            var patient = await _db.Patients
                .Include(p => p.Person)
                .FirstOrDefaultAsync(p => p.Id == Input.PatientId.Value && !p.Retired);
            if (patient == null)
            {
                ErrorMessage = "Selected patient no longer exists.";
                await LoadListsAsync();
                await ReloadCaseAsync();
                return Page();
            }

            if (Input.TheatreId == null)
            {
                ErrorMessage = "Select the theatre for this case.";
                await LoadListsAsync();
                await ReloadCaseAsync();
                return Page();
            }

            if (Input.Status == "Cancelled" && string.IsNullOrWhiteSpace(Input.CancelReason))
            {
                ErrorMessage = "A cancel reason is required when the case is cancelled.";
                await LoadListsAsync();
                await ReloadCaseAsync();
                return Page();
            }

            OtCase c;
            bool isNew = !(Id.HasValue && Id.Value > 0);

            if (isNew)
            {
                c = new OtCase
                {
                    CaseNo = await NextCaseNoAsync(Input.ScheduledDate.Date),
                    InstitutionId = _session.LoggedSite?.Id,
                    DepartmentId = _session.LoggedDepartment?.Id,
                    CreaterId = _session.WebUserId,
                    CreatedAt = DateTime.Now
                };
                Apply(c);
                _db.OtCases.Add(c);
            }
            else
            {
                c = await _db.OtCases.FindAsync(Id.Value);
                if (c == null)
                {
                    ErrorMessage = "OT case not found.";
                    await LoadListsAsync();
                    await ReloadCaseAsync();
                    return Page();
                }
                Apply(c);
                c.EditerId = _session.WebUserId;
                c.EditedAt = DateTime.Now;
            }

            // A completed case must have times: fall back to "now" rather than block
            // the user mid-shift.
            if (c.Status == "Completed")
            {
                if (!c.ActualStart.HasValue) c.ActualStart = DateTime.Now;
                if (!c.ActualEnd.HasValue) c.ActualEnd = DateTime.Now;
                if (c.ActualEnd.Value < c.ActualStart.Value) c.ActualEnd = c.ActualStart;
                if (string.IsNullOrWhiteSpace(c.ProcedurePerformed) && !string.IsNullOrWhiteSpace(c.ProcedurePlanned))
                    c.ProcedurePerformed = c.ProcedurePlanned;
            }
            else if (c.Status == "Cancelled" || c.Status == "Postponed")
            {
                c.ActualStart = null;
                c.ActualEnd = null;
            }

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ErrorMessage = "Could not save the case - a case with the same case number may already exist. Please try again.";
                await LoadListsAsync();
                await ReloadCaseAsync();
                return Page();
            }

            Success = isNew
                ? "Case " + c.CaseNo + " created for " + patientName(patient) + "."
                : "Case " + c.CaseNo + " updated.";
            ErrorMessage = "";

            if (isNew)
                return RedirectToPage("/Ot/CaseEntry", new { id = c.Id });

            Id = c.Id;
            await LoadListsAsync();
            await ReloadCaseAsync();
            return Page();
        }

        // ---- patient search / selection (new cases only) ----

        public async Task<IActionResult> OnPostSearchPatientsAsync()
        {
            await LoadListsAsync();
            await ReloadCaseAsync();
            PatientResults = await _common.SearchPatientsAsync(PatientSearch);
            return Page();
        }

        public async Task<IActionResult> OnPostSelectPatientAsync(long patientId)
        {
            Input.PatientId = patientId;
            await LoadListsAsync();
            await ReloadCaseAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostClearPatientAsync()
        {
            Input.PatientId = null;
            await LoadListsAsync();
            await ReloadCaseAsync();
            return Page();
        }

        // ---- consumables / implants used in the case ----

        public async Task<IActionResult> OnPostAddItemAsync(long caseId)
        {
            if (caseId <= 0)
            {
                ErrorMessage = "Save the case first, then add consumables.";
                await LoadListsAsync();
                return Page();
            }

            string itemName = (AddItemName ?? "").Trim();
            decimal? retailRate = null;

            if (AddItemId.HasValue && AddItemId.Value > 0)
            {
                var stock = await _db.PhItems.FindAsync(AddItemId.Value);
                if (stock == null)
                {
                    ErrorMessage = "Stock item not found.";
                    await LoadListsAsync();
                    await ReloadCaseAsync();
                    return Page();
                }
                if (string.IsNullOrEmpty(itemName)) itemName = stock.Name;
                retailRate = stock.RetailPrice;
            }

            if (string.IsNullOrEmpty(itemName))
            {
                ErrorMessage = "Select a stock item or type the item name.";
                await LoadListsAsync();
                await ReloadCaseAsync();
                return Page();
            }

            var qty = AddQty <= 0 ? 1 : AddQty;
            var rate = AddRate;
            if (rate == 0 && retailRate.HasValue) rate = retailRate.Value;

            _db.OtCaseItems.Add(new OtCaseItem
            {
                CaseId = caseId,
                ItemId = AddItemId.HasValue && AddItemId.Value > 0 ? AddItemId : null,
                ItemName = itemName.Length > 150 ? itemName.Substring(0, 150) : itemName,
                Qty = qty,
                Rate = rate,
                Amount = qty * rate,
                Chargeable = AddChargeable,
                Remarks = (AddRemarks ?? "").Trim()
            });
            await _db.SaveChangesAsync();

            Id = caseId;
            await LoadListsAsync();
            await ReloadCaseAsync();
            Success = "Line added.";
            ErrorMessage = "";
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteItemAsync(long itemId, long caseId)
        {
            var line = await _db.OtCaseItems.FindAsync(itemId);
            if (line != null)
            {
                _db.OtCaseItems.Remove(line);
                await _db.SaveChangesAsync();
            }

            Id = caseId > 0 ? caseId : Id;
            await LoadListsAsync();
            if (Id.HasValue && Id.Value > 0) await ReloadCaseAsync();
            return RedirectToPage("/Ot/CaseEntry", new { id = Id });
        }

        // ---- helpers ----

        private async Task ReloadCaseAsync()
        {
            if (Id.HasValue && Id.Value > 0)
            {
                var c = await _db.OtCases
                    .Include(x => x.Patient).ThenInclude(p => p.Person)
                    .FirstOrDefaultAsync(x => x.Id == Id.Value && !x.Retired);
                if (c != null)
                {
                    Case = c;
                    if (Input.PatientId == null) Input.PatientId = c.PatientId;
                }
            }
            await LoadItemsAsync();
        }

        private async Task LoadItemsAsync()
        {
            CaseItems = Id.HasValue && Id.Value > 0
                ? await _db.OtCaseItems.Where(i => i.CaseId == Id.Value).OrderBy(i => i.Id).ToListAsync()
                : new List<OtCaseItem>();
        }

        private async Task LoadListsAsync()
        {
            var theatres = await _db.OtTheatres
                .Where(t => !t.Retired)
                .OrderBy(t => t.Name)
                .ToListAsync();
            TheatreList = theatres.Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = t.Name + (string.IsNullOrWhiteSpace(t.TheatreType) ? "" : " (" + t.TheatreType + ")"),
                Disabled = !t.Active
            }).ToList();

            var surgeons = await _db.Consultants
                .Include(c => c.Person)
                .Where(c => !c.Retired)
                .OrderBy(c => c.Person.Name)
                .ToListAsync();
            SurgeonList = surgeons.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Person?.Name ?? "Consultant " + c.Id
            }).ToList();

            var doctors = await _db.Doctors
                .Include(d => d.Person)
                .Where(d => !d.Retired)
                .OrderBy(d => d.Person.Name)
                .ToListAsync();
            AnesthetistList = doctors.Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = d.Person?.Name ?? "Doctor " + d.Id
            }).ToList();

            var procs = await _db.Items
                .Where(i => !i.Retired && !i.Inactive)
                .OrderBy(i => i.Name)
                .Take(1000)
                .Select(i => new { i.Id, i.Code, i.Name, i.Total })
                .ToListAsync();
            ProcedureList = procs.Select(i => new SelectListItem
            {
                Value = i.Id.ToString(),
                Text = (i.Code == "" || i.Code == null ? "" : i.Code + " - ") + i.Name
            }).ToList();

            // Theatre consumables come from the pharmacy stock master, not services.
            var stock = await _db.PhItems
                .Where(i => !i.Retired && i.IsActive)
                .OrderBy(i => i.Name)
                .Take(2000)
                .Select(i => new { i.Id, i.Code, i.Name, i.RetailPrice })
                .ToListAsync();
            ConsumableList = stock.Select(i => new ConsumableOption
            {
                Id = i.Id,
                Text = (string.IsNullOrEmpty(i.Code) ? "" : i.Code + " - ") + i.Name,
                Rate = i.RetailPrice
            }).ToList();
        }

        private InputModel FromEntity(OtCase c) => new InputModel
        {
            PatientId = c.PatientId,
            TheatreId = c.TheatreId,
            SurgeonId = c.SurgeonId,
            AnesthetistId = c.AnesthetistId,
            ProcedureItemId = c.ProcedureItemId,
            ProcedurePlanned = c.ProcedurePlanned,
            CaseType = c.CaseType,
            ScheduledDate = c.ScheduledDate,
            SlotTime = c.SlotTime,
            DurationMins = c.DurationMins,
            Status = c.Status,
            AnesthesiaPlanned = c.AnesthesiaPlanned,
            AnesthesiaGiven = c.AnesthesiaGiven,
            AsaGrade = c.AsaGrade,
            Fitness = c.Fitness,
            ActualStart = c.ActualStart,
            ActualEnd = c.ActualEnd,
            ProcedurePerformed = c.ProcedurePerformed,
            Findings = c.Findings,
            Outcome = c.Outcome,
            Complications = c.Complications,
            PostOpInstructions = c.PostOpInstructions,
            CancelReason = c.CancelReason,
            BillNo = c.BillNo,
            Remarks = c.Remarks
        };

        private void Apply(OtCase c)
        {
            c.PatientId = Input.PatientId.Value;
            c.TheatreId = Input.TheatreId;
            c.SurgeonId = Input.SurgeonId;
            c.AnesthetistId = Input.AnesthetistId;
            c.ProcedureItemId = Input.ProcedureItemId;
            c.ProcedurePlanned = (Input.ProcedurePlanned ?? "").Trim();
            c.CaseType = CaseTypes.Contains(Input.CaseType) ? Input.CaseType : "Elective";
            c.ScheduledDate = Input.ScheduledDate.Date;
            c.SlotTime = (Input.SlotTime ?? "").Trim();
            c.DurationMins = Input.DurationMins < 0 ? 0 : Input.DurationMins;
            c.Status = Statuses.Contains(Input.Status) ? Input.Status : "Scheduled";
            c.AnesthesiaPlanned = Anesthetics.Contains(Input.AnesthesiaPlanned) ? Input.AnesthesiaPlanned : "None";
            c.AnesthesiaGiven = (Input.AnesthesiaGiven ?? "").Trim();
            c.AsaGrade = AsaGrades.Contains(Input.AsaGrade) ? Input.AsaGrade : "";
            c.Fitness = FitnessStates.Contains(Input.Fitness) ? Input.Fitness : "Not Done";
            c.ActualStart = Input.ActualStart;
            c.ActualEnd = Input.ActualEnd;
            c.ProcedurePerformed = (Input.ProcedurePerformed ?? "").Trim();
            c.Findings = (Input.Findings ?? "").Trim();
            c.Outcome = Outcomes.Contains(Input.Outcome) ? Input.Outcome : "";
            c.Complications = (Input.Complications ?? "").Trim();
            c.PostOpInstructions = (Input.PostOpInstructions ?? "").Trim();
            c.CancelReason = (Input.CancelReason ?? "").Trim();
            c.BillNo = (Input.BillNo ?? "").Trim();
            c.Remarks = (Input.Remarks ?? "").Trim();
            if (c.DepartmentId == null) c.DepartmentId = _session.LoggedDepartment?.Id;
            if (c.InstitutionId == null) c.InstitutionId = _session.LoggedSite?.Id;
        }

        /// <summary>OT-20261020-001 style running number for the scheduled day.</summary>
        private async Task<string> NextCaseNoAsync(DateTime day)
        {
            var prefix = "OT-" + day.ToString("yyyyMMdd") + "-";
            var last = await _db.OtCases
                .Where(c => c.CaseNo.StartsWith(prefix))
                .OrderByDescending(c => c.CaseNo)
                .Select(c => c.CaseNo)
                .FirstOrDefaultAsync();

            int seq = 1;
            if (!string.IsNullOrEmpty(last))
            {
                var tail = last.Substring(prefix.Length);
                if (int.TryParse(tail, out var parsed)) seq = parsed + 1;
            }

            // Unique index guard: bump until free (covers a gap-free concurrent race).
            while (true)
            {
                var candidate = prefix + seq.ToString("000");
                var taken = await _db.OtCases.AnyAsync(c => c.CaseNo == candidate);
                if (!taken) return candidate;
                seq++;
            }
        }

        private static string patientName(Patient p)
            => p?.Person?.NameWithInitials ?? p?.Person?.Name ?? "patient";
    }
}
