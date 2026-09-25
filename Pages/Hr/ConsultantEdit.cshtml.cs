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
    public class ConsultantEditModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public ConsultantEditModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        [BindProperty]
        public long? Id { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<SelectListItem> SpecialityList { get; set; } = new();
        public List<SelectListItem> DepartmentList { get; set; } = new();
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            public string Name { get; set; } = "";
            public string NameWithInitials { get; set; } = "";
            public string Nic { get; set; } = "";
            public string Mobile { get; set; } = "";
            public string Email { get; set; } = "";
            public Sex? Sex { get; set; }
            public long? SpecialityId { get; set; }
            public long? DepartmentId { get; set; }
            public string Degrees { get; set; } = "";
            public double FirstVisitFee { get; set; }
            public double SubsequentVisitFee { get; set; }
            public string SltRegNo { get; set; } = "";
            public string PmcNumber { get; set; } = "";
            public string WorkDays { get; set; } = "";
            public string SessionStartTime { get; set; } = "";
            public string SessionEndTime { get; set; } = "";
            public string DefaultRoomNo { get; set; } = "";
            public double DefaultHospitalFee { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
                return RedirectToPage("/Hr/Consultants");

            var c = await _db.Consultants
                .Include(x => x.Person)
                .FirstOrDefaultAsync(x => x.Id == id && !x.Retired);
            if (c == null)
                return RedirectToPage("/Hr/Consultants");

            await LoadLookupsAsync();

            Id = c.Id;
            Input.Name = c.Person?.Name ?? "";
            Input.NameWithInitials = c.Person?.NameWithInitials ?? "";
            Input.Nic = c.Person?.Nic ?? "";
            Input.Mobile = c.Person?.Mobile ?? "";
            Input.Email = c.Person?.Email ?? "";
            Input.Sex = c.Person?.Sex;
            Input.SpecialityId = c.SpecialityId;
            Input.DepartmentId = c.DepartmentId;
            Input.Degrees = c.Degrees ?? "";
            Input.FirstVisitFee = c.FirstVisitFee;
            Input.SubsequentVisitFee = c.SubsequentVisitFee;
            Input.SltRegNo = c.SltRegNo ?? "";
            Input.PmcNumber = c.PmcNumber ?? "";
            Input.WorkDays = c.WorkDays ?? "";
            Input.SessionStartTime = c.SessionStartTime ?? "";
            Input.SessionEndTime = c.SessionEndTime ?? "";
            Input.DefaultRoomNo = c.DefaultRoomNo ?? "";
            Input.DefaultHospitalFee = c.DefaultHospitalFee;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Id == null)
                return RedirectToPage("/Hr/Consultants");

            var c = await _db.Consultants
                .Include(x => x.Person)
                .FirstOrDefaultAsync(x => x.Id == Id && !x.Retired);
            if (c == null)
                return RedirectToPage("/Hr/Consultants");

            await LoadLookupsAsync();

            if (string.IsNullOrWhiteSpace(Input.Name) || string.IsNullOrWhiteSpace(Input.NameWithInitials))
            {
                ErrorMessage = "Consultant name and name with initials are required.";
                return Page();
            }

            if (c.Person == null)
            {
                c.Person = new Person { CreatedAt = DateTime.Now, CreaterId = _session.WebUserId };
                _db.Persons.Add(c.Person);
            }

            var person = c.Person;
            person.Name = Input.Name.Trim();
            person.NameWithInitials = Input.NameWithInitials.Trim();
            person.Nic = Input.Nic?.Trim() ?? "";
            person.Mobile = Input.Mobile?.Trim() ?? "";
            person.Email = Input.Email?.Trim() ?? "";
            person.Sex = Input.Sex;
            person.EditedAt = DateTime.Now;
            person.EditerId = _session.WebUserId;

            c.SpecialityId = Input.SpecialityId;
            c.DepartmentId = Input.DepartmentId;
            c.Degrees = Input.Degrees?.Trim() ?? "";
            c.FirstVisitFee = Math.Max(0, Input.FirstVisitFee);
            c.SubsequentVisitFee = Math.Max(0, Input.SubsequentVisitFee);
            c.SltRegNo = Input.SltRegNo?.Trim() ?? "";
            c.PmcNumber = Input.PmcNumber?.Trim() ?? "";
            c.WorkDays = Input.WorkDays?.Trim() ?? "";
            c.SessionStartTime = Input.SessionStartTime?.Trim() ?? "";
            c.SessionEndTime = Input.SessionEndTime?.Trim() ?? "";
            c.DefaultRoomNo = Input.DefaultRoomNo?.Trim() ?? "";
            c.DefaultHospitalFee = Math.Max(0, Input.DefaultHospitalFee);
            c.EditedAt = DateTime.Now;
            c.EditerId = _session.WebUserId;

            await _db.SaveChangesAsync();

            return RedirectToPage("/Hr/Consultants");
        }

        private async Task LoadLookupsAsync()
        {
            SpecialityList = await _db.Specialities
                .Where(x => !x.Retired)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name })
                .ToListAsync();

            DepartmentList = await _db.Departments
                .Where(d => !d.Retired)
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .ToListAsync();
        }
    }
}