using System;
using System.Collections.Generic;
using System.Linq;
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
     * File........: Services/Lab/CultureService.cs
     * Purpose.....: Culture sub-tab of lb0110: CULTURE header (growth 1-3 and
     *               species A-C) plus the CUL_TR medicine sensitivity grid.
     * Inputs......: CULTURE, CUL_TR, ORGANISM/GROWTHDESCRIPTION (or legacy
     *               ORGANISMS/GRREMARK) and PHITEM/MADIMS for medicine names.
     * Outputs.....: LabCultureView, LabCultureMedicine, JSON pick lists and the
     *               culture part of the save transaction.
     * Business
     *   rules.....: 1. One CULTURE row per LABNO+TID+TESTNO.
     *               2. Opening the culture sub-tab in edit mode creates the row
     *                  (if missing) and sets TESTTR.CULTURE = 1.
     *               3. Save: if growth/species are all blank, delete CULTURE and
     *                  set TESTTR.CULTURE = 0 (rule 3). CUL_TR rows whose A/B/C
     *                  are all blank are dropped (Remove Blank).
     *               4. "Load All" is capped (default 500) because the pharmacy
     *                  master has ~9k medicines; search covers the rest.
     * Developer...: Tajwer Jalal
     * Date/Time...: 2026-11-02 11:18
     * ============================================================================
     */
    public class CultureService
    {
        private const int LoadAllCap = 500;

        private readonly ApplicationDbContext _db;
        private readonly LabSchemaService _schema;
        private readonly ILogger<CultureService> _logger;

        public CultureService(ApplicationDbContext db, LabSchemaService schema, ILogger<CultureService> logger)
        {
            _db = db;
            _schema = schema;
            _logger = logger;
        }

        /// <summary>Loads the culture header and medicine grid for a test.</summary>
        public async Task<LabCultureView> GetAsync(string labNo, string tid, string testNo, bool ensure)
        {
            labNo = (labNo ?? "").Trim();
            tid = (tid ?? "").Trim();
            testNo = (testNo ?? "").Trim();

            if (ensure)
                await EnsureAsync(labNo, tid, testNo);

            var view = new LabCultureView();

            await using var conn = _schema.OpenConnection();
            await conn.OpenAsync();

            using (var cmd = new SqlCommand(@"
SELECT TOP 1 ISNULL(GROWTH1,''), ISNULL(S1,''), ISNULL(GROWTH2,''), ISNULL(S2,''),
             ISNULL(GROWTH3,''), ISNULL(S3,'')
FROM dbo.CULTURE
WHERE LABNO = @labno AND TID = @tid AND ISNULL(TESTNO,'') = @testno", conn))
            {
                cmd.Parameters.AddWithValue("@labno", labNo);
                cmd.Parameters.AddWithValue("@tid", tid);
                cmd.Parameters.AddWithValue("@testno", testNo);
                await using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    view.Growth1 = S(r, 0);
                    view.Species1 = S(r, 1);
                    view.Growth2 = S(r, 2);
                    view.Species2 = S(r, 3);
                    view.Growth3 = S(r, 4);
                    view.Species3 = S(r, 5);
                }
            }

            var schema = await _schema.GetAsync();
            var sql = schema.HasMadims
                ? @"SELECT c.ID, ISNULL(m.NAME, c.ID) AS MedicineName, ISNULL(c.S1,''), ISNULL(c.S2,''), ISNULL(c.S3,'')
                    FROM dbo.CUL_TR c
                    LEFT JOIN dbo.MADIMS m ON m.ID = c.ID
                    WHERE c.LABNO = @labno AND c.TID = @tid AND ISNULL(c.TESTNO,'') = @testno
                    ORDER BY MedicineName, c.ID"
                : @"SELECT c.ID, ISNULL(p.NAME, c.ID) AS MedicineName, ISNULL(c.S1,''), ISNULL(c.S2,''), ISNULL(c.S3,'')
                    FROM dbo.CUL_TR c
                    LEFT JOIN dbo.PHITEM p ON p.CODE = c.ID
                    WHERE c.LABNO = @labno AND c.TID = @tid AND ISNULL(c.TESTNO,'') = @testno
                    ORDER BY MedicineName, c.ID";

            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@labno", labNo);
                cmd.Parameters.AddWithValue("@tid", tid);
                cmd.Parameters.AddWithValue("@testno", testNo);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    view.Medicines.Add(new LabCultureMedicine
                    {
                        Code = S(r, 0),
                        Name = S(r, 1),
                        S1 = S(r, 2),
                        S2 = S(r, 3),
                        S3 = S(r, 4)
                    });
                }
            }

            return view;
        }

        /// <summary>
        /// Activation rule: create the CULTURE row and set TESTTR.CULTURE = 1 the
        /// first time the culture sub-tab is opened in edit mode.
        /// </summary>
        public async Task EnsureAsync(string labNo, string tid, string testNo)
        {
            if (string.IsNullOrWhiteSpace(labNo) || string.IsNullOrWhiteSpace(tid)) return;

            await using var conn = _schema.OpenConnection();
            await conn.OpenAsync();
            await using var tx = conn.BeginTransaction();
            try
            {
                using (var cmd = new SqlCommand(@"
IF NOT EXISTS (SELECT 1 FROM dbo.CULTURE
               WHERE LABNO = @labno AND TID = @tid AND ISNULL(TESTNO,'') = @testno)
BEGIN
    INSERT INTO dbo.CULTURE
        (LABNO, TID, TESTNO, GROWTH1, S1, GROWTH2, S2, GROWTH3, S3, USERID, UDATE, UTIME)
    VALUES (@labno, @tid, @testno, '', '', '', '', '', '', @user, @today, @time);
END
UPDATE dbo.TESTTR SET CULTURE = 1
WHERE LABNO = @labno AND TID = @tid AND ISNULL(TESTNO,'') = @testno;", conn, tx))
                {
                    var now = DateTime.Now;
                    cmd.Parameters.AddWithValue("@labno", labNo);
                    cmd.Parameters.AddWithValue("@tid", tid);
                    cmd.Parameters.AddWithValue("@testno", testNo);
                    cmd.Parameters.AddWithValue("@user", "WEB");
                    cmd.Parameters.AddWithValue("@today", now.Date);
                    cmd.Parameters.AddWithValue("@time", now.ToString("HH:mm"));
                    await cmd.ExecuteNonQueryAsync();
                }
                tx.Commit();
            }
            catch
            {
                try { await tx.RollbackAsync(); } catch { }
                throw;
            }
        }

        // =====================================================================
        // PICK LISTS
        // =====================================================================

        /// <summary>Species pick list (ORGANISM, falling back to legacy ORGANISMS).</summary>
        public async Task<List<string>> GetOrganismsAsync()
        {
            try
            {
                var list = await _db.Organisms.AsNoTracking()
                    .Where(x => !x.Retired && x.Name != null && x.Name != "")
                    .OrderBy(x => x.Code).ThenBy(x => x.Name)
                    .Select(x => x.Name)
                    .ToListAsync();
                if (list.Count > 0) return list;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ORGANISM lookup failed");
            }

            var schema = await _schema.GetAsync();
            if (!schema.HasOrganisms) return new List<string>();

            var fallback = new List<string>();
            await using var conn = _schema.OpenConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(
                "SELECT NAME FROM dbo.ORGANISMS WHERE ISNULL(NAME,'') <> '' ORDER BY ID", conn);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) fallback.Add(S(r, 0));
            return fallback;
        }

        /// <summary>Growth remark pick list (GROWTHDESCRIPTION, falling back to GRREMARK).</summary>
        public async Task<List<string>> GetGrowthRemarksAsync()
        {
            try
            {
                var list = await _db.GrowthDescriptions.AsNoTracking()
                    .Where(x => !x.Retired && x.Description != null && x.Description != "")
                    .OrderBy(x => x.Code)
                    .Select(x => x.Description)
                    .ToListAsync();
                if (list.Count > 0) return list;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GROWTHDESCRIPTION lookup failed");
            }

            var schema = await _schema.GetAsync();
            if (!schema.HasGrremark) return new List<string>();

            var fallback = new List<string>();
            await using var conn = _schema.OpenConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(
                "SELECT [DESC] FROM dbo.GRREMARK WHERE ISNULL([DESC],'') <> '' ORDER BY ID", conn);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) fallback.Add(S(r, 0));
            return fallback;
        }

        /// <summary>Type-ahead medicine search (PHITEM itemtype 'M', or legacy MADIMS).</summary>
        public async Task<List<LabCultureMedicine>> GetMedicinesAsync(string term, int take = 50)
        {
            take = take is < 1 or > 100 ? 50 : take;
            term = (term ?? "").Trim();
            var list = new List<LabCultureMedicine>();

            var schema = await _schema.GetAsync();
            await using var conn = _schema.OpenConnection();
            await conn.OpenAsync();

            var sql = schema.HasMadims
                ? @"SELECT TOP (@take) ID, NAME FROM dbo.MADIMS
                    WHERE (@term = '' OR NAME LIKE @like OR ID LIKE @like)
                    ORDER BY NAME"
                : @"SELECT TOP (@take) CODE, NAME FROM dbo.PHITEM
                    WHERE ITEMTYPE = 'M' AND ISNULL(RETIRED,0) = 0 AND ISNULL(ISACTIVE,1) = 1
                      AND (@term = '' OR NAME LIKE @like OR CODE LIKE @like)
                    ORDER BY NAME";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@take", take);
            cmd.Parameters.AddWithValue("@term", term);
            cmd.Parameters.AddWithValue("@like", "%" + term + "%");
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                list.Add(new LabCultureMedicine { Code = S(r, 0), Name = S(r, 1) });
            return list;
        }

        /// <summary>
        /// "Load All": every medicine not yet on the test, capped at 500 to keep
        /// the browser responsive (the legacy MADIMS table had only 3 rows).
        /// </summary>
        public async Task<List<LabCultureMedicine>> GetAllMedicinesAsync(string labNo, string tid, string testNo)
        {
            var list = new List<LabCultureMedicine>();
            var schema = await _schema.GetAsync();
            await using var conn = _schema.OpenConnection();
            await conn.OpenAsync();

            var sql = schema.HasMadims
                ? @"SELECT TOP (@take) m.ID, m.NAME
                    FROM dbo.MADIMS m
                    WHERE NOT EXISTS (SELECT 1 FROM dbo.CUL_TR c
                                      WHERE c.LABNO = @labno AND c.TID = @tid
                                        AND ISNULL(c.TESTNO,'') = @testno AND c.ID = m.ID)
                    ORDER BY m.NAME"
                : @"SELECT TOP (@take) p.CODE, p.NAME
                    FROM dbo.PHITEM p
                    WHERE p.ITEMTYPE = 'M' AND ISNULL(p.RETIRED,0) = 0 AND ISNULL(p.ISACTIVE,1) = 1
                      AND NOT EXISTS (SELECT 1 FROM dbo.CUL_TR c
                                      WHERE c.LABNO = @labno AND c.TID = @tid
                                        AND ISNULL(c.TESTNO,'') = @testno AND c.ID = p.CODE)
                    ORDER BY p.NAME";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@take", LoadAllCap);
            cmd.Parameters.AddWithValue("@labno", (labNo ?? "").Trim());
            cmd.Parameters.AddWithValue("@tid", (tid ?? "").Trim());
            cmd.Parameters.AddWithValue("@testno", (testNo ?? "").Trim());
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                list.Add(new LabCultureMedicine { Code = S(r, 0), Name = S(r, 1) });
            return list;
        }

        // =====================================================================
        // SAVE (called inside the LabResultService transaction)
        // =====================================================================

        /// <summary>
        /// Saves CULTURE + CUL_TR. Returns true when a culture header or at least
        /// one sensitivity line exists (used for TESTTR.CULTURE).
        /// </summary>
        public async Task<bool> SaveAsync(SqlConnection conn, SqlTransaction tx, LabSaveRequest request,
            string user, DateTime now)
        {
            var labNo = (request.LabNo ?? "").Trim();
            var tid = (request.Tid ?? "").Trim();
            var testNo = (request.TestNo ?? "").Trim();
            var culture = request.Culture ?? new LabSaveCulture();

            var g1 = Clip(culture.Growth1, 60);
            var s1 = Clip(culture.Species1, 36);
            var g2 = Clip(culture.Growth2, 60);
            var s2 = Clip(culture.Species2, 36);
            var g3 = Clip(culture.Growth3, 60);
            var s3 = Clip(culture.Species3, 36);

            var headerEmpty = string.IsNullOrWhiteSpace(g1) && string.IsNullOrWhiteSpace(s1)
                           && string.IsNullOrWhiteSpace(g2) && string.IsNullOrWhiteSpace(s2)
                           && string.IsNullOrWhiteSpace(g3) && string.IsNullOrWhiteSpace(s3);

            // Rule 3: empty culture header deletes the row and clears the flag.
            if (headerEmpty)
            {
                using (var cmd = new SqlCommand(@"
DELETE FROM dbo.CULTURE
WHERE LABNO = @labno AND TID = @tid AND ISNULL(TESTNO,'') = @testno", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@labno", labNo);
                    cmd.Parameters.AddWithValue("@tid", tid);
                    cmd.Parameters.AddWithValue("@testno", testNo);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            else
            {
                using var cmd = new SqlCommand(@"
IF EXISTS (SELECT 1 FROM dbo.CULTURE
           WHERE LABNO = @labno AND TID = @tid AND ISNULL(TESTNO,'') = @testno)
    UPDATE dbo.CULTURE
    SET GROWTH1 = @g1, S1 = @s1, GROWTH2 = @g2, S2 = @s2, GROWTH3 = @g3, S3 = @s3,
        USERID = @user, UDATE = @today, UTIME = @time
    WHERE LABNO = @labno AND TID = @tid AND ISNULL(TESTNO,'') = @testno
ELSE
    INSERT INTO dbo.CULTURE
        (LABNO, TID, TESTNO, GROWTH1, S1, GROWTH2, S2, GROWTH3, S3, USERID, UDATE, UTIME)
    VALUES (@labno, @tid, @testno, @g1, @s1, @g2, @s2, @g3, @s3, @user, @today, @time)", conn, tx);
                cmd.Parameters.AddWithValue("@labno", labNo);
                cmd.Parameters.AddWithValue("@tid", tid);
                cmd.Parameters.AddWithValue("@testno", testNo);
                cmd.Parameters.AddWithValue("@g1", (object)g1 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@s1", (object)s1 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@g2", (object)g2 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@s2", (object)s2 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@g3", (object)g3 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@s3", (object)s3 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@user", user);
                cmd.Parameters.AddWithValue("@today", now.Date);
                cmd.Parameters.AddWithValue("@time", now.ToString("HH:mm"));
                await cmd.ExecuteNonQueryAsync();
            }

            // Rule 2 + Remove Blank: rebuild CUL_TR, dropping id/blank lines.
            using (var cmd = new SqlCommand(@"
DELETE FROM dbo.CUL_TR
WHERE LABNO = @labno AND TID = @tid AND ISNULL(TESTNO,'') = @testno", conn, tx))
            {
                cmd.Parameters.AddWithValue("@labno", labNo);
                cmd.Parameters.AddWithValue("@tid", tid);
                cmd.Parameters.AddWithValue("@testno", testNo);
                await cmd.ExecuteNonQueryAsync();
            }

            var saved = 0;
            foreach (var row in culture.Medicines ?? new List<LabCultureMedicine>())
            {
                var code = Clip(row.Code, 20);
                var a = Clip(row.S1, 1);
                var b = Clip(row.S2, 1);
                var c = Clip(row.S3, 1);
                if (string.IsNullOrWhiteSpace(code) ||
                    (string.IsNullOrWhiteSpace(a) && string.IsNullOrWhiteSpace(b) && string.IsNullOrWhiteSpace(c)))
                    continue;

                using var cmd = new SqlCommand(@"
INSERT INTO dbo.CUL_TR (LABNO, TID, TESTNO, ID, S1, S2, S3)
VALUES (@labno, @tid, @testno, @id, @s1, @s2, @s3)", conn, tx);
                cmd.Parameters.AddWithValue("@labno", labNo);
                cmd.Parameters.AddWithValue("@tid", tid);
                cmd.Parameters.AddWithValue("@testno", testNo);
                cmd.Parameters.AddWithValue("@id", code);
                cmd.Parameters.AddWithValue("@s1", (object)a ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@s2", (object)b ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@s3", (object)c ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
                saved++;
            }

            return !headerEmpty || saved > 0;
        }

        private static string Clip(string value, int max)
        {
            value = (value ?? "").Trim();
            if (value.Length == 0) return null;
            return value.Length > max ? value.Substring(0, max) : value;
        }

        private static string S(SqlDataReader r, int i)
            => r.IsDBNull(i) ? "" : r.GetValue(i)?.ToString()?.Trim() ?? "";
    }
}
