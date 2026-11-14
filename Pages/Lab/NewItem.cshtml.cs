using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Lab
{
    /// <summary>
    /// Quick "new laboratory test" entry (legacy NewItem helper).
    ///
    /// CONSOLIDATION (22-Oct-2026, Tajwer Jalal): creates the test on the
    /// shared department services master [SERVICES] with DEPT='01' instead of
    /// [ITEM], and lets the lab billing mirror create the INV-LB-{code} row
    /// that bills reference. [ITEM] no longer holds laboratory test masters.
    /// </summary>
    public class NewItemModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IDeptBillingMirrorService _mirrors;
        private readonly IAppSession _session;

        public NewItemModel(ApplicationDbContext db, IDeptBillingMirrorService mirrors, IAppSession session)
        {
            _db = db;
            _mirrors = mirrors;
            _session = session;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public string Name { get; set; } = "";
            public string Code { get; set; } = "";
            public decimal? Price { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.Name))
                return Page();

            var code = Input.Code?.Trim().ToUpper();
            if (!string.IsNullOrWhiteSpace(code) &&
                await _db.LabServices().AnyAsync(s => s.Code == code))
            {
                ModelState.AddModelError(string.Empty, $"Code '{code}' is already used by another laboratory test.");
                return Page();
            }

            var now = DateTime.Now;
            var service = new DeptService
            {
                Name = Input.Name.Trim(),
                Code = code,
                Dept = DeptServiceScopes.LabDept,      // DEPT='01' - laboratory
                Rate = Input.Price,
                RateForeigner = null,
                Billable = true,
                Active = true,
                Retired = false,
                // legacy last-edit stamp trio: USERID / UDATE / UTIME
                StampUser = _session?.LoggedUser?.RealUserName ?? _session?.LoggedUser?.UserName ?? "system",
                StampDate = now,
                StampTime = now.ToString("HH:mm")
            };

            _db.DeptServices.Add(service);
            await _db.SaveChangesAsync();

            // billable straight away: create the INV-LB-{code} billing mirror
            await _mirrors.EnsureMirrorsAsync(new[] { service.Id }, DeptServiceScopes.LabDept);

            return RedirectToPage("/Lab/Investigation");
        }
    }
}
