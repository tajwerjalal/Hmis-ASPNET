using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Channel
{
    public class BookingModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public BookingModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<SelectListItem> DoctorList { get; set; } = new();

        public string Message { get; set; } = "";

        public class InputModel
        {
            public string PatientName { get; set; } = "";
            public string PatientNic { get; set; } = "";
            public long? StaffId { get; set; }
            public DateTime? AppointmentDate { get; set; }
            public double FeeAmount { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadDoctors();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadDoctors();

            if (string.IsNullOrWhiteSpace(Input.PatientName) || Input.StaffId == null || Input.AppointmentDate == null)
                return Page();

            var person = new Person
            {
                Name = Input.PatientName.Trim(),
                NameWithInitials = Input.PatientName.Trim(),
                Nic = Input.PatientNic?.Trim() ?? "",
                CreatedAt = DateTime.Now,
                CreaterId = _session.WebUserId
            };
            _db.Persons.Add(person);
            await _db.SaveChangesAsync();

            var patient = new Patient
            {
                PersonId = person.Id,
                CreatedAt = DateTime.Now,
                CreaterId = _session.WebUserId
            };
            _db.Patients.Add(patient);
            await _db.SaveChangesAsync();

            var bill = new Bill
            {
                BillType = BillType.ChannelBill,
                BillClassType = BillClassType.PreBill,
                PatientId = patient.Id,
                PersonId = person.Id,
                StaffId = Input.StaffId,
                NetTotal = Input.FeeAmount,
                Total = Input.FeeAmount,
                BillTotal = Input.FeeAmount,
                PaidAmount = Input.FeeAmount,
                Paid = true,
                PaymentMethod = PaymentMethod.Cash,
                BillDate = Input.AppointmentDate,
                AppointmentAt = Input.AppointmentDate,
                CreatedAt = DateTime.Now,
                BillTime = DateTime.Now,
                PaidAt = DateTime.Now,
                InstitutionId = _session.LoggedSite?.Id,
                DepartmentId = _session.LoggedDepartment?.Id,
                CreaterId = _session.WebUserId
            };
            _db.Bills.Add(bill);
            await _db.SaveChangesAsync();

            var payment = new Payment
            {
                BillId = bill.Id,
                PaymentMethod = PaymentMethod.Cash,
                PaidValue = Input.FeeAmount,
                Realized = true,
                RealizedAt = DateTime.Now,
                WrittenAt = DateTime.Now,
                CreatedAt = DateTime.Now,
                InstitutionId = _session.LoggedSite?.Id,
                DepartmentId = _session.LoggedDepartment?.Id,
                CreaterId = _session.WebUserId
            };
            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();

            Message = $"Booking saved. Bill #{bill.Id}";
            Input = new InputModel();
            return Page();
        }

        private async Task LoadDoctors()
        {
            DoctorList = await _db.Staffs
                .Include(s => s.Person)
                .Where(s => !s.Retired && s.PersonId != null)
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
