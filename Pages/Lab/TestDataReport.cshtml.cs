using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services.Lab;

namespace HmisWeb.Pages.Lab
{
    /*
     * ============================================================================
     * File........: Pages/Lab/TestDataReport.cshtml.cs
     * Purpose.....: Test Data Report - the parameters (with units/ranges)
     *               recorded against tests (VFP Labsys report tbdata).
     * Inputs......: TESTDATA/CATEGORY plus the migrated legacy READINGS master.
     * Outputs.....: Paged list/CSV of test parameters with units and ranges.
     * Business
     *   rules.....: 1. TESTDATA is the primary master. When a matching
     *                  READINGS row exists, its non-empty legacy RANGE is
     *                  preferred because it is what the VFP form printed.
     *               2. The legacy match key is Category.Sname + TestData.Code.
     *                  Unlinked rows keep their TESTDATA range.
     * Developer...: Tajwer Jalal
     * Date/Time...: 2026-11-03 13:56
     * ============================================================================
     */
    public class TestDataReportModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly LabSchemaService _schema;

        public TestDataReportModel(ApplicationDbContext db, LabSchemaService schema)
        {
            _db = db;
            _schema = schema;
        }

        [BindProperty(SupportsGet = true)] public string SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)] public long? CategoryId { get; set; }

        public List<TestData> Items { get; set; } = new();
        public List<SelectListItem> CategoryList { get; set; } = new();

        public async Task OnGetAsync() => await LoadAsync();

        public async Task<IActionResult> OnGetExportAsync()
        {
            await LoadAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Code,Name,Unit,Normal Range,Classification");
            foreach (var d in Items)
                sb.AppendLine(string.Join(",", LabCollectionQuery.Q(d.Code), LabCollectionQuery.Q(d.Name), LabCollectionQuery.Q(d.Unit), LabCollectionQuery.Q(d.Range), LabCollectionQuery.Q(d.Category?.Name)));
            return File(new UTF8Encoding(false).GetBytes(sb.ToString()), "text/csv", "TestDataReport.csv");
        }

        private async Task LoadAsync()
        {
            var q = _db.TestData.Include(d => d.Category).Where(d => !d.Retired);
            if (CategoryId.HasValue && CategoryId.Value > 0) q = q.Where(d => d.CategoryId == CategoryId.Value);
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var t = SearchTerm.Trim().ToLower();
                q = q.Where(d => (d.Name != null && d.Name.ToLower().Contains(t)) || (d.Code != null && d.Code.ToLower().Contains(t)));
            }
            Items = await q.OrderBy(d => d.Category.Name).ThenBy(d => d.OrderNo).ThenBy(d => d.Name).ToListAsync();
            CategoryList = await _db.Categories.Where(c => !c.Retired).OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToListAsync();
            await ApplyLegacyRangesAsync();
        }

        /*
         * ====================================================================
         * Method......: ApplyLegacyRangesAsync
         * Purpose.....: Prefer the migrated legacy READINGS.RANGE over a blank
         *               TESTDATA.Range so the master report matches the form.
         * Inputs......: Loaded Items (with Category) and the optional READINGS
         *               table detected by LabSchemaService.
         * Outputs.....: Items[].Range updated in memory (not persisted).
         * Developer...: Tajwer Jalal
         * Date/Time...: 2026-11-03 13:56
         * ====================================================================
         */
        private async Task ApplyLegacyRangesAsync()
        {
            if (Items.Count == 0) return;

            var schema = await _schema.GetAsync();
            if (!schema.HasReadings) return;

            var byKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            await using var conn = _schema.OpenConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(
                "SELECT GID, ID, NAME, ISNULL([RANGE],'') FROM dbo.READINGS", conn);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var gid = r.IsDBNull(0) ? "" : r.GetValue(0).ToString().Trim();
                var id = r.IsDBNull(1) ? "" : r.GetValue(1).ToString().Trim();
                var name = r.IsDBNull(2) ? "" : r.GetValue(2).ToString().Trim();
                var range = r.IsDBNull(3) ? "" : r.GetValue(3).ToString().Trim();
                if (string.IsNullOrWhiteSpace(range)) continue;

                if (gid.Length > 0 && id.Length > 0)
                    byKey[gid + "|" + id] = range;
                if (name.Length > 0 && !byName.ContainsKey(name))
                    byName[name] = range;
            }

            foreach (var item in Items)
            {
                var gid = item.Category?.Sname?.Trim() ?? "";
                var id = item.Code?.Trim() ?? "";
                string range = null;

                if (gid.Length > 0 && id.Length > 0
                    && byKey.TryGetValue(gid + "|" + id, out var keyed))
                {
                    range = keyed;
                }

                if (string.IsNullOrWhiteSpace(range) && !string.IsNullOrWhiteSpace(item.Name)
                    && byName.TryGetValue(item.Name.Trim(), out var named))
                {
                    range = named;
                }

                if (!string.IsNullOrWhiteSpace(range))
                    item.Range = range;
            }
        }
    }
}