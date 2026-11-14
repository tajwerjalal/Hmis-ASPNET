using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Corporate
{
    /// <summary>
    /// Corporate Rates - the rate card of one company per department, fed by
    /// the legacy VFP company charge master (compchg.DBF: company + service +
    /// department + rate, reloaded into [COMPANYSERVICERATE] on 22-Oct-2026).
    ///
    /// The screen works like this:
    ///   1. pick a company (the legacy register [COMPANY], joined to
    ///      [INSTITUTION] on Code = ID for the billing key),
    ///   2. pick a department (the legacy DEPT codes of the shared master),
    ///   3. every service of that department is listed with the base rate of
    ///      the master and the rate of the selected company (or a blank cell
    ///      when the company has no rate for it).
    ///
    /// Tables
    ///   [SERVICES]          - the service master of the department
    ///   [COMPANYSERVICERATE] - the company rates (legacy CompanyCode /
    ///                          ServiceCode / DEPT / stamp columns)
    ///   [COMPANY] / [INSTITUTION] - the company register
    ///   [ITEM]              - the billing item of a service (the rate rows
    ///                          reference it, so a new rate needs the id)
    ///
    /// The grid is server-side paged (25/50/100/200 rows).
    ///
    /// Developer : [TAJWER JALAL]
    /// Change Log: 22-Oct-2026 - rewritten from the matrix to the company and
    ///             department selection with the company's rate card.
    /// </summary>
    public class RatesModel : Shared.PagedPageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public RatesModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        /// <summary>A company of the legacy register.</summary>
        public class CompanyOption
        {
            /// <summary>Institution id (the billing key) when the row is linked.</summary>
            public long? Id { get; set; }
            /// <summary>Legacy company code (COMPANY.ID).</summary>
            public string Code { get; set; }
            public string Name { get; set; }
            public string City { get; set; }
        }

        /// <summary>One service of the department with the company's rate.</summary>
        public class RateRow
        {
            public string ServiceCode { get; set; }
            public string ServiceName { get; set; }
            public string Dept { get; set; }
            public double BaseRate { get; set; }
            public double BaseForeignerRate { get; set; }
            /// <summary>The company's rate (null when the company has none).</summary>
            public CompanyServiceRate Rate { get; set; }
            /// <summary>Billing item of the service (mirror first, then the master code).</summary>
            public long? ItemId { get; set; }
        }

        public List<CompanyOption> Companies { get; set; } = new();
        public List<RateRow> Rows { get; set; } = new();
        public List<SelectListItem> DeptFilterList { get; set; } = new();
        public List<SelectListItem> CompanyFilterList { get; set; } = new();
        public List<SelectListItem> ServiceList { get; set; } = new();
        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }

        public int TotalCompaniesWithRates { get; set; }
        public int TotalServicesWithRates { get; set; }
        public int TotalRateConfigurations { get; set; }
        /// <summary>How many services of the department the selected company has a rate for.</summary>
        public int CompanyRateCount { get; set; }

        /// <summary>
        /// True only when BOTH a company and a department are selected. A rate
        /// card is a company x department view: with either side missing the
        /// grid stays empty (previously the page listed every service of the
        /// master with no company rate, and a department-only selection listed
        /// all of its services with an empty rate column).
        /// </summary>
        public bool ShowRates { get; set; }

        /// <summary>Selected company (legacy company code).</summary>
        [BindProperty(SupportsGet = true)]
        public string SelectedCompanyCode { get; set; }

        /// <summary>Selected department (legacy DEPT code).</summary>
        [BindProperty(SupportsGet = true)]
        public string FilterDept { get; set; }

        /// <summary>Free text search over the service code and name.</summary>
        [BindProperty(SupportsGet = true)]
        public string Q { get; set; }

        [BindProperty]
        public RateInputModel RateInput { get; set; } = new();

        public class RateInputModel
        {
            public long? Id { get; set; }
            public long CompanyId { get; set; }
            public long? ItemId { get; set; }
            /// <summary>Legacy company code of the rate row.</summary>
            public string CompanyCode { get; set; }
            /// <summary>Legacy service code of the rate row.</summary>
            public string ServiceCode { get; set; }
            /// <summary>Legacy department code of the rate row.</summary>
            public string Dept { get; set; }
            public decimal Rate { get; set; }
            public decimal? URate { get; set; }
            public decimal? StaffRate { get; set; }
            public decimal? UnitRate { get; set; }
            public decimal? DiscountPercent { get; set; }
            public DateTime? EffectiveFrom { get; set; }
            public DateTime? EffectiveTo { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostSaveRateAsync()
        {
            if (RateInput.CompanyId <= 0)
            {
                ErrorMessage = "Company is required.";
                await LoadAsync();
                return Page();
            }

            var companyCode = RateInput.CompanyCode;
            if (string.IsNullOrWhiteSpace(companyCode))
            {
                // resolve the legacy company code from the institution that
                // backs the selected company - a NULL code would fall outside
                // the unique legacy key and hide the rate from the rate card
                companyCode = await _db.Institutions
                    .Where(i => i.Id == RateInput.CompanyId)
                    .Select(i => i.Code)
                    .FirstOrDefaultAsync();
            }
            if (string.IsNullOrWhiteSpace(companyCode))
            {
                // last resort: the code already stored for this company
                companyCode = await _db.CompanyServiceRates
                    .Where(r => r.CompanyId == RateInput.CompanyId
                                && r.CompanyCode != null && r.CompanyCode != "")
                    .OrderByDescending(r => r.EditedAt ?? r.CreatedAt)
                    .Select(r => r.CompanyCode)
                    .FirstOrDefaultAsync();
            }

            if (string.IsNullOrWhiteSpace(RateInput.ServiceCode))
            {
                ErrorMessage = "Service is required.";
                await LoadAsync();
                return Page();
            }

            // one live rate per company + service + department (the shape of
            // the filtered unique index) - never match on the service alone,
            // the same legacy code can exist in two departments
            var deptCode = RateInput.Dept ?? string.Empty;
            var existing = await _db.CompanyServiceRates
                .FirstOrDefaultAsync(r => r.CompanyId == RateInput.CompanyId
                                         && r.ServiceCode == RateInput.ServiceCode
                                         && (r.DEPT ?? "") == deptCode
                                         && !r.Retired);

            if (existing == null && RateInput.Id.HasValue)
                existing = await _db.CompanyServiceRates.FindAsync(RateInput.Id.Value);

            if (existing != null)
            {
                existing.Rate = RateInput.Rate;
                existing.URate = RateInput.URate;
                existing.StaffRate = RateInput.StaffRate;
                existing.UnitRate = RateInput.UnitRate;
                existing.DiscountPercent = RateInput.DiscountPercent;
                existing.EffectiveFrom = RateInput.EffectiveFrom;
                existing.EffectiveTo = RateInput.EffectiveTo;
                existing.ItemId = RateInput.ItemId ?? existing.ItemId;
                existing.CompanyCode = companyCode;
                existing.ServiceCode = RateInput.ServiceCode;
                existing.DEPT = RateInput.Dept ?? existing.DEPT;
                existing.EditerId = _session.WebUserId;
                existing.EditedAt = DateTime.Now;
            }
            else
            {
                _db.CompanyServiceRates.Add(new CompanyServiceRate
                {
                    CompanyId = RateInput.CompanyId,
                    ItemId = RateInput.ItemId,
                    CompanyCode = companyCode,
                    ServiceCode = RateInput.ServiceCode,
                    DEPT = RateInput.Dept,
                    Rate = RateInput.Rate,
                    URate = RateInput.URate,
                    StaffRate = RateInput.StaffRate,
                    UnitRate = RateInput.UnitRate,
                    DiscountPercent = RateInput.DiscountPercent,
                    EffectiveFrom = RateInput.EffectiveFrom,
                    EffectiveTo = RateInput.EffectiveTo,
                    Retired = false,
                    CreatedAt = DateTime.Now,
                    CreaterId = _session.WebUserId
                });
            }

            try
            {
                await _db.SaveChangesAsync();
                SuccessMessage = $"Rate for {RateInput.ServiceCode} saved.";
            }
            catch (DbUpdateException)
            {
                // the filtered unique index allows one live rate per company +
                // service + department - report the clash instead of a 500
                ErrorMessage = "That company, service and department already has a live rate. "
                             + "Retire the old rate first or pick the other department.";
            }
            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteRateAsync(long id)
        {
            var rate = await _db.CompanyServiceRates.FindAsync(id);
            if (rate != null)
            {
                rate.Retired = true;

                rate.RetireComments = "Retired from Corporate Rates.";
                await _db.SaveChangesAsync();
                SuccessMessage = "Rate retired.";
            }
            await LoadAsync();
            return Page();
        }

        private async Task LoadAsync()
        {
            // companies of the legacy register
            var companyRows = await _db.CompanyMasters
                .Where(c => c.Id != null && c.Id != "")
                .OrderBy(c => c.Name)
                .ToListAsync();

            var institutions = await _db.Institutions
                .Where(i => i.Code != null)
                .Select(i => new { i.Id, i.Code })
                .ToListAsync();
            var instByCode = institutions.ToDictionary(i => i.Code, i => i.Id, StringComparer.OrdinalIgnoreCase);

            Companies = companyRows.Select(c => new CompanyOption
            {
                Id = instByCode.TryGetValue(c.Id ?? "", out var id) ? id : (long?)null,
                Code = c.Id ?? "",
                Name = c.Name ?? "",
                City = c.City ?? ""
            }).ToList();

            CompanyFilterList = Companies
                .Select(c => new SelectListItem { Value = c.Code, Text = string.IsNullOrEmpty(c.Name) ? c.Code : $"{c.Code} - {c.Name}" })
                .ToList();

            DeptFilterList = DeptServiceScopes.AllDepts
                .Select(d => new SelectListItem
                {
                    Value = d,
                    Text = $"{DeptServiceScopes.HospitalDeptName(d)} ({d})"
                })
                .ToList();

            // the services of the selected department, straight from the master
            var dept = DeptServiceScopes.AllDepts.Contains(FilterDept ?? string.Empty) ? FilterDept : null;
            var hasCompany = !string.IsNullOrWhiteSpace(SelectedCompanyCode);

            // a rate belongs to one company AND one department - with either
            // side missing there is nothing to show
            ShowRates = hasCompany && dept != null;

            // page counters (independent of the selection)
            TotalCompaniesWithRates = await _db.CompanyServiceRates
                .Where(r => !r.Retired && r.CompanyId != null)
                .Select(r => r.CompanyId)
                .Distinct()
                .CountAsync();
            TotalServicesWithRates = await _db.CompanyServiceRates
                .Where(r => !r.Retired && r.ItemId != null)
                .Select(r => r.ItemId)
                .Distinct()
                .CountAsync();
            TotalRateConfigurations = await _db.CompanyServiceRates.CountAsync(r => !r.Retired);

            if (!ShowRates)
            {
                // no company or no department: no grid at all
                Rows = new List<RateRow>();
                ServiceList = new List<SelectListItem>();
                TotalCount = 0;
                CompanyRateCount = 0;
                return;
            }

            var serviceQuery = _db.DeptServices.Where(s => s.Code != null && s.Code != "");
            serviceQuery = serviceQuery.Where(s => s.Dept == dept);

            if (!string.IsNullOrWhiteSpace(Q))
            {
                var term = Q.Trim().ToLower();
                serviceQuery = serviceQuery.Where(s =>
                    (s.Name != null && s.Name.ToLower().Contains(term))
                    || (s.Code != null && s.Code.ToLower().Contains(term)));
            }

            var serviceCount = await serviceQuery.CountAsync();
            TotalCount = serviceCount;
            ClampPaging();

            var services = await ApplyPaging(serviceQuery
                .OrderBy(s => s.Code))
                .ToListAsync();

            // the rates of the selected company IN the selected department,
            // keyed by the legacy service code
            var rates = new Dictionary<string, CompanyServiceRate>(StringComparer.OrdinalIgnoreCase);
            var companyRates = await _db.CompanyServiceRates
                .Where(r => !r.Retired
                            && r.CompanyCode == SelectedCompanyCode
                            && r.DEPT == dept)
                .ToListAsync();

            foreach (var r in companyRates)
            {
                if (!string.IsNullOrWhiteSpace(r.ServiceCode))
                    rates[r.ServiceCode] = r;
            }
            CompanyRateCount = companyRates.Count;

            // the billing item of the services (the mirror first)
            var codes = services.Select(s => s.Code).ToList();
            var prefix = dept != null ? DeptBillingMirrorService.PrefixFor(dept) : null;
            var itemRows = await _db.Items
                .Where(i => i.Code != null
                            && (codes.Contains(i.Code) || (prefix != null && codes.Contains(prefix + i.Code))))
                .Select(i => new { i.Id, i.Code })
                .ToListAsync();

            var itemByCode = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            foreach (var i in itemRows)
            {
                if (prefix != null && i.Code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    var rest = i.Code.Substring(prefix.Length);
                    if (!itemByCode.ContainsKey(rest)) itemByCode[rest] = i.Id;
                }
                else if (!itemByCode.ContainsKey(i.Code))
                {
                    itemByCode[i.Code] = i.Id;
                }
            }

            Rows = services.Select(s =>
            {
                rates.TryGetValue(s.Code ?? "", out var rate);
                itemByCode.TryGetValue(s.Code ?? "", out var itemId);
                return new RateRow
                {
                    ServiceCode = s.Code ?? "",
                    ServiceName = s.Name ?? "",
                    Dept = s.Dept ?? "",
                    BaseRate = (double)(s.Rate ?? 0m),
                    BaseForeignerRate = (double)(s.RateForeigner ?? 0m),
                    Rate = rate,
                    ItemId = itemId == 0 ? (long?)null : itemId
                };
            }).ToList();

            // the service picker of the popup (the department of the selection)
            var pickerQuery = _db.DeptServices.Where(s => s.Code != null && s.Code != "");
            if (dept != null)
                pickerQuery = pickerQuery.Where(s => s.Dept == dept);

            ServiceList = await pickerQuery
                .OrderBy(s => s.Code)
                .Select(s => new SelectListItem { Value = s.Code, Text = $"{s.Code} - {s.Name}" })
                .ToListAsync();
        }
    }
}
