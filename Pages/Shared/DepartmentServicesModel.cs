using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Shared
{
    /// <summary>
    /// Shared base for the per-department service setup pages
    /// (/hospital/Services, /cnp/Services, /dental/Services, /ot/Services).
    ///
    /// All of them manage the SAME canonical master [HMIS].[dbo].[SERVICES];
    /// each page simply scopes itself to the legacy DEPT codes of its own
    /// department (see <see cref="HmisWeb.Services.DeptServiceScopes"/>), so a
    /// hospital page never shows dental or operation-theatre rows.
    ///
    /// Features (identical for every module):
    ///   * paged grid (25/50/100/200 rows) with dept filter and search
    ///   * add / edit popup form, retire (soft) and activate
    ///   * legacy audit stamp trio USERID / UDATE / UTIME on every write
    ///
    /// Developer : [TAJWER JALAL]
    /// Created   : 22-Oct-2026 19:45
    /// Change Log: 22-Oct-2026 19:45 - created from the first hospital page
    ///             so dental, OT and consultancy reuse the same behaviour.
    /// </summary>
    public abstract class DepartmentServicesModel : PagedPageModel
    {
        protected readonly ApplicationDbContext Db;
        protected readonly IAppSession Session;

        protected DepartmentServicesModel(ApplicationDbContext db, IAppSession session)
        {
            Db = db;
            Session = session;
        }

        /// <summary>Heading shown on the page.</summary>
        public abstract string ModuleTitle { get; }

        /// <summary>Legacy DEPT codes this module owns.</summary>
        public abstract string[] AllowedDepts { get; }

        /// <summary>Scoped query over the shared master.</summary>
        protected abstract IQueryable<DeptService> Scoped();

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string DeptFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Status { get; set; } = "active";


        /// <summary>Id of the service being edited (0 = new service).</summary>
        [BindProperty(SupportsGet = true)]
        public long EditId { get; set; }

        /// <summary>The service currently loaded in the edit form.</summary>
        public DeptService Editing { get; set; }



        public List<DeptService> Services { get; set; } = new();

        /// <summary>Department options (code + display name).</summary>
        public List<(string Code, string Name)> Departments => AllowedDepts
            .Select(d => (d, HmisWeb.Services.DeptServiceScopes.HospitalDeptName(d)))
            .ToList();

        public string Success { get; set; }
        public string ErrorMessage { get; set; }
        public int ActiveCount { get; set; }
        public int RetiredCount { get; set; }

        public async Task OnGetAsync()
        {
            await LoadAsync();
            Success = TempData["DepartmentServiceSuccess"] as string;
            ErrorMessage = TempData["DepartmentServiceError"] as string;

            if (EditId > 0)
            {
                // the edited row may sit on another page - load it directly
                Editing = await Scoped().FirstOrDefaultAsync(s => s.Id == EditId);
            }
        }

        public async Task<IActionResult> OnPostSaveAsync(long id, string name, string code, string sname,
            string dept, decimal? rate, decimal? rateForeigner, string requirements)
        {
            await LoadAsync();

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["DepartmentServiceError"] = "Service name is required.";
                return Back();
            }

            if (!AllowedDepts.Contains(dept))
            {
                TempData["DepartmentServiceError"] = "Pick one of the department codes of this module.";
                return Back();
            }

            var now = DateTime.Now;
            var trimmed = name.Trim();
            var normalizedCode = string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();

            if (normalizedCode != null &&
                await Scoped().AnyAsync(s => s.Code == normalizedCode && s.Id != id))
            {
                TempData["DepartmentServiceError"] = $"Code '{normalizedCode}' is already used by another service.";
                return Back();
            }

            DeptService service;
            if (id > 0)
            {
                service = await Scoped().FirstOrDefaultAsync(s => s.Id == id);
                if (service == null)
                {
                    TempData["DepartmentServiceError"] = "That service no longer exists.";
                    return Back();
                }
            }
            else
            {
                service = new DeptService { Retired = false, Active = true, Billable = true };
                Db.DeptServices.Add(service);
            }

            service.Name = trimmed;
            service.Code = normalizedCode;
            service.Sname = string.IsNullOrWhiteSpace(sname) ? null : sname.Trim();
            service.Dept = dept;
            service.Rate = rate;
            service.RateForeigner = rateForeigner;
            service.Requirements = string.IsNullOrWhiteSpace(requirements) ? null : requirements.Trim();
            // legacy last-edit stamp trio: USERID / UDATE / UTIME
            service.StampUser = Session.LoggedUser?.RealUserName ?? Session.LoggedUser?.UserName ?? "system";
            service.StampDate = now;
            service.StampTime = now.ToString("HH:mm");

            await Db.SaveChangesAsync();
            TempData["DepartmentServiceSuccess"] = id > 0
                ? $"Service '{service.Name}' updated."
                : $"Service '{service.Name}' added.";
            return Back();
        }

        public async Task<IActionResult> OnPostRetireAsync(long id)
        {
            var service = await Scoped().FirstOrDefaultAsync(s => s.Id == id);
            if (service != null)
            {
                var now = DateTime.Now;
                service.Retired = true;
                service.RetiredAt = now;
                service.StampUser = Session.LoggedUser?.RealUserName ?? Session.LoggedUser?.UserName ?? "system";
                service.StampDate = now;
                service.StampTime = now.ToString("HH:mm");
                await Db.SaveChangesAsync();
                TempData["DepartmentServiceSuccess"] = $"Service '{service.Name}' retired.";
            }
            return Back();
        }

        public async Task<IActionResult> OnPostActivateAsync(long id)
        {
            var service = await Scoped().FirstOrDefaultAsync(s => s.Id == id);
            if (service != null)
            {
                var now = DateTime.Now;
                service.Retired = false;
                service.RetiredAt = null;
                service.StampUser = Session.LoggedUser?.RealUserName ?? Session.LoggedUser?.UserName ?? "system";
                service.StampDate = now;
                service.StampTime = now.ToString("HH:mm");
                await Db.SaveChangesAsync();
                TempData["DepartmentServiceSuccess"] = $"Service '{service.Name}' activated.";
            }
            return Back();
        }

        protected async Task LoadAsync()
        {
            var baseQuery = Scoped();

            ActiveCount = await baseQuery.CountAsync(s => !s.Retired);
            RetiredCount = await baseQuery.CountAsync(s => s.Retired);

            var query = baseQuery;

            switch (Status)
            {
                case "retired":
                    query = query.Where(s => s.Retired);
                    break;
                case "all":
                    break;
                default:
                    query = query.Where(s => !s.Retired);
                    break;
            }

            if (!string.IsNullOrWhiteSpace(DeptFilter) && AllowedDepts.Contains(DeptFilter))
                query = query.Where(s => s.Dept == DeptFilter);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(s =>
                    (s.Name != null && s.Name.ToLower().Contains(term)) ||
                    (s.Sname != null && s.Sname.ToLower().Contains(term)) ||
                    (s.Code != null && s.Code.ToLower().Contains(term)));
            }

            TotalCount = await query.CountAsync();
            ClampPaging();

            Services = await ApplyPaging(query
                .OrderBy(s => s.Dept)
                .ThenBy(s => s.Code)
                .ThenBy(s => s.Name))
                .ToListAsync();
        }

        /// <summary>
        /// Returns to the same module page with the current filter / pager state.
        /// A base page model has no page of its own, so the redirect is built
        /// from the current request path (the module URL) instead of
        /// RedirectToPage() - that call fails with "No page named ''".
        /// </summary>
        protected IActionResult Back()
        {
            var query = new List<string>
            {
                "SearchTerm=" + Uri.EscapeDataString(SearchTerm ?? string.Empty),
                "DeptFilter=" + Uri.EscapeDataString(DeptFilter ?? string.Empty),
                "Status=" + Uri.EscapeDataString(Status ?? "active"),
                "PageNumber=" + PageNumber.ToString(),
                "PageSize=" + PageSize.ToString()
            };
            return LocalRedirect(Request.Path + "?" + string.Join("&", query));
        }
    }
}
