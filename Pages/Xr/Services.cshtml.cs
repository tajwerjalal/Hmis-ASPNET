using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Xr
{
    public class ServicesModel : HmisWeb.Pages.Shared.PagedPageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;
        private readonly IXrBillingMirrorService _mirrors;

        public ServicesModel(ApplicationDbContext db, IAppSession appSession, IXrBillingMirrorService mirrors)
        {
            _db = db;
            _appSession = appSession;
            _mirrors = mirrors;
        }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        public List<DeptService> Services { get; set; } = new();
        public List<XrServiceCategory> Categories { get; set; } = new();
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string Success { get; set; }

        // Create form. The ultrasound module imported its catalogue from the VFP
        // services.DBF, but there is no X-ray equivalent to import, so X-ray services are
        // entered here by hand and mirrored to the item master on save.
        [BindProperty]
        public string NewName { get; set; }

        [BindProperty]
        public string NewCode { get; set; }

        [BindProperty]
        public double? NewRate { get; set; }

        [BindProperty]
        public long? NewCategoryId { get; set; }

        public async Task OnGetAsync()
        {
            await LoadDataAsync();

            if (TempData["XrServiceSuccess"] != null)
                Success = TempData["XrServiceSuccess"].ToString();
            if (TempData["XrServiceError"] != null)
                ErrorMessage = TempData["XrServiceError"].ToString();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            var name = (NewName ?? "").Trim();
            var code = (NewCode ?? "").Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["XrServiceError"] = "Service name is required.";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                TempData["XrServiceError"] = "Service code is required; it becomes the billing item code.";
                return RedirectToPage();
            }

            if (await _db.XrServices().AnyAsync(s => s.Code == code))
            {
                TempData["XrServiceError"] = $"Code '{code}' is already used by another X-ray service.";
                return RedirectToPage();
            }

            var service = new DeptService
            {
                Name = name,
                Code = code,
                Dept = DeptServiceScopes.XrayDept,
                Rate = (decimal?)NewRate,
                CategoryId = NewCategoryId,
                Retired = false,
                Active = true,
                Billable = true,
                // legacy last-edit stamp trio: USERID / UDATE / UTIME
                StampUser = _appSession.LoggedUser?.UserName ?? "system",
                StampDate = System.DateTime.Now,
                StampTime = System.DateTime.Now.ToString("HH:mm")
            };

            _db.DeptServices.Add(service);
            await _db.SaveChangesAsync();

            // Create the billing item straight away so the service can be indented.
            var mirrored = await _mirrors.EnsureMirrorsAsync(new[] { service.Id });

            var message = $"Service '{service.Name}' created.";
            if (mirrored.Created > 0)
                message += $" Billing item {string.Join(", ", mirrored.CreatedCodes)} created.";
            else
                message += " Billing item could not be created; use Sync Billing Items to retry.";

            TempData["XrServiceSuccess"] = message;
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRetireAsync(long id)
        {
            var service = await _db.DeptServices.FindAsync(id);
            if (service != null)
            {
                service.Retired = true;
                service.RetiredAt = System.DateTime.Now;
                service.StampUser = _appSession.LoggedUser?.UserName ?? "system";
                service.StampDate = System.DateTime.Now;
                service.StampTime = System.DateTime.Now.ToString("HH:mm");
                await _db.SaveChangesAsync();
                TempData["XrServiceSuccess"] = $"Service '{service.Name}' retired successfully.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostActivateAsync(long id)
        {
            var service = await _db.DeptServices.FindAsync(id);
            if (service != null)
            {
                service.Retired = false;
                service.RetiredAt = null;
                service.RetireComments = null;
                service.StampUser = _appSession.LoggedUser?.UserName ?? "system";
                service.StampDate = System.DateTime.Now;
                service.StampTime = System.DateTime.Now.ToString("HH:mm");
                await _db.SaveChangesAsync();

                // An activated service has to be chargeable straight away, so give it its
                // billing item now instead of letting the indent screen refuse the cart later.
                var mirrored = await _mirrors.EnsureMirrorsAsync(new[] { service.Id });

                var message = $"Service '{service.Name}' activated successfully.";
                if (mirrored.Created > 0)
                    message += $" Billing item {string.Join(", ", mirrored.CreatedCodes)} created.";
                foreach (var skipped in mirrored.Skipped)
                    message += " " + skipped + ".";

                TempData["XrServiceSuccess"] = message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostBackfillMirrorsAsync()
        {
            var mirrored = await _mirrors.EnsureAllMissingAsync();

            var message = mirrored.Created > 0
                ? $"{mirrored.Created} billing item(s) created: {string.Join(", ", mirrored.CreatedCodes)}."
                : "Every X-ray service already has a billing item.";

            foreach (var skipped in mirrored.Skipped)
                message += " Skipped " + skipped + ".";

            TempData["XrServiceSuccess"] = message;
            return RedirectToPage();
        }

        private async Task LoadDataAsync()
        {
            var query = _db.XrServices()
                .Include(s => s.Category)
                .Where(s => !s.Retired);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(s =>
                    (s.Name != null && s.Name.ToLower().Contains(term)) ||
                    (s.Code != null && s.Code.ToLower().Contains(term)) ||
                    (s.Sname != null && s.Sname.ToLower().Contains(term))
                );
            }

            TotalCount = await query.CountAsync();
            ClampPaging();
            Services = await ApplyPaging(query
                .OrderBy(s => s.Code)
                .ThenBy(s => s.Name))
                .ToListAsync();

            Categories = await _db.XrServiceCategories
                .Where(c => !c.Retired)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
    }
}
