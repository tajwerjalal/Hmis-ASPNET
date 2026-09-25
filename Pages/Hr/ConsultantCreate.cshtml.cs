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
    public class ConsultantCreateModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public ConsultantCreateModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

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
        }

        public async Task OnGetAsync()
        {
            await LoadLookupsAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadLookupsAsync();

            if (string.IsNullOrWhiteSpace(Input.Name) || string.IsNullOrWhiteSpace(Input.NameWithInitials))
            {
                ErrorMessage = "Consultant name and name with initials are required.";
                return Page();
            }

            var person = new Person
            {
                Name = Input.Name.Trim(),
                NameWithInitials = Input.NameWithInitials.Trim(),
                Nic = Input.Nic?.Trim() ?? "",
                Mobile = Input.Mobile?.Trim() ?? "",
                Email = Input.Email?.Trim() ?? "",
                Sex = Input.Sex,
                CreatedAt = DateTime.Now,
                CreaterId = _session.WebUserId
            };
            _db.Persons.Add(person);
            await _db.SaveChangesAsync();

            var consultant = new Consultant
            {
                PersonId = person.Id,
                SpecialityId = Input.SpecialityId,
                DepartmentId = Input.DepartmentId,
                InstitutionId = _session.LoggedSite?.Id,
                Code = "C" + DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Degrees = Input.Degrees?.Trim() ?? "",
                FirstVisitFee = Math.Max(0, Input.FirstVisitFee),
                SubsequentVisitFee = Math.Max(0, Input.SubsequentVisitFee),
                SltRegNo = Input.SltRegNo?.Trim() ?? "",
                PmcNumber = Input.PmcNumber?.Trim() ?? "",
                CreatedAt = DateTime.Now,
                CreaterId = _session.WebUserId
            };
            _db.Consultants.Add(consultant);
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