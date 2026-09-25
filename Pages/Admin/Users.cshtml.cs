using HmisWeb.Data;
using HmisWeb.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Admin
{
    public class UsersModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public UsersModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<WebUser> Users { get; set; } = new();

        public async Task OnGetAsync()
        {
            Users = await _db.WebUsers
                .Include(u => u.WebUserPerson)
                .Include(u => u.WebUserRole)
                .Include(u => u.Institution)
                .Include(u => u.Department)
                .Where(u => !u.Retired)
                .OrderBy(u => u.UserName)
                .ToListAsync();
        }
    }
}
