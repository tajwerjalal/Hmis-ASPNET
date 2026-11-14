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
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Corporate
{
    /// <summary>
    /// Corporate Companies - the company register of the legacy VFP master
    /// [HMIS].[dbo].[COMPANY] (ID, NAME, ADDRESS, CITY, ATTN, DESG, PHONE,
    /// BILLMODE, STATUS, USERID/UDATE/UTIME).
    ///
    /// The screen used to read [INSTITUTION] filtered by
    /// InstitutionType == InsuranceCompany, but every migrated institution row
    /// carries type Hospital - the list was therefore empty. It now lists the
    /// real company master and joins [INSTITUTION] on Code = ID for the
    /// registration number, the e-mail and the company service rates (the
    /// billing engine keys those by the INSTITUTION id).
    ///
    /// Writes stay dual: [INSTITUTION] keeps the row the billing engine
    /// references, and the matching [COMPANY] row is written as well so the
    /// register and the app master stay aligned.
    ///
    /// The grid is server-side paged (25/50/100/200 rows) through the shared
    /// <see cref="HmisWeb.Pages.Shared.PagedPageModel"/> base.
    ///
    /// Developer : [TAJWER JALAL]
    /// Change Log: 22-Oct-2026 - switched the list to [COMPANY] and added
    ///             pagination.
    /// </summary>
    public class CompaniesModel : Shared.PagedPageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;

        public CompaniesModel(ApplicationDbContext db, IAppSession appSession)
        {
            _db = db;
            _appSession = appSession;
        }

        public List<CompanyCard> Companies { get; set; } = new();
        public int ActiveCount { get; set; }
        public int RetiredCount { get; set; }

        /// <summary>Service rates summed over ALL matching rows (not just the page).</summary>
        public int TotalServiceRates { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Q { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool ShowRetired { get; set; }

        [BindProperty]
        public Institution FormCompany { get; set; } = new();

        /// <summary>Company code of the row being edited (legacy COMPANY.ID).</summary>
        [BindProperty]
        public string EditCode { get; set; }

        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }

        public class CompanyCard
        {
            /// <summary>Company code - legacy COMPANY.ID (the action key).</summary>
            public string Code { get; set; }
            /// <summary>Linked INSTITUTION row (billing master) when one exists.</summary>
            public long? InstitutionId { get; set; }
            public string Name { get; set; }
            public string RegNo { get; set; }
            public string Phone { get; set; }
            public string Email { get; set; }
            public string Address { get; set; }
            public string City { get; set; }
            public string Attention { get; set; }
            public string AttentionDesignation { get; set; }
            public string BillMode { get; set; }
            public bool Retired { get; set; }
            public int ServiceRateCount { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadListAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            FormCompany.InstitutionType = InstitutionType.InsuranceCompany;
            FormCompany.Retired = false;
            FormCompany.CreatedAt = DateTime.Now;
            FormCompany.CreaterId = _appSession.WebUserId;

            if (string.IsNullOrWhiteSpace(FormCompany.Name))
            {
                ErrorMessage = "Company name is required.";
                await LoadListAsync();
                return Page();
            }

            var nextCode = await GenerateCompanyCodeAsync();
            FormCompany.Code = nextCode;

            _db.Institutions.Add(FormCompany);
            await _db.SaveChangesAsync();

            // keep the legacy company master in step with the app master
            await UpsertCompanyMasterAsync(nextCode, FormCompany.Name, FormCompany.Address, FormCompany.City,
                FormCompany.Attention, FormCompany.AttentionDesignation, FormCompany.Phone, "A");

            SuccessMessage = $"Company \"{FormCompany.Name}\" created with code {nextCode}.";
            FormCompany = new Institution();
            await LoadListAsync();
            return Page();
        }

        private async Task<string> GenerateCompanyCodeAsync()
        {
            // next free CO-nnnn code, checked against both masters
            var used = await _db.CompanyMasters
                .Select(c => c.Id)
                .Concat(_db.Institutions.Select(i => i.Code))
                .Where(c => c != null && c.StartsWith("CO-"))
                .ToListAsync();

            var next = 1;
            while (used.Contains($"CO-{next:D4}")) next++;
            return $"CO-{next:D4}";
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (string.IsNullOrWhiteSpace(EditCode))
            {
                ErrorMessage = "Company not found.";
                await LoadListAsync();
                return Page();
            }

            var company = await _db.Institutions.FirstOrDefaultAsync(i => i.Code == EditCode);
            if (company == null)
            {
                // the legacy register row has no app counterpart yet - create it
                company = new Institution
                {
                    Code = EditCode,
                    InstitutionType = InstitutionType.InsuranceCompany,
                    CreatedAt = DateTime.Now,
                    CreaterId = _appSession.WebUserId
                };
                _db.Institutions.Add(company);
            }

            company.Name = FormCompany.Name;
            company.Sname = FormCompany.Sname;
            company.RegNo = FormCompany.RegNo;
            company.Phone = FormCompany.Phone;
            company.Fax = FormCompany.Fax;
            company.Email = FormCompany.Email;
            company.Website = FormCompany.Website;
            company.Address = FormCompany.Address;
            company.City = FormCompany.City;
            company.Attention = FormCompany.Attention;
            company.AttentionDesignation = FormCompany.AttentionDesignation;
            company.PrintingName = FormCompany.PrintingName;
            company.EditerId = _appSession.WebUserId;
            company.EditedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            await UpsertCompanyMasterAsync(EditCode, FormCompany.Name, FormCompany.Address, FormCompany.City,
                FormCompany.Attention, FormCompany.AttentionDesignation, FormCompany.Phone,
                company.Retired ? "I" : "A");

            SuccessMessage = $"Company \"{company.Name}\" updated successfully.";
            await LoadListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostRetireAsync(string code)
        {
            var company = await _db.Institutions.FirstOrDefaultAsync(i => i.Code == code);
            if (company == null)
            {
                ErrorMessage = "Company not found.";
                await LoadListAsync();
                return Page();
            }

            company.Retired = true;
            company.RetiredAt = DateTime.Now;
            company.RetirerId = _appSession.WebUserId;
            company.RetireComments = "Retired from Corporate Companies page.";

            await _db.SaveChangesAsync();
            await SetCompanyStatusAsync(code, "I");

            SuccessMessage = $"Company \"{company.Name}\" has been retired.";
            await LoadListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostUnretireAsync(string code)
        {
            var company = await _db.Institutions.FirstOrDefaultAsync(i => i.Code == code);
            if (company == null)
            {
                ErrorMessage = "Company not found.";
                await LoadListAsync();
                return Page();
            }

            company.Retired = false;
            company.RetiredAt = null;
            company.RetirerId = null;
            company.RetireComments = null;

            await _db.SaveChangesAsync();
            await SetCompanyStatusAsync(code, "A");

            SuccessMessage = $"Company \"{company.Name}\" has been restored.";
            await LoadListAsync();
            return Page();
        }

        /// <summary>
        /// Loads the company register from [COMPANY] and joins [INSTITUTION]
        /// on Code = ID for the registration data and the service rates.
        /// </summary>
        private async Task LoadListAsync()
        {
            var companies = _db.CompanyMasters.AsQueryable();

            if (!ShowRetired)
                companies = companies.Where(c => c.Status == null || c.Status != "I");

            if (!string.IsNullOrWhiteSpace(Q))
            {
                var term = Q.Trim().ToLower();
                companies = companies.Where(c =>
                    (c.Name != null && c.Name.ToLower().Contains(term))
                    || (c.Id != null && c.Id.ToLower().Contains(term))
                    || (c.Phone != null && c.Phone.Contains(term))
                    || (c.City != null && c.City.ToLower().Contains(term))
                    || (c.Address != null && c.Address.ToLower().Contains(term)));
            }

            var rows = await companies
                .OrderBy(c => c.Name)
                .ThenBy(c => c.Id)
                .ToListAsync();

            // linked app rows (registration number, e-mail, service rates)
            var codes = rows.Where(c => !string.IsNullOrWhiteSpace(c.Id)).Select(c => c.Id).ToList();
            var institutions = await _db.Institutions
                .Where(i => i.Code != null && codes.Contains(i.Code))
                .Select(i => new { i.Id, i.Code, i.RegNo, i.Email, i.Retired })
                .ToListAsync();
            var instMap = institutions.ToDictionary(i => i.Code, i => i, StringComparer.OrdinalIgnoreCase);

            var instIds = institutions.Select(i => (long?)i.Id).ToList();
            var rateCounts = await _db.CompanyServiceRates
                .Where(r => instIds.Contains(r.CompanyId) && !r.Retired)
                .GroupBy(r => r.CompanyId)
                .Select(g => new { CompanyId = g.Key, Count = g.Count() })
                .ToListAsync();
            var rateMap = rateCounts.ToDictionary(r => r.CompanyId, r => r.Count);

            List<CompanyCard> cards = rows.Select(c =>
            {
                instMap.TryGetValue(c.Id ?? "", out var inst);
                var rateCount = 0;
                if (inst != null && rateMap.TryGetValue(inst.Id, out var rateNo))
                    rateCount = rateNo;

                return new CompanyCard
                {
                    Code = c.Id ?? "",
                    InstitutionId = inst?.Id,
                    Name = c.Name ?? "",
                    RegNo = inst?.RegNo ?? "",
                    Phone = c.Phone ?? "",
                    Email = inst?.Email ?? "",
                    Address = c.Address ?? "",
                    City = c.City ?? "",
                    Attention = c.Attention ?? "",
                    AttentionDesignation = c.AttentionDesignation ?? "",
                    BillMode = c.BillMode ?? "",
                    Retired = string.Equals(c.Status, "I", StringComparison.OrdinalIgnoreCase)
                              || (inst?.Retired ?? false),
                    ServiceRateCount = rateCount
                };
            }).ToList();

            ActiveCount = await _db.CompanyMasters.CountAsync(c => c.Status == null || c.Status != "I");
            RetiredCount = await _db.CompanyMasters.CountAsync(c => c.Status == "I");

            TotalCount = cards.Count;
            TotalServiceRates = cards.Sum(c => c.ServiceRateCount);
            ClampPaging();
            Companies = ApplyPaging(cards);
        }

        /// <summary>
        /// Writes the legacy company row (the table has no key, so the update
        /// is a parameterised statement against the company code).
        /// </summary>
        private async Task UpsertCompanyMasterAsync(string code, string name, string address, string city,
            string attention, string designation, string phone, string status)
        {
            var now = DateTime.Now;
            var user = _appSession.LoggedUser?.RealUserName ?? _appSession.LoggedUser?.UserName ?? "system";

            var exists = await _db.CompanyMasters.AnyAsync(c => c.Id == code);
            if (exists)
            {
                await _db.Database.ExecuteSqlRawAsync(
                    @"UPDATE COMPANY SET NAME = @p0, ADDRESS = @p1, CITY = @p2, ATTN = @p3, DESG = @p4, PHONE = @p5,
                       STATUS = @p6, USERID = @p7, UDATE = @p8, UTIME = @p9 WHERE ID = @p10",
                    name ?? "", address ?? "", city ?? "", attention ?? "", designation ?? "", phone ?? "",
                    status ?? "A", user, now, now.ToString("HH:mm"), code);
            }
            else
            {
                await _db.Database.ExecuteSqlRawAsync(
                    @"INSERT INTO COMPANY (ID, NAME, ADDRESS, CITY, ATTN, DESG, PHONE, BILLMODE, STATUS, USERID, UDATE, UTIME)
                      VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11)",
                    code, name ?? "", address ?? "", city ?? "", attention ?? "", designation ?? "", phone ?? "",
                    "", status ?? "A", user, now, now.ToString("HH:mm"));
            }
        }

        /// <summary>Sets the legacy STATUS flag ('A' active / 'I' inactive).</summary>
        private async Task SetCompanyStatusAsync(string code, string status)
        {
            var user = _appSession.LoggedUser?.RealUserName ?? _appSession.LoggedUser?.UserName ?? "system";
            var now = DateTime.Now;

            await _db.Database.ExecuteSqlRawAsync(
                @"UPDATE COMPANY SET STATUS = @p0, USERID = @p1, UDATE = @p2, UTIME = @p3 WHERE ID = @p4",
                status, user, now, now.ToString("HH:mm"), code);
        }
    }
}
