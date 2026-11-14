using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HmisWeb.Data;
using HmisWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Services
{
    /// <summary>
    /// Billing mirrors for the ONE canonical services master.
    ///
    /// Every department service lives on [SERVICES] (DEPT = the legacy
    /// department code), but the billing engine charges through [ITEM]
    /// (BILLITEM.ItemId1 -> ITEM). So each service gets one mirror row:
    ///
    ///   DEPT 01 laboratory            -> INV-LB-{code}
    ///   DEPT 02 X-ray / CT scan       -> INV-XR-{code}
    ///   DEPT 03 ultrasound            -> INV-US-{code}
    ///   DEPT 04 dental                -> INV-DNT-{code}
    ///   DEPT 06 operation theatre     -> INV-OT-{code}
    ///   DEPT 07 + 08 hospitalization   -> INV-HSP-{code}
    ///   DEPT 11 consultancy           -> INV-CNP-{code}
    ///
    /// The prefixes are also the scope filter of each module's indent /
    /// company-charge screens, so a hospital indent never lists laboratory
    /// tests and a dental indent never lists operation-theatre procedures.
    ///
    /// Developer : [TAJWER JALAL]
    /// Created   : 22-Oct-2026 18:10 (as LabBillingMirrorService)
    /// Change Log: 22-Oct-2026 20:05 - generalised to every department after
    ///             the hospital / dental / OT / consultancy services pages
    ///             were wired to the shared master.
    /// </summary>
    public interface IDeptBillingMirrorService
    {
        string MirrorCodeFor(string serviceCode, string dept);
        Task<UsBillingMirrorResult> EnsureMirrorsAsync(IEnumerable<long> serviceIds, string dept);
        Task<UsBillingMirrorResult> EnsureAllMissingAsync();
    }

    public class DeptBillingMirrorService : IDeptBillingMirrorService
    {
        private const string SourceTag = "Services master mirror";

        private readonly ApplicationDbContext _db;

        public DeptBillingMirrorService(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>Mirror prefix per legacy department code.</summary>
        public static string PrefixFor(string dept) => dept switch
        {
            "01" => "INV-LB-",
            "02" => "INV-XR-",
            "03" => "INV-US-",
            "04" => "INV-DNT-",
            "06" => "INV-OT-",
            "07" => "INV-HSP-",
            "08" => "INV-HSP-",
            "11" => "INV-CNP-",
            _ => "INV-SVC-"
        };

        /// <summary>Mirror item type per legacy department code.</summary>
        private static Data.Enums.ItemType ItemTypeFor(string dept)
            => dept == "01" ? Data.Enums.ItemType.Investigation : Data.Enums.ItemType.Service;

        public string MirrorCodeFor(string serviceCode, string dept)
        {
            if (string.IsNullOrWhiteSpace(serviceCode)) return null;
            return PrefixFor(dept) + serviceCode.Trim();
        }

        public async Task<UsBillingMirrorResult> EnsureMirrorsAsync(IEnumerable<long> serviceIds, string dept)
        {
            var result = new UsBillingMirrorResult();
            var ids = (serviceIds ?? Enumerable.Empty<long>()).Where(id => id > 0).Distinct().ToList();
            if (ids.Count == 0) return result;

            var services = await _db.DeptServices
                .Where(s => ids.Contains(s.Id) && s.Dept == dept)
                .Select(s => new { s.Id, s.Code, s.Name, s.Sname, s.Dept, s.Rate, s.RateForeigner, s.Retired, s.Billable, s.Active })
                .ToListAsync();

            return await CreateMissingAsync(result, services.Select(s => new MirrorSource
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                Sname = s.Sname,
                Dept = s.Dept,
                Rate = s.Rate,
                RateForeigner = s.RateForeigner,
                Retired = s.Retired,
                Billable = s.Billable,
                Active = s.Active
            }));
        }

        /// <summary>
        /// Backfills every missing mirror of every department. Serialized with
        /// a transaction-scoped SQL application lock, because two application
        /// instances starting together used to double-insert every row
        /// (observed 22-Oct-2026: 735 services -> 1,470 mirror rows).
        /// </summary>
        public async Task<UsBillingMirrorResult> EnsureAllMissingAsync()
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            await _db.Database.ExecuteSqlRawAsync(
                "EXEC sp_getapplock @Resource = N'DeptMirrorBackfill', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 60000");

            var services = await _db.DeptServices
                .Where(s => s.Dept != null && s.Code != null)
                .Select(s => new { s.Id, s.Code, s.Name, s.Sname, s.Dept, s.Rate, s.RateForeigner, s.Retired, s.Billable, s.Active })
                .ToListAsync();

            var result = await CreateMissingAsync(new UsBillingMirrorResult(), services.Select(s => new MirrorSource
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                Sname = s.Sname,
                Dept = s.Dept,
                Rate = s.Rate,
                RateForeigner = s.RateForeigner,
                Retired = s.Retired,
                Billable = s.Billable,
                Active = s.Active
            }));

            await tx.CommitAsync();
            return result;
        }

        /// <summary>
        /// Creates the missing mirror rows. Rows already mirrored (or without
        /// a code) are skipped, so the method is idempotent.
        /// </summary>
        private async Task<UsBillingMirrorResult> CreateMissingAsync(
            UsBillingMirrorResult result, IEnumerable<MirrorSource> services)
        {
            var rows = services.ToList();
            if (rows.Count == 0) return result;

            var codes = rows
                .Where(s => !string.IsNullOrWhiteSpace(s.Code))
                .Select(s => MirrorCodeFor(s.Code, s.Dept))
                .Distinct()
                .ToList();

            var existing = codes.Count == 0
                ? new HashSet<string>()
                : (await _db.Items
                    .Where(i => codes.Contains(i.Code))
                    .Select(i => i.Code)
                    .ToListAsync()).ToHashSet();

            var now = DateTime.Now;

            foreach (var service in rows)
            {
                if (string.IsNullOrWhiteSpace(service.Code))
                {
                    result.Skipped.Add($"{service.Name} (service #{service.Id}) has no code to mirror");
                    continue;
                }

                var mirrorCode = MirrorCodeFor(service.Code, service.Dept);
                if (existing.Contains(mirrorCode)) continue;

                _db.Items.Add(new Item
                {
                    Name = service.Name,
                    Code = mirrorCode,
                    PrintName = service.Name,
                    Sname = service.Sname,
                    Total = (double)(service.Rate ?? 0m),
                    TotalForForeigner = (double)(service.RateForeigner ?? 0m),
                    UnitRate = (double)(service.Rate ?? 0m),
                    // the shared master carries no per-item discount flag
                    DiscountAllowed = false,
                    Billable = service.Billable,
                    Inactive = !service.Active,
                    Retired = service.Retired,
                    ItemType = ItemTypeFor(service.Dept),
                    CreatedAt = now,
                    Source = SourceTag,
                });

                existing.Add(mirrorCode);
                result.Created++;
                result.CreatedCodes.Add(mirrorCode);
            }

            if (result.Created > 0) await _db.SaveChangesAsync();
            return result;
        }

        /// <summary>Row shape used by the mirror builder.</summary>
        private class MirrorSource
        {
            public long Id { get; set; }
            public string Code { get; set; }
            public string Name { get; set; }
            public string Sname { get; set; }
            public string Dept { get; set; }
            public decimal? Rate { get; set; }
            public decimal? RateForeigner { get; set; }
            public bool Retired { get; set; }
            public bool Billable { get; set; }
            public bool Active { get; set; }
        }
    }
}
