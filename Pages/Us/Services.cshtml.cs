using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Us
{
    public class ServicesModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;
        private readonly IUsBillingMirrorService _mirrors;

        public ServicesModel(ApplicationDbContext db, IAppSession appSession, IUsBillingMirrorService mirrors)
        {
            _db = db;
            _appSession = appSession;
            _mirrors = mirrors;
        }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        public List<UsService> Services { get; set; } = new();
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string Success { get; set; }

        public async Task OnGetAsync()
        {
            await LoadDataAsync();

            if (TempData["UsServiceSuccess"] != null)
                Success = TempData["UsServiceSuccess"].ToString();
        }

        public async Task<IActionResult> OnPostRetireAsync(long id)
        {
            var service = await _db.UsServices.FindAsync(id);
            if (service != null)
            {
                service.Retired = true;
                service.RetiredAt = System.DateTime.Now;
                service.RetirerId = _appSession.WebUserId;
                await _db.SaveChangesAsync();
                TempData["UsServiceSuccess"] = $"Service '{service.Name}' retired successfully.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostActivateAsync(long id)
        {
            var service = await _db.UsServices.FindAsync(id);
            if (service != null)
            {
                service.Retired = false;
                service.RetiredAt = null;
                service.RetirerId = null;
                service.EditerId = _appSession.WebUserId;
                service.EditedAt = System.DateTime.Now;
                await _db.SaveChangesAsync();

                // An activated service has to be chargeable straight away, so give it its
                // billing item now instead of letting the indent screen refuse the cart later.
                var mirrored = await _mirrors.EnsureMirrorsAsync(new[] { service.Id });

                var message = $"Service '{service.Name}' activated successfully.";
                if (mirrored.Created > 0)
                    message += $" Billing item {string.Join(", ", mirrored.CreatedCodes)} created.";
                foreach (var skipped in mirrored.Skipped)
                    message += " " + skipped + ".";

                TempData["UsServiceSuccess"] = message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostBackfillMirrorsAsync()
        {
            var mirrored = await _mirrors.EnsureAllMissingAsync();

            var message = mirrored.Created > 0
                ? $"{mirrored.Created} billing item(s) created: {string.Join(", ", mirrored.CreatedCodes)}."
                : "Every ultrasound service already has a billing item.";

            foreach (var skipped in mirrored.Skipped)
                message += " Skipped " + skipped + ".";

            TempData["UsServiceSuccess"] = message;
            return RedirectToPage();
        }

        private async Task LoadDataAsync()
        {
            var query = _db.UsServices
                .Include(s => s.Category)
                .Where(s => !s.Retired);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(s =>
                    (s.Name != null && s.Name.ToLower().Contains(term)) ||
                    (s.Code != null && s.Code.ToLower().Contains(term)) ||
                    (s.Description != null && s.Description.ToLower().Contains(term))
                );
            }

            Services = await query
                .OrderBy(s => s.Code)
                .ThenBy(s => s.Name)
                .ToListAsync();
        }
    }
}
