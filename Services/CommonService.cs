using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Models.Legacy;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HmisWeb.Services
{
    public interface ICommonService
    {
        Task<List<Institution>> GetActiveInstitutionsAsync();
        Task<List<Department>> GetDepartmentsAsync(long? institutionId = null);
        Task<List<Patient>> SearchPatientsAsync(string searchTerm);
        Task<Patient> GetOrCreatePatientAsync(Patient patient, Person person);
        List<WebUser> GetActiveWebUsers();
        Task<double> GetAppliedRateAsync(Item item, long? companyId = null, long? consultantId = null, long? resourceId = null);
        Task<double> GetAppliedRateAsync(UsService service, long? companyId = null);
        Task<double> GetAppliedRateAsync(DeptService service, long? companyId = null);
        Task<long?> GetPatientCompanyIdAsync(long personId);

        /// <summary>
        /// Fills <see cref="Patient.LegacyName"/> for patients whose PERSON row is
        /// missing or empty, by looking their code / registration number up in
        /// Patient (joined to REGISTER).
        /// </summary>
        Task EnrichLegacyNamesAsync(IEnumerable<Patient> patients);

        /// <summary>
        /// Gets patient information from legacy Patient joined with REGISTER by registration number.
        /// Mirrors the VFP patient lookup query.
        /// </summary>
        Task<PatientLegacyInfo> GetPatientLegacyInfoAsync(string regNo);

        /// <summary>
        /// Same lookup but resolves the key from the patient (PHN, legacy code,
        /// then PHN number) so callers do not need to know which column matches.
        /// </summary>
        Task<PatientLegacyInfo> GetPatientLegacyInfoAsync(Patient patient);

        /// <summary>
        /// Registration-based search used by the indent screens. Returns REGISTER
        /// rows (newest first) joined to PATIENT through REGISTER.pid, matching the
        /// patient name, registration number, patient code or any phone number.
        /// </summary>
        Task<List<LegacyPatientRow>> SearchLegacyPatientsAsync(string term);

        /// <summary>
        /// Creates a patient in PATIENT/PERSON and issues the permanent
        /// PATIENT.CODE (PCODE / MRNo). The legacy patient columns used by
        /// opdreg.scx / ipdreg.scx are filled from the PERSON row.
        /// </summary>
        Task<Patient> CreatePatientWithLegacyCodeAsync(Person person, Patient patient, DateTime registrationDate, long? userId);

        /// <summary>
        /// Issues the next permanent patient code (PCODE / MRNo) using the legacy
        /// formula: (two-digit year - 18) + year + '01' + 5-digit sequence.
        /// </summary>
        Task<string> GeneratePatientCodeAsync(DateTime date);

        /// <summary>
        /// Keeps an existing PATIENT row in step with its PERSON row and issues a
        /// PCODE if the patient does not have one yet. Used before a visit is
        /// registered so REGISTER.PCODE is always populated.
        /// </summary>
        Task SyncPatientLegacyAsync(Patient patient, long? userId, bool overwrite = false);

        /// <summary>
        /// Inserts one REGISTER row and returns the new REGNO. REGNO is the
        /// visit identity; PCODE identifies the patient for life.
        /// </summary>
        Task<long> CreateRegisterVisitAsync(RegisterVisitRequest request);
    }

    public class CommonService : ICommonService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<CommonService> _logger;

        public CommonService(ApplicationDbContext db, ILogger<CommonService> logger = null)
        {
            _db = db;
            _logger = logger;
        }

        public Task<List<Institution>> GetActiveInstitutionsAsync()
        {
            return _db.Institutions.Where(i => !i.Retired).OrderBy(i => i.Name).ToListAsync();
        }

        public Task<List<Department>> GetDepartmentsAsync(long? institutionId = null)
        {
            var q = _db.Departments.Where(d => !d.Retired);
            if (institutionId != null)
                q = q.Where(d => d.InstitutionId == institutionId);
            return q.OrderBy(d => d.Name).ToListAsync();
        }

        public Task<List<Patient>> SearchPatientsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Task.FromResult(new List<Patient>());
            return SearchPatientsCoreAsync(searchTerm);
        }

        private async Task<List<Patient>> SearchPatientsCoreAsync(string searchTerm)
        {
            searchTerm = searchTerm.Trim();
            long phn;
            bool isNumeric = long.TryParse(searchTerm, out phn);

            var q = _db.Patients
                .Include(p => p.Person)
                .Where(p => !p.Retired);

            if (isNumeric)
                q = q.Where(p => p.PatientId == phn || p.Phn == searchTerm
                    || (p.Person != null && (p.Person.Mobile == searchTerm
                        || p.Person.Phone == searchTerm
                        || p.Person.Nic == searchTerm)));
            else
                q = q.Where(p => p.Person != null &&
                    (p.Person.NameWithInitials.Contains(searchTerm)
                    || p.Person.FullName.Contains(searchTerm)
                    || p.Person.Initials.Contains(searchTerm)
                    || p.Person.Name.Contains(searchTerm)
                    || p.Person.Nic.Contains(searchTerm)
                    || p.Person.Surname.Contains(searchTerm)
                    || p.Person.LastName.Contains(searchTerm)
                    || p.Person.Mobile.Contains(searchTerm)));

            var patients = await q.OrderByDescending(p => p.CreatedAt).Take(50).ToListAsync();

            // Legacy patients often have no PERSON row at all, so a name / phone
            // search finds nothing and a PHN search returns "Unnamed". Widen the
            // match to Patient only when the fast app search found nothing
            // (this query scans the legacy tables, so it must stay a fallback).
            if (patients.Count == 0)
            {
                var legacyMatches = await FindLegacyPatientsAsync(searchTerm, isNumeric ? phn : (long?)null);
                if (legacyMatches.Count > 0)
                {
                    var known = patients.Select(p => p.Id).ToHashSet();
                    patients.AddRange(legacyMatches.Where(p => known.Add(p.Id)));
                }
            }

            await EnrichLegacyNamesAsync(patients);
            return patients;
        }

        /// <summary>
        /// Finds app Patients whose legacy Patient / REGISTER row matches
        /// the search text (code, regno, name, NIC or phone). Used as a fallback
        /// for imported patients with no PERSON row.
        /// </summary>
        private async Task<List<Patient>> FindLegacyPatientsAsync(string term, long? numeric)
        {
            if (string.IsNullOrWhiteSpace(term)) return new List<Patient>();

            // Two separate statements keep every predicate index-friendly:
            //   * numeric input (PHN / regno / phone) never touches Patient
            //     names and never scans by name;
            //   * text input uses a PREFIX match on NAME (a leading '%' would force
            //     a full table scan and is what caused the SQL timeouts).
            // Both are bounded by TOP so the IN lists below stay small.
            var sql = numeric.HasValue
                ? @"
SELECT DISTINCT TOP (50) r.REGNO, r.PCODE
FROM REGISTER r
WHERE r.REGNO = @term
   OR r.PCODE = @term"
                : @"
SELECT DISTINCT TOP (100) r.REGNO, r.PCODE
FROM REGISTER r
INNER JOIN Patient pl ON pl.CODE = r.PCODE
WHERE pl.NAME LIKE @prefix
   OR pl.NIC = @term
   OR pl.CODE = @term
   OR r.REGNO = @term
   OR r.PCODE = @term";

            var keys = new List<(string RegNo, string PCode)>();
            var conn = _db.Database.GetDbConnection();
            var openedHere = false;
            if (conn.State != System.Data.ConnectionState.Open)
            {
                await conn.OpenAsync();
                openedHere = true;
            }

            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.CommandTimeout = 10;
                cmd.Parameters.Add(new SqlParameter("@term", term));
                cmd.Parameters.Add(new SqlParameter("@prefix", term + "%"));

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    keys.Add((
                        reader.IsDBNull(0) ? null : Convert.ToString(reader.GetValue(0))?.Trim(),
                        reader.IsDBNull(1) ? null : Convert.ToString(reader.GetValue(1))?.Trim()));
                }
            }
            catch (SqlException ex) when (ex.Number == -2)
            {
                // Timeout on the legacy fallback must never break patient search.
                _logger?.LogWarning(ex, "Legacy patient lookup timed out for term {Term}", term);
                return new List<Patient>();
            }
            finally
            {
                if (openedHere) await conn.CloseAsync();
            }

            if (keys.Count == 0) return new List<Patient>();

            var regNos = keys.Where(k => !string.IsNullOrWhiteSpace(k.RegNo)).Select(k => k.RegNo).Distinct().Take(100).ToList();
            var codes = keys.Where(k => !string.IsNullOrWhiteSpace(k.PCode)).Select(k => k.PCode).Distinct().Take(100).ToList();

            // Some imports leave Patient.Phn/Code empty and only keep the legacy
            // registration number as PatientId, so match numeric keys there too.
            var numericKeys = regNos
                .Concat(codes)
                .Select(k => long.TryParse(k, out var n) ? n : (long?)null)
                .Where(n => n.HasValue)
                .Select(n => n.Value)
                .Distinct()
                .ToList();

            var q = _db.Patients.Include(p => p.Person).Where(p => !p.Retired);
            q = q.Where(p => (p.Phn != null && regNos.Contains(p.Phn))
                          || (p.Code != null && codes.Contains(p.Code))
                          || (p.PatientId != null && numericKeys.Contains(p.PatientId.Value)));

            if (numeric.HasValue)
                q = q.Where(p => p.PatientId == numeric.Value || (p.Phn != null && regNos.Contains(p.Phn))
                    || (p.Code != null && codes.Contains(p.Code))
                    || (p.PatientId != null && numericKeys.Contains(p.PatientId.Value)));

            return await q.OrderByDescending(p => p.CreatedAt).Take(50).ToListAsync();
        }

        /// <summary>
        /// Registration-based patient search for the indent screens.
        /// Returns REGISTER rows (newest first) joined to their PATIENT master,
        /// matching name, registration number, patient code or any of the three
        /// legacy phone numbers. REGISTER.pid carries the PATIENT.id link.
        /// </summary>
        public async Task<List<LegacyPatientRow>> SearchLegacyPatientsAsync(string term)
        {
            var results = new List<LegacyPatientRow>();
            if (string.IsNullOrWhiteSpace(term)) return results;
            var trimmed = term.Trim();

            // Legacy registration search: REGISTER is the visit table and its
            // PCODE is the permanent patient code, so the exact legacy join is
            // REGISTER.PCODE = PATIENT.CODE. RDATE/RTIME are always taken from
            // REGISTER (never from PATIENT.CreatedAt) so a patient who has no
            // visit row cannot show a fabricated registration date.
            const string sql = @"
SELECT TOP (50)
     r.[TYPE]                                AS [Type]
    ,r.[REGNO]                               AS [RegNo]
    ,r.[REGNO]                               AS [VisitRegNo]
    ,r.[RDATE]                               AS [RDate]
    ,r.[RTIME]                               AS [RTime]
    ,r.[PCODE]                               AS [PCode]
    ,COALESCE(NULLIF(p.[CODE], ''), CONVERT(varchar(20), NULLIF(p.[PatientId], 0))) AS [PatientCode]
    ,COALESCE(NULLIF(p.[Phn], ''), CONVERT(varchar(20), NULLIF(p.[PatientId], 0)), NULLIF(p.[CODE], '')) AS [Phn]
    ,p.[NAME]                                AS [Name]
    ,p.[SEX]                                 AS [Sex]
    ,p.[AGE]                                 AS [Age]
    ,p.[ATYPE]                               AS [AType]
    ,COALESCE(NULLIF(p.[PHONE1], ''), NULLIF(p.[PHONE2], ''), NULLIF(p.[PHONE3], '')) AS [Phone]
    ,p.[ADDRESS]                             AS [Address]
    ,p.[NIC]                                 AS [Nic]
    ,p.[id]                                  AS [PatientAppId]
    ,r.[CID]                                 AS [Cid]
    ,r.[CONCODE]                             AS [Concode]
    ,r.[TBILLAMT]                            AS [BillAmount]
    ,r.[TRCVD]                               AS [Received]
    ,r.[TPYMT]                               AS [Paid]
    ,r.[CASHDISC]                            AS [Discount]
    ,r.[SETTLE]                              AS [Settled]
    ,r.[BILLGEN]                             AS [BillGenerated]
    ,r.[BILLSTATUS]                          AS [BillStatus]
FROM [HMIS].[dbo].[REGISTER] r
INNER JOIN [HMIS].[dbo].[Patient] p ON p.[CODE] = r.[PCODE]
WHERE (p.[NAME]     LIKE @like
   OR r.[REGNO]    LIKE @like
   OR p.[CODE]     LIKE @like
   OR p.[Phn]      LIKE @like
   OR r.[PCODE]    LIKE @like
   OR p.[PHONE1]   LIKE @like
   OR p.[PHONE2]   LIKE @like
   OR p.[PHONE3]   LIKE @like)
ORDER BY r.[RDATE] DESC, r.[REGNO] DESC";

            var conn = _db.Database.GetDbConnection();
            var openedHere = false;
            if (conn.State != System.Data.ConnectionState.Open)
            {
                await conn.OpenAsync();
                openedHere = true;
            }

            static string S(System.Data.Common.DbDataReader r, int i) =>
                r.IsDBNull(i) ? null : Convert.ToString(r.GetValue(i))?.Trim();

            static decimal? M(System.Data.Common.DbDataReader r, int i) =>
                r.IsDBNull(i) ? (decimal?)null : Convert.ToDecimal(r.GetValue(i));

            static bool? B(System.Data.Common.DbDataReader r, int i) =>
                r.IsDBNull(i) ? (bool?)null : Convert.ToBoolean(r.GetValue(i));

            static byte? By(System.Data.Common.DbDataReader r, int i) =>
                r.IsDBNull(i) ? (byte?)null : Convert.ToByte(r.GetValue(i));

            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.CommandTimeout = 10;
                cmd.Parameters.Add(new SqlParameter("@like", "%" + trimmed + "%"));

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new LegacyPatientRow
                    {
                        Type = S(reader, 0),
                        RegNo = S(reader, 1),
                        VisitRegNo = S(reader, 2),
                        RDate = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                        RTime = S(reader, 4),
                        PCode = S(reader, 5),
                        PatientCode = S(reader, 6),
                        Phn = S(reader, 7),
                        Name = S(reader, 8),
                        Sex = S(reader, 9),
                        Age = S(reader, 10),
                        Phone = S(reader, 12),
                        Address = S(reader, 13),
                        Nic = S(reader, 14),
                        PatientAppId = reader.IsDBNull(15) ? 0 : Convert.ToInt64(reader.GetValue(15)),
                        Cid = S(reader, 16),
                        Concode = S(reader, 17),
                        BillAmount = M(reader, 18),
                        Received = M(reader, 19),
                        Paid = M(reader, 20),
                        Discount = M(reader, 21),
                        Settled = B(reader, 22),
                        BillGenerated = B(reader, 23),
                        BillStatus = By(reader, 24)
                    });
                }
            }
            catch (SqlException ex) when (ex.Number == -2)
            {
                _logger?.LogWarning(ex, "Legacy patient search timed out for {Term}", term);
            }
            catch (Exception ex)
            {
                // A bad legacy row/column must not stop the app-side fallback below.
                _logger?.LogWarning(ex, "Legacy patient search failed for {Term}", term);
            }
            finally
            {
                if (openedHere) await conn.CloseAsync();
            }

            // Fallback / merge: search the PATIENT table directly as well. A patient
            // registered by this application may not have a REGISTER row yet, so
            // searching by their registration number (PATIENT.PatientId or PHN)
            // would otherwise return nothing. Always merge this result set so a
            // recent registration is visible even when the legacy query matched
            // other patients first.
            var seenPatientIds = new HashSet<long>(
                results.Where(r => r.PatientAppId > 0).Select(r => r.PatientAppId));

            try
            {
                if (conn.State != System.Data.ConnectionState.Open)
                {
                    await conn.OpenAsync();
                    openedHere = true;
                }
                else
                {
                    openedHere = false;
                }

                // PATIENT-only fallback: keeps patients who have no REGISTER row
                // selectable, but RDATE/RTIME stay NULL instead of borrowing
                // PATIENT.CreatedAt. The UI shows "-" for those rows.
                const string sql2 = @"
SELECT TOP (50)
     NULL                                AS [Type]
    ,COALESCE(NULLIF(p.[Phn], ''), CONVERT(varchar(20), NULLIF(p.[PatientId], 0)), NULLIF(p.[CODE], '')) AS [RegNo]
    ,lr.[REGNO]                          AS [VisitRegNo]
    ,lr.[RDATE]                          AS [RDate]
    ,lr.[RTIME]                          AS [RTime]
    ,p.[CODE]                            AS [PCode]
    ,COALESCE(NULLIF(p.[CODE], ''), CONVERT(varchar(20), NULLIF(p.[PatientId], 0))) AS [PatientCode]
    ,COALESCE(NULLIF(p.[Phn], ''), CONVERT(varchar(20), NULLIF(p.[PatientId], 0)), NULLIF(p.[CODE], '')) AS [Phn]
    ,p.[NAME]                            AS [Name]
    ,p.[SEX]                             AS [Sex]
    ,p.[AGE]                             AS [Age]
    ,p.[ATYPE]                           AS [AType]
    ,COALESCE(NULLIF(p.[PHONE1],''), NULLIF(p.[PHONE2],''), NULLIF(p.[PHONE3],'')) AS [Phone]
    ,p.[ADDRESS]                         AS [Address]
    ,p.[NIC]                             AS [Nic]
    ,p.[id]                              AS [PatientAppId]
    ,NULL                                AS [Cid]
    ,NULL                                AS [Concode]
    ,lr.[TBILLAMT]                       AS [BillAmount]
    ,lr.[TRCVD]                          AS [Received]
    ,lr.[TPYMT]                          AS [Paid]
    ,lr.[CASHDISC]                       AS [Discount]
    ,lr.[SETTLE]                         AS [Settled]
    ,lr.[BILLGEN]                        AS [BillGenerated]
    ,lr.[BILLSTATUS]                     AS [BillStatus]
FROM [HMIS].[dbo].[Patient] p
OUTER APPLY (
    SELECT TOP (1)
         r.[REGNO], r.[RDATE], r.[RTIME]
        ,r.[TBILLAMT], r.[TRCVD], r.[TPYMT], r.[CASHDISC]
        ,r.[SETTLE], r.[BILLGEN], r.[BILLSTATUS]
    FROM [HMIS].[dbo].[REGISTER] r
    WHERE r.[PCODE] = p.[CODE]
    ORDER BY r.[RDATE] DESC, r.[REGNO] DESC
) lr
WHERE ISNULL(p.[Retired], 0) = 0
  AND (p.[NAME]      LIKE @like
   OR p.[CODE]       LIKE @like
   OR p.[Phn]        LIKE @like
   OR p.[PHONE1]     LIKE @like
   OR p.[PHONE2]     LIKE @like
   OR p.[PHONE3]     LIKE @like
   OR p.[NIC]        LIKE @like
   OR p.[PatientId]  = @num
   OR CONVERT(varchar(20), p.[PatientId]) LIKE @like)
ORDER BY CASE WHEN lr.[REGNO] IS NULL THEN 1 ELSE 0 END,
         lr.[RDATE] DESC, p.[id] DESC";

                long.TryParse(trimmed, out var numericTerm);

                await using var cmd2 = conn.CreateCommand();
                cmd2.CommandText = sql2;
                cmd2.CommandTimeout = 10;
                cmd2.Parameters.Add(new SqlParameter("@like", "%" + trimmed + "%"));
                cmd2.Parameters.Add(new SqlParameter("@num", System.Data.SqlDbType.BigInt)
                {
                    Value = numericTerm != 0 ? numericTerm : (object)DBNull.Value
                });

                await using var reader2 = await cmd2.ExecuteReaderAsync();
                while (await reader2.ReadAsync())
                {
                    var row = new LegacyPatientRow
                    {
                        Type = S(reader2, 0),
                        RegNo = S(reader2, 1),
                        VisitRegNo = S(reader2, 2),
                        RDate = reader2.IsDBNull(3) ? (DateTime?)null : reader2.GetDateTime(3),
                        RTime = S(reader2, 4),
                        PCode = S(reader2, 5),
                        PatientCode = S(reader2, 6),
                        Phn = S(reader2, 7),
                        Name = S(reader2, 8),
                        Sex = S(reader2, 9),
                        Age = S(reader2, 10),
                        Phone = S(reader2, 12),
                        Address = S(reader2, 13),
                        Nic = S(reader2, 14),
                        PatientAppId = reader2.IsDBNull(15) ? 0 : Convert.ToInt64(reader2.GetValue(15)),
                        Cid = S(reader2, 16),
                        Concode = S(reader2, 17),
                        BillAmount = M(reader2, 18),
                        Received = M(reader2, 19),
                        Paid = M(reader2, 20),
                        Discount = M(reader2, 21),
                        Settled = B(reader2, 22),
                        BillGenerated = B(reader2, 23),
                        BillStatus = By(reader2, 24)
                    };

                    if (row.PatientAppId > 0 && !seenPatientIds.Add(row.PatientAppId))
                        continue;

                    results.Add(row);
                }
            }
            catch (SqlException ex) when (ex.Number == -2)
            {
                _logger?.LogWarning(ex, "Legacy patient fallback search timed out for {Term}", term);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Legacy patient fallback search failed for {Term}", term);
            }
            finally
            {
                if (openedHere) await conn.CloseAsync();
            }

            // Put an exact patient-code / registration-number match first, then
            // the newest registrations, while preserving the legacy ordering for
            // rows that share the same date.
            return results
                .OrderByDescending(r =>
                    string.Equals(r.PatientCode?.Trim(), trimmed, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(r.VisitRegNo?.Trim(), trimmed, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(r.RegNo?.Trim(), trimmed, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(r => r.RDate ?? DateTime.MinValue)
                .ThenByDescending(r => r.PatientAppId)
                .Take(50)
                .ToList();
        }

        public async Task EnrichLegacyNamesAsync(IEnumerable<Patient> patients)
        {
            if (patients == null) return;
            var list = patients.Where(p => p != null).ToList();
            if (list.Count == 0) return;

            // Only patients with no usable app-side name need the legacy lookup.
            var missing = list
                .Where(p => string.IsNullOrWhiteSpace(p.Person?.NameWithInitials)
                         && string.IsNullOrWhiteSpace(p.Person?.Name)
                         && string.IsNullOrWhiteSpace(p.Person?.FullName))
                .ToList();
            if (missing.Count == 0) return;

            var codes = missing.Where(p => !string.IsNullOrWhiteSpace(p.Code))
                .Select(p => p.Code.Trim()).Distinct().Take(100).ToList();
            var regNos = missing.Where(p => !string.IsNullOrWhiteSpace(p.Phn))
                .Select(p => p.Phn.Trim()).Distinct().ToList();
            // PHN (PatientId) is also used as the registration number by some imports.
            regNos.AddRange(missing.Where(p => p.PatientId.HasValue)
                .Select(p => p.PatientId.Value.ToString()).Distinct());
            regNos = regNos.Distinct().Take(100).ToList();

            if (codes.Count == 0 && regNos.Count == 0) return;

            var byCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var byRegNo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var conn = _db.Database.GetDbConnection();
            var openedHere = false;
            if (conn.State != System.Data.ConnectionState.Open)
            {
                await conn.OpenAsync();
                openedHere = true;
            }

            try
            {
                // Two small, separately indexable statements instead of one
                // LEFT JOIN + OR query. Casting the key columns (the previous
                // version) prevented index seeks and caused statement timeouts.
                if (codes.Count > 0)
                {
                    var codeList = string.Join(",", codes.Select((c, i) => "@c" + i));
                    await using var codeCmd = conn.CreateCommand();
                    codeCmd.CommandText = $@"
SELECT pl.CODE, pl.NAME
FROM Patient pl
WHERE pl.CODE IN ({codeList})";
                    codeCmd.CommandTimeout = 10;
                    for (int i = 0; i < codes.Count; i++)
                        codeCmd.Parameters.Add(new SqlParameter("@c" + i, codes[i]));

                    await using var codeReader = await codeCmd.ExecuteReaderAsync();
                    while (await codeReader.ReadAsync())
                    {
                        var code = codeReader.IsDBNull(0) ? null : Convert.ToString(codeReader.GetValue(0))?.Trim();
                        var name = codeReader.IsDBNull(1) ? null : Convert.ToString(codeReader.GetValue(1))?.Trim();
                        if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(name))
                            byCode[code] = name;
                    }
                }

                if (regNos.Count > 0)
                {
                    var regList = string.Join(",", regNos.Select((r, i) => "@r" + i));
                    await using var regCmd = conn.CreateCommand();
                    regCmd.CommandText = $@"
SELECT pl.CODE, pl.NAME, r.REGNO
FROM REGISTER r
INNER JOIN Patient pl ON pl.CODE = r.PCODE
WHERE r.REGNO IN ({regList})";
                    regCmd.CommandTimeout = 10;
                    for (int i = 0; i < regNos.Count; i++)
                        regCmd.Parameters.Add(new SqlParameter("@r" + i, regNos[i]));

                    await using var regReader = await regCmd.ExecuteReaderAsync();
                    while (await regReader.ReadAsync())
                    {
                        var code = regReader.IsDBNull(0) ? null : Convert.ToString(regReader.GetValue(0))?.Trim();
                        var name = regReader.IsDBNull(1) ? null : Convert.ToString(regReader.GetValue(1))?.Trim();
                        var regNo = regReader.IsDBNull(2) ? null : Convert.ToString(regReader.GetValue(2))?.Trim();

                        if (string.IsNullOrWhiteSpace(name)) continue;
                        if (!string.IsNullOrWhiteSpace(code)) byCode[code] = name;
                        if (!string.IsNullOrWhiteSpace(regNo)) byRegNo[regNo] = name;
                    }
                }
            }
            catch (SqlException ex) when (ex.Number == -2)
            {
                // A slow legacy lookup must not break the screen; the caller
                // falls back to "Unnamed" for these patients.
                _logger?.LogWarning(ex, "Legacy name enrichment timed out");
            }
            finally
            {
                if (openedHere) await conn.CloseAsync();
            }

            foreach (var p in missing)
            {
                string name = null;
                var candidates = new List<string>();
                if (!string.IsNullOrWhiteSpace(p.Code)) candidates.Add(p.Code.Trim());
                if (!string.IsNullOrWhiteSpace(p.Phn)) candidates.Add(p.Phn.Trim());
                if (p.PatientId.HasValue) candidates.Add(p.PatientId.Value.ToString());

                foreach (var key in candidates)
                {
                    if (byCode.TryGetValue(key, out var cName)) { name = cName; break; }
                    if (byRegNo.TryGetValue(key, out var rName)) { name = rName; break; }
                }

                if (!string.IsNullOrWhiteSpace(name))
                    p.LegacyName = name;
            }
        }


        public async Task<Patient> CreatePatientWithLegacyCodeAsync(Person person, Patient patient, DateTime registrationDate, long? userId)
        {
            if (patient == null) throw new ArgumentNullException(nameof(patient));
            person ??= new Person();

            await using var tran = await _db.Database.BeginTransactionAsync();
            try
            {
                // PCODE is the permanent patient code (MRNo). It is issued once
                // and never changes, even though a new REGNO is issued per visit.
                if (string.IsNullOrWhiteSpace(patient.Code))
                    patient.Code = await GeneratePatientCodeAsync(registrationDate);

                ApplyLegacyPatientFields(person, patient, registrationDate, userId, overwrite: true);

                if (patient.PatientId == null || patient.PatientId <= 0)
                {
                    var maxId = await _db.Patients.MaxAsync(p => (long?)p.PatientId);
                    patient.PatientId = (maxId ?? 0) + 1;
                }

                person.CreatedAt ??= DateTime.Now;
                person.CreaterId ??= userId;
                patient.CreatedAt ??= DateTime.Now;
                patient.CreaterId ??= userId;

                if (person.Id == 0)
                {
                    _db.Persons.Add(person);
                    patient.Person = person;
                }
                else
                {
                    patient.PersonId = person.Id;
                }

                _db.Patients.Add(patient);
                await _db.SaveChangesAsync();
                await tran.CommitAsync();
                return patient;
            }
            catch
            {
                await tran.RollbackAsync();
                throw;
            }
        }

        public async Task SyncPatientLegacyAsync(Patient patient, long? userId, bool overwrite = false)
        {
            if (patient == null) return;

            await using var tran = await _db.Database.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrWhiteSpace(patient.Code))
                    patient.Code = await GeneratePatientCodeAsync(patient.CreatedAt ?? DateTime.Today);

                var person = patient.Person;
                if (person == null && patient.PersonId.HasValue)
                {
                    person = await _db.Persons.FirstOrDefaultAsync(p => p.Id == patient.PersonId.Value);
                    patient.Person = person;
                }

                ApplyLegacyPatientFields(person, patient, patient.CreatedAt ?? DateTime.Today, userId, overwrite);
                await _db.SaveChangesAsync();
                await tran.CommitAsync();
            }
            catch
            {
                await tran.RollbackAsync();
                throw;
            }
        }

        public async Task<long> CreateRegisterVisitAsync(RegisterVisitRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.PCode))
                throw new ArgumentException("PCODE is required for a REGISTER visit.", nameof(request));
            if (request.PatientAppId <= 0)
                throw new ArgumentException("PATIENT.id is required for a REGISTER visit.", nameof(request));

            var now = DateTime.Now;
            var visitDate = request.VisitDate == default ? now.Date : request.VisitDate.Date;
            var visitTime = string.IsNullOrWhiteSpace(request.VisitTime)
                ? now.ToString("HH:mm")
                : request.VisitTime.Trim();
            var type = string.IsNullOrWhiteSpace(request.Type) ? "O" : request.Type.Trim().ToUpperInvariant();
            var cUserId = TrimValue(request.CUserId, 15);
            var userId = TrimValue(request.UserId, 15);

            // REGISTER.CID / CONCODE are legacy varchar codes, not app ids.
            string cid = null;
            if (request.CompanyId.HasValue)
            {
                cid = await _db.Institutions.AsNoTracking()
                    .Where(i => i.Id == request.CompanyId.Value)
                    .Select(i => i.Code)
                    .FirstOrDefaultAsync();
                cid = TrimValue(cid, 8) ?? TrimValue(request.CompanyId.Value.ToString(), 8);
            }

            string concode = null;
            if (request.ConsultantId.HasValue)
            {
                concode = await _db.Doctors.AsNoTracking()
                    .Where(d => d.Id == request.ConsultantId.Value)
                    .Select(d => d.Code)
                    .FirstOrDefaultAsync();
                concode = TrimValue(concode, 8) ?? TrimValue(request.ConsultantId.Value.ToString(), 8);
            }

            var conn = _db.Database.GetDbConnection();
            var openedHere = false;
            if (conn.State != System.Data.ConnectionState.Open)
            {
                await conn.OpenAsync();
                openedHere = true;
            }

            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = _db.Database.CurrentTransaction?.GetDbTransaction();
                // UPDLOCK/HOLDLOCK keeps two simultaneous registrations from
                // receiving the same REGNO. The transaction is committed before
                // the caller starts the modern BILL/TOKEN rows.
                cmd.CommandText = @"
BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @regno bigint;
    SELECT @regno = ISNULL(MAX(REGNO), 0) + 1
    FROM REGISTER WITH (UPDLOCK, HOLDLOCK);

    INSERT INTO REGISTER
        (TYPE, REGNO, RDATE, RTIME, PCODE, GNAME, GRELATION, DIAGNOSE, BEDNO, BEDENTITLE,
         DDATE, DTIME, CID, ECODE, ERELATION, CONCODE, REFNO, REFVALID, CARDNO, CSTAT,
         BILLGEN, TRCVD, TPYMT, TBILLAMT, CASHDISC, SETTLE, USERID, UDATE, UTIME,
         CUSERID, CDATE, CTIME, BILLSTATUS, BILLSTATUSDT, BILLSTATUSBY, pid)
    VALUES
        (@type, @regno, @rdate, @rtime, @pcode, @gname, @grelation, @diagnose, @bedno, @bedentitle,
         NULL, NULL, @cid, @ecode, @erelation, @concode, @refno, @refvalid, @cardno, @cstat,
         0, 0, 0, 0, 0, 0, @userid, @udate, @utime,
         @cuserid, @cdate, @ctime, 0, @now, @cuserid, @pid);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT @regno;";
                cmd.CommandTimeout = 15;

                void AddParam(string name, System.Data.SqlDbType dbType, object value, int? size = null)
                {
                    var parameter = size.HasValue
                        ? new SqlParameter(name, dbType, size.Value)
                        : new SqlParameter(name, dbType);
                    parameter.Value = value ?? DBNull.Value;
                    cmd.Parameters.Add(parameter);
                }

                AddParam("@type", System.Data.SqlDbType.VarChar, type, 1);
                AddParam("@rdate", System.Data.SqlDbType.Date, visitDate);
                AddParam("@rtime", System.Data.SqlDbType.VarChar, visitTime, 8);
                AddParam("@pcode", System.Data.SqlDbType.VarChar, TrimValue(request.PCode, 15), 15);
                AddParam("@gname", System.Data.SqlDbType.VarChar, TrimValue(request.GuardianName, 100), 100);
                AddParam("@grelation", System.Data.SqlDbType.VarChar, TrimValue(request.GuardianRelation, 20), 20);
                AddParam("@diagnose", System.Data.SqlDbType.VarChar, TrimValue(request.Diagnosis, 250), 250);
                AddParam("@bedno", System.Data.SqlDbType.VarChar, TrimValue(request.BedNo, 10), 10);
                AddParam("@bedentitle", System.Data.SqlDbType.VarChar, TrimValue(request.BedEntitlement, 100), 100);
                AddParam("@cid", System.Data.SqlDbType.VarChar, cid, 8);
                AddParam("@ecode", System.Data.SqlDbType.VarChar, TrimValue(request.EmployeeCode, 20), 20);
                AddParam("@erelation", System.Data.SqlDbType.VarChar, TrimValue(request.EmployeeRelation, 20), 20);
                AddParam("@concode", System.Data.SqlDbType.VarChar, concode, 8);
                AddParam("@refno", System.Data.SqlDbType.VarChar, TrimValue(request.ReferenceNo, 100), 100);
                AddParam("@refvalid", System.Data.SqlDbType.VarChar, request.RefValid?.ToString("yyyy-MM-dd"), 20);
                AddParam("@cardno", System.Data.SqlDbType.VarChar, TrimValue(request.CardNo, 20), 20);
                AddParam("@cstat", System.Data.SqlDbType.VarChar, TrimValue(request.CStat, 100), 100);
                AddParam("@userid", System.Data.SqlDbType.VarChar, userId, 15);
                AddParam("@udate", System.Data.SqlDbType.VarChar, now.ToString("yyyy-MM-dd"), 15);
                AddParam("@utime", System.Data.SqlDbType.VarChar, now.ToString("HH:mm"), 5);
                AddParam("@cuserid", System.Data.SqlDbType.VarChar, cUserId, 15);
                AddParam("@cdate", System.Data.SqlDbType.VarChar, now.ToString("yyyy-MM-dd"), 100);
                AddParam("@ctime", System.Data.SqlDbType.VarChar, now.ToString("HH:mm"), 5);
                AddParam("@now", System.Data.SqlDbType.DateTime2, now);
                AddParam("@pid", System.Data.SqlDbType.BigInt, request.PatientAppId);

                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt64(result);
            }
            finally
            {
                if (openedHere) await conn.CloseAsync();
            }
        }

        public async Task<string> GeneratePatientCodeAsync(DateTime date)
        {
            // Mirrors the VFP pcode.dbf key: (two-digit year - 18) + year + '01'
            // and a five-digit sequence, e.g. 2026 -> 22601 + 00001.
            var year = date.Year;
            var prefix = (year / 100 - 18).ToString() + (year % 100).ToString("00") + "01";

            var conn = _db.Database.GetDbConnection();
            var openedHere = false;
            if (conn.State != System.Data.ConnectionState.Open)
            {
                await conn.OpenAsync();
                openedHere = true;
            }

            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = _db.Database.CurrentTransaction?.GetDbTransaction();
                cmd.CommandText = @"
DECLARE @next int;
SELECT @next = ISNULL(MAX(TRY_CONVERT(int, RIGHT(CODE, 5))), 0) + 1
FROM PATIENT WITH (UPDLOCK, HOLDLOCK)
WHERE CODE LIKE @prefix + '%'
  AND LEN(CODE) = 10;

SELECT @prefix + RIGHT('00000' + CONVERT(varchar(5), @next), 5);";
                cmd.CommandTimeout = 10;
                cmd.Parameters.Add(new SqlParameter("@prefix", System.Data.SqlDbType.VarChar, 5) { Value = prefix });

                return Convert.ToString(await cmd.ExecuteScalarAsync())?.Trim();
            }
            finally
            {
                if (openedHere) await conn.CloseAsync();
            }
        }

        private static void ApplyLegacyPatientFields(Person person, Patient patient, DateTime date, long? userId, bool overwrite)
        {
            var now = DateTime.Now;

            if (overwrite || string.IsNullOrWhiteSpace(patient.LegacyName))
                patient.LegacyName = TrimValue(person?.Name, 50);
            if (overwrite || string.IsNullOrWhiteSpace(patient.LegacySex))
                patient.LegacySex = TrimValue(person?.Sex?.ToString(), 15);
            if (overwrite || string.IsNullOrWhiteSpace(patient.Nic))
                patient.Nic = TrimValue(person?.Nic, 15);
            if (overwrite || string.IsNullOrWhiteSpace(patient.Address))
                patient.Address = TrimValue(person?.Address, 100);
            if (overwrite || string.IsNullOrWhiteSpace(patient.Phone1))
                patient.Phone1 = TrimValue(person?.Phone, 25);
            if (overwrite || string.IsNullOrWhiteSpace(patient.Phone2))
                patient.Phone2 = TrimValue(person?.Mobile, 25);
            if (overwrite || string.IsNullOrWhiteSpace(patient.RDate))
                patient.RDate = date.ToString("yyyyMMdd");
            if (overwrite || string.IsNullOrWhiteSpace(patient.RegDate))
                patient.RegDate = date.ToString("yyyyMMdd");
            if (string.IsNullOrWhiteSpace(patient.AType))
                patient.AType = "Y";
            if (string.IsNullOrWhiteSpace(patient.LegacyAge) && person?.Dob != null)
            {
                var age = DateTime.Today.Year - person.Dob.Value.Year;
                if (person.Dob.Value.Date > DateTime.Today.AddYears(-age)) age--;
                if (age < 0) age = 0;
                patient.LegacyAge = age > 999 ? "999" : age.ToString();
            }
            if (overwrite || string.IsNullOrWhiteSpace(patient.UserId))
                patient.UserId = TrimValue(userId?.ToString(), 15);

            patient.UDate = now;
            patient.UTime = now.ToString("HH:mm");
        }

        private static string TrimValue(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed.Substring(0, maxLength);
        }

        public async Task<Patient> GetOrCreatePatientAsync(Patient patient, Person person)
        {
            if (person.Id == 0)
            {
                _db.Persons.Add(person);
                await _db.SaveChangesAsync();
            }

            if (patient.Id == 0)
            {
                patient.PersonId = person.Id;
                _db.Patients.Add(patient);
                await _db.SaveChangesAsync();
            }
            return patient;
        }

        public List<WebUser> GetActiveWebUsers()
        {
            return _db.WebUsers.Where(u => !u.Retired).OrderBy(u => u.UserName).ToList();
        }

        public async Task<long?> GetPatientCompanyIdAsync(long personId)
        {
            if (personId <= 0) return null;
            return await _db.CompanyEmployees
                .Where(e => e.PersonId == personId && !e.Retired)
                .Select(e => (long?)e.CompanyId)
                .FirstOrDefaultAsync();
        }

        public async Task<double> GetAppliedRateAsync(Item item, long? companyId = null, long? consultantId = null, long? resourceId = null)
        {
            if (item == null) return 0;

            if (resourceId.HasValue)
            {
                var resourceRate = await _db.ResourceServiceRates
                    .Where(r => r.ResourceId == resourceId.Value && r.ItemId == item.Id && !r.Retired && r.StaffRate > 0)
                    .OrderByDescending(r => r.EditedAt)
                    .ThenByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync();
                if (resourceRate != null && resourceRate.StaffRate > 0)
                    return resourceRate.StaffRate;
            }

            if (consultantId.HasValue)
            {
                var consultantRate = await _db.ConsultantServiceRates
                    .Where(c => c.ConsultantId == consultantId.Value && c.ItemId == item.Id && !c.Retired && c.StaffRate > 0)
                    .OrderByDescending(c => c.EditedAt)
                    .ThenByDescending(c => c.CreatedAt)
                    .ThenByDescending(c => c.Id)
                    .FirstOrDefaultAsync();
                if (consultantRate != null && consultantRate.StaffRate > 0)
                    return (double)consultantRate.StaffRate;
            }

            if (companyId.HasValue)
            {
                var companyRate = await _db.CompanyServiceRates
                    .Where(c => c.CompanyId == companyId.Value && c.ItemId == item.Id && !c.Retired && c.Rate > 0)
                    .OrderByDescending(c => c.EditedAt)
                    .ThenByDescending(c => c.CreatedAt)
                    .ThenByDescending(c => c.Id)
                    .FirstOrDefaultAsync();
                if (companyRate != null && companyRate.Rate > 0)
                    return (double)companyRate.Rate;
            }

            return item.Total ?? 0;
        }

        public async Task<double> GetAppliedRateAsync(UsService service, long? companyId = null)
        {
            if (service == null) return 0;

            if (companyId.HasValue)
            {
                var companyRate = await _db.CompanyServiceRates
                    .Where(c => c.CompanyId == companyId.Value && c.ItemId == service.Id && !c.Retired && c.Rate > 0)
                    .OrderByDescending(c => c.EditedAt)
                    .ThenByDescending(c => c.CreatedAt)
                    .ThenByDescending(c => c.Id)
                    .FirstOrDefaultAsync();
                if (companyRate != null && companyRate.Rate > 0)
                    return (double)companyRate.Rate;
            }

            return service.Rate ?? 0;
        }

        /// <summary>
        /// Department-service twin of the ultrasound overload.  Company rates are
        /// stored against the billing [ITEM], so the service is resolved to its
        /// department mirror first (INV-LB-, INV-XR-, INV-US-, INV-DNT-, INV-OT-,
        /// INV-HSP- or INV-CNP- per SERVICES.DEPT); comparing a SERVICES id
        /// straight to COMPANYSERVICERATE.ItemId would read another item's rate.
        ///
        /// Modified by : AI Assistant | 27-Oct-2026 10:05
        /// What changed: the hard-coded "INV-XR-" prefix was replaced by
        /// DeptBillingMirrorService.PrefixFor(service.Dept) so the same overload
        /// serves X-ray, CNP, Dental, OT and Hospital indents.
        /// </summary>
        public async Task<double> GetAppliedRateAsync(DeptService service, long? companyId = null)
        {
            if (service == null) return 0;

            if (companyId.HasValue && !string.IsNullOrWhiteSpace(service.Code))
            {
                // DeptBillingMirrorService owns the canonical DEPT -> prefix map;
                // reuse it instead of duplicating the switch here.
                var prefix = DeptBillingMirrorService.PrefixFor(service.Dept);
                var mirrorCode = prefix + service.Code.Trim();

                var itemId = await _db.Items
                    .Where(i => i.Code == mirrorCode && !i.Retired)
                    .Select(i => (long?)i.Id)
                    .FirstOrDefaultAsync();

                if (itemId.HasValue)
                {
                    var companyRate = await _db.CompanyServiceRates
                        .Where(c => c.CompanyId == companyId.Value && c.ItemId == itemId.Value
                                    && !c.Retired && c.Rate > 0)
                        .OrderByDescending(c => c.EditedAt)
                        .ThenByDescending(c => c.CreatedAt)
                        .ThenByDescending(c => c.Id)
                        .FirstOrDefaultAsync();

                    if (companyRate != null && companyRate.Rate > 0)
                        return (double)companyRate.Rate;
                }
            }

            // SRATE is numeric(18,2) on the legacy master; billing works in doubles.
            return Convert.ToDouble(service.Rate ?? 0m);
        }

        public async Task<PatientLegacyInfo> GetPatientLegacyInfoAsync(string regNo)
        {
            if (string.IsNullOrWhiteSpace(regNo))
                return null;

            var key = regNo.Trim();
            long.TryParse(key, out var numericKey);

            // No CAST on the columns and no OR across joined tables: the patient
            // is resolved first (index seek on CODE / PHN / PatientId) and only
            // then one registration is attached through IX_REGISTER_PCODE.
            // This mirrors the VFP query:
            //   REGISTER r LEFT JOIN Patient p ON p.CODE = r.PCODE
            const string sql = @"
SELECT TOP (1)
     p.[CODE]                            AS [Code]
    ,p.[NAME]                            AS [Name]
    ,p.[SEX]                             AS [Sex]
    ,p.[AGE]                             AS [Age]
    ,p.[ATYPE]                           AS [AType]
    ,p.[ADDRESS]                         AS [Address]
    ,p.[PHONE1]                          AS [Phone1]
    ,p.[PHONE2]                          AS [Phone2]
    ,p.[PHONE3]                          AS [Phone3]
    ,p.[RDATE]                           AS [RDate]
    ,p.[NIC]                             AS [Nic]
    ,p.[REGDATE]                         AS [RegDate]
    ,p.[CASTE]                           AS [Caste]
    ,r.[GNAME]                           AS [GName]
    ,r.[GRELATION]                       AS [GRelation]
    ,r.[DIAGNOSE]                        AS [Diagnose]
    ,r.[BEDNO]                           AS [BedNo]
    ,r.[REGNO]                           AS [RegNo]
    ,r.[RDATE]                           AS [RegisterRDate]
    ,r.[RTIME]                           AS [RTime]
    ,r.[TYPE]                            AS [RegType]
FROM dbo.Patient p
OUTER APPLY (
    SELECT TOP (1) r.[GNAME], r.[GRELATION], r.[DIAGNOSE], r.[BEDNO],
                   r.[REGNO], r.[RDATE], r.[RTIME], r.[TYPE]
    FROM dbo.REGISTER r
    WHERE r.[PCODE] = p.[CODE]
    ORDER BY CASE WHEN r.[pid] = p.[id] THEN 0 ELSE 1 END,
             r.[RDATE] DESC, r.[REGNO] DESC
) r
WHERE p.[CODE] = @key
   OR p.[Phn] = @key
   OR p.[PatientId] = @num";

            var conn = _db.Database.GetDbConnection();
            var openedHere = false;
            if (conn.State != System.Data.ConnectionState.Open)
            {
                await conn.OpenAsync();
                openedHere = true;
            }

            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.CommandTimeout = 10;

                // varchar keeps the CODE index usable (varchar column vs varchar parameter).
                cmd.Parameters.Add(new SqlParameter("@key", System.Data.SqlDbType.VarChar, 15) { Value = key });
                cmd.Parameters.Add(new SqlParameter("@num", System.Data.SqlDbType.BigInt)
                {
                    Value = numericKey > 0 ? numericKey : (object)DBNull.Value
                });

                await using var reader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.SingleRow);
                if (!await reader.ReadAsync()) return null;

                string S(int i) => reader.IsDBNull(i) ? null : Convert.ToString(reader.GetValue(i))?.Trim();
                DateTime? D(int i) => reader.IsDBNull(i) ? (DateTime?)null : reader.GetDateTime(i);

                return new PatientLegacyInfo
                {
                    Code = S(0),
                    Name = S(1),
                    Sex = S(2),
                    Age = S(3),
                    AType = S(4),
                    Address = S(5),
                    Phone1 = S(6),
                    Phone2 = S(7),
                    Phone3 = S(8),
                    RDate = S(9),
                    Nic = S(10),
                    RegDate = S(11),
                    Caste = S(12),
                    GName = S(13),
                    GRelation = S(14),
                    Diagnose = S(15),
                    BedNo = S(16),
                    RegNo = S(17),
                    RegisterRDate = D(18),
                    RTime = S(19),
                    RegType = S(20)
                };
            }
            catch (SqlException ex) when (ex.Number == -2)
            {
                // A slow legacy lookup must not break the page.
                _logger?.LogWarning(ex, "Legacy patient info timed out for {Key}", key);
                return null;
            }
            finally
            {
                if (openedHere) await conn.CloseAsync();
            }
        }

        public Task<PatientLegacyInfo> GetPatientLegacyInfoAsync(Patient patient)
        {
            if (patient == null) return Task.FromResult<PatientLegacyInfo>(null);

            // One call covers every key: the lookup matches CODE, PHN or PatientId.
            var key = !string.IsNullOrWhiteSpace(patient.Phn) ? patient.Phn
                    : !string.IsNullOrWhiteSpace(patient.Code) ? patient.Code
                    : patient.PatientId?.ToString();

            return GetPatientLegacyInfoAsync(key);
        }
    }
}
