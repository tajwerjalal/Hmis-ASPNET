using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;

namespace HmisWeb.Pages.Institutions
{
    /// <summary>
    /// Institution list (Administration &rarr; Institutions).
    ///
    /// Source table: [HMIS].[dbo].[INSTITUTION] - the legacy VFP institution
    /// master, mapped by the <see cref="Institution"/> model. It is the parent
    /// of DEPARTMENT, ITEM, PERSON, USER and the company/panel masters through
    /// their InstitutionId foreign keys, so this list is the hospital-group
    /// reference screen.
    ///
    /// The grid is server-side paged (25/50/100/200 rows) through the shared
    /// <see cref="HmisWeb.Pages.Shared.PagedPageModel"/> base.
    ///
    /// Developer : [TAJWER JALAL]
    /// Change Log: 22-Oct-2026 - added server-side pagination (170 institutions
    ///             were rendered in one page).
    /// </summary>
    public class IndexModel : Shared.PagedPageModel
    {
        private readonly ApplicationDbContext _db;

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<Institution> Institutions { get; set; } = new();

        public async Task OnGetAsync()
        {
            var query = _db.Institutions
                .Where(i => !i.Retired)
                .OrderBy(i => i.Name);

            TotalCount = await query.CountAsync();
            ClampPaging();

            Institutions = await ApplyPaging(query).ToListAsync();
        }
    }
}
