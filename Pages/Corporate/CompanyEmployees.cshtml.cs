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
    public class CompanyEmployeesModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public CompanyEmployeesModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        public List<CompanyEmployee> Employees { get; set; } = new();
        public List<SelectListItem> CompanyList { get; set; } = new();
        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? FilterCompanyId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Q { get; set; }

        [BindProperty]
        public long EditId { get; set; }

        [BindProperty]
        public InputModel EmployeeInput { get; set; } = new();

        public class InputModel
        {
            public long CompanyId { get; set; }
            public string EmployeeCode { get; set; }
            public string Name { get; set; }
            public string Designation { get; set; }
            public string Nic { get; set; }
            public string Mobile { get; set; }
            public string Address { get; set; }
            public string Email { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (EmployeeInput.CompanyId <= 0)
            {
                ErrorMessage = "Company is required.";
                await LoadAsync();
                return Page();
            }

            if (string.IsNullOrWhiteSpace(EmployeeInput.Name))
            {
                ErrorMessage = "Employee name is required.";
                await LoadAsync();
                return Page();
            }

            var nextCode = await GenerateEmployeeCodeAsync(EmployeeInput.CompanyId);

            var person = new Person
            {
                Name = EmployeeInput.Name.Trim(),
                Nic = EmployeeInput.Nic?.Trim() ?? "",
                Mobile = EmployeeInput.Mobile?.Trim() ?? "",
                Address = EmployeeInput.Address?.Trim() ?? "",
                Email = EmployeeInput.Email?.Trim() ?? "",
                InstitutionId = EmployeeInput.CompanyId,
                CreatedAt = DateTime.Now,
                CreaterId = _session.WebUserId
            };
            _db.Persons.Add(person);
            await _db.SaveChangesAsync();

            _db.CompanyEmployees.Add(new CompanyEmployee
            {
                CompanyId = EmployeeInput.CompanyId,
                EmployeeCode = string.IsNullOrWhiteSpace(EmployeeInput.EmployeeCode) ? nextCode : EmployeeInput.EmployeeCode.Trim(),
                PersonId = person.Id,
                Name = EmployeeInput.Name.Trim(),
                Designation = EmployeeInput.Designation?.Trim() ?? "",
                Nic = EmployeeInput.Nic?.Trim() ?? "",
                Mobile = EmployeeInput.Mobile?.Trim() ?? "",
                Address = EmployeeInput.Address?.Trim() ?? "",
                Email = EmployeeInput.Email?.Trim() ?? "",
                CreatedAt = DateTime.Now,
                CreaterId = _session.WebUserId
            });
            await _db.SaveChangesAsync();

            SuccessMessage = $"Employee \"{EmployeeInput.Name.Trim()}\" added.";
            EmployeeInput = new InputModel();
            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            var employee = await _db.CompanyEmployees
                .Include(e => e.Person)
                .FirstOrDefaultAsync(e => e.Id == EditId && !e.Retired);

            if (employee == null)
            {
                ErrorMessage = "Employee not found.";
                await LoadAsync();
                return Page();
            }

            if (EmployeeInput.CompanyId <= 0)
            {
                ErrorMessage = "Company is required.";
                await LoadAsync();
                return Page();
            }

            if (string.IsNullOrWhiteSpace(EmployeeInput.Name))
            {
                ErrorMessage = "Employee name is required.";
                await LoadAsync();
                return Page();
            }

            employee.CompanyId = EmployeeInput.CompanyId;
            employee.EmployeeCode = EmployeeInput.EmployeeCode?.Trim() ?? employee.EmployeeCode;
            employee.Name = EmployeeInput.Name.Trim();
            employee.Designation = EmployeeInput.Designation?.Trim() ?? "";
            employee.Nic = EmployeeInput.Nic?.Trim() ?? "";
            employee.Mobile = EmployeeInput.Mobile?.Trim() ?? "";
            employee.Address = EmployeeInput.Address?.Trim() ?? "";
            employee.Email = EmployeeInput.Email?.Trim() ?? "";
            employee.EditedAt = DateTime.Now;
            employee.EditerId = _session.WebUserId;

            if (employee.Person != null)
            {
                employee.Person.Name = employee.Name;
                employee.Person.Nic = employee.Nic;
                employee.Person.Mobile = employee.Mobile;
                employee.Person.Address = employee.Address;
                employee.Person.Email = employee.Email;
                employee.Person.InstitutionId = employee.CompanyId;
                employee.Person.EditedAt = DateTime.Now;
                employee.Person.EditerId = _session.WebUserId;
            }

            await _db.SaveChangesAsync();

            SuccessMessage = $"Employee \"{employee.Name}\" updated.";
            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostRetireAsync(long id)
        {
            var employee = await _db.CompanyEmployees.FirstOrDefaultAsync(e => e.Id == id && !e.Retired);
            if (employee != null)
            {
                employee.Retired = true;
                employee.RetireComments = "Retired from Company Employees page.";
                await _db.SaveChangesAsync();
            }

            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostRestoreAsync(long id)
        {
            var employee = await _db.CompanyEmployees.FirstOrDefaultAsync(e => e.Id == id && e.Retired);
            if (employee != null)
            {
                employee.Retired = false;
                employee.RetireComments = null;
                await _db.SaveChangesAsync();
            }

            await LoadAsync();
            return Page();
        }

        private async Task<string> GenerateEmployeeCodeAsync(long companyId)
        {
            var company = await _db.Institutions.FindAsync(companyId);
            var prefix = company?.Code?.Replace("-", "") ?? "EMP";

            var last = await _db.CompanyEmployees
                .Where(e => e.EmployeeCode != null && e.EmployeeCode.StartsWith(prefix))
                .OrderByDescending(e => e.EmployeeCode)
                .Select(e => e.EmployeeCode)
                .FirstOrDefaultAsync();

            if (last != null && int.TryParse(last.Substring(prefix.Length), out var lastNum))
                return $"{prefix}{(lastNum + 1):D3}";

            return $"{prefix}001";
        }

        private async Task LoadAsync()
        {
            CompanyList = await _db.Institutions
                .Where(i => i.InstitutionType == InstitutionType.InsuranceCompany && !i.Retired)
                .OrderBy(i => i.Name)
                .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = i.Name ?? "" })
                .ToListAsync();

            var query = _db.CompanyEmployees
                .Include(e => e.Company)
                .AsQueryable();

            if (FilterCompanyId.HasValue && FilterCompanyId.Value > 0)
                query = query.Where(e => e.CompanyId == FilterCompanyId.Value);

            if (!string.IsNullOrWhiteSpace(Q))
            {
                var term = Q.Trim().ToLower();
                query = query.Where(e =>
                    (e.Name != null && e.Name.ToLower().Contains(term))
                    || (e.EmployeeCode != null && e.EmployeeCode.ToLower().Contains(term))
                    || (e.Designation != null && e.Designation.ToLower().Contains(term))
                    || (e.Mobile != null && e.Mobile.Contains(term)));
            }

            Employees = await query
                .OrderBy(e => e.Company != null ? e.Company.Name : "")
                .ThenBy(e => e.Name)
                .ToListAsync();
        }
    }
}