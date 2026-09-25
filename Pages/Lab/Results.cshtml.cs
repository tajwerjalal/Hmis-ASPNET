using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Lab
{
    public class ResultsModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public ResultsModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = "";

        public List<ResultRow> Results { get; set; } = new();

        public class ResultRow
        {
            public long BillId { get; set; }
            public string PatientName { get; set; } = "";
            public string ItemName { get; set; } = "";
            public double Qty { get; set; }
            public double Rate { get; set; }
            public DateTime? CreatedAt { get; set; }
        }

        public async Task OnGetAsync()
        {
            var query = _db.BillItems
                .Include(bi => bi.Bill).ThenInclude(b => b.Patient).ThenInclude(p => p.Person)
                .Include(bi => bi.Item)
                .Where(bi => !bi.Retired
                    && bi.Item != null
                    && bi.Item.ItemType == ItemType.Investigation)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(bi =>
                    bi.Bill != null && bi.Bill.Patient != null && bi.Bill.Patient.Person != null
                    && (bi.Bill.Patient.Person.Name != null && bi.Bill.Patient.Person.Name.ToLower().Contains(term)
                        || bi.Bill.Patient.Person.FullName != null && bi.Bill.Patient.Person.FullName.ToLower().Contains(term)
                        || bi.Bill.Patient.Person.NameWithInitials != null && bi.Bill.Patient.Person.NameWithInitials.ToLower().Contains(term)));
            }

            Results = await query
                .OrderByDescending(bi => bi.CreatedAt)
                .Take(50)
                .Select(bi => new ResultRow
                {
                    BillId = bi.BillId ?? 0,
                    PatientName = bi.Bill != null && bi.Bill.Patient != null && bi.Bill.Patient.Person != null
                        ? (bi.Bill.Patient.Person.NameWithInitials ?? bi.Bill.Patient.Person.Name ?? "")
                        : "",
                    ItemName = bi.Item != null ? bi.Item.Name ?? "" : "",
                    Qty = bi.Qty ?? 0,
                    Rate = bi.Rate,
                    CreatedAt = bi.CreatedAt
                })
                .ToListAsync();
        }
    }
}
