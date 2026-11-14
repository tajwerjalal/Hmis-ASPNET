using HmisWeb.Data;
using HmisWeb.Services;

namespace HmisWeb.Pages.Cnp
{
    /// <summary>
    /// Consultancy &amp; Procedures indent (route /cnp/indent).
    ///
    /// What it does
    ///   * Runs the shared department indenting process
    ///     (Pages/Shared/DepartmentIndentPageModel) - the same flow as
    ///     /us/indent: patient search, IPD detection, service popup, cart and
    ///     one-transaction save.
    ///   * Catalogue: [SERVICES] rows owned by DEPT 11
    ///     (DeptServiceScopes.CnpDept).
    ///
    /// Why it exists
    ///   * The old page read INV-CNP-* mirror rows from [ITEM] and used a
    ///     different three-step layout. Consultancy now shares the same
    ///     catalogue and indenting process as Laboratory, Ultrasound, X-ray,
    ///     Dental and OT.
    ///
    /// Business rules / validations
    ///   * Inherited from DepartmentIndentPageModel: patient and one line
    ///     required, dept-scoped services only, panel rate resolution, discount
    ///     clamping, IPD account vs unpaid OPD bill.
    ///
    /// Parameters / return values
    ///   * Inherited handlers; Save redirects back to /cnp/indent with a
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

        public override string ServiceDept => DeptServiceScopes.CnpDept;
        public override string ModuleName => "Consultancy & Procedures";
        public override string ModuleShortName => "CNP";
        public override string RoutePath => "/Cnp/Indent";
        public override string InvoicePrintRoute => "/Cnp/Reports/IndentPrintout";
        public override string PatientSessionKey => "CnpIndentPatientId";
        public override string CartSessionKey => "CnpIndentCart";
    }
}
