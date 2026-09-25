using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Departments
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;

        public CreateModel(ApplicationDbContext db, IAppSession appSession)
        {
            _db = db;
            _appSession = appSession;
        }

        [BindProperty]
        public string Name { get; set; }

        [BindProperty]
        public string Sname { get; set; }

        [BindProperty]
        public DepartmentType? DepartmentType { get; set; }

        [BindProperty]
        public long? InstitutionId { get; set; }

        [BindProperty]
        public string PrintingName { get; set; }

        public string AutoCode { get; set; }

        public List<Institution> Institutions { get; set; } = new();
        public string ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadInstitutionsAsync();
            AutoCode = await GenerateNextCodeAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadInstitutionsAsync();
            AutoCode = await GenerateNextCodeAsync();

            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Name is required.";
                return Page();
            }

            var department = new Department
            {
                Name = Name.Trim(),
                Sname = string.IsNullOrWhiteSpace(Sname) ? null : Sname.Trim(),
                Code = AutoCode,
                PrintingName = PrintingName,
                DepartmentType = DepartmentType,
                InstitutionId = InstitutionId,
                CreatedAt = DateTime.Now,
                CreaterId = _appSession.WebUserId,
                Retired = false
            };

            _db.Departments.Add(department);
            await _db.SaveChangesAsync();

            return RedirectToPage("/departments/Index");
        }

        private async Task<string> GenerateNextCodeAsync()
        {
            var lastCode = await _db.Departments
                .Where(d => d.Code != null && d.Code.StartsWith("DPT-") && !d.Retired)
                .OrderByDescending(d => d.Code)
                .Select(d => d.Code)
                .FirstOrDefaultAsync();

            if (lastCode != null && int.TryParse(lastCode.Substring(4), out var lastNum))
                return $"DPT-{(lastNum + 1):D3}";

            return "DPT-001";
        }

        private async Task LoadInstitutionsAsync()
        {
            Institutions = await _db.Institutions
                .Where(i => !i.Retired)
                .OrderBy(i => i.Name)
                .ToListAsync();
        }
    }
}