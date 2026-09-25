using HmisWeb.Data;
using HmisWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Services
{
    public interface ICommonService
    {
        Task<List<Institution>> GetActiveInstitutionsAsync();
        Task<List<Department>> GetDepartmentsAsync(long? institutionId = null);
        Task<List<Patient>> SearchPatientsAsync(string searchTerm);
        Task<Patient> GetOrCreatePatientAsync(Patient patient, Person person);
        List<WebUser> GetActiveWebUsers();
        Task<double> GetAppliedRateAsync(Item item, long? companyId = null, long? consultantId = null, long? resourceId = null);
        Task<long?> GetPatientCompanyIdAsync(long personId);
    }

    public class CommonService : ICommonService
    {
        private readonly ApplicationDbContext _db;

        public CommonService(ApplicationDbContext db)
        {
            _db = db;
        }

        public Task<List<Institution>> GetActiveInstitutionsAsync()
        {
            return _db.Institutions.Where(i => !i.Retired).OrderBy(i => i.Name).ToListAsync();
        }

        public Task<List<Department>> GetDepartmentsAsync(long? institutionId = null)
        {
            var q = _db.Departments.Where(d => !d.Retired);
            if (institutionId != null)
                q = q.Where(d => d.InstitutionId == institutionId);
            return q.OrderBy(d => d.Name).ToListAsync();
        }

        public Task<List<Patient>> SearchPatientsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Task.FromResult(new List<Patient>());

            searchTerm = searchTerm.Trim();
            long phn;
            bool isNumeric = long.TryParse(searchTerm, out phn);

            var q = _db.Patients
                .Include(p => p.Person)
                .Where(p => !p.Retired);

            if (isNumeric)
                q = q.Where(p => p.PatientId == phn || p.Phn == searchTerm
                    || (p.Person != null && (p.Person.Mobile == searchTerm
                        || p.Person.Phone == searchTerm
                        || p.Person.Nic == searchTerm)));
            else
                q = q.Where(p => p.Person != null &&
                    (p.Person.NameWithInitials.Contains(searchTerm)
                    || p.Person.FullName.Contains(searchTerm)
                    || p.Person.Initials.Contains(searchTerm)
                    || p.Person.Name.Contains(searchTerm)
                    || p.Person.Nic.Contains(searchTerm)
                    || p.Person.Surname.Contains(searchTerm)
                    || p.Person.LastName.Contains(searchTerm)
                    || p.Person.Mobile.Contains(searchTerm)));

            return q.OrderByDescending(p => p.CreatedAt).Take(50).ToListAsync();
        }

        public async Task<Patient> GetOrCreatePatientAsync(Patient patient, Person person)
        {
            if (person.Id == 0)
            {
                _db.Persons.Add(person);
                await _db.SaveChangesAsync();
            }

            if (patient.Id == 0)
            {
                patient.PersonId = person.Id;
                _db.Patients.Add(patient);
                await _db.SaveChangesAsync();
            }
            return patient;
        }

        public List<WebUser> GetActiveWebUsers()
        {
            return _db.WebUsers.Where(u => !u.Retired).OrderBy(u => u.UserName).ToList();
        }

        public async Task<long?> GetPatientCompanyIdAsync(long personId)
        {
            if (personId <= 0) return null;
            return await _db.CompanyEmployees
                .Where(e => e.PersonId == personId && !e.Retired)
                .Select(e => (long?)e.CompanyId)
                .FirstOrDefaultAsync();
        }

        public async Task<double> GetAppliedRateAsync(Item item, long? companyId = null, long? consultantId = null, long? resourceId = null)
        {
            if (item == null) return 0;

            if (resourceId.HasValue)
            {
                var resourceRate = await _db.ResourceServiceRates
                    .Where(r => r.ResourceId == resourceId.Value && r.ItemId == item.Id && !r.Retired && r.StaffRate > 0)
                    .OrderByDescending(r => r.EditedAt)
                    .ThenByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync();
                if (resourceRate != null && resourceRate.StaffRate > 0)
                    return resourceRate.StaffRate;
            }

            if (consultantId.HasValue)
            {
                var consultantRate = await _db.ConsultantServiceRates
                    .Where(c => c.ConsultantId == consultantId.Value && c.ItemId == item.Id && !c.Retired && c.StaffRate > 0)
                    .OrderByDescending(c => c.EditedAt)
                    .ThenByDescending(c => c.CreatedAt)
                    .FirstOrDefaultAsync();
                if (consultantRate != null && consultantRate.StaffRate > 0)
                    return consultantRate.StaffRate;
            }

            if (companyId.HasValue)
            {
                var companyRate = await _db.CompanyServiceRates
                    .Where(c => c.CompanyId == companyId.Value && c.ItemId == item.Id && !c.Retired && c.Rate > 0)
                    .OrderByDescending(c => c.EditedAt)
                    .ThenByDescending(c => c.CreatedAt)
                    .FirstOrDefaultAsync();
                if (companyRate != null && companyRate.Rate > 0)
                    return companyRate.Rate;
            }

            return item.Total ?? 0;
        }
    }
}
