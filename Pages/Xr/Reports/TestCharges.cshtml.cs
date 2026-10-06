using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;

namespace HmisWeb.Pages.Xr.Reports
{
    /// <summary>
    /// Migrated from the VFP "Test Charges Report" (us0210). VFP read compchg.DBF
    /// (company-wise charge overrides) joined to services.DBF for the logged-in
    /// department. In HMIS the same data lives in COMPANYSERVICERATE, keyed by the
    /// company (an Institution) and the billing ITEM, so the service is resolved back
    /// through its INV-XR-{code} mirror.
    /// </summary>
    public class TestChargesModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public TestChargesModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public long? CompanyId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SelectedCodes { get; set; }

        [BindProperty(SupportsGet = true)]
        public string OrderBy { get; set; } = "Code";

        public List<CompanyOption> Companies { get; set; } = new();
        public List<ChargeRow> Rows { get; set; } = new();
        public string CompanyName { get; set; }
        public string ErrorMessage { get; set; }

        public double TotalStandard { get => Rows.Sum(r => r.StandardRate); }
        public double TotalCompany { get => Rows.Sum(r => r.CompanyRate); }

        private HashSet<string> SelectedSet()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(SelectedCodes)) return set;
            foreach (var c in SelectedCodes.Split(',', StringSplitOptions.RemoveEmptyEntries))
                set.Add(c.Trim());
            return set;
        }

        public async Task OnGetAsync()
        {
            // Panel companies only: the VFP company master imported into INSTITUTION rows
            // that carry a Code. Facilities such as "Main Hospital" have no code and are
            // not panel companies, so they are left out of the list.
            var panels = await _db.Institutions
                .Where(i => !i.Retired && i.Code != null)
                .OrderBy(i => i.Name)
                .ToListAsync();

            // Show how many company-wise charges each panel company actually has, so the
            // user can tell an empty company from one that simply has no overrides yet.
            var chargeCounts = await _db.CompanyServiceRates
                .Where(r => !r.Retired)
                .Select(r => r.CompanyId)
                .ToListAsync();

            Companies = panels
                .Select(p => new CompanyOption
                {
                    Id = p.Id,
                    Name = p.Name,
                    Code = p.Code,
                    ChargeCount = chargeCounts.Count(id => id == p.Id)
                })
                .ToList();

            if (!CompanyId.HasValue) return;

            CompanyName = Companies.FirstOrDefault(c => c.Id == CompanyId.Value)?.Name
                          ?? (await _db.Institutions.FirstOrDefaultAsync(i => i.Id == CompanyId.Value))?.Name;

            // CompanyServiceRate.ItemId points at the billing ITEM; the X-ray service
            // is the item whose code is INV-XR-{service code}.
            var rates = await _db.CompanyServiceRates
                .Include(r => r.Item)
                .Where(r => r.CompanyId == CompanyId.Value && !r.Retired)
                .ToListAsync();

            var byCode = new Dictionary<string, ChargeRow>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rates)
            {
                var itemCode = r.Item?.Code;
                if (string.IsNullOrEmpty(itemCode) || !itemCode.StartsWith("INV-XR-", StringComparison.OrdinalIgnoreCase))
                    continue;

                var svcCode = itemCode.Substring("INV-XR-".Length);
                var svc = await _db.XrServices.FirstOrDefaultAsync(s => s.Code == svcCode);
                if (svc == null) continue;

                byCode[svcCode] = new ChargeRow
                {
                    Code = svc.Code,
                    Name = svc.Name,
                    Group = svc.Category?.Name,
                    StandardRate = svc.Rate ?? 0,
                    StandardForeigner = svc.RateForeigner ?? 0,
                    CompanyRate = r.Rate,
                    CompanyUnitRate = r.UnitRate ?? r.Rate,
                    DiscountPercent = r.DiscountPercent ?? 0
                };
            }

            // Load category names for the matched services.
            var ids = await _db.XrServices
                .Where(s => byCode.Keys.Contains(s.Code))
                .Select(s => new { s.Id, s.CategoryId, Cat = s.Category != null ? s.Category.Name : null })
                .ToDictionaryAsync(x => x.Id);

            foreach (var code in byCode.Keys.ToList())
            {
                var svcId = await _db.XrServices.Where(s => s.Code == code).Select(s => s.Id).FirstOrDefaultAsync();
                if (ids.TryGetValue(svcId, out var m)) byCode[code].Group = m.Cat;
            }

            var selected = SelectedSet();
            var list = selected.Count == 0
                ? byCode.Values.ToList()
                : byCode.Values.Where(r => selected.Contains(r.Code)).ToList();

            Rows = (OrderBy == "Name"
                    ? list.OrderBy(r => r.Name)
                    : list.OrderBy(r => r.Code)).ToList();
        }

        public async Task<IActionResult> OnGetExportAsync()
        {
            await OnGetAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Code,Service,Group,Standard Rate,Foreigner Rate,Company Rate,Company Unit Rate,Discount %");
            foreach (var r in Rows)
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    Csv(r.Code), Csv(r.Name), Csv(r.Group),
                    r.StandardRate.ToString("0.00", CultureInfo.InvariantCulture),
                    r.StandardForeigner.ToString("0.00", CultureInfo.InvariantCulture),
                    r.CompanyRate.ToString("0.00", CultureInfo.InvariantCulture),
                    r.CompanyUnitRate.ToString("0.00", CultureInfo.InvariantCulture),
                    r.DiscountPercent.ToString("0.00", CultureInfo.InvariantCulture)
                }));
            }

            return File(new UTF8Encoding(false).GetBytes(sb.ToString()), "text/csv", "TestCharges.csv");
        }

        private static string Csv(string v)
        {
            if (string.IsNullOrEmpty(v)) return "";
            return v.Contains(',') || v.Contains('"') ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
        }

        /// <summary>A panel company offered in the dropdown, with its VFP code and how many
        /// company-wise charge rows it actually has.</summary>
        public class CompanyOption
        {
            public long Id { get; set; }
            public string Name { get; set; } = "";
            public string Code { get; set; } = "";
            public int ChargeCount { get; set; }
        }

        public class ChargeRow
        {
            public string Code { get; set; } = "";
            public string Name { get; set; } = "";
            public string Group { get; set; } = "";
            public double StandardRate { get; set; }
            public double StandardForeigner { get; set; }
            public double CompanyRate { get; set; }
            public double CompanyUnitRate { get; set; }
            public double DiscountPercent { get; set; }
        }
    }
}