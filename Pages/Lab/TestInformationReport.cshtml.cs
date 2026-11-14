using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;

namespace HmisWeb.Pages.Lab
{
    /// <summary>Test Information Report - descriptive information for tests. VFP Labsys report lb0230.</summary>
    public class TestInformationReportModel : HmisWeb.Pages.Shared.PagedPageModel
    {
        /// <summary>Mirror prefix of the laboratory bank on the shared services master.</summary>

        private const string ItemPrefix = "INV-LB-";


        private readonly ApplicationDbContext _db;
        public TestInformationReportModel(ApplicationDbContext db) { _db = db; }

        [BindProperty(SupportsGet = true)] public long? TestId { get; set; }

        public List<Item> Tests { get; set; } = new();
        public Item Selected { get; set; }
        public List<TestData> Parameters { get; set; } = new();

        public async Task OnGetAsync() => await LoadAsync();

        public async Task<IActionResult> OnGetExportAsync()
        {
            await LoadAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Code,Name,Classification,Requirements,Min Duration,Max Duration,Method");
            foreach (var i in Tests)
                sb.AppendLine(string.Join(",", LabCollectionQuery.Q(i.Code), LabCollectionQuery.Q(i.Name), LabCollectionQuery.Q(i.Category?.Name),
                    LabCollectionQuery.Q(i.Requirements), LabCollectionQuery.Q(i.MinDuration), LabCollectionQuery.Q(i.MaxDuration), LabCollectionQuery.Q(i.Descreption)));
            return File(new UTF8Encoding(false).GetBytes(sb.ToString()), "text/csv", "TestInformationReport.csv");
        }

        private async Task LoadAsync()
        {
            var testQuery = _db.Items.Include(i => i.Category)
                .Where(i => !i.Retired && (i.ItemType == ItemType.Investigation || i.ItemType == ItemType.Service)
                && i.Code.StartsWith(ItemPrefix))
                .OrderBy(i => i.Name)
                ;

            TotalCount = await testQuery.CountAsync();
            ClampPaging();
            Tests = await ApplyPaging(testQuery).ToListAsync();

            if (TestId.HasValue && TestId.Value > 0)
            {
                Selected = Tests.FirstOrDefault(t => t.Id == TestId.Value);
                if (Selected?.CategoryId != null)
                    Parameters = await _db.TestData.Where(d => !d.Retired && d.CategoryId == Selected.CategoryId).OrderBy(d => d.OrderNo).ToListAsync();
            }
        }
    }
}