using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Hr
{
    public class NewSalaryModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public NewSalaryModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<SelectListItem> StaffList { get; set; } = new();

        public class InputModel
        {
            public long? StaffId { get; set; }
            public DateTime? SalaryDate { get; set; }
            public double? GrossSalary { get; set; }
            public double? EpfEmployee { get; set; }
            public double? Etf { get; set; }
            public double? Payee { get; set; }
            public double? NetSalary { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadStaff();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadStaff();

            if (Input.StaffId == null)
                return Page();

            var salary = new StaffSalary
            {
                StaffId = Input.StaffId,
                SalaryDate = Input.SalaryDate,
                GrossSalary = Input.GrossSalary,
                EpfEmployee = Input.EpfEmployee,
                Etf = Input.Etf,
                Payee = Input.Payee,
                NetSalary = Input.NetSalary ?? Input.GrossSalary,
                InstitutionId = _session.LoggedSite?.Id,
                CreatedAt = DateTime.Now,
                CreaterId = _session.WebUserId
            };

            _db.StaffSalaries.Add(salary);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Hr/Salary");
        }

        private async Task LoadStaff()
        {
            StaffList = await _db.Staffs
                .Include(s => s.Person)
                .Where(s => !s.Retired)
                .OrderBy(s => s.Person != null ? s.Person.Name : "")
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Person != null
                        ? (s.Person.NameWithInitials ?? s.Person.Name ?? "Staff #" + s.Id)
                        : "Staff #" + s.Id
                })
                .ToListAsync();
        }
    }
}
