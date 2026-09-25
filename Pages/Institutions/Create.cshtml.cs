using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Institutions
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
        public InstitutionType? InstitutionType { get; set; }

        [BindProperty]
        public string Address { get; set; }

        [BindProperty]
        public string Phone { get; set; }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Fax { get; set; }

        [BindProperty]
        public string Website { get; set; }

        [BindProperty]
        public string RegNo { get; set; }

        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Name is required.";
                return Page();
            }

            var institution = new Institution
            {
                Name = Name.Trim(),
                Sname = Name.Trim(),
                InstitutionType = InstitutionType,
                Address = Address,
                Phone = Phone,
                Email = Email,
                Fax = Fax,
                Website = Website,
                RegNo = RegNo,
                CreatedAt = DateTime.Now,
                CreaterId = _appSession.WebUserId,
                Retired = false
            };

            _db.Institutions.Add(institution);
            await _db.SaveChangesAsync();

            return RedirectToPage("/institutions/Index");
        }
    }
}