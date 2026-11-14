using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace HmisWeb.Services.Lab
{
    /*
     * ============================================================================
     * File........: Services/Lab/LabSchemaService.cs
     * Purpose.....: Detects, once per process, which of the legacy Labsys master
     *               tables actually exist in the connected HMIS database and
     *               exposes the result to the other lab services.
     * Inputs......: IConfiguration -> ConnectionStrings:DefaultConnection.
     * Outputs.....: LabSchemaInfo flags (READINGS, RESULTS, NRANGE, SERGRP,
     *               MADIMS, TSREMARK, GRREMARK, ORGANISMS).
     * Business
     *   rules.....: 1. The migration kept REPORT/TESTTR/READTR/CULTURE/CUL_TR
     *                  but some VFP masters were migrated to modern tables
     *                  (TESTDATA, CATEGORY, TESTREMARK, GROWTHDESCRIPTION,
     *                  ORGANISM, PHITEM). The page must work in both worlds.
     *               2. Detection is read-only (sys.objects, type U or V) and
     *                  cached; no table is ever created or altered here.
     *               3. If a legacy table is added later, restart the app (or
     *                  recycle the pool) to pick it up.
     * Developer...: Tajwer Jalal
     * Date/Time...: 2026-11-02 11:13
     * ============================================================================
     */

    /// <summary>Availability of the optional legacy Labsys master tables.</summary>
    public sealed class LabSchemaInfo
    {
        public bool HasReadings { get; init; }
        public bool HasResults { get; init; }
        public bool HasNRange { get; init; }
        public bool HasSergrp { get; init; }
        public bool HasMadims { get; init; }
        public bool HasTsremark { get; init; }
        public bool HasGrremark { get; init; }
        public bool HasOrganisms { get; init; }
    }

    /// <summary>
    /// Read-only schema probe. Registered as a scoped service; the probe result is
    /// cached statically because the database schema does not change at runtime.
    /// </summary>
    public class LabSchemaService
    {
        private static readonly SemaphoreSlim Gate = new(1, 1);
        private static LabSchemaInfo _cached;

        private readonly string _connectionString;

        public LabSchemaService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
        }

        /// <summary>Returns the cached schema map, probing the database on first use.</summary>
        public async Task<LabSchemaInfo> GetAsync()
        {
            if (_cached != null) return _cached;

            await Gate.WaitAsync();
            try
            {
                if (_cached != null) return _cached;

                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(@"
SELECT name
FROM sys.objects
WHERE type IN ('U', 'V')
  AND name IN ('READINGS','RESULTS','NRANGE','SERGRP','MADIMS','TSREMARK','GRREMARK','ORGANISMS');", conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    names.Add(reader.GetString(0));

                _cached = new LabSchemaInfo
                {
                    HasReadings = names.Contains("READINGS"),
                    HasResults = names.Contains("RESULTS"),
                    HasNRange = names.Contains("NRANGE"),
                    HasSergrp = names.Contains("SERGRP"),
                    HasMadims = names.Contains("MADIMS"),
                    HasTsremark = names.Contains("TSREMARK"),
                    HasGrremark = names.Contains("GRREMARK"),
                    HasOrganisms = names.Contains("ORGANISMS")
                };
                return _cached;
            }
            finally
            {
                Gate.Release();
            }
        }

        /// <summary>Opens a new SQL connection using the application connection string.</summary>
        public SqlConnection OpenConnection() => new(_connectionString);
    }
}
