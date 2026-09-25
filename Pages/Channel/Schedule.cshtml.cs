using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Channel
{
    public class ScheduleModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public ScheduleModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<ScheduleRow> Bills { get; set; } = new();

        public class ScheduleRow
        {
            public long BillId { get; set; }
            public string PatientName { get; set; } = "";
            public string DoctorName { get; set; } = "";
            public DateTime? AppointmentDate { get; set; }
            public double Amount { get; set; }
            public bool Paid { get; set; }
        }

        public async Task OnGetAsync()
        {
            Bills = await _db.Bills
                .Include(b => b.Patient).ThenInclude(p => p.Person)
                .Include(b => b.Staff).ThenInclude(s => s.Person)
                .Where(b => !b.Retired && b.BillType == BillType.ChannelBill)
                .OrderByDescending(b => b.CreatedAt)
                .Take(50)
                .Select(b => new ScheduleRow
                {
                    BillId = b.Id,
                    PatientName = b.Patient != null && b.Patient.Person != null
                        ? (b.Patient.Person.NameWithInitials ?? b.Patient.Person.Name ?? "")
                        : "",
                    DoctorName = b.Staff != null && b.Staff.Person != null
                        ? (b.Staff.Person.NameWithInitials ?? b.Staff.Person.Name ?? "")
                        : "",
                    AppointmentDate = b.AppointmentAt,
                    Amount = b.NetTotal,
                    Paid = b.Paid
                })
                .ToListAsync();
        }
    }
}
