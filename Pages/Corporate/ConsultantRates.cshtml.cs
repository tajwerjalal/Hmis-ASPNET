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

namespace HmisWeb.Pages.Corporate
{
    /// <summary>
    /// Consultant rate card - /corporate/consultantrates.
    ///
    /// The screen now runs on the legacy file E:\Projects\HMC\Master\con_rat.DBF
    /// (10,461 live rows), imported into [CONSULTANTSERVICERATE]:
    ///
    ///   company (COMPANY.ID) x consultant (STAFF.Code) x ward class
    ///   (WCODES.CODE) x service 11001 "Visit (s)" -> Staff Rate (RATE_1)
    ///   and Unit Rate (RATE_2), with the legacy RT_FLG (V/F).
    ///
    /// List + popup entry, server-side paging (the card has 10k rows), soft
    /// retire with a reason (SweetAlert), restore, and login / MASTER_CONSULTANT_EDIT
    /// gates on every write.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 22-Oct-2026
    /// Time      : 15:00
    /// Change Log: 22-Oct-2026 15:00 - rebuilt on the con_rat rows: company /
    ///             consultant / ward class filters, paging, add-edit popup,
    ///             retire with reason, legacy-only consultant codes, gates.
    /// </summary>
    public class ConsultantRatesModel : Shared.PagedPageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public ConsultantRatesModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        /// <summary>One row of the rate grid, already resolved to names.</summary>
        public class RateRow
        {
            public long Id { get; set; }
            public string CompanyCode { get; set; }
            public string CompanyName { get; set; }
            public long? ConsultantId { get; set; }
            public string ConsultantCode { get; set; }
            public string ConsultantName { get; set; }
            public string WardCode { get; set; }
            public string WardName { get; set; }
            public string ServiceCode { get; set; }
            public string ServiceName { get; set; }
            public decimal StaffRate { get; set; }
            public decimal UnitRate { get; set; }
            public string RateFlag { get; set; }
            public bool Retired { get; set; }
            public string RetireComments { get; set; }
            public string LegacyStamp { get; set; }
        }

        public List<RateRow> Rates { get; set; } = new();

        /// <summary>Companies (COMPANY master) for the popup and the filter.</summary>
        public List<SelectListItem> CompanyList { get; set; } = new();

        /// <summary>Companies that actually carry rates (filter list).</summary>
        public List<SelectListItem> CompanyFilterList { get; set; } = new();

        /// <summary>Live app consultants.</summary>
        public List<SelectListItem> ConsultantList { get; set; } = new();

        /// <summary>Consultant codes of the rate card that are no longer in STAFF.</summary>
        public List<SelectListItem> LegacyConsultantList { get; set; } = new();

        /// <summary>Ward classes of WCODES (plus E/M/T that only exist in con_rat).</summary>
        public List<SelectListItem> WardList { get; set; } = new();

        public List<SelectListItem> FlagList { get; set; } = new()
        {
            new SelectListItem { Value = "V", Text = "V" },
            new SelectListItem { Value = "F", Text = "F" }
        };

        // ----------------------------------------------------------- filters

        /// <summary>Filter: company code (COMPANY.ID).</summary>
        [BindProperty(SupportsGet = true)]
        public string Company { get; set; }

        /// <summary>Filter: app consultant id (query key "Consultant").</summary>
        [BindProperty(Name = "Consultant", SupportsGet = true)]
        public long? ConsultantId { get; set; }

        /// <summary>Filter: ward class code.</summary>
        [BindProperty(SupportsGet = true)]
        public string Ward { get; set; }

        /// <summary>Filter: legacy RT_FLG.</summary>
        [BindProperty(SupportsGet = true)]
        public string Flag { get; set; }

        /// <summary>Shows retired rows together with the live ones.</summary>
        [BindProperty(SupportsGet = true)]
        public bool ShowRetired { get; set; }

        // ------------------------------------------------------------ inputs

        [BindProperty]
        public RateInputModel Rate { get; set; } = new();

        public class RateInputModel
        {
            public long? Id { get; set; }
            public string CompanyCode { get; set; }
            public long? ConsultantId { get; set; }

            /// <summary>Legacy code of a consultant that is not in STAFF.</summary>
            public string ConsultantCode { get; set; }

            public string WardCode { get; set; }
            public decimal StaffRate { get; set; }
            public decimal UnitRate { get; set; }
            public string RateFlag { get; set; } = "V";
        }

        /// <summary>
        /// Ward classes of WCODES.DBF (13 rows) plus E, M and T, which the legacy
        /// con_rat file uses but WCODES no longer lists. The code is what the
        /// rate card keys on; the names are only for display.
        /// </summary>
        public static readonly (string Code, string Name)[] WardClasses =
        {
            ("A", "AC ROOM"),
            ("C", "CASUALTY"),
            ("D", "DELEX ROOM"),
            ("E", "LEGACY CLASS E (not in WCODES)"),
            ("G", "GENERAL WARD"),
            ("I", "INTENSIVE CARE UNIT"),
            ("L", "LABOUR ROOM"),
            ("M", "LEGACY CLASS M (not in WCODES)"),
            ("N", "NEONATAL ICU (COT CARE)"),
            ("O", "OUT PATIENT"),
            ("P", "PRIVATE ROOM"),
            ("R", "NEONATAL ICU (INCUBATOR)"),
            ("S", "SEMI PRIVATE ROOM"),
            ("T", "LEGACY CLASS T (not in WCODES)"),
            ("V", "VIP ROOM"),
            ("Z", "PEDIATRIC ICU")
        };

        /// <summary>The legacy consultation service the file prices.</summary>
        public const string VisitServiceCode = "11001";

        [TempData]
        public string Success { get; set; }

        [TempData]
        public string Error { get; set; }

        public bool CanEdit => _session.HasPrivilege("MASTER_CONSULTANT_EDIT")
                               || _session.HasPrivilege("ALL");

        /// <summary>Display name of a ward class code.</summary>
        public static string WardClassName(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return "-";

            foreach (var w in WardClasses)
            {
                if (string.Equals(w.Code, code, StringComparison.OrdinalIgnoreCase))
                    return w.Name;
            }

            return $"CLASS {code} (unknown)";
        }

        /// <summary>Badge colour of the legacy RT_FLG.</summary>
        public static string FlagCss(string flag)
        {
            return string.Equals(flag, "F", StringComparison.OrdinalIgnoreCase)
                ? "bg-warning text-dark"
                : "bg-info text-dark";
        }

        // --------------------------------------------------------------- get

        public async Task<IActionResult> OnGetAsync()
        {
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");

            await LoadAsync();
            return Page();
        }

        // -------------------------------------------------------------- save

        /// <summary>
        /// Saves one rate row. New rows are keyed on company + consultant code +
        /// ward class + service, the legacy key of con_rat.
        /// </summary>
        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");
            if (!CanEdit)
            {
                Error = "You do not have the right to change consultant rates.";
                return RedirectToPage(new { Company, Consultant = ConsultantId, Ward, Flag, ShowRetired });
            }

            var companyCode = Rate.CompanyCode?.Trim().ToUpperInvariant();
            var wardCode = Rate.WardCode?.Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(companyCode))
            {
                Error = "Company is required.";
                return RedirectToPage(new { Company, Consultant = ConsultantId, Ward, Flag, ShowRetired });
            }

            if (string.IsNullOrWhiteSpace(wardCode))
            {
                Error = "Ward class is required.";
                return RedirectToPage(new { Company, Consultant = ConsultantId, Ward, Flag, ShowRetired });
            }

            if (Rate.StaffRate < 0 || Rate.UnitRate < 0)
            {
                Error = "Rates cannot be negative.";
                return RedirectToPage(new { Company, Consultant = ConsultantId, Ward, Flag, ShowRetired });
            }

            // consultant: either an app consultant (id) or a legacy code that the
            // staff master no longer carries (A16, A21, I03, J02, M09, N08, N09, V04)
            long? consultantId = null;
            string consultantCode;

            if (Rate.ConsultantId.HasValue && Rate.ConsultantId.Value > 0)
            {
                var staff = await _db.Consultants.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == Rate.ConsultantId.Value);
                if (staff == null)
                {
                    Error = "The selected consultant was not found.";
                    return RedirectToPage(new { Company, Consultant = ConsultantId, Ward, Flag, ShowRetired });
                }

                consultantId = staff.Id;
                consultantCode = staff.Code?.Trim();
            }
            else
            {
                consultantCode = Rate.ConsultantCode?.Trim().ToUpperInvariant();
            }

            if (string.IsNullOrWhiteSpace(consultantCode))
            {
                Error = "Consultant is required.";
                return RedirectToPage(new { Company, Consultant = ConsultantId, Ward, Flag, ShowRetired });
            }

            // the legacy file prices one service: 11001 "Visit (s)"
            var item = await _db.Items.AsNoTracking()
                .FirstOrDefaultAsync(i => i.Code == VisitServiceCode);
            if (item == null)
            {
                Error = $"The visit service ({VisitServiceCode}) was not found in the item master.";
                return RedirectToPage(new { Company, Consultant = ConsultantId, Ward, Flag, ShowRetired });
            }

            var flag = string.Equals(Rate.RateFlag, "F", StringComparison.OrdinalIgnoreCase) ? "F" : "V";

            ConsultantServiceRate entity;
            if (Rate.Id.HasValue && Rate.Id.Value > 0)
            {
                entity = await _db.ConsultantServiceRates.FirstOrDefaultAsync(r => r.Id == Rate.Id.Value);
                if (entity == null)
                {
                    Error = "Consultant rate not found.";
                    return RedirectToPage(new { Company, Consultant = ConsultantId, Ward, Flag, ShowRetired });
                }
            }
            else
            {
                entity = await _db.ConsultantServiceRates.FirstOrDefaultAsync(r =>
                    !r.Retired
                    && r.CompanyCode == companyCode
                    && r.ConsultantCode == consultantCode
                    && r.WardCode == wardCode
                    && r.ServiceCode == VisitServiceCode);
            }

            if (entity != null)
            {
                // an edit may move the row to another company / consultant / ward,
                // so the legacy key is checked again
                var clash = await _db.ConsultantServiceRates.AnyAsync(r =>
                    !r.Retired
                    && r.Id != entity.Id
                    && r.CompanyCode == companyCode
                    && r.ConsultantCode == consultantCode
                    && r.WardCode == wardCode
                    && r.ServiceCode == VisitServiceCode);
                if (clash)
                {
                    Error = "A rate for this company, consultant and ward class already exists.";
                    return RedirectToPage(new { Company, Consultant = ConsultantId, Ward, Flag, ShowRetired });
                }

                entity.EditedAt = DateTime.Now;
                entity.EditerId = _session.WebUserId;
                Success = "Consultant rate updated.";
            }
            else
            {
                entity = new ConsultantServiceRate
                {
                    CreatedAt = DateTime.Now,
                    CreaterId = _session.WebUserId
                };
                _db.ConsultantServiceRates.Add(entity);
                Success = "Consultant rate added.";
            }

            entity.CompanyCode = companyCode;
            entity.ConsultantId = consultantId;
            entity.ConsultantCode = consultantCode;
            entity.WardCode = wardCode;
            entity.ItemId = item.Id;
            entity.ServiceCode = VisitServiceCode;
            entity.StaffRate = decimal.Round(Rate.StaffRate, 2);
            entity.UnitRate = decimal.Round(Rate.UnitRate, 2);
            entity.RateFlag = flag;
            entity.Retired = false;
            entity.RetireComments = null;

            var now = DateTime.Now;
            entity.UserId = _session.LoggedUser?.UserName ?? "SYSTEM";
            entity.UDate = now.Date;
            entity.UTime = now.ToString("HH:mm");

            await _db.SaveChangesAsync();
            Error = null;

            return RedirectToPage(new { Company, Consultant = ConsultantId, Ward, Flag, ShowRetired });
        }

        // ------------------------------------------------------------ retire

        /// <summary>Soft-retires one rate row, keeping the reason.</summary>
        public async Task<IActionResult> OnPostRetireAsync(long id, string reason)
        {
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");
            if (!CanEdit)
            {
                Error = "You do not have the right to change consultant rates.";
                return RedirectToPage(new { Company, Consultant = ConsultantId, Ward, Flag, ShowRetired });
            }

            var entity = await _db.ConsultantServiceRates.FirstOrDefaultAsync(r => r.Id == id);
            if (entity == null)
            {
                Error = "Consultant rate not found.";
                return RedirectToPage(new { Company, Consultant = ConsultantId, Ward, Flag, ShowRetired });
            }

            entity.Retired = true;
            entity.RetireComments = string.IsNullOrWhiteSpace(reason)
                ? "Retired from the consultant rate card."
                : reason.Trim();
            entity.EditedAt = DateTime.Now;
            entity.EditerId = _session.WebUserId;

            await _db.SaveChangesAsync();
            Success = "Consultant rate retired.";
            return RedirectToPage(new { Company, Consultant = ConsultantId, Ward, Flag, ShowRetired });
        }

        /// <summary>Brings a retired rate row back (its legacy key must be free).</summary>
        public async Task<IActionResult> OnPostRestoreAsync(long id)
        {
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");
            if (!CanEdit)
            {
                Error = "You do not have the right to change consultant rates.";
                return RedirectToPage(new { Company, Consultant = ConsultantId, Ward, Flag, ShowRetired });
            }

            var entity = await _db.ConsultantServiceRates.FirstOrDefaultAsync(r => r.Id == id);
            if (entity == null)
            {
                Error = "Consultant rate not found.";
                return RedirectToPage(new { Company, Consultant = ConsultantId, Ward, Flag, ShowRetired });
            }

            var clash = await _db.ConsultantServiceRates.AnyAsync(r =>
                !r.Retired
                && r.Id != entity.Id
                && r.CompanyCode == entity.CompanyCode
                && r.ConsultantCode == entity.ConsultantCode
                && r.WardCode == entity.WardCode
                && r.ServiceCode == entity.ServiceCode);
            if (clash)
            {
                Error = "A live rate with the same company, consultant and ward class already exists.";
                return RedirectToPage(new { Company, Consultant = ConsultantId, Ward, Flag, ShowRetired });
            }

            entity.Retired = false;
            entity.RetireComments = null;
            entity.EditedAt = DateTime.Now;
            entity.EditerId = _session.WebUserId;

            await _db.SaveChangesAsync();
            Success = "Consultant rate restored.";
            return RedirectToPage(new { Company, Consultant = ConsultantId, Ward, Flag, ShowRetired });
        }

        // -------------------------------------------------------------- load

        private async Task LoadAsync()
        {
            // company master (keyless legacy COMPANY table)
            var companies = await _db.CompanyMasters.AsNoTracking()
                .OrderBy(c => c.Id)
                .ToListAsync();

            CompanyList = companies.Select(c => new SelectListItem
            {
                Value = c.Id,
                Text = string.IsNullOrWhiteSpace(c.Name) ? c.Id : $"{c.Id} - {c.Name}"
            }).ToList();

            var companyNames = companies
                .GroupBy(c => c.Id ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Name ?? string.Empty, StringComparer.OrdinalIgnoreCase);

            // app consultants (STAFF with DTYPE='Consultant')
            var consultants = await _db.Consultants.AsNoTracking()
                .Include(c => c.Person)
                .Where(c => !c.Retired)
                .OrderBy(c => c.Person != null ? c.Person.Name : "")
                .ToListAsync();

            ConsultantList = consultants.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = string.IsNullOrWhiteSpace(c.Code)
                    ? ConsultantName(c)
                    : $"{ConsultantName(c)} ({c.Code})"
            }).ToList();

            var staffCodes = new HashSet<string>(
                consultants.Where(c => !string.IsNullOrWhiteSpace(c.Code)).Select(c => c.Code.Trim()),
                StringComparer.OrdinalIgnoreCase);

            // the legacy codes that never made it into STAFF
            var rateCodes = await _db.ConsultantServiceRates.AsNoTracking()
                .Where(r => r.ConsultantCode != null && r.ConsultantCode != "")
                .Select(r => r.ConsultantCode)
                .Distinct()
                .ToListAsync();

            LegacyConsultantList = rateCodes
                .Where(c => !staffCodes.Contains(c))
                .OrderBy(c => c)
                .Select(c => new SelectListItem { Value = c, Text = $"{c} - legacy (not in staff master)" })
                .ToList();

            WardList = WardClasses.Select(w => new SelectListItem
            {
                Value = w.Code,
                Text = $"{w.Code} - {w.Name}"
            }).ToList();

            // companies that carry at least one rate (filter list)
            var usedCompanies = await _db.ConsultantServiceRates.AsNoTracking()
                .Where(r => !r.Retired)
                .Select(r => r.CompanyCode)
                .Distinct()
                .ToListAsync();

            CompanyFilterList = usedCompanies
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .Select(c => new SelectListItem
                {
                    Value = c,
                    Text = companyNames.TryGetValue(c, out var name) && !string.IsNullOrWhiteSpace(name)
                        ? $"{c} - {name}"
                        : c
                })
                .ToList();

            // ------------------------------------------------------- grid
            var query = _db.ConsultantServiceRates.AsNoTracking()
                .Include(r => r.Consultant).ThenInclude(c => c.Person)
                .AsQueryable();

            if (!ShowRetired)
                query = query.Where(r => !r.Retired);

            if (!string.IsNullOrWhiteSpace(Company))
                query = query.Where(r => r.CompanyCode == Company.Trim());

            if (ConsultantId.HasValue && ConsultantId.Value > 0)
                query = query.Where(r => r.ConsultantId == ConsultantId.Value);

            if (!string.IsNullOrWhiteSpace(Ward))
                query = query.Where(r => r.WardCode == Ward.Trim());

            if (!string.IsNullOrWhiteSpace(Flag))
                query = query.Where(r => r.RateFlag == Flag.Trim());

            TotalCount = await query.CountAsync();
            ClampPaging();

            var rows = await ApplyPaging(query
                .OrderBy(r => r.CompanyCode)
                .ThenBy(r => r.ConsultantCode)
                .ThenBy(r => r.WardCode))
                .ToListAsync();

            // service names are resolved separately: one old test row points at an
            // item id that no longer exists, and a required Include would have
            // dropped that row from the page while the count still included it
            var itemIds = rows.Select(r => r.ItemId).Distinct().ToList();
            var itemNames = await _db.Items.AsNoTracking()
                .Where(i => itemIds.Contains(i.Id))
                .Select(i => new { i.Id, i.Name })
                .ToDictionaryAsync(i => i.Id, i => i.Name);

            Rates = rows.Select(r => new RateRow
            {
                Id = r.Id,
                CompanyCode = r.CompanyCode,
                CompanyName = !string.IsNullOrWhiteSpace(r.CompanyCode)
                               && companyNames.TryGetValue(r.CompanyCode, out var cname)
                    ? cname
                    : null,
                ConsultantId = r.ConsultantId,
                ConsultantCode = r.ConsultantCode,
                ConsultantName = r.Consultant != null
                    ? ConsultantName(r.Consultant)
                    : (string.IsNullOrWhiteSpace(r.ConsultantCode) ? "-" : $"{r.ConsultantCode} (legacy)"),
                WardCode = r.WardCode,
                WardName = WardClassName(r.WardCode),
                ServiceCode = string.IsNullOrWhiteSpace(r.ServiceCode) ? "-" : r.ServiceCode,
                ServiceName = itemNames.TryGetValue(r.ItemId, out var itemName) ? itemName : null,
                StaffRate = r.StaffRate,
                UnitRate = r.UnitRate,
                RateFlag = r.RateFlag,
                Retired = r.Retired,
                RetireComments = r.RetireComments,
                LegacyStamp = BuildStamp(r.UserId, r.UDate, r.UTime)
            }).ToList();
        }

        /// <summary>Name (with initials when known) of an app consultant.</summary>
        private static string ConsultantName(Consultant consultant)
        {
            if (consultant?.Person == null)
                return consultant?.Code ?? "Consultant";

            return consultant.Person.NameWithInitials
                   ?? consultant.Person.Name
                   ?? consultant.Code
                   ?? "Consultant";
        }

        /// <summary>"user 22-Oct-2026 14:30" from the legacy stamp columns.</summary>
        private static string BuildStamp(string user, DateTime? date, string time)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(user)) parts.Add(user);
            if (date.HasValue) parts.Add(date.Value.ToString("dd-MMM-yyyy"));
            if (!string.IsNullOrWhiteSpace(time)) parts.Add(time);

            return parts.Count == 0 ? "-" : string.Join(" ", parts);
        }
    }
}
