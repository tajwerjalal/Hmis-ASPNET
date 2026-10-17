using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Billing
{
    /// <summary>
    /// Assign Patient Ownership - assign the owning consultant for a patient
    /// registration. Migrated from the VFP Billsys form <c>patowner</c>
    /// (powner.dbf). Legacy semantics: powner holds TYPE('I') + REGNO + OWNER
    /// (a consultant code from consult.dbf). During consultant recalculation the
    /// owning consultant receives 100% of his line's net amount. Ownership cannot
    /// be changed once the registration's bill has been generated
    /// (register.BILLGEN set) - the legacy form blocked the edit with
    /// "Bill Generated for this Registration NO.".
    /// </summary>
    public class PatientOwnershipModel : Shared.PagedPageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public PatientOwnershipModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        [BindProperty(SupportsGet = true)] public string PatientSearch { get; set; }
        [BindProperty(SupportsGet = true)] public string Type { get; set; } = "inward";
        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; }

        [BindProperty] public long EncounterId { get; set; }
        [BindProperty] public long? OwnerId { get; set; }

        public List<PatientEncounter> Encounters { get; set; } = new();
        public List<SelectListItem> ConsultantList { get; set; } = new();

        /// <summary>Ownership rows (POWNER) keyed by encounter id, for the current page of encounters.</summary>
        public Dictionary<long, PatientOwner> OwnershipByEncounter { get; set; } = new();

        /// <summary>Encounters whose bill has already been generated (ownership locked).</summary>
        public HashSet<long> LockedEncounters { get; set; } = new();

        public string Success { get; set; }
        public string ErrorMessage { get; set; }

        public bool IsInward => string.Equals(Type, "inward", StringComparison.OrdinalIgnoreCase);
        public string TypeLabel => IsInward ? "IPD" : "OPD";

        public async Task OnGetAsync() => await LoadAsync();

        public async Task<IActionResult> OnPostAssignAsync()
        {
            var enc = await _db.PatientEncounters.FirstOrDefaultAsync(e => e.Id == EncounterId && !e.Retired);
            if (enc == null)
            {
                TempData["PoError"] = "Encounter not found.";
                return RedirectToPage(new { Type, PatientSearch, FromDate, ToDate });
            }

            if (await IsBillGeneratedAsync(enc.Id))
            {
                TempData["PoError"] = "Bill Generated for this Registration NO.";
                return RedirectToPage(new { Type, PatientSearch, FromDate, ToDate });
            }

            var now = DateTime.Now;
            var userId = _session.WebUserId;
            var typeCode = enc.EncounterType == EncounterType.Inward ? "I" : "O";
            var regno = enc.EncounterType == EncounterType.Inward ? enc.BhtNo : enc.ReferenceNo;

            var owner = await _db.PatientOwners
                .FirstOrDefaultAsync(p => p.EncounterId == enc.Id && !p.Retired);

            if (OwnerId.HasValue && OwnerId.Value > 0)
            {
                var staff = await _db.Staffs.Include(s => s.Person).FirstOrDefaultAsync(s => s.Id == OwnerId.Value && !s.Retired);
                if (staff == null)
                {
                    TempData["PoError"] = "Selected consultant not found.";
                    return RedirectToPage(new { Type, PatientSearch, FromDate, ToDate });
                }

                if (owner == null)
                {
                    owner = new PatientOwner
                    {
                        CreatedAt = now,
                        CreaterId = userId
                    };
                    _db.PatientOwners.Add(owner);
                }
                owner.Type = typeCode;
                owner.Regno = regno;
                owner.EncounterId = enc.Id;
                owner.OwnerId = staff.Id;
                owner.OwnerCode = staff.Code;
                owner.Retired = false;
                owner.EditedAt = now;
                owner.EditerId = userId;

                TempData["PoSuccess"] = $"Ownership assigned to '{staff.Person?.Name}'.";
            }
            else
            {
                if (owner != null)
                {
                    owner.Retired = true;
                    owner.RetireComments = "Ownership cleared";
                    owner.EditedAt = now;
                    owner.EditerId = userId;
                }
                TempData["PoSuccess"] = "Ownership cleared.";
            }

            await _db.SaveChangesAsync();
            return RedirectToPage(new { Type, PatientSearch, FromDate, ToDate });
        }

        /// <summary>
        /// Legacy guard: register.BILLGEN = 'T'. In HMIS a migrated/legacy bill exists for
        /// the encounter, or a final (non-PreBill) bill was already generated.
        /// </summary>
        private async Task<bool> IsBillGeneratedAsync(long encounterId)
        {
            var generated = await _db.Bills.AnyAsync(b =>
                b.PatientEncounterId == encounterId && !b.Retired
                && (b.BillClassType == null
                    || (b.BillClassType != BillClassType.PreBill)));
            return generated;
        }

        private async Task LoadAsync()
        {
            Success = TempData["PoSuccess"] as string;
            ErrorMessage = TempData["PoError"] as string;

            ConsultantList = await _db.Staffs.OfType<Consultant>()
                .Where(s => !s.Retired)
                .OrderBy(s => s.Code)
                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = (s.Person != null && s.Person.Name != null) ? s.Person.Name : s.Code })
                .ToListAsync();

            var type = IsInward ? EncounterType.Inward : EncounterType.Opd;
            var q = _db.PatientEncounters
                .Include(e => e.Patient).ThenInclude(p => p.Person)
                .Include(e => e.CreditCompany)
                .Where(e => !e.Retired && e.EncounterType == type);

            if (FromDate.HasValue) q = q.Where(e => e.EncounterDate >= FromDate.Value.Date);
            if (ToDate.HasValue) q = q.Where(e => e.EncounterDate < ToDate.Value.Date.AddDays(1));
            if (!string.IsNullOrWhiteSpace(PatientSearch))
            {
                var t = PatientSearch.Trim().ToLower();
                q = q.Where(e => (e.Patient.Person.Name != null && e.Patient.Person.Name.ToLower().Contains(t))
                              || (e.Patient.Phn != null && e.Patient.Phn.ToLower().Contains(t))
                              || (e.BhtNo != null && e.BhtNo.ToLower().Contains(t))
                              || (e.ReferenceNo != null && e.ReferenceNo.ToLower().Contains(t)));
            }

            var ordered = q.OrderByDescending(e => e.Id);
            TotalCount = await ordered.CountAsync();
            Encounters = await ordered.Skip(Skip).Take(Take).ToListAsync();

            if (Encounters.Count > 0)
            {
                var ids = Encounters.Select(e => e.Id).ToList();

                var owners = await _db.PatientOwners
                    .Where(p => p.EncounterId != null && ids.Contains(p.EncounterId.Value) && !p.Retired)
                    .Include(p => p.Owner)
                    .ToListAsync();
                foreach (var o in owners)
                    OwnershipByEncounter[o.EncounterId.Value] = o;

                var locked = await _db.Bills
                    .Where(b => ids.Contains(b.PatientEncounterId.Value) && !b.Retired
                        && (b.BillClassType == null || b.BillClassType != BillClassType.PreBill))
                    .Select(b => b.PatientEncounterId.Value)
                    .Distinct()
                    .ToListAsync();
                LockedEncounters = new HashSet<long>(locked);
            }
        }
    }
}