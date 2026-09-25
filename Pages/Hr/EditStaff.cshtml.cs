using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Hr
{
    public class EditStaffModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public EditStaffModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        [BindProperty]
        public long? Id { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<SelectListItem> DesignationList { get; set; } = new();
        public List<SelectListItem> DepartmentList { get; set; } = new();
        public List<SelectListItem> StaffCategoryList { get; set; } = new();

        public string ErrorMessage { get; set; }

        public class InputModel
        {
            public string Name { get; set; } = "";
            public string NameWithInitials { get; set; } = "";
            public string Nic { get; set; } = "";
            public string Mobile { get; set; } = "";
            public Sex? Sex { get; set; }
            public long? DesignationId { get; set; }
            public long? DepartmentId { get; set; }
            public long? StaffCategoryId { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
                return RedirectToPage("/Hr/Staff");

            var staff = await _db.Staffs
                .Include(s => s.Person)
                .FirstOrDefaultAsync(s => s.Id == id && !s.Retired);
            if (staff == null)
                return RedirectToPage("/Hr/Staff");

            await LoadDropdowns();

            Id = staff.Id;
            Input.Name = staff.Person?.Name ?? "";
            Input.NameWithInitials = staff.Person?.NameWithInitials ?? "";
            Input.Nic = staff.Person?.Nic ?? "";
            Input.Mobile = staff.Person?.Mobile ?? "";
            Input.Sex = staff.Person?.Sex;
            Input.DesignationId = staff.DesignationId;
            Input.DepartmentId = staff.DepartmentId;
            Input.StaffCategoryId = staff.StaffCategoryId;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Id == null)
                return RedirectToPage("/Hr/Staff");

            var staff = await _db.Staffs
                .Include(s => s.Person)
                .FirstOrDefaultAsync(s => s.Id == Id && !s.Retired);
            if (staff == null)
                return RedirectToPage("/Hr/Staff");

            await LoadDropdowns();

            if (string.IsNullOrWhiteSpace(Input.Name) || string.IsNullOrWhiteSpace(Input.NameWithInitials))
            {
                ErrorMessage = "Name and Name with Initials are required.";
                return Page();
            }

            if (staff.Person == null)
            {
                staff.Person = new Person
                {
                    CreatedAt = DateTime.Now,
                    CreaterId = _session.WebUserId
                };
                _db.Persons.Add(staff.Person);
            }

            var person = staff.Person;
            person.Name = Input.Name.Trim();
            person.NameWithInitials = Input.NameWithInitials.Trim();
            person.Nic = Input.Nic?.Trim() ?? "";
            person.Mobile = Input.Mobile?.Trim() ?? "";
            person.Sex = Input.Sex;
            person.EditedAt = DateTime.Now;
            person.EditerId = _session.WebUserId;

            staff.DesignationId = Input.DesignationId;
            staff.DepartmentId = Input.DepartmentId;
            staff.StaffCategoryId = Input.StaffCategoryId;
            staff.EditedAt = DateTime.Now;
            staff.EditerId = _session.WebUserId;

            await _db.SaveChangesAsync();

            return RedirectToPage("/Hr/Staff");
        }

        private async Task LoadDropdowns()
        {
            DesignationList = await _db.Designations
                .Where(d => !d.Retired)
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .ToListAsync();

            DepartmentList = await _db.Departments
                .Where(d => !d.Retired)
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .ToListAsync();

            StaffCategoryList = await _db.StaffCategories
                .Where(c => !c.Retired)
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToListAsync();
        }
    }
}