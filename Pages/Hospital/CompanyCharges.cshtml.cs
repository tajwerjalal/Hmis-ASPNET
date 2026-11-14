using HmisWeb.Data;
using HmisWeb.Pages.Shared;
using HmisWeb.Services;

namespace HmisWeb.Pages.Hospital
{
    /// <summary>
    /// Company wise charges of the HOSPITALIZATION module (SERVICES DEPT 07
    /// room &amp; bed charges + 08 ward procedures, billing mirror INV-HSP-).
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
        protected override string ItemPrefix => "INV-HSP-";

        /// <summary>Legacy department codes owned by this module (DEPT 07 + 08).</summary>
        protected override string[] DeptCodes => DeptServiceScopes.HospitalDepts;
    }
}
