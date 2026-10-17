using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;

namespace HmisWeb.Pages.Lab
{
    /// <summary>Company wise Test Charges Report - VFP Labsys report lb0250.</summary>
    public class CompanyTestChargesReportModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public CompanyTestChargesReportModel(ApplicationDbContext db) { _db = db; }

        [BindProperty(SupportsGet = true)] public long? CompanyId { get; set; }

        public List<CompanyServiceRate> Items { get; set; } = new();
        public List<SelectListItem> CompanyList { get; set; } = new();

        public async Task OnGetAsync() => await LoadAsync();

        public async Task<IActionResult> OnGetExportAsync()
        {
            await LoadAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Company,Test,Rate,Discount %,Staff Rate,Unit Rate");
            foreach (var r in Items)
                sb.AppendLine(string.Join(",", LabCollectionQuery.Q(r.Company?.Name), LabCollectionQuery.Q(((r.Item?.Code ?? "") + " " + r.Item?.Name).Trim()),
                    r.Rate.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    (r.DiscountPercent ?? 0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    (r.StaffRate ?? 0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    (r.UnitRate ?? 0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
            return File(new UTF8Encoding(false).GetBytes(sb.ToString()), "text/csv", "CompanyTestChargesReport.csv");
        }

        private async Task LoadAsync()
        {
            var q = _db.CompanyServiceRates.Include(r => r.Company).Include(r => r.Item)
                .Where(r => !r.Retired && (r.Item.ItemType == ItemType.Investigation || r.Item.ItemType == ItemType.Service));
            if (CompanyId.HasValue && CompanyId.Value > 0) q = q.Where(r => r.CompanyId == CompanyId.Value);
            Items = await q.OrderBy(r => r.Company.Name).ThenBy(r => r.Item.Name).ToListAsync();
            CompanyList = await _db.Institutions.Where(i => !i.Retired && i.Code != null).OrderBy(i => i.Name)
                .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = i.Name }).ToListAsync();
        }
    }
}