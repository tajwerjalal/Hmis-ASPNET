using System.Linq;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Ot
{
    /// <summary>
    /// Operation Theatre Services Setup - the OT bank of the shared [SERVICES]
    /// master (DEPT 06: major / medium / minor operations and procedures).
    /// Wired from the sidebar "Operation Theatre &rarr; Setup &rarr; Services
    /// Setup" (it used to open the generic [ITEM] editor).
    ///
    /// Developer : [TAJWER JALAL]
    /// Created   : 22-Oct-2026 19:53
    /// Change Log: 22-Oct-2026 19:53 - created for the operation-theatre bank
    ///             of the shared SERVICES master.
    /// </summary>
    public class ServicesModel : Shared.DepartmentServicesModel
    {
        public ServicesModel(ApplicationDbContext db, IAppSession session) : base(db, session) { }

        public override string ModuleTitle => "Operation Theatre Services Setup";

        public override string[] AllowedDepts => DeptServiceScopes.OtDepts;

        protected override IQueryable<DeptService> Scoped() => Db.OtServices();
    }
}
