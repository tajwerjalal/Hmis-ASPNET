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
    /// Migrated from the VFP "Service List" report (us0220). The VFP screen queried
    /// services.DBF left-joined to sergrp.DBF for the logged-in department and offered
    /// three layouts: a flat list, a group-included list, or a single selected group.
    /// </summary>
    public class ServiceListModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public ServiceListModel(ApplicationDbContext db)
        {
            _db = db;
        }

        // VFP optgroup: 1 = Exclude groups, 2 = Include groups, 3 = Selected group
        [BindProperty(SupportsGet = true)]
        public string LayoutMode { get; set; } = "Grouped";

        // VFP optorder: code or desc
        [BindProperty(SupportsGet = true)]
        public string OrderBy { get; set; } = "Code";

        [BindProperty(SupportsGet = true)]
        public long? CategoryId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        public List<Category> Categories { get; set; } = new();
        public List<ServiceListRow> Rows { get; set; } = new();

        public int TotalServices => Rows.Count;
        public double TotalRate => Rows.Sum(r => r.Rate);
        public string ReportTitle => "Service List";
        public bool Grouped => !string.Equals(LayoutMode, "Flat", StringComparison.OrdinalIgnoreCase);
        public string PeriodText => "All active services";

        public async Task OnGetAsync()
        {
            Categories = await _db.Categories
                .Where(c => !c.Retired)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var query = _db.Items
                .Include(s => s.Category)
                .Where(s => !s.Retired);

            // "Selected" restricts to one group, matching the VFP 'and group=xgroup' clause.
            if (string.Equals(LayoutMode, "Selected", StringComparison.OrdinalIgnoreCase) && CategoryId.HasValue)
                query = query.Where(s => s.CategoryId == CategoryId.Value);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var t = SearchTerm.Trim();
                query = query.Where(s => (s.Name != null && s.Name.Contains(t))
                                      || (s.Code != null && s.Code.Contains(t)));
            }

            // The VFP layout ordered by group first whenever the group column was shown.
            if (Grouped)
                query = OrderBy == "Name"
                    ? query.OrderBy(s => s.Category == null ? 1 : 0).ThenBy(s => s.Category.Name).ThenBy(s => s.Name)
                    : query.OrderBy(s => s.Category == null ? 1 : 0).ThenBy(s => s.Category.Name).ThenBy(s => s.Code);
            else
                query = OrderBy == "Name"
                    ? query.OrderBy(s => s.Name)
                    : query.OrderBy(s => s.Code);

            Rows = await query
                .Select(s => new ServiceListRow
                {
                    Code = s.Code,
                    Name = s.Name,
                    Rate = s.Total ?? 0,
                    RateForeigner = s.TotalForForeigner ?? 0,
                    Group = s.Category == null ? "" : s.Category.Name,
                    Active = !s.Inactive,
                    Requirements = s.Requirements
                })
                .ToListAsync();
        }

        public async Task<IActionResult> OnGetExportAsync()
        {
            await OnGetAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Code,Service,Group,Rate,Foreigner Rate,Status");
            foreach (var r in Rows)
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    Csv(r.Code), Csv(r.Name), Csv(r.Group),
                    r.Rate.ToString("0.00", CultureInfo.InvariantCulture),
                    r.RateForeigner.ToString("0.00", CultureInfo.InvariantCulture),
                    r.Active ? "Active" : "Inactive"
                }));
            }

            return File(new UTF8Encoding(false).GetBytes(sb.ToString()), "text/csv", "ServiceList.csv");
        }

        private static string Csv(string v)
        {
            if (string.IsNullOrEmpty(v)) return "";
            return v.Contains(',') || v.Contains('"')
                ? "\"" + v.Replace("\"", "\"\"") + "\""
                : v;
        }

        public class ServiceListRow
        {
            public string Code { get; set; } = "";
            public string Name { get; set; } = "";
            public string Group { get; set; } = "";
            public string Requirements { get; set; } = "";
            public double Rate { get; set; }
            public double RateForeigner { get; set; }
            public bool Active { get; set; }
        }
    }
}