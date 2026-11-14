using HmisWeb.Data;
using HmisWeb.Pages.Shared;
using HmisWeb.Services;

namespace HmisWeb.Pages.Ot
{
    /// <summary>
    /// Company wise charges of the OPERATION THEATRE module (SERVICES DEPT 06,
    /// billing mirror INV-OT-).
    ///
    /// All behaviour lives in <see cref="ModuleCompanyChargesModel"/> - this
    /// class only names the module's mirror prefix and departments.
    ///
    /// Developer : [TAJWER JALAL]
    /// Change Log: 22-Oct-2026 11:15 - reduced to the shared base (the list is
    ///             now department-scoped + paged and the save writes the
    ///             legacy company/service/department key).
    /// </summary>
    public class CompanyChargesModel : ModuleCompanyChargesModel
    {
        public CompanyChargesModel(ApplicationDbContext db, IAppSession session)
            : base(db, session)
        {
        }

        /// <summary>Mirror prefix of this module on the shared services master.</summary>
        protected override string ItemPrefix => "INV-OT-";

        /// <summary>Legacy department codes owned by this module (DEPT 06).</summary>
        protected override string[] DeptCodes => DeptServiceScopes.OtDepts;
    }
}
