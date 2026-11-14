using HmisWeb.Data;
using HmisWeb.Services;

namespace HmisWeb.Pages.Ot
{
    /// <summary>
    /// Operation Theatre indent (route /ot/indent).
    ///
    /// What it does
    ///   * Runs the shared department indenting process
    ///     (Pages/Shared/DepartmentIndentPageModel) - the same flow as
    ///     /us/indent: patient search, IPD detection, service popup, cart and
    ///     one-transaction save.
    ///   * Catalogue: [SERVICES] rows owned by DEPT 06 (DeptServiceScopes.OtDept).
    ///
    /// Why it exists
    ///   * The old page read INV-OT-* mirror rows from [ITEM]. OT now shares the
    ///     same catalogue and process as Laboratory, Ultrasound, X-ray and
    ///     Dental.
    ///
    /// Business rules / validations
    ///   * Inherited from DepartmentIndentPageModel: patient and one line
    ///     required, dept-scoped services only, panel rate resolution, discount
    ///     clamping, IPD account vs unpaid OPD bill.
    ///   * OT case records (OtCase / OtCaseItem) stay on the Case Entry screen;
    ///     this page only raises the charge.
    ///
    /// Parameters / return values
    ///   * Inherited handlers; Save redirects back to /ot/indent with a
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

        public override string ServiceDept => DeptServiceScopes.OtDept;
        public override string ModuleName => "Operation Theatre";
        public override string ModuleShortName => "OT";
        public override string RoutePath => "/Ot/Indent";
        public override string InvoicePrintRoute => "/Ot/Reports/IndentPrintout";
        public override string PatientSessionKey => "OtIndentPatientId";
        public override string CartSessionKey => "OtIndentCart";
    }
}
