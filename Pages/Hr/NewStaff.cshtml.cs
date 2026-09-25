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
    public class NewStaffModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public NewStaffModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<SelectListItem> DesignationList { get; set; } = new();
        public List<SelectListItem> DepartmentList { get; set; } = new();
        public List<SelectListItem> StaffCategoryList { get; set; } = new();

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

        public async Task OnGetAsync()
        {
            await LoadDropdowns();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadDropdowns();

            if (string.IsNullOrWhiteSpace(Input.Name) || string.IsNullOrWhiteSpace(Input.NameWithInitials))
                return Page();

            var person = new Person
            {
                Name = Input.Name.Trim(),
                NameWithInitials = Input.NameWithInitials.Trim(),
                Nic = Input.Nic?.Trim() ?? "",
                Mobile = Input.Mobile?.Trim() ?? "",
                Sex = Input.Sex,
                CreatedAt = DateTime.Now,
                CreaterId = _session.WebUserId
            };
            _db.Persons.Add(person);
            await _db.SaveChangesAsync();

            var staff = new Staff
            {
                PersonId = person.Id,
                DesignationId = Input.DesignationId,
                DepartmentId = Input.DepartmentId,
                StaffCategoryId = Input.StaffCategoryId,
                InstitutionId = _session.LoggedSite?.Id,
                Code = "S" + DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                CreatedAt = DateTime.Now,
                CreaterId = _session.WebUserId
            };
            _db.Staffs.Add(staff);
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
