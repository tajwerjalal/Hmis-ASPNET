using HmisWeb.Data;
using HmisWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Cnp.Reports
{
    public class ConsultantChargesModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public ConsultantChargesModel(ApplicationDbContext db) { _db = db; }

        [BindProperty(SupportsGet = true)] public long? ConsultantId { get; set; }

        public List<SelectListItem> Consultants { get; set; } = new();
        public List<ConsultantServiceRate> Rows { get; set; } = new();
        public bool Searched { get; set; }

        public async Task OnGetAsync()
        {
            Consultants = await _db.Consultants
                .Where(c => !c.Retired)
                 .OrderBy(c => c.Person.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Person.Name })
                .ToListAsync();

            var q = _db.ConsultantServiceRates
                .Include(r => r.Consultant)
                .Include(r => r.Item)
                .Where(r => !r.Retired);

            if (ConsultantId.HasValue)
                q = q.Where(r => r.ConsultantId == ConsultantId.Value);

            Rows = await q.OrderBy(r => r.Consultant.Person.Name).ThenBy(r => r.Item.Name).ToListAsync();
            Searched = true;
        }
    }
}