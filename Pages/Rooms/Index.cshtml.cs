using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Rooms
{
    /// <summary>
    /// Rooms &amp; Beds - the hospital bed master, rebuilt on the two legacy
    /// files the wards actually use:
    ///
    ///   [WARD]     &lt;- wards.DBF     (15 wards: PW, GW, SW, IC, CC ...)
    ///   [BED_POS]  &lt;- bed_pos.DBF   (113 beds: PW102, GW423, OT ...)
    ///
    /// The screen used to list [ROOM], which never received a single row, so
    /// the page was always empty. It now shows one card per master:
    ///
    ///   * Wards - code, name, number of beds, number of occupied beds; a city
    ///     ward cannot be retired while it still has live beds.
    ///   * Beds  - ward, bed code, description, status (Vacant / Occupied),
    ///     patient and room-charge service code, paged and filtered by ward,
    ///     status and free text.
    ///
    /// Both masters are list + popup entry, soft-retire with a reason and keep
    /// the legacy stamp (USERID/UDATE/UTIME) on every save. Writes need the
    /// MASTER_ROOM_EDIT privilege (or ALL); the lists need a login.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 22-Oct-2026
    /// Time      : 14:05
    /// Change Log: 22-Oct-2026 14:05 - rewritten on WARD + BED_POS (the ROOM
    ///             table it used before was empty), paging, popups, soft-retire
    ///             with reason, SweetAlert, server-side gates.
    /// </summary>
    public class IndexModel : Shared.PagedPageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public IndexModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        // ------------------------------------------------------------- rows

        /// <summary>One ward of the grid, with its bed counters.</summary>
        public class WardRow
        {
            public long Id { get; set; }
            public string Code { get; set; }
            public string Name { get; set; }
            public int BedCount { get; set; }
            public int OccupiedCount { get; set; }
            public string LegacyStamp { get; set; }
            public bool Retired { get; set; }
            public string RetireComments { get; set; }
        }

        /// <summary>One bed of the grid.</summary>
        public class BedRow
        {
            public long Id { get; set; }
            public string WardCode { get; set; }
            public string WardName { get; set; }
            public string BedCode { get; set; }
            public string Description { get; set; }
            public string Status { get; set; }
            public string PatientCode { get; set; }
            public string ServiceCode { get; set; }
            public string LegacyStamp { get; set; }
            public bool Retired { get; set; }
            public string RetireComments { get; set; }
        }

        public List<WardRow> Wards { get; set; } = new();
        public List<BedRow> Beds { get; set; } = new();

        /// <summary>Wards for the filter and the bed popup.</summary>
        public List<SelectListItem> WardList { get; set; } = new();

        // ----------------------------------------------------------- filters

        /// <summary>Bed filter: ward code (query key "Ward").</summary>
        [BindProperty(Name = "Ward", SupportsGet = true)]
        public string WardCode { get; set; }

        /// <summary>Bed filter: Vacant / Occupied.</summary>
        [BindProperty(SupportsGet = true)]
        public string Status { get; set; }

        /// <summary>Bed filter: bed code, patient or description.</summary>
        [BindProperty(SupportsGet = true)]
        public string Q { get; set; }

        /// <summary>Shows the retired wards and beds together with the live ones.</summary>
        [BindProperty(SupportsGet = true)]
        public bool ShowRetired { get; set; }

        public int WardCount { get; set; }
        public int BedCount { get; set; }
        public int OccupiedCount { get; set; }
        public int VacantCount => BedCount - OccupiedCount;

        // ------------------------------------------------------------ inputs

        [BindProperty]
        public WardInputModel WardInput { get; set; } = new();

        [BindProperty]
        public BedInputModel BedInput { get; set; } = new();

        public class WardInputModel
        {
            public long Id { get; set; }
            public string Code { get; set; }
            public string Name { get; set; }
        }

        public class BedInputModel
        {
            public long Id { get; set; }
            public string WardCode { get; set; }
            public string BedCode { get; set; }
            public string Description { get; set; }
            public string Status { get; set; }
            public string PatientCode { get; set; }
            public string ServiceCode { get; set; }
        }

        /// <summary>Status values a bed can carry.</summary>
        public static readonly string[] BedStatuses = { "Vacant", "Occupied" };

        [TempData]
        public string Success { get; set; }

        [TempData]
        public string Error { get; set; }

        /// <summary>Bed and ward masters may only be changed with this right.</summary>
        public bool CanEdit => _session.HasPrivilege("MASTER_ROOM_EDIT")
                               || _session.HasPrivilege("ALL");

        /// <summary>Badge colour of a bed status.</summary>
        public static string BedStatusCss(string status)
        {
            return string.Equals(status, "Occupied", StringComparison.OrdinalIgnoreCase)
                ? "bg-danger"
                : "bg-success";
        }

        // --------------------------------------------------------------- get

        public async Task<IActionResult> OnGetAsync()
        {
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");

            await LoadAsync();
            return Page();
        }

        // ---------------------------------------------------------- save ward

        /// <summary>Creates or updates one ward.</summary>
        public async Task<IActionResult> OnPostSaveWardAsync()
        {
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");
            if (!CanEdit)
            {
                Error = "You do not have the right to change wards and beds.";
                return RedirectToPage();
            }

            var code = WardInput.Code?.Trim().ToUpperInvariant();
            var name = WardInput.Name?.Trim();

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                Error = "Ward code and name are required.";
                return RedirectToPage(new { Ward = WardCode, Status, Q, ShowRetired });
            }

            var clash = await _db.Wards.AnyAsync(w => !w.Retired
                                                      && w.Code == code
                                                      && w.Id != WardInput.Id);
            if (clash)
            {
                Error = $"Ward code {code} is already used by a live ward.";
                return RedirectToPage(new { Ward = WardCode, Status, Q, ShowRetired });
            }

            Ward entity;
            if (WardInput.Id > 0)
            {
                entity = await _db.Wards.FirstOrDefaultAsync(w => w.Id == WardInput.Id);
                if (entity == null)
                {
                    Error = "Ward not found.";
                    return RedirectToPage(new { Ward = WardCode, Status, Q, ShowRetired });
                }
            }
            else
            {
                entity = new Ward { Retired = false };
                _db.Wards.Add(entity);
            }

            entity.Code = code;
            entity.Name = name;
            StampWard(entity);

            await _db.SaveChangesAsync();

            // the beds show the ward name of their ward code: keep them in step
            var beds = await _db.BedPositions.Where(b => b.WardCode == code && !b.Retired).ToListAsync();
            foreach (var bed in beds)
            {
                if (string.IsNullOrWhiteSpace(bed.Description))
                    bed.Description = name;
            }
            if (beds.Count > 0)
                await _db.SaveChangesAsync();

            Success = $"Ward {code} - {name} saved.";
            return RedirectToPage(new { Ward = WardCode, Status, Q, ShowRetired });
        }

        /// <summary>Soft-retires a ward - refused while it still has beds.</summary>
        public async Task<IActionResult> OnPostRetireWardAsync(long id, string reason)
        {
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");
            if (!CanEdit)
            {
                Error = "You do not have the right to change wards and beds.";
                return RedirectToPage();
            }

            var ward = await _db.Wards.FirstOrDefaultAsync(w => w.Id == id);
            if (ward == null)
            {
                Error = "Ward not found.";
                return RedirectToPage(new { Ward = WardCode, Status, Q, ShowRetired });
            }

            var liveBeds = await _db.BedPositions.CountAsync(b => b.WardCode == ward.Code && !b.Retired);
            if (liveBeds > 0)
            {
                Error = $"Ward {ward.Code} still has {liveBeds} live bed(s). Retire or move them first.";
                return RedirectToPage(new { Ward = WardCode, Status, Q, ShowRetired });
            }

            ward.Retired = true;
            ward.RetireComments = string.IsNullOrWhiteSpace(reason)
                ? "Retired from ward master."
                : reason.Trim();
            StampWard(ward);

            await _db.SaveChangesAsync();
            Success = $"Ward {ward.Code} retired.";
            return RedirectToPage(new { Ward = WardCode, Status, Q, ShowRetired });
        }

        /// <summary>Brings a retired ward back (its code must be free).</summary>
        public async Task<IActionResult> OnPostRestoreWardAsync(long id)
        {
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");
            if (!CanEdit)
            {
                Error = "You do not have the right to change wards and beds.";
                return RedirectToPage();
            }

            var ward = await _db.Wards.FirstOrDefaultAsync(w => w.Id == id);
            if (ward == null)
            {
                Error = "Ward not found.";
                return RedirectToPage(new { Ward = WardCode, Status, Q, ShowRetired });
            }

            var clash = await _db.Wards.AnyAsync(w => !w.Retired
                                                      && w.Code == ward.Code
                                                      && w.Id != ward.Id);
            if (clash)
            {
                Error = $"Ward code {ward.Code} is already used by another live ward.";
                return RedirectToPage(new { Ward = WardCode, Status, Q, ShowRetired });
            }

            ward.Retired = false;
            ward.RetireComments = null;
            StampWard(ward);

            await _db.SaveChangesAsync();
            Success = $"Ward {ward.Code} restored.";
            return RedirectToPage(new { Ward = WardCode, Status, Q, ShowRetired });
        }

        // ----------------------------------------------------------- save bed

        /// <summary>Creates or updates one bed position.</summary>
        public async Task<IActionResult> OnPostSaveBedAsync()
        {
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");
            if (!CanEdit)
            {
                Error = "You do not have the right to change wards and beds.";
                return RedirectToPage();
            }

            var bedCode = BedInput.BedCode?.Trim().ToUpperInvariant();
            var wardCode = BedInput.WardCode?.Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(bedCode) || string.IsNullOrWhiteSpace(wardCode))
            {
                Error = "Ward and bed code are required.";
                return RedirectToPage(new { Ward = WardCode, Status, Q, ShowRetired });
            }

            var ward = await _db.Wards.FirstOrDefaultAsync(w => w.Code == wardCode && !w.Retired);
            if (ward == null)
            {
                Error = $"Ward {wardCode} was not found (or is retired).";
                return RedirectToPage(new { Ward = WardCode, Status, Q, ShowRetired });
            }

            var bedStatus = string.Equals(BedInput.Status, "Occupied", StringComparison.OrdinalIgnoreCase)
                ? "Occupied"
                : "Vacant";

            var clash = await _db.BedPositions.AnyAsync(b => !b.Retired
                                                             && b.BedCode == bedCode
                                                             && b.Id != BedInput.Id);
            if (clash)
            {
                Error = $"Bed {bedCode} already exists.";
                return RedirectToPage(new { Ward = WardCode, Status, Q, ShowRetired });
            }

            BedPosition entity;
            if (BedInput.Id > 0)
            {
                entity = await _db.BedPositions.FirstOrDefaultAsync(b => b.Id == BedInput.Id);
                if (entity == null)
                {
                    Error = "Bed not found.";
                    return RedirectToPage(new { Ward = WardCode, Status, Q, ShowRetired });
                }
            }
            else
            {
                entity = new BedPosition { Retired = false };
                _db.BedPositions.Add(entity);
            }

            entity.WardCode = wardCode;
            entity.BedCode = bedCode;
            entity.Description = string.IsNullOrWhiteSpace(BedInput.Description)
                ? ward.Name
                : BedInput.Description.Trim();
            entity.Status = bedStatus;
            entity.PatientCode = bedStatus == "Occupied"
                ? BedInput.PatientCode?.Trim()
                : null;
            entity.ServiceCode = BedInput.ServiceCode?.Trim();
            StampBed(entity);

            await _db.SaveChangesAsync();
            Success = $"Bed {bedCode} saved.";
            return RedirectToPage(new { Ward = WardCode, Status, Q, ShowRetired });
        }

        /// <summary>Soft-retires one bed, with the reason.</summary>
        public async Task<IActionResult> OnPostRetireBedAsync(long id, string reason)
        {
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");
            if (!CanEdit)
            {
                Error = "You do not have the right to change wards and beds.";
                return RedirectToPage();
            }

            var bed = await _db.BedPositions.FirstOrDefaultAsync(b => b.Id == id);
            if (bed == null)
            {
                Error = "Bed not found.";
                return RedirectToPage(new { Ward = WardCode, Status, Q, ShowRetired });
            }

            if (string.Equals(bed.Status, "Occupied", StringComparison.OrdinalIgnoreCase))
            {
                Error = $"Bed {bed.BedCode} is occupied - discharge or move the patient first.";
                return RedirectToPage(new { Ward = WardCode, Status, Q, ShowRetired });
            }

            bed.Retired = true;
            bed.RetireComments = string.IsNullOrWhiteSpace(reason)
                ? "Retired from bed master."
                : reason.Trim();
            StampBed(bed);

            await _db.SaveChangesAsync();
            Success = $"Bed {bed.BedCode} retired.";
            return RedirectToPage(new { Ward = WardCode, Status, Q, ShowRetired });
        }

        /// <summary>Brings a retired bed back (its code must be free).</summary>
        public async Task<IActionResult> OnPostRestoreBedAsync(long id)
        {
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");
            if (!CanEdit)
            {
                Error = "You do not have the right to change wards and beds.";
                return RedirectToPage();
            }

            var bed = await _db.BedPositions.FirstOrDefaultAsync(b => b.Id == id);
            if (bed == null)
            {
                Error = "Bed not found.";
                return RedirectToPage(new { Ward = WardCode, Status, Q, ShowRetired });
            }

            var clash = await _db.BedPositions.AnyAsync(b => !b.Retired
                                                             && b.BedCode == bed.BedCode
                                                             && b.Id != bed.Id);
            if (clash)
            {
                Error = $"Bed {bed.BedCode} already exists.";
                return RedirectToPage(new { Ward = WardCode, Status, Q, ShowRetired });
            }

            var wardLive = await _db.Wards.AnyAsync(w => w.Code == bed.WardCode && !w.Retired);
            if (!wardLive)
            {
                Error = $"Ward {bed.WardCode} is not live - restore the ward first.";
                return RedirectToPage(new { Ward = WardCode, Status, Q, ShowRetired });
            }

            bed.Retired = false;
            bed.RetireComments = null;
            StampBed(bed);

            await _db.SaveChangesAsync();
            Success = $"Bed {bed.BedCode} restored.";
            return RedirectToPage(new { Ward = WardCode, Status, Q, ShowRetired });
        }

        // -------------------------------------------------------------- load

        private async Task LoadAsync()
        {
            // wards (the list is 15 rows: the retired ones are simply badged)
            var wards = await _db.Wards.AsNoTracking()
                .OrderBy(w => w.Retired)
                .ThenBy(w => w.Code)
                .ToListAsync();

            var bedStats = await _db.BedPositions.AsNoTracking()
                .Where(b => !b.Retired)
                .GroupBy(b => b.WardCode)
                .Select(g => new
                {
                    WardCode = g.Key,
                    Total = g.Count(),
                    Occupied = g.Count(x => x.Status == "Occupied")
                })
                .ToListAsync();

            var statsByWard = bedStats.ToDictionary(
                s => s.WardCode ?? string.Empty,
                s => s,
                StringComparer.OrdinalIgnoreCase);

            Wards = wards.Select(w =>
            {
                statsByWard.TryGetValue(w.Code ?? string.Empty, out var st);
                return new WardRow
                {
                    Id = w.Id,
                    Code = w.Code,
                    Name = w.Name,
                    BedCount = st == null ? 0 : st.Total,
                    OccupiedCount = st == null ? 0 : st.Occupied,
                    LegacyStamp = BuildStamp(w.UserId, w.UDate, w.UTime),
                    Retired = w.Retired,
                    RetireComments = w.RetireComments
                };
            }).ToList();

            WardList = wards.Where(w => !w.Retired)
                .Select(w => new SelectListItem
                {
                    Value = w.Code,
                    Text = $"{w.Code} - {w.Name}"
                })
                .ToList();

            WardCount = wards.Count(w => !w.Retired);
            BedCount = bedStats.Sum(s => s.Total);
            OccupiedCount = bedStats.Sum(s => s.Occupied);

            // beds (paged)
            var query = _db.BedPositions.AsNoTracking();

            if (!ShowRetired)
                query = query.Where(b => !b.Retired);

            if (!string.IsNullOrWhiteSpace(WardCode))
                query = query.Where(b => b.WardCode == WardCode.Trim());

            if (string.Equals(Status, "Vacant", StringComparison.OrdinalIgnoreCase))
                query = query.Where(b => b.Status != "Occupied");
            else if (string.Equals(Status, "Occupied", StringComparison.OrdinalIgnoreCase))
                query = query.Where(b => b.Status == "Occupied");

            if (!string.IsNullOrWhiteSpace(Q))
            {
                var term = Q.Trim();
                query = query.Where(b =>
                    (b.BedCode != null && b.BedCode.Contains(term))
                    || (b.PatientCode != null && b.PatientCode.Contains(term))
                    || (b.Description != null && b.Description.Contains(term)));
            }

            TotalCount = await query.CountAsync();
            ClampPaging();

            var beds = await ApplyPaging(query
                .OrderBy(b => b.WardCode)
                .ThenBy(b => b.BedCode))
                .ToListAsync();

            var wardNames = wards.ToDictionary(
                w => w.Code ?? string.Empty,
                w => w.Name ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);

            Beds = beds.Select(b => new BedRow
            {
                Id = b.Id,
                WardCode = b.WardCode,
                WardName = !string.IsNullOrWhiteSpace(b.WardCode) && wardNames.TryGetValue(b.WardCode, out var wn)
                    ? wn
                    : "-",
                BedCode = b.BedCode,
                Description = b.Description,
                Status = string.IsNullOrWhiteSpace(b.Status) ? "Vacant" : b.Status,
                PatientCode = b.PatientCode,
                ServiceCode = b.ServiceCode,
                LegacyStamp = BuildStamp(b.UserId, b.UDate, b.UTime),
                Retired = b.Retired,
                RetireComments = b.RetireComments
            }).ToList();
        }

        /// <summary>"user 22-Oct-2026 13:45" from the legacy stamp columns.</summary>
        private static string BuildStamp(string user, DateTime? date, string time)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(user)) parts.Add(user);
            if (date.HasValue) parts.Add(date.Value.ToString("dd-MMM-yyyy"));
            if (!string.IsNullOrWhiteSpace(time)) parts.Add(time);

            return parts.Count == 0 ? "-" : string.Join(" ", parts);
        }

        /// <summary>Writes the legacy stamp of the logged user and the clock.</summary>
        private void StampWard(Ward ward)
        {
            var now = DateTime.Now;
            ward.UserId = _session.LoggedUser?.UserName ?? "SYSTEM";
            ward.UDate = now.Date;
            ward.UTime = now.ToString("HH:mm");
        }

        /// <summary>Writes the legacy stamp of the logged user and the clock.</summary>
        private void StampBed(BedPosition bed)
        {
            var now = DateTime.Now;
            bed.UserId = _session.LoggedUser?.UserName ?? "SYSTEM";
            bed.UDate = now.Date;
            bed.UTime = now.ToString("HH:mm");
        }
    }
}
