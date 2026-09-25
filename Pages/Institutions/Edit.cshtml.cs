using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Institutions
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
        public long Id { get; set; }

        [BindProperty]
        public string Name { get; set; }

        [BindProperty]
        public InstitutionType? InstitutionType { get; set; }

        [BindProperty]
        public string Address { get; set; }

        [BindProperty]
        public string RegNo { get; set; }

        [BindProperty]
        public string Phone { get; set; }

        [BindProperty]
        public string Fax { get; set; }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Website { get; set; }

        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            var institution = await _db.Institutions.FirstOrDefaultAsync(i => i.Id == id && !i.Retired);
            if (institution == null) return NotFound();

            Id = institution.Id;
            Name = institution.Name;
            InstitutionType = institution.InstitutionType;
            Address = institution.Address;
            RegNo = institution.RegNo;
            Phone = institution.Phone;
            Fax = institution.Fax;
            Email = institution.Email;
            Website = institution.Website;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Name is required.";
                return Page();
            }

            var institution = await _db.Institutions.FirstOrDefaultAsync(i => i.Id == Id && !i.Retired);
            if (institution == null) return NotFound();

            institution.Name = Name.Trim();
            institution.Sname = Name.Trim();
            institution.InstitutionType = InstitutionType;
            institution.Address = Address;
            institution.RegNo = RegNo;
            institution.Phone = Phone;
            institution.Fax = Fax;
            institution.Email = Email;
            institution.Website = Website;
            institution.EditedAt = DateTime.Now;
            institution.EditerId = _appSession.WebUserId;

            await _db.SaveChangesAsync();
            return RedirectToPage("/institutions/Index");
        }
    }
}