using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace HmisWeb.Services.Lab
{
    /*
     * ============================================================================
     * File........: Services/Lab/NormalRangeService.cs
     * Purpose.....: C# port of the VFP function udfrange(gid, id, sex, age, atype)
     *               used by lb0110 to print the normal range of a reading.
     * Inputs......: gid (group), read id, patient sex/age/age-type and the
     *               fallback range read from the reading master.
     * Outputs.....: The matching NRANGE.RANGE value, or the reading master range
     *               (READINGS.RANGE / TESTDATA.Range) when no NRANGE row matches.
     * Business
     *   rules.....: 1. Age is converted to HOURS exactly like the legacy code:
     *                  HOUR x1, DAY x24, MONTH x24x30, YEAR x24x30x12, else 0.
     *               2. NRANGE is searched by gid + readid + sex; rows are walked
     *                  in HRRANGE order and the first row with
     *                  HRRANGE >= age-in-hours wins.
     *               3. If gid/id is empty, NRANGE is absent, or no row matches,
     *                  the reading master range is returned (legacy fallback).
     *               4. The legacy NRANGE/READINGS tables are used when present;
     *                  otherwise the migrated TESTDATA/CATEGORY masters are used.
     *                  This keeps the page working on the current HMIS database
     *                  without creating or altering any table.
     * Developer...: Tajwer Jalal
     * Date/Time...: 2026-11-02 11:14
     * ============================================================================
     */
    public class NormalRangeService
    {
        private readonly LabSchemaService _schema;

        public NormalRangeService(LabSchemaService schema)
        {
            _schema = schema;
        }

        /// <summary>
        /// Resolves the normal range for every reading of a group in a single pass.
        /// </summary>
        /// <param name="gid">Test group (TESTTR.GID or SERVICES.GROUP).</param>
        /// <param name="sex">Patient sex as stored in REPORT.SEX (M/F/MALE/FEMALE).</param>
        /// <param name="age">Patient age as stored in REPORT.AGE.</param>
        /// <param name="atype">Age type (Y/M/D/H or YEAR/MONTH/DAY/HOUR).</param>
        /// <param name="fallbacks">Reading id -> range from the reading master.</param>
        public async Task<Dictionary<string, string>> ResolveAsync(
            string gid,
            string sex,
            string age,
            string atype,
            IDictionary<string, string> fallbacks)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (fallbacks == null) return result;

            foreach (var pair in fallbacks)
                result[pair.Key] = pair.Value ?? "";

            if (string.IsNullOrWhiteSpace(gid) || result.Count == 0)
                return result;

            var schema = await _schema.GetAsync();

            // 1. Exact legacy udfrange behaviour when NRANGE exists.
            if (schema.HasNRange)
            {
                var hours = AgeToHours(age, atype);
                var byReading = new Dictionary<string, List<(long HrRange, string Range)>>(StringComparer.OrdinalIgnoreCase);

                await using var conn = _schema.OpenConnection();
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(@"
SELECT READID, HRRANGE, RANGE
FROM dbo.NRANGE
WHERE GID = @gid
  AND (SEX = @sex OR SEX LIKE @sex + '%')
ORDER BY HRRANGE;", conn);
                cmd.Parameters.AddWithValue("@gid", gid.Trim());
                cmd.Parameters.AddWithValue("@sex", NormalizeSex(sex));

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var readId = reader.IsDBNull(0) ? "" : reader.GetValue(0).ToString().Trim();
                    if (readId.Length == 0) continue;
                    var hr = reader.IsDBNull(1) ? 0L : Convert.ToInt64(reader.GetValue(1));
                    var range = reader.IsDBNull(2) ? "" : reader.GetValue(2).ToString().Trim();

                    if (!byReading.TryGetValue(readId, out var list))
                    {
                        list = new List<(long, string)>();
                        byReading[readId] = list;
                    }
                    list.Add((hr, range));
                }

                foreach (var pair in byReading)
                {
                    var match = pair.Value.FirstOrDefault(x => x.HrRange >= hours);
                    // Keep the reading-master fallback when the NRANGE row has
                    // no text; this prevents a blank NRANGE row from hiding a
                    // valid READINGS/TESTDATA range.
                    if (!string.IsNullOrWhiteSpace(match.Range))
                        result[pair.Key] = match.Range;
                }
            }

            // 2. Legacy fallback table READINGS (when it was migrated).
            if (schema.HasReadings)
            {
                await ApplyReadingsFallbackAsync(gid, result);
            }

            return result;
        }

        /// <summary>Resolves the range for a single reading (used by the reading editor).</summary>
        public async Task<string> ResolveOneAsync(string gid, string readId, string sex, string age,
            string atype, string fallbackRange)
        {
            var map = await ResolveAsync(gid, sex, age, atype,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [readId ?? ""] = fallbackRange ?? "" });
            return map.TryGetValue(readId ?? "", out var value) ? value : (fallbackRange ?? "");
        }

        /// <summary>
        /// Port of the VFP age-to-hours conversion. Returns 0 for an unknown type,
        /// which makes the first NRANGE row match (same as the legacy code).
        /// </summary>
        public static long AgeToHours(string age, string atype)
        {
            var hours = 0L;
            var ageText = (age ?? "").Trim();
            if (ageText.Length == 0) return 0;

            // REPORT.AGE is numeric; tolerate values such as "30" or "30.0".
            var digits = new string(ageText.TakeWhile(char.IsDigit).ToArray());
            if (!long.TryParse(digits, out var value)) return 0;

            switch (NormalizeAType(atype))
            {
                case "HOUR": hours = value * 1; break;
                case "DAY": hours = value * 24; break;
                case "MONTH": hours = value * 24 * 30; break;
                case "YEAR": hours = value * 24 * 30 * 12; break;
                default: hours = 0; break;
            }
            return hours;
        }

        /// <summary>Normalises Y/M/D/H and YEAR/MONTH/DAY/HOUR to the long form.</summary>
        public static string NormalizeAType(string atype)
        {
            return (atype ?? "").Trim().ToUpperInvariant() switch
            {
                "Y" or "YEAR" => "YEAR",
                "M" or "MONTH" => "MONTH",
                "D" or "DAY" => "DAY",
                "H" or "HOUR" => "HOUR",
                _ => (atype ?? "").Trim().ToUpperInvariant()
            };
        }

        /// <summary>Normalises sex to the one-letter legacy key where possible.</summary>
        public static string NormalizeSex(string sex)
        {
            var value = (sex ?? "").Trim().ToUpperInvariant();
            if (value.StartsWith("M")) return "M";
            if (value.StartsWith("F")) return "F";
            return value;
        }

        /// <summary>Overrides the fallback ranges with READINGS.RANGE when that table exists.</summary>
        private async Task ApplyReadingsFallbackAsync(string gid, Dictionary<string, string> result)
        {
            var ids = result.Keys.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct().ToList();
            if (ids.Count == 0) return;

            var sql = new StringBuilder("SELECT ID, RANGE FROM dbo.READINGS WHERE GID = @gid AND ID IN (");
            await using var conn = _schema.OpenConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand();
            cmd.Connection = conn;
            cmd.Parameters.AddWithValue("@gid", gid.Trim());

            for (var i = 0; i < ids.Count; i++)
            {
                if (i > 0) sql.Append(',');
                var name = "@id" + i;
                sql.Append(name);
                cmd.Parameters.AddWithValue(name, ids[i]);
            }
            sql.Append(");");
            cmd.CommandText = sql.ToString();

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var id = reader.IsDBNull(0) ? "" : reader.GetValue(0).ToString().Trim();
                var range = reader.IsDBNull(1) ? "" : reader.GetValue(1).ToString().Trim();
                // Only a non-empty legacy range may override the reading master
                // (TESTDATA) fallback. A blank READINGS row must not erase it.
                if (id.Length > 0 && !string.IsNullOrWhiteSpace(range))
                    result[id] = range;
            }
        }
    }
}
