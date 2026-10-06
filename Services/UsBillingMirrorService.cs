using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HmisWeb.Data;
using HmisWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Services
{
    public class UsBillingMirrorResult
    {
        public int Created { get; set; }
        public List<string> CreatedCodes { get; set; } = new();
        public List<string> Skipped { get; set; } = new();
    }

    public interface IUsBillingMirrorService
    {
        string MirrorCodeFor(string serviceCode);
        Task<UsBillingMirrorResult> EnsureMirrorsAsync(IEnumerable<long> serviceIds);
        Task<UsBillingMirrorResult> EnsureAllMissingAsync();
    }

    /// <summary>
    /// An ultrasound service cannot be put on a bill until the item master has a row that
    /// billing can charge, so every USSERVICE is mirrored to an ITEM as INV-US-{code}.
    /// The indent screen used to only report the gap; this closes it automatically so an
    /// activated service is always billable.
    /// </summary>
    public class UsBillingMirrorService : IUsBillingMirrorService
    {
        private const string SourceTag = "Ultrasound Setup mirror";
        private readonly ApplicationDbContext _db;

        public UsBillingMirrorService(ApplicationDbContext db)
        {
            _db = db;
        }

        public string MirrorCodeFor(string serviceCode)
        {
            if (string.IsNullOrWhiteSpace(serviceCode)) return null;
            return "INV-US-" + serviceCode.Trim();
        }

        public async Task<UsBillingMirrorResult> EnsureMirrorsAsync(IEnumerable<long> serviceIds)
        {
            var result = new UsBillingMirrorResult();
            var ids = (serviceIds ?? Enumerable.Empty<long>()).Where(id => id > 0).Distinct().ToList();
            if (ids.Count == 0) return result;

            var services = await _db.UsServices
                .Where(s => ids.Contains(s.Id))
                .Select(s => new { s.Id, s.Code, s.Name, s.Rate, s.RateForeigner, s.DiscountAllowed, s.Retired, s.Billable })
                .ToListAsync();

            var codes = services
                .Where(s => !string.IsNullOrWhiteSpace(s.Code))
                .Select(s => MirrorCodeFor(s.Code))
                .ToList();

            var existing = codes.Count == 0
                ? new HashSet<string>()
                : (await _db.Items
                    .Where(i => codes.Contains(i.Code))
                    .Select(i => i.Code)
                    .ToListAsync()).ToHashSet();

            var now = DateTime.Now;

            foreach (var service in services)
            {
                if (string.IsNullOrWhiteSpace(service.Code))
                {
                    result.Skipped.Add($"{service.Name} (service #{service.Id}) has no code to mirror");
                    continue;
                }

                var mirrorCode = MirrorCodeFor(service.Code);
                if (existing.Contains(mirrorCode)) continue;

                _db.Items.Add(new Item
                {
                    Name = service.Name,
                    Code = mirrorCode,
                    PrintName = service.Name,
                    Total = service.Rate,
                    TotalForForeigner = service.RateForeigner,
                    UnitRate = service.Rate,
                    DiscountAllowed = service.DiscountAllowed,
                    Billable = true,
                    Retired = service.Retired,
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

        public async Task<UsBillingMirrorResult> EnsureAllMissingAsync()
        {
            var ids = await _db.UsServices
                .Where(s => !string.IsNullOrWhiteSpace(s.Code))
                .Select(s => s.Id)
                .ToListAsync();

            return await EnsureMirrorsAsync(ids);
        }
    }
}