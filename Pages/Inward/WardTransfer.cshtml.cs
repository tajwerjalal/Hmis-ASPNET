using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Inward
{
    /// <summary>
    /// Ward / Bed Transfer - /inward/WardTransfer.
    ///
    /// Replaces the legacy VFP screen bedtrans.scx: it moves an admitted
    /// patient from one bed to another (or records the bed of a fresh
    /// admission), keeps the full movement history in [BEDTRANSFER] and
    /// updates the occupancy of [BED_POS] (old bed -> Vacant, new bed ->
    /// Occupied) plus the current stay in [PATIENTROOM].
    ///
    /// List + popup entry, server-side paging, soft-retire with a reason
    /// (SweetAlert), and login / INWARD_BED_TRANSFER_EDIT gates.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 22-Oct-2026
    /// Time      : 17:30
    /// Change Log: 22-Oct-2026 - created on WARD + BED_POS + BEDTRANSFER.
    /// </summary>
    public class WardTransferModel : Shared.PagedPageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public WardTransferModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        public List<BedTransfer> Transfers { get; set; } = new();
        public List<SelectListItem> WardList { get; set; } = new();
        public List<SelectListItem> VacantBedList { get; set; } = new();
        public List<PatientEncounter> SearchResults { get; set; } = new();
        public PatientEncounter SelectedEncounter { get; set; }
        public BedPosition CurrentBed { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Q { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Ward { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool ShowRetired { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? SelectedEncounterId { get; set; }

        // transfer inputs
        [BindProperty]
        public long? TransferEncounterId { get; set; }

        [BindProperty]
        public long? ToBedId { get; set; }

        [BindProperty]
        public DateTime? TransferDate { get; set; }

        [BindProperty]
        public string TransferTime { get; set; }

        [BindProperty]
        public string IndentNo { get; set; }

        [BindProperty]
        public DateTime? IndentDate { get; set; }

        [TempData]
        public string Success { get; set; }

        [TempData]
        public string Error { get; set; }

        public bool CanEdit => _session.HasPrivilege("INWARD_BED_TRANSFER_EDIT")
                               || _session.HasPrivilege("ALL");

        public async Task<IActionResult> OnGetAsync()
        {
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");

            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSearchAsync()
        {
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");

            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostTransferAsync()
        {
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");
            if (!CanEdit)
            {
                Error = "You do not have the right to transfer patients between beds.";
                return RedirectToPage(new { Q, Ward, ShowRetired, SearchTerm, SelectedEncounterId });
            }

            var encounter = await _db.PatientEncounters
                .Include(e => e.Patient).ThenInclude(p => p.Person)
                .FirstOrDefaultAsync(e => e.Id == TransferEncounterId && !e.Retired);
            if (encounter == null || encounter.DateOfDischarge != null)
            {
                Error = "The selected inpatient stay was not found or is already discharged.";
                return RedirectToPage(new { Q, Ward, ShowRetired, SearchTerm, SelectedEncounterId });
            }

            var toBed = await _db.BedPositions.FirstOrDefaultAsync(b => b.Id == ToBedId && !b.Retired);
            if (toBed == null || !string.Equals(toBed.Status, "Vacant", StringComparison.OrdinalIgnoreCase))
            {
                Error = "The selected bed is not available.";
                return RedirectToPage(new { Q, Ward, ShowRetired, SearchTerm, SelectedEncounterId });
            }

            var transferAt = CombineDateAndTime(TransferDate ?? DateTime.Today, TransferTime);

            // current stay location
            var currentRoom = await _db.PatientRooms
                .Include(pr => pr.BedPosition)
                .Where(pr => pr.PatientEncounterId == encounter.Id && !pr.Retired && pr.ToTime == null)
                .OrderByDescending(pr => pr.FromTime)
                .FirstOrDefaultAsync();

            var fromBed = currentRoom?.BedPosition;
            var fromBedCode = fromBed?.BedCode;
            long? fromBedId = fromBed?.Id;

            var admission = await _db.Admissions
                .Where(a => a.PatientEncounterId == encounter.Id && !a.Retired)
                .OrderByDescending(a => a.Id)
                .FirstOrDefaultAsync();

            // move the beds
            if (fromBed != null)
            {
                fromBed.Status = "Vacant";
                fromBed.PatientCode = null;
                StampBed(fromBed);
            }

            toBed.Status = "Occupied";
            toBed.PatientCode = encounter.Patient?.Phn ?? encounter.PatientId?.ToString();
            StampBed(toBed);

            // close the current stay and open a new one on the new bed
            if (currentRoom != null)
            {
                currentRoom.ToTime = transferAt;
                currentRoom.Discharged = false;
            }

            var newRoom = new PatientRoom
            {
                PatientEncounterId = encounter.Id,
                AdmissionId = admission?.Id,
                WardId = null,
                BedPositionId = toBed.Id,
                FromTime = transferAt,
                CreaterId = _session.WebUserId,
                CreatedAt = DateTime.Now,
                Retired = false
            };
            // resolve ward id from the bed's ward code
            var ward = await _db.Wards.FirstOrDefaultAsync(w => w.Code == toBed.WardCode && !w.Retired);
            newRoom.WardId = ward?.Id;
            _db.PatientRooms.Add(newRoom);

            var transfer = new BedTransfer
            {
                Type = "I",
                RegNo = encounter.BhtNo,
                PatientEncounterId = encounter.Id,
                AdmissionId = admission?.Id,
                FromBedPositionId = fromBedId,
                ToBedPositionId = toBed.Id,
                FromBedCode = fromBedCode,
                ToBedCode = toBed.BedCode,
                TransferDate = transferAt,
                IndentNo = string.IsNullOrWhiteSpace(IndentNo) ? null : IndentNo.Trim(),
                IndentDate = IndentDate,
                CreaterId = _session.WebUserId,
                CreatedAt = DateTime.Now,
                Retired = false,
                UserId = _session.LoggedUser?.UserName ?? "SYSTEM",
                UDate = DateTime.Now.Date,
                UTime = DateTime.Now.ToString("HH:mm")
            };
            _db.BedTransfers.Add(transfer);

            await _db.SaveChangesAsync();

            Success = $"Patient moved to bed {toBed.BedCode}.";
            return RedirectToPage(new { Q, Ward, ShowRetired, SearchTerm, SelectedEncounterId });
        }

        public async Task<IActionResult> OnPostRetireAsync(long id, string reason)
        {
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");
            if (!CanEdit)
            {
                Error = "You do not have the right to retire bed transfers.";
                return RedirectToPage(new { Q, Ward, ShowRetired });
            }

            var transfer = await _db.BedTransfers.FirstOrDefaultAsync(t => t.Id == id && !t.Retired);
            if (transfer == null)
            {
                Error = "Transfer not found.";
                return RedirectToPage(new { Q, Ward, ShowRetired });
            }

            transfer.Retired = true;
            transfer.RetireComments = string.IsNullOrWhiteSpace(reason) ? "Retired from ward transfer." : reason.Trim();
            transfer.RetiredAt = DateTime.Now;
            transfer.RetirerId = _session.WebUserId;
            transfer.EditedAt = DateTime.Now;
            transfer.EditerId = _session.WebUserId;

            // revert the beds only when this is the latest live transfer of the stay
            var newer = await _db.BedTransfers.AnyAsync(t =>
                t.PatientEncounterId == transfer.PatientEncounterId && !t.Retired && t.Id > transfer.Id);
            if (!newer && transfer.ToBedPositionId.HasValue)
            {
                var toBed = await _db.BedPositions.FirstOrDefaultAsync(b => b.Id == transfer.ToBedPositionId.Value);
                if (toBed != null)
                {
                    toBed.Status = "Vacant";
                    toBed.PatientCode = null;
                    StampBed(toBed);
                }

                if (transfer.FromBedPositionId.HasValue)
                {
                    var fromBed = await _db.BedPositions.FirstOrDefaultAsync(b => b.Id == transfer.FromBedPositionId.Value);
                    if (fromBed != null)
                    {
                        fromBed.Status = "Occupied";
                        fromBed.PatientCode = transfer.RegNo;
                        StampBed(fromBed);
                    }

                    var currentRoom = await _db.PatientRooms
                        .Where(pr => pr.PatientEncounterId == transfer.PatientEncounterId && !pr.Retired && pr.ToTime == null)
                        .OrderByDescending(pr => pr.FromTime)
                        .FirstOrDefaultAsync();
                    if (currentRoom != null && fromBed != null)
                    {
                        currentRoom.BedPositionId = fromBed.Id;
                        var w = await _db.Wards.FirstOrDefaultAsync(x => x.Code == fromBed.WardCode && !x.Retired);
                        currentRoom.WardId = w?.Id;
                    }
                }
                else
                {
                    // The retired transfer was the first bed assignment of the
                    // stay (admission): there is no old bed to go back to, so the
                    // current [PATIENTROOM] row is simply closed again and the
                    // patient is left without a bed.
                    var currentRoom = await _db.PatientRooms
                        .Where(pr => pr.PatientEncounterId == transfer.PatientEncounterId && !pr.Retired && pr.ToTime == null)
                        .OrderByDescending(pr => pr.FromTime)
                        .FirstOrDefaultAsync();
                    if (currentRoom != null)
                    {
                        currentRoom.ToTime = DateTime.Now;
                    }
                }
            }

            await _db.SaveChangesAsync();

            Success = "Bed transfer retired.";
            return RedirectToPage(new { Q, Ward, ShowRetired });
        }

        private async Task LoadAsync()
        {
            // wards for the filter
            WardList = await _db.Wards.AsNoTracking()
                .Where(w => !w.Retired)
                .OrderBy(w => w.Code)
                .Select(w => new SelectListItem { Value = w.Code, Text = $"{w.Code} - {w.Name}" })
                .ToListAsync();

            // vacant beds for the popup
            VacantBedList = await _db.BedPositions.AsNoTracking()
                .Where(b => !b.Retired && b.Status == "Vacant")
                .OrderBy(b => b.WardCode).ThenBy(b => b.BedCode)
                .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = $"{b.BedCode} - {b.WardCode}" })
                .ToListAsync();

            // patient search
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim();
                SearchResults = await _db.PatientEncounters
                    .Include(e => e.Patient).ThenInclude(p => p.Person)
                    .Where(e => e.EncounterType == EncounterType.Inward
                                && !e.Retired
                                && e.DateOfDischarge == null
                                && ((e.BhtNo != null && e.BhtNo.Contains(term))
                                    || (e.Patient != null && e.Patient.Person != null
                                        && e.Patient.Person.Name != null
                                        && e.Patient.Person.Name.Contains(term))))
                    .OrderByDescending(e => e.DateOfAdmission)
                    .Take(20)
                    .ToListAsync();
            }

            if (SelectedEncounterId.HasValue)
            {
                SelectedEncounter = await _db.PatientEncounters
                    .Include(e => e.Patient).ThenInclude(p => p.Person)
                    .FirstOrDefaultAsync(e => e.Id == SelectedEncounterId.Value && !e.Retired);
                if (SelectedEncounter != null)
                {
                    CurrentBed = await _db.PatientRooms
                        .Include(pr => pr.BedPosition)
                        .Where(pr => pr.PatientEncounterId == SelectedEncounter.Id && !pr.Retired && pr.ToTime == null)
                        .OrderByDescending(pr => pr.FromTime)
                        .Select(pr => pr.BedPosition)
                        .FirstOrDefaultAsync();
                    TransferDate = TransferDate ?? DateTime.Today;
                    TransferTime = string.IsNullOrWhiteSpace(TransferTime) ? DateTime.Now.ToString("HH:mm") : TransferTime;
                    TransferEncounterId = SelectedEncounter.Id;
                }
            }

            // transfer list
            var query = _db.BedTransfers.AsNoTracking().AsQueryable();

            if (!ShowRetired)
                query = query.Where(t => !t.Retired);

            if (!string.IsNullOrWhiteSpace(Ward))
                query = query.Where(t => t.ToBedCode != null && t.ToBedCode.StartsWith(Ward.Trim()));

            if (!string.IsNullOrWhiteSpace(Q))
            {
                var term = Q.Trim();
                query = query.Where(t =>
                    (t.RegNo != null && t.RegNo.Contains(term))
                    || (t.FromBedCode != null && t.FromBedCode.Contains(term))
                    || (t.ToBedCode != null && t.ToBedCode.Contains(term))
                    || (t.IndentNo != null && t.IndentNo.Contains(term)));
            }

            TotalCount = await query.CountAsync();
            ClampPaging();

            Transfers = await ApplyPaging(query
                .OrderByDescending(t => t.TransferDate)
                .ThenByDescending(t => t.Id))
                .ToListAsync();
        }

        private static DateTime? CombineDateAndTime(DateTime? date, string time)
        {
            if (date == null) return null;
            if (TimeSpan.TryParse(time, out var ts)) return date.Value.Date.Add(ts);
            return date.Value.Date;
        }

        private static void StampBed(BedPosition bed)
        {
            var now = DateTime.Now;
            bed.UserId = "SYSTEM";
            bed.UDate = now.Date;
            bed.UTime = now.ToString("HH:mm");
        }
    }
}
