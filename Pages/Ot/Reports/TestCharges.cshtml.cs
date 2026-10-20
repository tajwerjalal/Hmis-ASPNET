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

namespace HmisWeb.Pages.Ot.Reports
{
    /// <summary>
    /// Migrated from the VFP "Test Charges Report" (us0210), which listed the company-wise
    /// charges (compchg.DBF) for one company. VFP stored every department in that one table,
    /// so this report reads PANELSERVICECHARGE and can be switched to any department:
    /// laboratory, X-ray, ultrasound, dental, operation theatre, hospitalisation, consultancy.
    /// </summary>
    public class TestChargesModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public TestChargesModel(ApplicationDbContext db) { _db = db; }

        [BindProperty(SupportsGet = true)]
        public long? CompanyId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SelectedCodes { get; set; }

        [BindProperty(SupportsGet = true)]
        public string OrderBy { get; set; } = "Code";

        [BindProperty(SupportsGet = true)]
        public string Department { get; set; } = "03";

        public List<CompanyOption> Companies { get; set; } = new();
        public List<DepartmentOption> Departments { get; set; } = new();
        public List<ChargeRow> Rows { get; set; } = new();
        public string CompanyName { get; set; }
        public string ErrorMessage { get; set; }

        public double TotalStandard => Rows.Sum(r => r.StandardRate);
        public double TotalCompany => Rows.Sum(r => r.CompanyRate);
        public int LinkedCount => Rows.Count(r => r.Linked);

        public bool AllDepartments =>
            string.IsNullOrWhiteSpace(Department)
            || string.Equals(Department, "All", StringComparison.OrdinalIgnoreCase);

        public async Task OnGetAsync()
        {
            Departments = await _db.PanelDepartments
                .Where(d => !d.Retired)
                .OrderBy(d => d.Code)
                .Select(d => new DepartmentOption { Code = d.Code, Name = d.Name })
                .ToListAsync();

            // Only offer companies that actually have charges in the chosen department.
            var companyScope = _db.PanelServiceCharges.Where(p => !p.Retired);
            if (!AllDepartments) companyScope = companyScope.Where(p => p.DepartmentCode == Department);

            var companyIds = await companyScope.Select(p => p.CompanyId).Distinct().ToListAsync();

            var panels = await _db.Institutions
                .Where(i => !i.Retired && i.Code != null && companyIds.Contains(i.Id))
                .OrderBy(i => i.Name)
                .ToListAsync();

            var counts = await companyScope
                .GroupBy(p => p.CompanyId)
                .Select(g => new { CompanyId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CompanyId, x => x.Count);

            Companies = panels
                .Select(p => new CompanyOption
                {
                    Id = p.Id,
                    Name = p.Name,
                    Code = p.Code,
                    ChargeCount = counts.TryGetValue(p.Id, out var c) ? c : 0
                })
                .ToList();

            if (!CompanyId.HasValue) return;

            CompanyName = Companies.FirstOrDefault(c => c.Id == CompanyId.Value)?.Name
                          ?? (await _db.Institutions.FirstOrDefaultAsync(i => i.Id == CompanyId.Value))?.Name;

            var query = _db.PanelServiceCharges
                .Where(p => !p.Retired && p.CompanyId == CompanyId.Value);
            if (!AllDepartments) query = query.Where(p => p.DepartmentCode == Department);

            if (!string.IsNullOrWhiteSpace(SelectedCodes))
            {
                var wanted = SelectedCodes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                          .Select(s => s.Trim())
                                          .ToList();
                query = query.Where(p => wanted.Contains(p.ServiceCode));
            }

            var data = await query.ToListAsync();

            Rows = (OrderBy == "Name"
                    ? data.OrderBy(p => p.ServiceName).ThenBy(p => p.ServiceCode)
                    : data.OrderBy(p => p.ServiceCode))
                   .Select(p => new ChargeRow
                   {
                       Code = p.ServiceCode,
                       Name = p.ServiceName,
                       Group = p.ServiceGroup,
                       Department = p.DepartmentName,
                       StandardRate = p.StandardRate ?? 0,
                       StandardForeigner = p.StandardForeignerRate ?? 0,
                       CompanyRate = p.Rate,
                       CompanyUnitRate = p.Rate,
                       DiscountPercent = 0,
                       Linked = p.ItemId.HasValue
                   })
                   .ToList();
        }

        public async Task<IActionResult> OnGetExportAsync()
        {
            await OnGetAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Department,Code,Service,Group,Standard Rate,Foreigner Rate,Company Rate,Linked to billing");
            foreach (var r in Rows)
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    Csv(r.Department), Csv(r.Code), Csv(r.Name), Csv(r.Group),
                    r.StandardRate.ToString("0.00", CultureInfo.InvariantCulture),
                    r.StandardForeigner.ToString("0.00", CultureInfo.InvariantCulture),
                    r.CompanyRate.ToString("0.00", CultureInfo.InvariantCulture),
                    r.Linked ? "yes" : "no"
                }));
            }

            return File(new UTF8Encoding(false).GetBytes(sb.ToString()), "text/csv", "TestCharges.csv");
        }

        private static string Csv(string v)
        {
            if (string.IsNullOrEmpty(v)) return "";
            return v.Contains(',') || v.Contains('"') ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
        }

        public class CompanyOption
        {
            public long Id { get; set; }
            public string Name { get; set; } = "";
            public string Code { get; set; } = "";
            public int ChargeCount { get; set; }
        }

        public class DepartmentOption
        {
            public string Code { get; set; } = "";
            public string Name { get; set; } = "";
        }

        public class ChargeRow
        {
            public string Code { get; set; } = "";
            public string Name { get; set; } = "";
            public string Group { get; set; } = "";
            public string Department { get; set; } = "";
            public double StandardRate { get; set; }
            public double StandardForeigner { get; set; }
            public double CompanyRate { get; set; }
            public double CompanyUnitRate { get; set; }
            public double DiscountPercent { get; set; }
            public bool Linked { get; set; }
        }
    }
}