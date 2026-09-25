using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Corporate
{
    public class CompaniesModel : PageModel
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

        [BindProperty(SupportsGet = true)]
        public string Q { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool ShowRetired { get; set; }

        [BindProperty]
        public Institution FormCompany { get; set; } = new();

        [BindProperty]
        public long EditId { get; set; }

        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }

        public class CompanyCard
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public string Code { get; set; }
            public string RegNo { get; set; }
            public string Phone { get; set; }
            public string Email { get; set; }
            public string Address { get; set; }
            public string City { get; set; }
            public string Attention { get; set; }
            public string AttentionDesignation { get; set; }
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

            SuccessMessage = $"Company \"{FormCompany.Name}\" created with code {nextCode}.";
            FormCompany = new Institution();
            await LoadListAsync();
            return Page();
        }

        private async Task<string> GenerateCompanyCodeAsync()
        {
            var lastCode = await _db.Institutions
                .Where(i => i.InstitutionType == InstitutionType.InsuranceCompany
                    && i.Code != null && i.Code.StartsWith("CO-"))
                .OrderByDescending(i => i.Code)
                .Select(i => i.Code)
                .FirstOrDefaultAsync();

            if (lastCode != null && int.TryParse(lastCode.Substring(3), out var lastNum))
                return $"CO-{(lastNum + 1):D4}";

            return "CO-0001";
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            var company = await _db.Institutions
                .FirstOrDefaultAsync(i => i.Id == EditId && i.InstitutionType == InstitutionType.InsuranceCompany);

            if (company == null)
            {
                ErrorMessage = "Company not found.";
                await LoadListAsync();
                return Page();
            }

            company.Name = FormCompany.Name;
            company.Sname = FormCompany.Sname;
            company.Code = FormCompany.Code;
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

            SuccessMessage = $"Company \"{company.Name}\" updated successfully.";
            await LoadListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostRetireAsync(long id)
        {
            var company = await _db.Institutions
                .FirstOrDefaultAsync(i => i.Id == id && i.InstitutionType == InstitutionType.InsuranceCompany);

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

            SuccessMessage = $"Company \"{company.Name}\" has been retired.";
            await LoadListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostUnretireAsync(long id)
        {
            var company = await _db.Institutions
                .FirstOrDefaultAsync(i => i.Id == id && i.InstitutionType == InstitutionType.InsuranceCompany);

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

            SuccessMessage = $"Company \"{company.Name}\" has been restored.";
            await LoadListAsync();
            return Page();
        }

        private async Task LoadListAsync()
        {
            var query = _db.Institutions
                .Where(i => i.InstitutionType == InstitutionType.InsuranceCompany);

            if (!ShowRetired)
                query = query.Where(i => !i.Retired);

            if (!string.IsNullOrWhiteSpace(Q))
            {
                var term = Q.Trim().ToLower();
                query = query.Where(i =>
                    (i.Name != null && i.Name.ToLower().Contains(term))
                    || (i.Code != null && i.Code.ToLower().Contains(term))
                    || (i.RegNo != null && i.RegNo.ToLower().Contains(term))
                    || (i.Phone != null && i.Phone.Contains(term))
                    || (i.Email != null && i.Email.ToLower().Contains(term)));
            }

            var list = await query
                .OrderBy(i => i.Name)
                .ToListAsync();

            var companyIds = list.Select(i => i.Id).ToList();

            var rateCounts = await _db.CompanyServiceRates
                .Where(r => companyIds.Contains(r.CompanyId) && !r.Retired)
                .GroupBy(r => r.CompanyId)
                .Select(g => new { CompanyId = g.Key, Count = g.Count() })
                .ToListAsync();

            var rateMap = rateCounts.ToDictionary(r => r.CompanyId, r => r.Count);

            Companies = list.Select(i => new CompanyCard
            {
                Id = i.Id,
                Name = i.Name ?? "",
                Code = i.Code ?? "",
                RegNo = i.RegNo ?? "",
                Phone = i.Phone ?? "",
                Email = i.Email ?? "",
                Address = i.Address ?? "",
                City = i.City ?? "",
                Attention = i.Attention ?? "",
                AttentionDesignation = i.AttentionDesignation ?? "",
                Retired = i.Retired,
                ServiceRateCount = rateMap.ContainsKey(i.Id) ? rateMap[i.Id] : 0
            }).ToList();

            ActiveCount = await _db.Institutions
                .CountAsync(i => i.InstitutionType == InstitutionType.InsuranceCompany && !i.Retired);
            RetiredCount = await _db.Institutions
                .CountAsync(i => i.InstitutionType == InstitutionType.InsuranceCompany && i.Retired);
        }
    }
}
