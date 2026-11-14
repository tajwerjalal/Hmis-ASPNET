using HmisWeb.Data;
using HmisWeb.Services;

namespace HmisWeb.Pages.Dental
{
    /// <summary>
    /// Dental indent (route /dental/indent).
    ///
    /// What it does
    ///   * Runs the shared department indenting process
    ///     (Pages/Shared/DepartmentIndentPageModel) - the same flow as
    ///     /us/indent: patient search, IPD detection, service popup, cart and
    ///     one-transaction save.
    ///   * Catalogue: [SERVICES] rows owned by DEPT 04 (DeptServiceScopes.DentalDept).
    ///
    /// Why it exists
    ///   * The old page read INV-DNT-* mirror rows from [ITEM] and had its own
    ///     layout. Dental now behaves exactly like Laboratory, Ultrasound and
    ///     the other departments: one catalogue, one indenting flow.
    ///
    /// Business rules / validations
    ///   * Inherited from DepartmentIndentPageModel: patient and one line
    ///     required, dept-scoped services only, panel rate resolution, discount
    ///     clamping, IPD account vs unpaid OPD bill.
    ///
    /// Parameters / return values
    ///   * Inherited handlers; Save redirects back to /dental/indent with a
    ///     TempData confirmation.
    ///
    /// Developer : AI Assistant
    /// Created   : 27-Oct-2026 11:40
    /// </summary>
    public class IndentModel : Shared.DepartmentIndentPageModel
    {
        public IndentModel(ApplicationDbContext db, ICommonService common, IAppSession appSession)
            : base(db, common, appSession)
        {
        }

        public override string ServiceDept => DeptServiceScopes.DentalDept;
        public override string ModuleName => "Dental";
        public override string ModuleShortName => "DNT";
        public override string RoutePath => "/Dental/Indent";
        public override string InvoicePrintRoute => "/Dental/Reports/IndentPrintout";
        public override string PatientSessionKey => "DentalIndentPatientId";
        public override string CartSessionKey => "DentalIndentCart";
    }
}
