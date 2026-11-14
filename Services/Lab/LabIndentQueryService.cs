using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Models.Legacy;

namespace HmisWeb.Services.Lab
{
    /// <summary>
    /// READ-ONLY queries for the single-screen LAB SERVICE INDENT (INVOICE) page.
    ///
    /// Purpose
    ///   * Serve the patient search, the patient header (OPD/IPD context, panel,
    ///     unpaid balance) and the live laboratory-test search.  All queries are
    ///     server-side and capped; the page never downloads the full catalogue.
    ///   * Reproduce the legacy LAB indent numbering shape (decade digit + day +
    ///     6-digit sequence, displayed as 999-999999) for the title-bar preview.
    ///   * Resolve the company/panel rate from COMPANYSERVICERATE against the
    ///     INV-LB-{CODE} mirror ITEM created by DeptBillingMirrorService.
    ///
    /// Key rules
    ///   * Laboratory tests live in SERVICES (DEPT = '01'), not ITEM.
    ///   * Only !Retired && Billable && Active services are returned.
    ///   * Patient search never returns more than 7 suggestions.
    ///   * Test search returns at most 7 suggestions plus the total remaining count.
    ///   * No writes happen here.  The save path is LabIndentService.
    ///
    /// Developer : Tajwer Jalal
    /// Created   : 26-Oct-2026 13:10
    /// </summary>
    public class LabIndentQueryService
    {
        /// <summary>Legacy department code of the laboratory (SERVICES.DEPT).</summary>
        public const string LabDept = "01";

        /// <summary>Mirror prefix used by DeptBillingMirrorService for lab items.</summary>
        public const string LabItemPrefix = "INV-LB-";

        private readonly ApplicationDbContext _db;

        public LabIndentQueryService(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Returns the next indent number preview for today.
        /// The value is NOT reserved; the save allocates it under an application lock.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Raw 9-digit value and its 999-999999 display form.</returns>
        public async Task<IndentNumberDto> GetNextIndentPreviewAsync(CancellationToken ct = default)
        {
            var now = DateTime.Now;
            var prefix = BuildPrefix(now);
            var max = await GetMaxSlipAsync(prefix, ct);
            var next = ParseSequence(max) + 1;
            if (next > 999999) next = 1;
            var raw = prefix + next.ToString("D6");
            return new IndentNumberDto { IndentNo = raw, Display = FormatDisplay(raw) };
        }

        /// <summary>
        /// Searches patients by registration number, PHN, mobile, phone, NIC, name
        /// or a multi-word name fragment. Returns at most 7 suggestions, ranked
        /// exact identifier -> name prefix -> name contains.
        /// </summary>
        /// <param name="term">User input (>= 1 character).</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Up to 7 patient suggestions.</returns>
        public async Task<List<PatientSuggestionDto>> SearchPatientsAsync(string term, CancellationToken ct = default)
        {
            term = (term ?? string.Empty).Trim();
            if (term.Length == 0) return new List<PatientSuggestionDto>();

            var words = term.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            long.TryParse(term, out var numeric);

            var q = _db.Patients.AsNoTracking()
                .Where(p => !p.Retired && p.Person != null);

            if (numeric > 0)
            {
                q = q.Where(p => p.PatientId == numeric
                    || p.Phn == term
                    || p.Code == term
                    || p.Person.Mobile == term
                    || p.Person.Phone == term
                    || p.Person.Nic == term);
            }
            else
            {
                foreach (var word in words)
                {
                    var w = word;
                    q = q.Where(p =>
                        (p.Person.Name != null && p.Person.Name.Contains(w))
                        || (p.Person.NameWithInitials != null && p.Person.NameWithInitials.Contains(w))
                        || (p.Person.FullName != null && p.Person.FullName.Contains(w))
                        || (p.Person.Surname != null && p.Person.Surname.Contains(w))
                        || (p.Person.LastName != null && p.Person.LastName.Contains(w))
                        || (p.Person.Initials != null && p.Person.Initials.Contains(w))
                        || (p.Person.Nic != null && p.Person.Nic.Contains(w))
                        || (p.Person.Mobile != null && p.Person.Mobile.Contains(w))
                        || (p.Person.Phone != null && p.Person.Phone.Contains(w)));
                }
            }

            var rows = await q
                .OrderBy(p => numeric > 0 && p.PatientId == numeric ? 0 : 1)
                .ThenBy(p => p.Person.Name != null && p.Person.Name.StartsWith(term) ? 0 : 1)
                .ThenBy(p => p.Person.Name)
                .Take(7)
                .Select(p => new
                {
                    p.Id,
                    p.PatientId,
                    p.Phn,
                    p.Code,
                    p.Person.Name,
                    p.Person.NameWithInitials,
                    p.Person.Dob,
                    p.Person.Sex,
                    p.Person.Mobile,
                    p.Person.Nic
                })
                .ToListAsync(ct);

            var ids = rows.Select(r => r.Id).ToList();
            var admitted = await _db.PatientEncounters.AsNoTracking()
                .Where(e => e.PatientId != null
                    && ids.Contains(e.PatientId ?? 0)
                    && !e.Retired
                    && e.EncounterType == EncounterType.Inward
                    && e.DateOfDischarge == null)
                .Select(e => e.PatientId ?? 0)
                .Distinct()
                .ToListAsync(ct);
            var admittedSet = admitted.ToHashSet();

            return rows.Select(r => new PatientSuggestionDto
            {
                Id = r.Id,
                PatientId = r.PatientId,
                Phn = r.Phn,
                Code = r.Code,
                Name = r.Name ?? string.Empty,
                NameWithInitials = r.NameWithInitials ?? string.Empty,
                Age = AgeText(r.Dob),
                Sex = r.Sex?.ToString() ?? string.Empty,
                Mobile = r.Mobile ?? string.Empty,
                Nic = r.Nic ?? string.Empty,
                IsAdmitted = admittedSet.Contains(r.Id),
                IsExact = (numeric > 0 && (r.PatientId == numeric || r.Phn == term || r.Mobile == term || r.Nic == term))
                    || string.Equals(r.Name, term, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(r.NameWithInitials, term, StringComparison.OrdinalIgnoreCase)
            }).ToList();
        }

        /// <summary>
        /// Builds the read-only patient header: demographics, latest OPD encounter,
        /// all active IPD stays (with ward/bed/consultant), panel/company and the
        /// unpaid BILL balance.
        /// </summary>
        /// <param name="patientId">PATIENT.Id (not the legacy registration number).</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The context, or null when the patient does not exist/retired.</returns>
        public async Task<PatientContextDto> GetPatientContextAsync(long patientId, CancellationToken ct = default)
        {
            var patient = await _db.Patients.AsNoTracking()
                .Include(p => p.Person)
                .FirstOrDefaultAsync(p => p.Id == patientId && !p.Retired, ct);
            if (patient?.Person == null) return null;

            var ipdStays = await _db.PatientEncounters.AsNoTracking()
                .Include(e => e.OpdDoctor).ThenInclude(s => s.Person)
                .Include(e => e.ReferringConsultant).ThenInclude(s => s.Person)
                .Include(e => e.ReferringStaff).ThenInclude(s => s.Person)
                .Where(e => e.PatientId == patientId
                    && !e.Retired
                    && e.EncounterType == EncounterType.Inward
                    && e.DateOfDischarge == null)
                .OrderByDescending(e => e.DateOfAdmission)
                .ThenByDescending(e => e.Id)
                .ToListAsync(ct);

            var opd = await _db.PatientEncounters.AsNoTracking()
                .Include(e => e.OpdDoctor).ThenInclude(s => s.Person)
                .Include(e => e.ReferringConsultant).ThenInclude(s => s.Person)
                .Where(e => e.PatientId == patientId
                    && !e.Retired
                    && e.EncounterType == EncounterType.Opd)
                .OrderByDescending(e => e.EncounterDate)
                .ThenByDescending(e => e.Id)
                .FirstOrDefaultAsync(ct);

            var ipdIds = ipdStays.Select(e => e.Id).ToList();
            var beds = await _db.PatientRooms.AsNoTracking()
                .Include(r => r.Ward)
                .Include(r => r.BedPosition)
                .Where(r => r.PatientEncounterId != null
                    && ipdIds.Contains(r.PatientEncounterId ?? 0)
                    && !r.Retired
                    && r.ToTime == null
                    && !r.Discharged)
                .OrderByDescending(r => r.FromTime)
                .ToListAsync(ct);

            var ipdDtos = ipdStays.Select(e =>
            {
                var bed = beds.FirstOrDefault(b => b.PatientEncounterId == e.Id);
                return MapEncounter(e, bed, true);
            }).ToList();

            long? companyId = ipdStays.FirstOrDefault()?.CreditCompanyId
                ?? opd?.CreditCompanyId
                ?? await _db.CompanyEmployees.AsNoTracking()
                    .Where(e => e.PersonId == patient.PersonId && !e.Retired)
                    .Select(e => (long?)e.CompanyId)
                    .FirstOrDefaultAsync(ct);

            Institution company = null;
            if (companyId.HasValue)
            {
                company = await _db.Institutions.AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Id == companyId.Value && !i.Retired, ct);
            }

            var unpaid = await _db.Bills.AsNoTracking()
                .Where(b => b.PatientId == patient.Id && !b.Retired && !b.Cancelled && !b.Paid)
                .SumAsync(b => (double?)(b.NetTotal - b.PaidAmount), ct) ?? 0;

            var next = await GetNextIndentPreviewAsync(ct);

            return new PatientContextDto
            {
                Id = patient.Id,
                PatientId = patient.PatientId,
                Phn = patient.Phn,
                Code = patient.Code,
                Name = patient.Person.Name ?? string.Empty,
                NameWithInitials = patient.Person.NameWithInitials ?? string.Empty,
                Age = AgeText(patient.Person.Dob),
                Sex = patient.Person.Sex?.ToString() ?? string.Empty,
                Mobile = patient.Person.Mobile ?? string.Empty,
                Phone = patient.Person.Phone ?? string.Empty,
                Nic = patient.Person.Nic ?? string.Empty,
                IsAdmitted = ipdDtos.Count > 0,
                ActiveIpdEncounters = ipdDtos,
                OpdEncounter = opd == null ? null : MapEncounter(opd, null, false),
                CompanyId = companyId,
                CompanyName = company?.Name,
                CompanyCode = company?.Code ?? companyId?.ToString(),
                UnpaidBalance = Math.Round((decimal)unpaid, 2, MidpointRounding.AwayFromZero),
                NextIndentNo = next.IndentNo,
                NextIndentDisplay = next.Display
            };
        }

        /// <summary>
        /// Live laboratory test search. Multi-word terms are AND-ed; results are
        /// ranked exact code -> name prefix -> name. Already-added codes are
        /// excluded so the same test cannot be added twice.
        /// </summary>
        /// <param name="term">Typed text (empty returns the first 7 active tests).</param>
        /// <param name="excludeCodes">SERVICES.CODE values already on the indent.</param>
        /// <param name="companyId">Panel/company id for the applied rate (nullable).</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>At most 7 suggestions and the total remaining count.</returns>
        public async Task<TestSearchResultDto> SearchTestsAsync(string term, IEnumerable<string> excludeCodes,
            long? companyId, CancellationToken ct = default)
        {
            term = (term ?? string.Empty).Trim();
            var exclude = (excludeCodes ?? Enumerable.Empty<string>())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct()
                .ToList();

            var q = _db.DeptServices.AsNoTracking()
                .Where(d => d.Dept == LabDept && !d.Retired && d.Billable && d.Active);

            if (exclude.Count > 0)
                q = q.Where(d => d.Code == null || !exclude.Contains(d.Code));

            if (term.Length > 0)
            {
                var words = term.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    var w = word;
                    q = q.Where(d =>
                        (d.Name != null && d.Name.Contains(w))
                        || (d.Sname != null && d.Sname.Contains(w))
                        || (d.Code != null && d.Code.Contains(w)));
                }
            }

            var total = await q.CountAsync(ct);
            var services = await q
                .OrderBy(d => term.Length > 0 && d.Code == term ? 0 : 1)
                .ThenBy(d => term.Length > 0 && d.Name != null && d.Name.StartsWith(term) ? 0 : 1)
                .ThenBy(d => d.Name)
                .Take(7)
                .ToListAsync(ct);

            var rates = await GetLabRatesAsync(services, companyId, ct);
            return new TestSearchResultDto
            {
                Total = total,
                Items = services.Select(d => new TestSuggestionDto
                {
                    Id = d.Id,
                    Code = d.Code,
                    Name = d.Name,
                    Sname = d.Sname,
                    Rate = rates.TryGetValue(d.Code ?? string.Empty, out var rate)
                        ? rate
                        : (d.Rate ?? 0m)
                }).ToList()
            };
        }

        /// <summary>
        /// Resolves the applied rate for each service: COMPANYSERVICERATE on the
        /// INV-LB-{CODE} mirror item when the patient has a panel/company, otherwise
        /// SERVICES.SRATE.
        /// </summary>
        /// <param name="services">Services whose rates are needed.</param>
        /// <param name="companyId">Panel/company id (nullable).</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Dictionary keyed by SERVICES.CODE.</returns>
        public async Task<Dictionary<string, decimal>> GetLabRatesAsync(IEnumerable<DeptService> services,
            long? companyId, CancellationToken ct = default)
        {
            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var list = services?.Where(s => !string.IsNullOrWhiteSpace(s.Code)).ToList() ?? new List<DeptService>();
            foreach (var service in list)
                result[service.Code] = service.Rate ?? 0m;

            if (!companyId.HasValue || list.Count == 0) return result;

            var mirrorCodes = list.Select(s => LabItemPrefix + s.Code.Trim()).ToList();
            var items = await _db.Items.AsNoTracking()
                .Where(i => i.Code != null && mirrorCodes.Contains(i.Code) && !i.Retired)
                .Select(i => new { i.Id, i.Code })
                .ToListAsync(ct);
            if (items.Count == 0) return result;

            var itemIds = items.Select(i => i.Id).ToList();
            var companyRates = await _db.CompanyServiceRates.AsNoTracking()
                .Where(c => c.CompanyId == companyId.Value
                    && c.ItemId != null
                    && itemIds.Contains(c.ItemId ?? 0)
                    && !c.Retired
                    && c.Rate > 0)
                .OrderByDescending(c => c.EditedAt)
                .ThenByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id)
                .ToListAsync(ct);

            var byItem = companyRates
                .GroupBy(c => c.ItemId ?? 0)
                .ToDictionary(g => g.Key, g => g.First().Rate);

            foreach (var item in items)
            {
                if (item.Code == null || !item.Code.StartsWith(LabItemPrefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                var code = item.Code.Substring(LabItemPrefix.Length);
                if (byItem.TryGetValue(item.Id, out var rate))
                    result[code] = rate;
            }

            return result;
        }

        /// <summary>Legacy prefix = decade digit (2020s -> 2) + 2-digit day of month.</summary>
        public static string BuildPrefix(DateTime now)
            => ((now.Year / 10) % 10).ToString() + now.Day.ToString("D2");

        /// <summary>Formats 226000001 as 226-000001.</summary>
        public static string FormatDisplay(string raw)
            => !string.IsNullOrEmpty(raw) && raw.Length == 9
                ? raw.Substring(0, 3) + "-" + raw.Substring(3)
                : raw;

        /// <summary>Parses the 6-digit sequence of a 9-digit raw indent number.</summary>
        public static int ParseSequence(string raw)
        {
            if (!string.IsNullOrEmpty(raw) && raw.Length == 9
                && int.TryParse(raw.Substring(3), out var seq))
                return seq;
            return 0;
        }

        private static PatientEncounterDto MapEncounter(PatientEncounter encounter, PatientRoom bed, bool isIpd)
        {
            var staff = encounter.OpdDoctor ?? encounter.ReferringConsultant ?? encounter.ReferringStaff;
            return new PatientEncounterDto
            {
                Id = encounter.Id,
                BhtNo = encounter.BhtNo,
                BedNo = bed?.BedPosition?.BedCode,
                WardName = bed?.Ward?.Name,
                AdmissionDate = encounter.DateOfAdmission,
                ConsultantName = staff?.Person?.NameWithInitials ?? staff?.Person?.Name,
                ConsultantCode = staff?.Code,
                IsIpd = isIpd
            };
        }

        private static string AgeText(DateTime? dob)
        {
            if (dob == null) return string.Empty;
            var span = DateTime.Now - dob.Value;
            if (span.TotalDays < 0) return string.Empty;
            var years = span.Days / 365;
            if (years > 0) return years + "y";
            var months = span.Days / 30;
            if (months > 0) return months + "m";
            return Math.Max(span.Days, 0) + "d";
        }

        private async Task<string> GetMaxSlipAsync(string prefix, CancellationToken ct)
        {
            var conn = _db.Database.GetDbConnection();
            var wasClosed = conn.State != ConnectionState.Open;
            if (wasClosed) await conn.OpenAsync(ct);
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
SELECT ISNULL(MAX(x.SlipNo), '') FROM (
    SELECT REPLACE(SLIPNO, '-', '') AS SlipNo
      FROM OBIL_MST
     WHERE DEPT = @dept AND REPLACE(SLIPNO, '-', '') LIKE @prefix
    UNION ALL
    SELECT REPLACE(SLIPNO, '-', '')
      FROM IBIL_MST
     WHERE DEPT = @dept AND REPLACE(SLIPNO, '-', '') LIKE @prefix
) x;";
                var pDept = cmd.CreateParameter();
                pDept.ParameterName = "@dept";
                pDept.Value = LabDept;
                cmd.Parameters.Add(pDept);
                var pPrefix = cmd.CreateParameter();
                pPrefix.ParameterName = "@prefix";
                pPrefix.Value = prefix + "%";
                cmd.Parameters.Add(pPrefix);
                var result = await cmd.ExecuteScalarAsync(ct);
                return result?.ToString() ?? string.Empty;
            }
            finally
            {
                if (wasClosed) await conn.CloseAsync();
            }
        }
    }
}
