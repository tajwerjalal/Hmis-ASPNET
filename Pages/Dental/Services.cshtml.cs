using System.Linq;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Dental
{
    /// <summary>
    /// Dental Services Setup - the dental bank of the shared [SERVICES] master
    /// (DEPT 04: extraction, filling, root canal, dentures, orthodontics).
    /// Wired from the sidebar "Dental Services &rarr; Setup &rarr; Services
    /// Setup" (it used to open the generic [ITEM] editor).
    ///
    /// Developer : [TAJWER JALAL]
    /// Created   : 22-Oct-2026 19:52
    /// Change Log: 22-Oct-2026 19:52 - created for the dental bank of the
    ///             shared SERVICES master.
    /// </summary>
    public class ServicesModel : Shared.DepartmentServicesModel
    {
        public ServicesModel(ApplicationDbContext db, IAppSession session) : base(db, session) { }

        public override string ModuleTitle => "Dental Services Setup";

        public override string[] AllowedDepts => DeptServiceScopes.DentalDepts;

        protected override IQueryable<DeptService> Scoped() => Db.DentalServices();
    }
}
