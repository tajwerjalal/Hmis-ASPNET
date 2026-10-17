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
    /// <summary>Resource wise Test Charges Report - VFP Labsys report lb0260.</summary>
    public class ResourceTestChargesReportModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public ResourceTestChargesReportModel(ApplicationDbContext db) { _db = db; }

        [BindProperty(SupportsGet = true)] public long? ResourceId { get; set; }

        public List<ResourceServiceRate> Items { get; set; } = new();
        public List<SelectListItem> ResourceList { get; set; } = new();

        public async Task OnGetAsync() => await LoadAsync();

        public async Task<IActionResult> OnGetExportAsync()
        {
            await LoadAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Resource,Test,Staff Rate,Unit Rate");
            foreach (var r in Items)
                sb.AppendLine(string.Join(",", LabCollectionQuery.Q(r.Resource?.Name), LabCollectionQuery.Q(((r.Item?.Code ?? "") + " " + r.Item?.Name).Trim()),
                    r.StaffRate.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    r.UnitRate.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
            return File(new UTF8Encoding(false).GetBytes(sb.ToString()), "text/csv", "ResourceTestChargesReport.csv");
        }

        private async Task LoadAsync()
        {
            var q = _db.ResourceServiceRates.Include(r => r.Resource).Include(r => r.Item)
                .Where(r => !r.Retired && (r.Item.ItemType == ItemType.Investigation || r.Item.ItemType == ItemType.Service));
            if (ResourceId.HasValue && ResourceId.Value > 0) q = q.Where(r => r.ResourceId == ResourceId.Value);
            Items = await q.OrderBy(r => r.Resource.Name).ThenBy(r => r.Item.Name).ToListAsync();
            ResourceList = await _db.Resources.Where(r => !r.Retired).OrderBy(r => r.Name)
                .Select(r => new SelectListItem { Value = r.Id.ToString(), Text = (r.Code ?? "") + " - " + r.Name }).ToListAsync();
        }
    }
}