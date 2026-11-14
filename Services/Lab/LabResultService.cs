using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HmisWeb.Data;
using HmisWeb.Models.Lab;

namespace HmisWeb.Services.Lab
{
    /*
     * ============================================================================
     * File........: Services/Lab/LabResultService.cs
     * Purpose.....: Data + business logic behind the LB0110 Test Result Entry page
     *               (list tab, entry header, TESTTR lines, READTR result grid and
     *               the save transaction).
     * Inputs......: REPORT/TESTTR/READTR/SERVICES plus the migrated reading
     *               masters (TESTDATA/CATEGORY or the legacy READINGS table).
     * Outputs.....: LabIndentPage / LabEntryView / LabReadingRow / LabSaveResult.
     * Business
     *   rules.....: 1. A test line is LABNO+TID+TESTNO; TESTNO is kept even when
     *                  the UI hides it.
     *               2. Group = TESTTR.GID, falling back to SERVICES.GROUP.
     *               3. Readings = master rows for that group merged with the
     *                  already-entered READTR values (legacy "Load All Group").
     *               4. Save runs in one transaction: delete/re-insert READTR
     *                  (blank results are dropped = legacy "Remove Blank"),
     *                  culture rules handled by CultureService, TESTTR remarks /
     *                  report date / Sr.No / culture flag, REPORT.ATYPE.
     *               5. Optimistic concurrency: no rowversion exists, so the
     *                  original TESTTR values posted by the page are compared.
     * Developer...: Tajwer Jalal
     * Date/Time...: 2026-11-02 11:16
     * ============================================================================
     */

    /// <summary>Thrown when another user changed the TESTTR line while it was being edited.</summary>
    public class LabConcurrencyException : Exception
    {
        public LabConcurrencyException(string message) : base(message) { }
    }

    public class LabResultService
    {
        private readonly ApplicationDbContext _db;
        private readonly LabSchemaService _schema;
        private readonly NormalRangeService _normalRange;
        private readonly CultureService _culture;
        private readonly ILogger<LabResultService> _logger;

        // Words that carry no reading identity when matching TESTDATA.NAME.
        private static readonly HashSet<string> ReadNameStopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "for", "the", "and", "with", "from", "of", "to", "in", "on", "at", "by",
            "a", "an", "is", "per", "test", "tests", "hour", "hours", "hr", "hrs",
            "urine", "urinary", "serum", "blood", "fluid", "spot", "fasting", "random"
        };

        public LabResultService(ApplicationDbContext db, LabSchemaService schema,
            NormalRangeService normalRange, CultureService culture, ILogger<LabResultService> logger)
        {
            _db = db;
            _schema = schema;
            _normalRange = normalRange;
            _culture = culture;
            _logger = logger;
        }

        // =====================================================================
        // LIST TAB
        // =====================================================================

        /// <summary>Paged browse of REPORT with optional Labno / Hrno / Name filters.</summary>
        public async Task<LabIndentPage> GetIndentPageAsync(string labNo, string hrno, string name,
            bool pendingOnly, int page, int pageSize)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 200 ? 25 : pageSize;

            var result = new LabIndentPage
            {
                Page = page,
                PageSize = pageSize,
                LabNoFilter = (labNo ?? "").Trim(),
                HrnoFilter = (hrno ?? "").Trim(),
                NameFilter = (name ?? "").Trim(),
                IncludeReported = !pendingOnly
            };

            var where = new StringBuilder(" WHERE 1 = 1 ");
            if (!string.IsNullOrWhiteSpace(labNo)) where.Append(" AND r.LABNO LIKE @labLike ");
            if (!string.IsNullOrWhiteSpace(hrno)) where.Append(" AND CONVERT(varchar(20), r.HRNO) LIKE @hrLike ");
            if (!string.IsNullOrWhiteSpace(name)) where.Append(" AND r.NAME LIKE @nameLike ");
            if (pendingOnly) where.Append(" AND EXISTS (SELECT 1 FROM dbo.TESTTR t WHERE t.LABNO = r.LABNO AND t.REPODT IS NULL) ");

            await using var conn = _schema.OpenConnection();
            await conn.OpenAsync();

            using (var countCmd = new SqlCommand("SELECT COUNT(*) FROM dbo.REPORT r" + where, conn))
            {
                AddListParameters(countCmd, labNo, hrno, name);
                result.Total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            }

            var sql = $@"
SELECT r.LABNO, r.DATE, r.TIME, r.CID, r.HRNO, r.TYPE, r.NAME, r.SEX, r.AGE, r.ATYPE, r.BEDNO, r.POSTED, r.NET,
       (SELECT COUNT(*) FROM dbo.TESTTR t WHERE t.LABNO = r.LABNO) AS TestCount,
       (SELECT COUNT(*) FROM dbo.TESTTR t WHERE t.LABNO = r.LABNO AND t.REPODT IS NULL) AS PendingCount
FROM dbo.REPORT r
{where}
ORDER BY r.LABNO DESC
OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;";

            using (var cmd = new SqlCommand(sql, conn))
            {
                AddListParameters(cmd, labNo, hrno, name);
                cmd.Parameters.AddWithValue("@skip", (page - 1) * pageSize);
                cmd.Parameters.AddWithValue("@take", pageSize);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Items.Add(new LabIndentListItem
                    {
                        LabNo = S(reader, 0),
                        Date = D(reader, 1),
                        Time = S(reader, 2),
                        Cid = S(reader, 3),
                        Hrno = S(reader, 4),
                        Type = S(reader, 5),
                        Name = S(reader, 6),
                        Sex = S(reader, 7),
                        Age = S(reader, 8),
                        AType = S(reader, 9),
                        BedNo = S(reader, 10),
                        Posted = S(reader, 11),
                        Net = Num(reader, 12),
                        TestCount = Int(reader, 13),
                        PendingCount = Int(reader, 14)
                    });
                }
            }

            return result;
        }

        private static void AddListParameters(SqlCommand cmd, string labNo, string hrno, string name)
        {
            if (!string.IsNullOrWhiteSpace(labNo)) cmd.Parameters.AddWithValue("@labLike", "%" + labNo.Trim() + "%");
            if (!string.IsNullOrWhiteSpace(hrno)) cmd.Parameters.AddWithValue("@hrLike", "%" + hrno.Trim() + "%");
            if (!string.IsNullOrWhiteSpace(name)) cmd.Parameters.AddWithValue("@nameLike", "%" + name.Trim() + "%");
        }

        /// <summary>True when a LABNO exists (used by the scanner fast path).</summary>
        public async Task<bool> IndentExistsAsync(string labNo)
        {
            if (string.IsNullOrWhiteSpace(labNo)) return false;
            await using var conn = _schema.OpenConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand("SELECT 1 FROM dbo.REPORT WHERE LABNO = @labno", conn);
            cmd.Parameters.AddWithValue("@labno", labNo.Trim());
            return await cmd.ExecuteScalarAsync() != null;
        }

        // =====================================================================
        // ENTRY TAB
        // =====================================================================

        /// <summary>
        /// Loads one indent: REPORT header, all TESTTR lines, the selected test,
        /// its READTR rows, the culture block and the test remarks.
        /// </summary>
        public async Task<LabEntryView> GetEntryAsync(string labNo, string tid, string testNo)
        {
            var view = new LabEntryView();
            labNo = (labNo ?? "").Trim();
            if (labNo.Length == 0)
            {
                view.Error = "No Lab Indent No was supplied.";
                return view;
            }

            await using var conn = _schema.OpenConnection();
            await conn.OpenAsync();

            view.Header = await GetHeaderAsync(conn, labNo);
            if (view.Header == null)
            {
                view.Error = "Indent not found for LABNO: " + labNo;
                return view;
            }

            view.Tests = await GetTestsAsync(conn, labNo);
            await FillGroupNamesAsync(conn, view.Tests);

            var wantTid = (tid ?? "").Trim();
            var wantNo = (testNo ?? "").Trim();
            view.SelectedTest = view.Tests.FirstOrDefault(t =>
                    string.Equals(t.Tid, wantTid, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(t.TestNo ?? "", wantNo, StringComparison.OrdinalIgnoreCase))
                ?? view.Tests.FirstOrDefault(t => t.RepoDt == null)
                ?? view.Tests.FirstOrDefault();

            if (view.SelectedTest != null)
            {
                view.GroupCode = await ResolveGroupAsync(conn, labNo,
                    view.SelectedTest.Tid, view.SelectedTest.TestNo);
                view.GroupName = view.SelectedTest.GroupName;
                view.Readings = await GetReadingsAsync(labNo, view.SelectedTest.Tid,
                    view.SelectedTest.TestNo, includeAllMaster: true);
                view.Culture = await _culture.GetAsync(labNo, view.SelectedTest.Tid,
                    view.SelectedTest.TestNo, ensure: false);
                view.RemarkOptions = await GetRemarksAsync(view.SelectedTest.Tid);

                var next = view.Tests.FirstOrDefault(t => t.RepoDt == null
                    && !string.Equals(t.Key, view.SelectedTest.Key, StringComparison.OrdinalIgnoreCase));
                view.NextTid = next?.Tid ?? "";
                view.NextTestNo = next?.TestNo ?? "";
            }

            return view;
        }

        private async Task<LabIndentHeader> GetHeaderAsync(SqlConnection conn, string labNo)
        {
            using var cmd = new SqlCommand(@"
SELECT TOP 1 LABNO, DATE, TIME, NAME, PCODE, HRNO, SEX, AGE, ATYPE,
       CID, CONCODE, BEDNO, PHONE, TYPE, POSTED, NET, REMARKS
FROM dbo.REPORT WHERE LABNO = @labno", conn);
            cmd.Parameters.AddWithValue("@labno", labNo);
            await using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return null;

            return new LabIndentHeader
            {
                LabNo = S(r, 0),
                Date = D(r, 1),
                Time = S(r, 2),
                Name = S(r, 3),
                PCode = S(r, 4),
                Hrno = S(r, 5),
                Sex = S(r, 6),
                Age = S(r, 7),
                AType = S(r, 8),
                Cid = S(r, 9),
                Concode = S(r, 10),
                BedNo = S(r, 11),
                Phone = S(r, 12),
                Type = S(r, 13),
                Posted = S(r, 14),
                Net = Num(r, 15),
                Remarks = S(r, 16)
            };
        }

        /// <summary>Loads the TESTTR lines and resolves test description / group code.</summary>
        private async Task<List<LabTestRow>> GetTestsAsync(SqlConnection conn, string labNo)
        {
            var list = new List<LabTestRow>();
            using (var cmd = new SqlCommand(@"
SELECT t.TID, ISNULL(t.TESTNO, '') AS TESTNO,
       COALESCE(s.[DESC], t.TNAME) AS TestName,
       t.CHARGES, t.TCDATE, t.REPODT, ISNULL(t.GID, '') AS GID, t.CULTURE,
       ISNULL(t.REMARKS, '') AS REMARKS, ISNULL(t.SRNO, 0) AS SRNO, ISNULL(t.URGENT, '') AS URGENT,
       ISNULL(NULLIF(t.GID, ''), s.[GROUP]) AS GroupCode
FROM dbo.TESTTR t
OUTER APPLY (SELECT TOP 1 x.[DESC], x.[GROUP]
             FROM dbo.SERVICES x WHERE x.CODE = t.TID ORDER BY x.Id DESC) s
WHERE t.LABNO = @labno
ORDER BY t.SRNO, t.TID, t.TESTNO", conn))
            {
                cmd.Parameters.AddWithValue("@labno", labNo);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new LabTestRow
                    {
                        Tid = S(r, 0),
                        TestNo = S(r, 1),
                        Name = S(r, 2),
                        Charges = Num(r, 3),
                        TDate = D(r, 4),
                        RepoDt = D(r, 5),
                        Gid = S(r, 6),
                        Culture = !r.IsDBNull(7) && r.GetBoolean(7),
                        Remarks = S(r, 8),
                        SrNo = Int(r, 9),
                        Urgent = S(r, 10),
                        GroupCode = S(r, 11)
                    });
                }
            }
            return list;
        }

        /// <summary>
        /// Fills GroupName. Uses SERGRP when it was migrated; otherwise the group
        /// code itself (CATEGORY.Sname) because CATEGORY has no group-name row.
        /// </summary>
        private async Task FillGroupNamesAsync(SqlConnection conn, List<LabTestRow> tests)
        {
            var codes = tests.Select(t => t.GroupCode)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (codes.Count == 0) return;

            var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var schema = await _schema.GetAsync();

            if (schema.HasSergrp)
            {
                var sql = "SELECT ID, NAME FROM dbo.SERGRP WHERE ID IN (" +
                          string.Join(",", codes.Select((_, i) => "@c" + i)) + ")";
                using var cmd = new SqlCommand(sql, conn);
                for (var i = 0; i < codes.Count; i++) cmd.Parameters.AddWithValue("@c" + i, codes[i]);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    var id = S(r, 0);
                    if (!string.IsNullOrWhiteSpace(id)) names[id] = S(r, 1) ?? id;
                }
            }
            else
            {
                // CATEGORY.Sname is the group code (B1, BB, ...). It is the closest
                // migrated equivalent of sergrp.name/id.
                var sql = "SELECT DISTINCT Sname FROM dbo.CATEGORY WHERE Sname IN (" +
                          string.Join(",", codes.Select((_, i) => "@c" + i)) +
                          ") AND Code IS NOT NULL";
                using var cmd = new SqlCommand(sql, conn);
                for (var i = 0; i < codes.Count; i++) cmd.Parameters.AddWithValue("@c" + i, codes[i]);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    var code = S(r, 0);
                    if (!string.IsNullOrWhiteSpace(code)) names[code] = code;
                }
            }

            foreach (var test in tests)
            {
                var code = (test.GroupCode ?? "").Trim();
                test.GroupName = names.TryGetValue(code, out var name) ? name : code;
            }
        }

        /// <summary>Group = TESTTR.GID, falling back to SERVICES.GROUP (duplicate codes use newest row).</summary>
        public async Task<string> ResolveGroupAsync(SqlConnection conn, string labNo, string tid, string testNo)
        {
            using (var cmd = new SqlCommand(@"
SELECT TOP 1 ISNULL(GID, '') FROM dbo.TESTTR
WHERE LABNO = @labno AND TID = @tid AND ISNULL(TESTNO, '') = @testno", conn))
            {
                cmd.Parameters.AddWithValue("@labno", labNo ?? "");
                cmd.Parameters.AddWithValue("@tid", tid ?? "");
                cmd.Parameters.AddWithValue("@testno", testNo ?? "");
                var value = await cmd.ExecuteScalarAsync();
                var group = value == null || value == DBNull.Value ? "" : value.ToString().Trim();
                if (group.Length > 0) return group;
            }

            using (var cmd = new SqlCommand(
                "SELECT TOP 1 [GROUP] FROM dbo.SERVICES WHERE CODE = @tid ORDER BY Id DESC", conn))
            {
                cmd.Parameters.AddWithValue("@tid", tid ?? "");
                var value = await cmd.ExecuteScalarAsync();
                return value == null || value == DBNull.Value ? "" : value.ToString().Trim();
            }
        }

        // =====================================================================
        // RESULT GRID
        // =====================================================================

        /// <summary>
        /// Returns the readings for the test's group merged with the entered
        /// READTR values. includeAllMaster=true reproduces "Load All Group"
        /// (one row per reading); false returns only rows that already have a
        /// value (used after "Remove Blank").
        /// </summary>
        public async Task<List<LabReadingRow>> GetReadingsAsync(string labNo, string tid, string testNo,
            bool includeAllMaster = true)
        {
            var rows = new List<LabReadingRow>();
            labNo = (labNo ?? "").Trim();
            if (labNo.Length == 0) return rows;

            await using var conn = _schema.OpenConnection();
            await conn.OpenAsync();

            var sex = "";
            var age = "";
            var atype = "";
            using (var cmd = new SqlCommand(
                "SELECT TOP 1 ISNULL(SEX,''), ISNULL(AGE,0), ISNULL(ATYPE,'') FROM dbo.REPORT WHERE LABNO = @labno", conn))
            {
                cmd.Parameters.AddWithValue("@labno", labNo);
                await using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    sex = S(r, 0);
                    age = S(r, 1);
                    atype = S(r, 2);
                }
            }

            var group = await ResolveGroupAsync(conn, labNo, tid, testNo);
            var schema = await _schema.GetAsync();

            // ---- master rows -------------------------------------------------
            // TESTDATA/CATEGORY is the migrated per-test parameter list and is
            // the primary source. The legacy READINGS table is used for range
            // resolution and as a fallback for groups with no CATEGORY link.
            if (group.Length > 0)
            {
                using var cmd = new SqlCommand(@"
SELECT td.Code, td.Name, ISNULL(td.Unit,'') AS Unit, ISNULL(td.[Range],'') AS [Range],
       c.Code AS CategoryCode, c.Name AS CategoryName
FROM dbo.TESTDATA td
JOIN dbo.CATEGORY c ON c.Id = td.CategoryId
WHERE ISNULL(td.Retired, 0) = 0
  AND c.Code LIKE @groupLike
ORDER BY c.Code, td.OrderNo, td.Code", conn);
                cmd.Parameters.AddWithValue("@groupLike", group + "-%");
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    rows.Add(new LabReadingRow
                    {
                        Code = S(r, 0),
                        Name = S(r, 1),
                        Unit = S(r, 2),
                        Range = S(r, 3),
                        Category = S(r, 4),
                        CategoryName = S(r, 5)
                    });
                }
            }

            // ---- entered values ---------------------------------------------
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = new SqlCommand(@"
SELECT ID, VALUE FROM dbo.READTR
WHERE LABNO = @labno AND TID = @tid AND ISNULL(TESTNO,'') = @testno", conn))
            {
                cmd.Parameters.AddWithValue("@labno", labNo);
                cmd.Parameters.AddWithValue("@tid", tid ?? "");
                cmd.Parameters.AddWithValue("@testno", testNo ?? "");
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    var id = S(r, 0);
                    if (!string.IsNullOrWhiteSpace(id)) values[id] = S(r, 1) ?? "";
                }
            }

            // Migrated groups such as B3 have no CATEGORY row, so the normal
            // TESTDATA/CATEGORY join returns nothing. Fall back to the migrated
            // TESTDATA row whose description best matches this test; this keeps
            // the legacy unit and normal range visible instead of showing the
            // blank free-text row.
            if (rows.Count == 0)
            {
                var matched = await FindReadingByNameAsync(conn, labNo, tid, testNo);
                if (matched != null) rows.Add(matched);
            }

            // Last-resort master for groups that have neither a CATEGORY link
            // nor a TESTDATA name match: use the migrated legacy READINGS group
            // (equivalent to the VFP "Load All Group" source).
            if (rows.Count == 0 && schema.HasReadings && group.Length > 0)
            {
                using var cmd = new SqlCommand(@"
SELECT ID, NAME, UNIT, ISNULL(RANGE,'') AS RANGE, '' AS CategoryCode, '' AS CategoryName
FROM dbo.READINGS WHERE GID = @gid ORDER BY ID", conn);
                cmd.Parameters.AddWithValue("@gid", group);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    rows.Add(new LabReadingRow
                    {
                        Code = S(r, 0),
                        Name = S(r, 1),
                        Unit = S(r, 2),
                        Range = S(r, 3),
                        Category = S(r, 4),
                        CategoryName = S(r, 5)
                    });
                }
            }

            // A previously saved free-text B3 result uses reading id "01".
            // Keep that id on the matched master row so the old value is shown
            // instead of being treated as an unknown reading.
            if (rows.Count == 1 && values.Count == 1 && values.ContainsKey("01")
                && !string.Equals(rows[0].Code, "01", StringComparison.OrdinalIgnoreCase))
            {
                rows[0].Code = "01";
            }

            if (!includeAllMaster)
                rows = rows.Where(x => values.ContainsKey(x.Code)).ToList();

            // Tests without a reading master and without a name match store one
            // free-text row with ID "01"; show it with a friendly description.
            if (rows.Count == 0 && values.ContainsKey("01"))
            {
                rows.Add(new LabReadingRow { Code = "01", Name = "Result" });
            }

            // Keep any READTR row whose reading id is no longer in the master.
            foreach (var pair in values)
            {
                if (rows.Any(x => string.Equals(x.Code, pair.Key, StringComparison.OrdinalIgnoreCase)))
                    continue;
                rows.Add(new LabReadingRow
                {
                    Code = pair.Key,
                    Name = "(unknown reading)",
                    Unit = "",
                    Range = "",
                    IsUntraceable = true
                });
            }

            // No master and no value at all: still show one free-text row.
            if (rows.Count == 0 && values.Count == 0)
            {
                rows.Add(new LabReadingRow { Code = "01", Name = "Result", Unit = "", Range = "" });
            }

            // ---- normal ranges (udfrange) -----------------------------------
            var fallbacks = rows.Where(x => !x.IsUntraceable)
                .GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Range ?? "", StringComparer.OrdinalIgnoreCase);
            var ranges = await _normalRange.ResolveAsync(group, sex, age, atype, fallbacks);

            // ---- standard result suggestions --------------------------------
            var suggestions = await GetSuggestionsAsync(conn, group);

            foreach (var row in rows)
            {
                if (values.TryGetValue(row.Code, out var value)) row.Value = value;
                if (suggestions.TryGetValue(row.Code, out var list)) row.Suggestions = list;
                if (ranges.TryGetValue(row.Code, out var range) && !string.IsNullOrWhiteSpace(range))
                    row.Range = range;
            }

            return rows;
        }

        /*
         * ====================================================================
         * Method......: FindReadingByNameAsync
         * Purpose.....: Resolve a reading master row by test description when
         *               the migrated group has no CATEGORY/TESTDATA link.
         * Inputs......: Open connection, LABNO/TID/TESTNO.
         * Outputs.....: The best matching TESTDATA row (unit + normal range)
         *               or null when no significant word matches.
         * Business
         *   rules.....: 1. The legacy READINGS table was not migrated, so
         *                  groups such as B3 have no CATEGORY row.
         *               2. Match significant words in SERVICES.DESC/TESTTR.TNAME
         *                  against TESTDATA.NAME, preferring 24-hour and
         *                  urine rows and rows that carry unit/range.
         *               3. Ties are broken by the lowest numeric reading id,
         *                  which selects the canonical code 04 for B3 calcium.
         * Developer...: Tajwer Jalal
         * Date/Time...: 2026-11-03 11:52
         * ====================================================================
         */
        private async Task<LabReadingRow> FindReadingByNameAsync(SqlConnection conn,
            string labNo, string tid, string testNo)
        {
            var description = await GetTestDescriptionAsync(conn, labNo, tid, testNo);
            if (string.IsNullOrWhiteSpace(description)) return null;

            var testTokens = TokenizeReadName(description);
            if (testTokens.Count == 0) return null;

            var testHas24 = description.IndexOf("24", StringComparison.Ordinal) >= 0;
            var testHasUrine = description.IndexOf("urin", StringComparison.OrdinalIgnoreCase) >= 0;
            var testHasSerum = description.IndexOf("serum", StringComparison.OrdinalIgnoreCase) >= 0
                || description.IndexOf("blood", StringComparison.OrdinalIgnoreCase) >= 0;
            var testHasSpot = description.IndexOf("spot", StringComparison.OrdinalIgnoreCase) >= 0;
            var testHasFluid = description.IndexOf("fluid", StringComparison.OrdinalIgnoreCase) >= 0;

            var candidates = new List<LabReadingCandidate>();
            using (var cmd = new SqlCommand(@"
SELECT td.Id, td.Code, td.Name, ISNULL(td.Unit,'') AS Unit, ISNULL(td.[Range],'') AS [Range]
FROM dbo.TESTDATA td
WHERE ISNULL(td.Retired, 0) = 0
  AND ISNULL(td.Name, '') <> ''", conn))
            await using (var r = await cmd.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                {
                    candidates.Add(new LabReadingCandidate
                    {
                        Id = Convert.ToInt64(r.GetValue(0)),
                        Code = S(r, 1),
                        Name = S(r, 2),
                        Unit = S(r, 3),
                        Range = S(r, 4)
                    });
                }
            }

            LabReadingCandidate best = null;
            var bestScore = 0;
            foreach (var candidate in candidates)
            {
                var nameTokens = TokenizeReadName(candidate.Name);
                var score = 0;
                foreach (var token in testTokens)
                {
                    if (nameTokens.Contains(token)) score += 10;
                }
                if (score == 0) continue;

                var haystack = candidate.Name + " " + candidate.Unit + " " + candidate.Range;
                var has24 = haystack.IndexOf("24", StringComparison.Ordinal) >= 0;
                if (testHas24 && has24) score += 15;
                if (!testHas24 && has24) score -= 5;

                var nameHasUrine = candidate.Name.IndexOf("urin", StringComparison.OrdinalIgnoreCase) >= 0;
                var nameHasSerum = candidate.Name.IndexOf("serum", StringComparison.OrdinalIgnoreCase) >= 0
                    || candidate.Name.IndexOf("blood", StringComparison.OrdinalIgnoreCase) >= 0;
                if (testHasUrine && nameHasUrine) score += 8;
                if (testHasUrine && nameHasSerum) score -= 10;
                if (testHasSerum && nameHasSerum) score += 8;
                if (testHasSerum && nameHasUrine) score -= 10;

                if (!string.IsNullOrWhiteSpace(candidate.Unit)) score += 3;
                if (!string.IsNullOrWhiteSpace(candidate.Range)) score += 5;

                if (candidate.Name.IndexOf("spot", StringComparison.OrdinalIgnoreCase) >= 0 && !testHasSpot)
                    score -= 8;
                if (candidate.Name.IndexOf("fluid", StringComparison.OrdinalIgnoreCase) >= 0 && !testHasFluid)
                    score -= 8;

                if (score <= 0) continue;
                if (best == null || score > bestScore
                    || (score == bestScore && CompareReadingCandidates(candidate, best) < 0))
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            if (best == null) return null;
            return new LabReadingRow
            {
                Code = best.Code,
                Name = best.Name,
                Unit = best.Unit,
                Range = best.Range
            };
        }

        /*
         * ====================================================================
         * Method......: GetTestDescriptionAsync
         * Purpose.....: Read SERVICES.DESC (newest duplicate) with TESTTR.TNAME
         *               as the fallback for the name-matching fallback above.
         * Developer...: Tajwer Jalal
         * Date/Time...: 2026-11-03 11:52
         * ====================================================================
         */
        private static async Task<string> GetTestDescriptionAsync(SqlConnection conn,
            string labNo, string tid, string testNo)
        {
            using (var cmd = new SqlCommand(
                "SELECT TOP 1 ISNULL([DESC], '') FROM dbo.SERVICES WHERE CODE = @tid ORDER BY Id DESC", conn))
            {
                cmd.Parameters.AddWithValue("@tid", tid ?? "");
                var value = await cmd.ExecuteScalarAsync();
                var description = value == null || value == DBNull.Value ? "" : value.ToString().Trim();
                if (description.Length > 0) return description;
            }

            using (var cmd = new SqlCommand(@"
SELECT TOP 1 ISNULL(TNAME, '') FROM dbo.TESTTR
WHERE LABNO = @labno AND TID = @tid AND ISNULL(TESTNO, '') = @testno", conn))
            {
                cmd.Parameters.AddWithValue("@labno", labNo ?? "");
                cmd.Parameters.AddWithValue("@tid", tid ?? "");
                cmd.Parameters.AddWithValue("@testno", testNo ?? "");
                var value = await cmd.ExecuteScalarAsync();
                return value == null || value == DBNull.Value ? "" : value.ToString().Trim();
            }
        }

        /*
         * ====================================================================
         * Method......: TokenizeReadName
         * Purpose.....: Normalise a test/reading description into the set of
         *               significant words used by FindReadingByNameAsync.
         * Developer...: Tajwer Jalal
         * Date/Time...: 2026-11-03 11:52
         * ====================================================================
         */
        private static HashSet<string> TokenizeReadName(string text)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(text)) return set;
            var separators = new[] { ' ', '\t', '\r', '\n', '/', '\\', '-', '_', '(', ')',
                '[', ']', ',', '.', ':', ';', '+', '&', '\'' };
            foreach (var raw in text.Split(separators, StringSplitOptions.RemoveEmptyEntries))
            {
                var word = NormalizeReadWord(raw);
                if (word.Length < 2) continue;
                if (ReadNameStopWords.Contains(word)) continue;
                if (word.All(char.IsDigit)) continue;
                set.Add(word);
            }
            return set;
        }

        private static string NormalizeReadWord(string word)
        {
            word = (word ?? "").Trim().ToLowerInvariant();
            return word switch
            {
                "phosphorous" => "phosphorus",
                "phosporus" => "phosphorus",
                "haemoglobin" => "hemoglobin",
                "haematocrit" => "hematocrit",
                "leucocytes" => "leukocytes",
                "leucocyte" => "leukocyte",
                "colour" => "color",
                _ => word
            };
        }

        private static int CompareReadingCandidates(LabReadingCandidate a, LabReadingCandidate b)
        {
            var aNumeric = int.TryParse(a.Code, out var aCode);
            var bNumeric = int.TryParse(b.Code, out var bCode);
            if (aNumeric && bNumeric)
            {
                var compare = aCode.CompareTo(bCode);
                if (compare != 0) return compare;
            }
            else if (aNumeric != bNumeric)
            {
                return aNumeric ? -1 : 1;
            }
            var codeCompare = string.Compare(a.Code, b.Code, StringComparison.OrdinalIgnoreCase);
            if (codeCompare != 0) return codeCompare;
            return a.Id.CompareTo(b.Id);
        }

        private sealed class LabReadingCandidate
        {
            public long Id { get; set; }
            public string Code { get; set; } = "";
            public string Name { get; set; } = "";
            public string Unit { get; set; } = "";
            public string Range { get; set; } = "";
        }

        /// <summary>Historical values entered for the same group/reading (RESULTS was not migrated).</summary>
        private async Task<Dictionary<string, List<string>>> GetSuggestionsAsync(SqlConnection conn, string group)
        {
            var dict = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(group)) return dict;

            using var cmd = new SqlCommand(@"
SELECT ID, VALUE FROM dbo.READTR
WHERE GID = @gid AND ISNULL(VALUE, '') <> ''
GROUP BY ID, VALUE
ORDER BY VALUE", conn);
            cmd.Parameters.AddWithValue("@gid", group);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var id = S(r, 0);
                var value = S(r, 1);
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(value)) continue;
                if (!dict.TryGetValue(id, out var list))
                {
                    list = new List<string>();
                    dict[id] = list;
                }
                if (list.Count < 20 && !list.Contains(value)) list.Add(value);
            }
            return dict;
        }

        /// <summary>
        /// Standard result choices. Uses RESULTS when the legacy table exists;
        /// otherwise returns values previously entered for the same reading.
        /// </summary>
        public async Task<List<string>> GetResultChoicesAsync(string gid, string readId)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(gid) || string.IsNullOrWhiteSpace(readId)) return list;

            var schema = await _schema.GetAsync();
            await using var conn = _schema.OpenConnection();
            await conn.OpenAsync();

            if (schema.HasResults)
            {
                using var cmd = new SqlCommand(@"
SELECT RESULT FROM dbo.RESULTS
WHERE GID = @gid AND READID = @id AND ISNULL(RESULT,'') <> ''
ORDER BY RESULT", conn);
                cmd.Parameters.AddWithValue("@gid", gid.Trim());
                cmd.Parameters.AddWithValue("@id", readId.Trim());
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync()) list.Add(S(r, 0));
            }

            // RESULTS may have been created by the master migration but not
            // populated yet. Keep the historical READTR choices in that case.
            if (list.Count == 0)
            {
                using var cmd = new SqlCommand(@"
SELECT DISTINCT TOP (50) VALUE FROM dbo.READTR
WHERE GID = @gid AND ID = @id AND ISNULL(VALUE,'') <> ''
ORDER BY VALUE", conn);
                cmd.Parameters.AddWithValue("@gid", gid.Trim());
                cmd.Parameters.AddWithValue("@id", readId.Trim());
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync()) list.Add(S(r, 0));
            }
            return list;
        }

        /// <summary>Default remarks for the test (TESTREMARK, falling back to legacy TSREMARK).</summary>
        public async Task<List<string>> GetRemarksAsync(string tid)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(tid)) return list;

            try
            {
                list = await _db.TestRemarks.AsNoTracking()
                    .Where(x => !x.Retired && x.TestCode == tid
                                && x.Remarks != null && x.Remarks != "")
                    .OrderBy(x => x.Id)
                    .Select(x => x.Remarks)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "TESTREMARK lookup failed for test {Tid}", tid);
            }

            if (list.Count == 0)
            {
                var schema = await _schema.GetAsync();
                if (schema.HasTsremark)
                {
                    await using var conn = _schema.OpenConnection();
                    await conn.OpenAsync();
                    using var cmd = new SqlCommand(
                        "SELECT REMARKS FROM dbo.TSREMARK WHERE TID = @tid AND ISNULL(REMARKS,'') <> ''", conn);
                    cmd.Parameters.AddWithValue("@tid", tid);
                    await using var r = await cmd.ExecuteReaderAsync();
                    while (await r.ReadAsync()) list.Add(S(r, 0));
                }
            }
            return list;
        }

        // =====================================================================
        // SAVE  (legacy validrec port - one transaction)
        // =====================================================================

        /// <summary>
        /// Saves the result grid, culture block, remarks, report date and Sr.No.
        /// Implements the legacy validrec cleanup rules and optimistic concurrency.
        /// </summary>
        public async Task<LabSaveResult> SaveAsync(LabSaveRequest request, string user)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var labNo = (request.LabNo ?? "").Trim();
            var tid = (request.Tid ?? "").Trim();
            var testNo = (request.TestNo ?? "").Trim();
            if (labNo.Length == 0 || tid.Length == 0)
                throw new InvalidOperationException("Lab No and Test Id are required.");

            var now = DateTime.Now;
            user = string.IsNullOrWhiteSpace(user) ? "WEB" : user.Trim();
            if (user.Length > 10) user = user.Substring(0, 10);

            string nextTid = null;
            string nextNo = null;

            await using (var conn = _schema.OpenConnection())
            {
                await conn.OpenAsync();
                await using var tx = conn.BeginTransaction();
                try
                {
                    var current = await GetTestSnapshotAsync(conn, tx, labNo, tid, testNo);
                    if (current == null)
                        throw new InvalidOperationException("The selected test line no longer exists.");

                    EnsureNotModified(current, request);

                    var group = (current.Gid ?? "").Trim();
                    if (group.Length == 0)
                    {
                        using var gcmd = new SqlCommand(
                            "SELECT TOP 1 [GROUP] FROM dbo.SERVICES WHERE CODE = @tid ORDER BY Id DESC", conn, tx);
                        gcmd.Parameters.AddWithValue("@tid", tid);
                        var value = await gcmd.ExecuteScalarAsync();
                        group = value == null || value == DBNull.Value ? "" : value.ToString().Trim();
                    }

                    // ---- REPORT.ATYPE (age type is the only editable header field) ----
                    var existingAType = await GetReportATypeAsync(conn, tx, labNo);
                    var storageAType = NormalizeATypeForStorage(request.AType, existingAType);
                    if (storageAType.Length > 0)
                    {
                        using var cmd = new SqlCommand(@"
UPDATE dbo.REPORT
SET ATYPE = @atype, UDATE = @today, UTIME = @time, USERID = @user
WHERE LABNO = @labno", conn, tx);
                        cmd.Parameters.AddWithValue("@atype", storageAType);
                        cmd.Parameters.AddWithValue("@today", now.Date);
                        cmd.Parameters.AddWithValue("@time", now.ToString("HH:mm"));
                        cmd.Parameters.AddWithValue("@user", user);
                        cmd.Parameters.AddWithValue("@labno", labNo);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // ---- culture (rules 2 and 3 live in CultureService) ----
                    var hasCulture = await _culture.SaveAsync(conn, tx, request, user, now);

                    // ---- TESTTR (remarks, report date, Sr.No, culture flag) ----
                    var reportDate = ParseDate(request.ReportDate);
                    var origDate = ParseDate(request.OriginalReportDate);

                    using (var cmd = new SqlCommand(@"
UPDATE dbo.TESTTR
SET REMARKS = @remarks, REPODT = @repodt, SRNO = @srno, CULTURE = @culture,
    UDATE = @today, UTIME = @time, USERID = @user
WHERE LABNO = @labno AND TID = @tid AND ISNULL(TESTNO,'') = @testno
  AND ISNULL(REMARKS,'') = @origRemarks
  AND ISNULL(SRNO,0) = @origSrno
  AND ((REPODT IS NULL AND @origRepoDt IS NULL) OR REPODT = @origRepoDt)", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@remarks", (object)(request.Remarks ?? "").Trim() ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@repodt", (object)reportDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@srno", request.SrNo);
                        cmd.Parameters.AddWithValue("@culture", hasCulture ? 1 : 0);
                        cmd.Parameters.AddWithValue("@today", now.Date);
                        cmd.Parameters.AddWithValue("@time", now.ToString("HH:mm"));
                        cmd.Parameters.AddWithValue("@user", user);
                        cmd.Parameters.AddWithValue("@labno", labNo);
                        cmd.Parameters.AddWithValue("@tid", tid);
                        cmd.Parameters.AddWithValue("@testno", testNo);
                        cmd.Parameters.AddWithValue("@origRemarks", (current.Remarks ?? "").Trim());
                        cmd.Parameters.AddWithValue("@origSrno", current.SrNo);
                        cmd.Parameters.AddWithValue("@origRepoDt", (object)current.RepoDt ?? DBNull.Value);

                        var affected = await cmd.ExecuteNonQueryAsync();
                        if (affected == 0)
                            throw new LabConcurrencyException(
                                "This test line was changed by another user. Reload the indent and try again.");
                    }

                    // ---- READTR: rule 1 + Remove Blank (empty results are not saved) ----
                    using (var cmd = new SqlCommand(@"
DELETE FROM dbo.READTR
WHERE LABNO = @labno AND TID = @tid AND ISNULL(TESTNO,'') = @testno", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@labno", labNo);
                        cmd.Parameters.AddWithValue("@tid", tid);
                        cmd.Parameters.AddWithValue("@testno", testNo);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    foreach (var row in request.Readings ?? new List<LabSaveReading>())
                    {
                        if (string.IsNullOrWhiteSpace(row.Value)) continue;
                        var code = NormalizeCode(row.Code);
                        var value = Clip(row.Value, 30);
                        if (value == null) continue;

                        using var cmd = new SqlCommand(@"
INSERT INTO dbo.READTR (LABNO, TID, TESTNO, ID, VALUE, GID, CLASS)
VALUES (@labno, @tid, @testno, @id, @value, @gid, '')", conn, tx);
                        cmd.Parameters.AddWithValue("@labno", labNo);
                        cmd.Parameters.AddWithValue("@tid", tid);
                        cmd.Parameters.AddWithValue("@testno", testNo);
                        cmd.Parameters.AddWithValue("@id", code);
                        cmd.Parameters.AddWithValue("@value", value);
                        cmd.Parameters.AddWithValue("@gid", group);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    tx.Commit();
                }
                catch
                {
                    try { await tx.RollbackAsync(); } catch { /* connection already closed */ }
                    throw;
                }

                // ---- auto-advance to the next pending test (after commit) ----
                using (var cmd = new SqlCommand(@"
SELECT TOP 1 TID, ISNULL(TESTNO,'') FROM dbo.TESTTR
WHERE LABNO = @labno AND REPODT IS NULL
  AND NOT (TID = @tid AND ISNULL(TESTNO,'') = @testno)
ORDER BY SRNO, TID", conn))
                {
                    cmd.Parameters.AddWithValue("@labno", labNo);
                    cmd.Parameters.AddWithValue("@tid", tid);
                    cmd.Parameters.AddWithValue("@testno", testNo);
                    await using var r = await cmd.ExecuteReaderAsync();
                    if (await r.ReadAsync())
                    {
                        nextTid = S(r, 0);
                        nextNo = S(r, 1) ?? "";
                    }
                }
            }

            var targetTid = string.IsNullOrWhiteSpace(nextTid) ? tid : nextTid;
            var targetNo = string.IsNullOrWhiteSpace(nextTid) ? testNo : (nextNo ?? "");
            var url = $"/lab/TestResultEntry?labNo={Uri.EscapeDataString(labNo)}" +
                      $"&tid={Uri.EscapeDataString(targetTid)}&testNo={Uri.EscapeDataString(targetNo)}";

            return new LabSaveResult
            {
                Success = true,
                Message = $"Results saved for {LabIndentListItem.FormatLabNo(labNo)} / {tid}.",
                NextUrl = url
            };
        }

        /// <summary>Loads the original TESTTR values used for the optimistic concurrency check.</summary>
        private async Task<TestSnapshot> GetTestSnapshotAsync(SqlConnection conn, SqlTransaction tx,
            string labNo, string tid, string testNo)
        {
            using var cmd = new SqlCommand(@"
SELECT TOP 1 REPODT, ISNULL(REMARKS,''), ISNULL(SRNO,0), ISNULL(GID,'')
FROM dbo.TESTTR
WHERE LABNO = @labno AND TID = @tid AND ISNULL(TESTNO,'') = @testno", conn, tx);
            cmd.Parameters.AddWithValue("@labno", labNo);
            cmd.Parameters.AddWithValue("@tid", tid);
            cmd.Parameters.AddWithValue("@testno", testNo);
            await using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return null;
            return new TestSnapshot
            {
                RepoDt = D(r, 0),
                Remarks = S(r, 1) ?? "",
                SrNo = Int(r, 2),
                Gid = S(r, 3) ?? ""
            };
        }

        private static void EnsureNotModified(TestSnapshot current, LabSaveRequest request)
        {
            var origDate = ParseDate(request.OriginalReportDate ?? "");
            var remarksChanged = !string.Equals((current.Remarks ?? "").Trim(),
                (request.OriginalRemarks ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
            var srNoChanged = current.SrNo != request.OriginalSrNo;
            var dateChanged = !DatesEqual(current.RepoDt, origDate);

            if (remarksChanged || srNoChanged || dateChanged)
                throw new LabConcurrencyException(
                    "This test line was changed by another user. Reload the indent and try again.");
        }

        private async Task<string> GetReportATypeAsync(SqlConnection conn, SqlTransaction tx, string labNo)
        {
            using var cmd = new SqlCommand(
                "SELECT TOP 1 ISNULL(ATYPE,'') FROM dbo.REPORT WHERE LABNO = @labno", conn, tx);
            cmd.Parameters.AddWithValue("@labno", labNo);
            var value = await cmd.ExecuteScalarAsync();
            return value == null || value == DBNull.Value ? "" : value.ToString().Trim();
        }

        /// <summary>
        /// Stores the age type in the same convention as the existing data:
        /// single letter (Y/M/D/H) when the column already uses it, otherwise the
        /// full YEAR/MONTH/DAY/HOUR label. This keeps legacy reports unchanged.
        /// </summary>
        private static string NormalizeATypeForStorage(string requested, string existing)
        {
            var label = NormalRangeService.NormalizeAType(requested);
            if (label.Length == 0) return "";
            var first = label.Substring(0, 1);
            return (existing ?? "").Trim().Length <= 1 ? first : label;
        }

        // =====================================================================
        // GRID ACTIONS
        // =====================================================================

        /// <summary>"Load All Group": one row per reading of the test's group.</summary>
        public Task<List<LabReadingRow>> LoadAllGroupAsync(string labNo, string tid, string testNo)
            => GetReadingsAsync(labNo, tid, testNo, includeAllMaster: true);

        /// <summary>
        /// "Remove Blank": deletes READTR rows whose VALUE is empty, then returns
        /// only the rows that still have a value.
        /// </summary>
        public async Task<List<LabReadingRow>> RemoveBlankAsync(string labNo, string tid, string testNo)
        {
            await using (var conn = _schema.OpenConnection())
            {
                await conn.OpenAsync();
                using var cmd = new SqlCommand(@"
DELETE FROM dbo.READTR
WHERE LABNO = @labno AND TID = @tid AND ISNULL(TESTNO,'') = @testno
  AND (VALUE IS NULL OR LTRIM(RTRIM(VALUE)) = '')", conn);
                cmd.Parameters.AddWithValue("@labno", (labNo ?? "").Trim());
                cmd.Parameters.AddWithValue("@tid", (tid ?? "").Trim());
                cmd.Parameters.AddWithValue("@testno", (testNo ?? "").Trim());
                await cmd.ExecuteNonQueryAsync();
            }

            var rows = await GetReadingsAsync(labNo, tid, testNo, includeAllMaster: false);

            // GetReadingsAsync adds a friendly free-text row when nothing is left;
            // for "Remove Blank" an empty grid is the correct result.
            if (rows.Count == 1 && string.Equals(rows[0].Code, "01", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(rows[0].Value))
            {
                rows.Clear();
            }

            return rows;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private sealed class TestSnapshot
        {
            public DateTime? RepoDt { get; set; }
            public string Remarks { get; set; } = "";
            public int SrNo { get; set; }
            public string Gid { get; set; } = "";
        }

        private static DateTime? ParseDate(string value)
        {
            value = (value ?? "").Trim();
            if (value.Length == 0) return null;
            if (DateTime.TryParse(value, out var date)) return date.Date;
            return null;
        }

        private static bool DatesEqual(DateTime? a, DateTime? b)
            => a?.Date == b?.Date;

        private static string Clip(string value, int max)
        {
            value = (value ?? "").Trim();
            if (value.Length == 0) return null;
            return value.Length > max ? value.Substring(0, max) : value;
        }

        private static string NormalizeCode(string code)
        {
            code = (code ?? "").Trim();
            if (code.Length == 0) return "01";
            if (code.Length == 1) return "0" + code;
            return code.Length > 2 ? code.Substring(0, 2) : code;
        }

        private static string S(SqlDataReader r, int i)
            => r.IsDBNull(i) ? "" : r.GetValue(i)?.ToString()?.Trim() ?? "";

        private static DateTime? D(SqlDataReader r, int i)
            => r.IsDBNull(i) ? null : Convert.ToDateTime(r.GetValue(i));

        private static double Num(SqlDataReader r, int i)
            => r.IsDBNull(i) ? 0 : Convert.ToDouble(r.GetValue(i));

        private static int Int(SqlDataReader r, int i)
            => r.IsDBNull(i) ? 0 : Convert.ToInt32(r.GetValue(i));
    }
}
