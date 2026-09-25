using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Departments
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;

        public EditModel(ApplicationDbContext db, IAppSession appSession)
        {
            _db = db;
            _appSession = appSession;
        }

        [BindProperty]
        public long? Id { get; set; }

        [BindProperty]
        public string Name { get; set; }

        [BindProperty]
        public string Sname { get; set; }

        public string Code { get; set; }

        [BindProperty]
        public DepartmentType? DepartmentType { get; set; }

        [BindProperty]
        public long? InstitutionId { get; set; }

        [BindProperty]
        public string PrintingName { get; set; }

        public List<Institution> Institutions { get; set; } = new();
        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
                return RedirectToPage("/departments/Index");

            var department = await _db.Departments.FirstOrDefaultAsync(d => d.Id == id);
            if (department == null)
                return RedirectToPage("/departments/Index");

            await LoadInstitutionsAsync();

            Id = department.Id;
            Name = department.Name;
            Sname = department.Sname;
            Code = department.Code;
            DepartmentType = department.DepartmentType;
            InstitutionId = department.InstitutionId;
            PrintingName = department.PrintingName;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Id == null)
                return RedirectToPage("/departments/Index");

            var department = await _db.Departments.FirstOrDefaultAsync(d => d.Id == Id);
            if (department == null)
                return RedirectToPage("/departments/Index");

            await LoadInstitutionsAsync();

            if (string.IsNullOrWhiteSpace(Name))
            {
                Code = department.Code;
                ErrorMessage = "Name is required.";
                return Page();
            }

            department.Name = Name.Trim();
            department.Sname = string.IsNullOrWhiteSpace(Sname) ? null : Sname.Trim();
            department.PrintingName = PrintingName;
            department.DepartmentType = DepartmentType;
            department.InstitutionId = InstitutionId;
            department.EditedAt = DateTime.Now;
            department.EditerId = _appSession.WebUserId;

            await _db.SaveChangesAsync();

            return RedirectToPage("/departments/Index");
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