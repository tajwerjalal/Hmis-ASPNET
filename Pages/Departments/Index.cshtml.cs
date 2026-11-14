using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Departments
{
    /// <summary>
    /// Department master - /departments/Index.
    ///
    /// The page now lists every department of the hospital, including the nine
    /// legacy departments imported from E:\Projects\HMC\Master\dept.dbf
    /// (LABORATORY SERVICES, X-RAY/ CT SCAN SERVICES, ULTRA SOUND, DENTAL CLINIC,
    /// OPERATION THEATRE, HOSPITALIZATION, PHARMACY, CONSULTANCY &amp; PROCEDURE,
    /// MISC EXPENSES) on top of the four application departments that already
    /// existed. Rows are paged and filtered by name / code, and writes need the
    /// MASTER_DEPARTMENT_EDIT privilege (or ALL).
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 22-Oct-2026
    /// Time      : 17:10
    /// Change Log: 22-Oct-2026 - paged + filtered the list and gated writes,
    ///             legacy departments imported from dept.dbf.
    /// </summary>
    public class IndexModel : Shared.PagedPageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public IndexModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        public List<Department> Departments { get; set; } = new();

        /// <summary>Filter: name or code fragment.</summary>
        [BindProperty(SupportsGet = true)]
        public string Q { get; set; }

        /// <summary>Shows retired departments together with the live ones.</summary>
        [BindProperty(SupportsGet = true)]
        public bool ShowRetired { get; set; }

        public bool CanEdit => _session.HasPrivilege("MASTER_DEPARTMENT_EDIT")
                               || _session.HasPrivilege("ALL");

        public async Task<IActionResult> OnGetAsync()
        {
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");

            await LoadAsync();
            return Page();
        }

        private async Task LoadAsync()
        {
            var query = _db.Departments
                .Include(d => d.Institution)
                .AsQueryable();

            if (!ShowRetired)
                query = query.Where(d => !d.Retired);

            if (!string.IsNullOrWhiteSpace(Q))
            {
                var term = Q.Trim();
                query = query.Where(d =>
                    (d.Name != null && d.Name.Contains(term))
                    || (d.Code != null && d.Code.Contains(term))
                    || (d.Sname != null && d.Sname.Contains(term)));
            }

            TotalCount = await query.CountAsync();
            ClampPaging();

            Departments = await ApplyPaging(query
                .OrderBy(d => d.Name))
                .ToListAsync();
        }
    }
}
