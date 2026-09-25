using HmisWeb.Data;
using HmisWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Services
{
    public interface IAppSession
    {
        WebUser LoggedUser { get; }
        Institution LoggedSite { get; }
        Department LoggedDepartment { get; }
        long? WebUserId { get; }
        bool IsLoggedIn { get; }
        bool HasPrivilege(string privilege);
        void SetUser(WebUser user);
        void SetDepartment(long? departmentId);
        void Clear();
    }

    public class AppSession : IAppSession
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _db;

        public AppSession(IHttpContextAccessor httpContextAccessor, ApplicationDbContext db)
        {
            _httpContextAccessor = httpContextAccessor;
            _db = db;
        }

        private ISession Session => _httpContextAccessor.HttpContext?.Session;

        public WebUser LoggedUser
        {
            get
            {
                var id = GetLong("LoggedUserId");
                if (id == null) return null;
                return _db.WebUsers
                    .Include(u => u.WebUserPerson)
                    .Include(u => u.WebUserRole)
                    .FirstOrDefault(u => u.Id == id && !u.Retired);
            }
        }

        public long? WebUserId => GetLong("LoggedUserId");

        public Institution LoggedSite
        {
            get
            {
                var id = GetLong("LoggedSiteId");
                if (id == null) return null;
                return _db.Institutions.FirstOrDefault(i => i.Id == id && !i.Retired);
            }
        }

        public Department LoggedDepartment
        {
            get
            {
                var id = GetLong("LoggedDepartmentId");
                if (id == null) return null;
                return _db.Departments.FirstOrDefault(d => d.Id == id && !d.Retired);
            }
        }

        public bool IsLoggedIn => GetLong("LoggedUserId") != null;

        public void SetUser(WebUser user)
        {
            Session?.SetString("LoggedUserId", user.Id.ToString());
        }

        public void SetDepartment(long? departmentId)
        {
            if (departmentId == null)
                Session?.Remove("LoggedDepartmentId");
            else
                Session?.SetString("LoggedDepartmentId", departmentId.ToString());
        }

        public bool HasPrivilege(string privilege)
        {
            if (string.IsNullOrEmpty(privilege)) return true;

            var user = LoggedUser;
            if (user == null) return false;

            // Check cached privileges in session
            var cached = Session?.Get("Privileges");
            string[] rolePrivs = null;
            if (cached != null)
            {
                rolePrivs = System.Text.Json.JsonSerializer
                    .Deserialize<string[]>(System.Text.Encoding.UTF8.GetString(cached));
            }

            if (rolePrivs == null && user.WebUserRole != null)
            {
                rolePrivs = (user.WebUserRole.Privileges ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
                Session?.Set("Privileges", System.Text.Encoding.UTF8.GetBytes(
                    System.Text.Json.JsonSerializer.Serialize(rolePrivs)));
            }

            if (rolePrivs == null) return true; // legacy full access
            return rolePrivs.Contains(privilege, StringComparer.OrdinalIgnoreCase);
        }

        public void Clear()
        {
            Session?.Remove("LoggedUserId");
            Session?.Remove("LoggedSiteId");
            Session?.Remove("LoggedDepartmentId");
            Session?.Remove("Privileges");
        }

        private long? GetLong(string key)
        {
            var val = Session?.GetString(key);
            if (long.TryParse(val, out long result)) return result;
            return null;
        }
    }
}
