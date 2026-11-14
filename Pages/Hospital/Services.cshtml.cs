using System.Linq;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Hospital
{
    /// <summary>
    /// Hospitalization Services Setup - the inpatient bank of the shared
    /// [SERVICES] master: room &amp; bed charges (DEPT 07) and ward
    /// procedures (DEPT 08). Wired from the sidebar "Hospitalization
    /// Services &rarr; Setup &rarr; Services Setup".
    ///
    /// Developer : [TAJWER JALAL]
    /// Created   : 22-Oct-2026 19:50
    /// Change Log: 22-Oct-2026 19:50 - moved onto the shared department
    ///             services base (paged grid, dept scope 07/08).
    /// </summary>
    public class ServicesModel : Shared.DepartmentServicesModel
    {
        public ServicesModel(ApplicationDbContext db, IAppSession session) : base(db, session) { }

        public override string ModuleTitle => "Hospitalization Services Setup";

        public override string[] AllowedDepts => DeptServiceScopes.HospitalDepts;

        protected override IQueryable<DeptService> Scoped() => Db.HospitalServices();
    }
}
