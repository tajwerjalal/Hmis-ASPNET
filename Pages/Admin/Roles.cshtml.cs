using HmisWeb.Data;
using HmisWeb.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Admin
{
    public class RolesModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public RolesModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<WebUserRole> Roles { get; set; } = new();

        public async Task OnGetAsync()
        {
            Roles = await _db.WebUserRoles
                .OrderBy(r => r.Name)
                .ToListAsync();
        }
    }
}
