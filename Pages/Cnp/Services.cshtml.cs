using System.Linq;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Cnp
{
    /// <summary>
    /// Consultancy &amp; Procedures Services Setup - the outpatient bank of the
    /// shared [SERVICES] master (DEPT 11: visits / consultation charges).
    /// Wired from the sidebar "Consultancy &amp; Procedures &rarr; Setup
    /// &rarr; Services Setup" (it used to open the generic [ITEM] editor).
    ///
    /// Developer : [TAJWER JALAL]
    /// Created   : 22-Oct-2026 19:50
    /// Change Log: 22-Oct-2026 19:50 - created for the consultancy bank of
    ///             the shared SERVICES master.
    /// </summary>
    public class ServicesModel : Shared.DepartmentServicesModel
    {
        public ServicesModel(ApplicationDbContext db, IAppSession session) : base(db, session) { }

        public override string ModuleTitle => "Consultancy & Procedures Services Setup";

        public override string[] AllowedDepts => DeptServiceScopes.ConsultDepts;

        protected override IQueryable<DeptService> Scoped() => Db.ConsultServices();
    }
}
