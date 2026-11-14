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

namespace HmisWeb.Pages.Lab
{
    /// <summary>
    /// Lab Test Setup - the master list of laboratory tests.
    /// Mirrors the VFP Labsys form <c>lb0301</c> (test master), which stored one row per
    /// test with its rates, durations and specimen requirements.
    ///
    /// CONSOLIDATION (22-Oct-2026, Tajwer Jalal): the tests now live on the ONE
    /// canonical department services master [HMIS].[dbo].[SERVICES] with the
    /// legacy DEPT column = '01' (laboratory), reached through
    /// <see cref="DeptServiceScopes.LabServices"/>. The /lab/TestSetup page
    /// stops reading [ITEM] entirely - its Investigation/Service rows were
    /// dropped in the LabItemsPurge cleanup so [ITEM] stays the billing /
    /// medicine master only.
    ///
    /// Column mapping onto the legacy master: Code->CODE, Name->DESC,
    /// Sname->SNAME, Rate->SRATE, RateForeigner->URATE, Requirements->PRES,
    /// Min/MaxDuration->MINDUR/MAXDUR, ConsultantRequired->CONREQ.
    /// Last-edit stamp trio: USERID/UDATE/UTIME; retire kit: RETIRED/RETIREDAT.
    ///
    /// Developer: [TAJWER JALAL]
    /// Change Log: 22-Oct-2026 - rewritten from [ITEM] onto [SERVICES] DEPT='01'.
    /// </summary>
    public class TestSetupModel : HmisWeb.Pages.Shared.PagedPageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;
        private readonly IDeptBillingMirrorService _mirrors;

        public TestSetupModel(ApplicationDbContext db, IAppSession session, IDeptBillingMirrorService mirrors)
        {
            _db = db;
            _session = session;
            _mirrors = mirrors;
        }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Status { get; set; } = "active";

        [BindProperty]
        public TestInput Input { get; set; } = new();

        public List<DeptService> Tests { get; set; } = new();
        public string Success { get; set; }
        public string ErrorMessage { get; set; }
        public int ActiveCount { get; set; }
        public int RetiredCount { get; set; }

        public async Task OnGetAsync()
        {
            await LoadTestsAsync();
            Success = TempData["TestSetupSuccess"] as string;
            ErrorMessage = TempData["TestSetupError"] as string;

            if (TryGetId("edit", out var editId) && editId > 0)
            {
                var service = await _db.LabServices().FirstOrDefaultAsync(s => s.Id == editId);
                if (service != null)
                {
                    Input = new TestInput
                    {
                        Id = service.Id,
                        Code = service.Code,
                        Name = service.Name,
                        Sname = service.Sname,
                        Rate = service.Rate,
                        RateForeigner = service.RateForeigner,
                        Requirements = service.Requirements,
                        MinDuration = service.MinDuration,
                        MaxDuration = service.MaxDuration,
                        ConsultantRequired = service.ConsultantRequired,
                        Billable = service.Billable,
                        ShowForm = true
                    };
                }
            }

            if (Request.Query.ContainsKey("add"))
                Input.ShowForm = true;
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.Name))
            {
                TempData["TestSetupError"] = "Test name is required.";
                return RedirectToPage(new { SearchTerm, Status });
            }

            var name = Input.Name.Trim();
            var code = Input.Code?.Trim().ToUpper();
            var now = System.DateTime.Now;

            if (Input.Id > 0)
            {
                var service = await _db.LabServices().FirstOrDefaultAsync(s => s.Id == Input.Id);
                if (service == null)
                {
                    TempData["TestSetupError"] = "That test no longer exists.";
                    return RedirectToPage(new { SearchTerm, Status });
                }

                // a duplicated code would collide in the shared master
                if (!string.IsNullOrWhiteSpace(code) &&
                    await _db.LabServices().AnyAsync(s => s.Code == code && s.Id != service.Id))
                {
                    TempData["TestSetupError"] = $"Code '{code}' is already used by another laboratory test.";
                    return RedirectToPage(new { SearchTerm, Status });
                }

                service.Code = code;
                service.Name = name;
                service.Sname = Input.Sname?.Trim();
                service.Rate = Input.Rate;
                service.RateForeigner = Input.RateForeigner;
                service.Requirements = Input.Requirements?.Trim();
                service.MinDuration = Input.MinDuration?.Trim();
                service.MaxDuration = Input.MaxDuration?.Trim();
                service.ConsultantRequired = Input.ConsultantRequired;
                service.Billable = Input.Billable;
                // legacy last-edit stamp trio: USERID / UDATE / UTIME
                service.StampUser = (_session.LoggedUser?.RealUserName ?? _session.LoggedUser?.UserName ?? "system");
                service.StampDate = now;
                service.StampTime = now.ToString("HH:mm");

                await _db.SaveChangesAsync();
                TempData["TestSetupSuccess"] = $"Test '{service.Name}' updated.";
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(code) &&
                    await _db.LabServices().AnyAsync(s => s.Code == code))
                {
                    TempData["TestSetupError"] = $"Code '{code}' is already used by another laboratory test.";
                    return RedirectToPage(new { SearchTerm, Status });
                }

                var service = new DeptService
                {
                    Code = code,
                    Name = name,
                    Sname = Input.Sname?.Trim(),
                    Dept = DeptServiceScopes.LabDept,          // DEPT='01' - laboratory
                    Rate = Input.Rate,
                    RateForeigner = Input.RateForeigner,
                    Requirements = Input.Requirements?.Trim(),
                    MinDuration = Input.MinDuration?.Trim(),
                    MaxDuration = Input.MaxDuration?.Trim(),
                    ConsultantRequired = Input.ConsultantRequired,
                    Billable = Input.Billable,
                    Retired = false,
                    Active = true,
                    // legacy last-edit stamp trio: USERID / UDATE / UTIME
                    StampUser = _session.LoggedUser?.RealUserName
                                ?? _session.LoggedUser?.UserName ?? "system",
                    StampDate = now,
                    StampTime = now.ToString("HH:mm")
                };

                _db.DeptServices.Add(service);
                await _db.SaveChangesAsync();
                await _mirrors.EnsureMirrorsAsync(new[] { service.Id }, DeptServiceScopes.LabDept);
                TempData["TestSetupSuccess"] = $"Test '{service.Name}' added.";
            }

            return RedirectToPage(new { SearchTerm, Status });
        }

        public async Task<IActionResult> OnPostRetireAsync(long id)
        {
            var service = await _db.LabServices().FirstOrDefaultAsync(s => s.Id == id);
            if (service != null)
            {
                var now = System.DateTime.Now;
                service.Retired = true;
                service.RetiredAt = now;
                service.StampUser = (_session.LoggedUser?.RealUserName ?? _session.LoggedUser?.UserName ?? "system");
                service.StampDate = now;
                service.StampTime = now.ToString("HH:mm");
                await _db.SaveChangesAsync();
                TempData["TestSetupSuccess"] = $"Test '{service.Name}' retired.";
            }
            return RedirectToPage(new { SearchTerm, Status });
        }

        public async Task<IActionResult> OnPostActivateAsync(long id)
        {
            var service = await _db.LabServices().FirstOrDefaultAsync(s => s.Id == id);
            if (service != null)
            {
                var now = System.DateTime.Now;
                service.Retired = false;
                service.RetiredAt = null;
                service.StampUser = (_session.LoggedUser?.RealUserName ?? _session.LoggedUser?.UserName ?? "system");
                service.StampDate = now;
                service.StampTime = now.ToString("HH:mm");
                await _db.SaveChangesAsync();
                await _mirrors.EnsureMirrorsAsync(new[] { service.Id }, DeptServiceScopes.LabDept);
                TempData["TestSetupSuccess"] = $"Test '{service.Name}' activated.";
            }
            return RedirectToPage(new { SearchTerm, Status });
        }

        private async Task LoadTestsAsync()
        {
            var baseQuery = _db.LabServices();

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
            Tests = await ApplyPaging(query
                .OrderBy(s => s.Code)
                .ThenBy(s => s.Name))
                .ToListAsync();
        }

        private bool TryGetId(string key, out long id)
        {
            id = 0;
            if (Request.Query.TryGetValue(key, out var raw))
                return long.TryParse(raw, out id);
            return false;
        }

        public class TestInput
        {
            public long Id { get; set; }
            public string Code { get; set; }
            public string Name { get; set; }
            public string Sname { get; set; }
            public decimal? Rate { get; set; }
            public decimal? RateForeigner { get; set; }
            public string Requirements { get; set; }
            public string MinDuration { get; set; }
            public string MaxDuration { get; set; }
            public string ConsultantRequired { get; set; }
            public bool Billable { get; set; } = true;
            public bool ShowForm { get; set; }
        }
    }
}
