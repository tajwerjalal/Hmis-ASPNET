using System.Linq;
using HmisWeb.Data;
using HmisWeb.Models;

namespace HmisWeb.Services
{
    /// <summary>
    /// Department scoping for the ONE canonical services master
    /// ([HMIS].[dbo].[SERVICES]). Every department's service forms call this
    /// table through the matching extension so a module only ever sees its
    /// own department's rows - as agreed on 22-Oct-2026 (Tajwer Jalal):
    /// "all services form each department must be linked to SERVICES only,
    /// records filtered according to the department calling it".
    ///
    /// Legacy DEPT codes (MIG_DEPT.tsv, verified against the live data
    /// 22-Oct-2026) and the module that owns them:
    ///   01 Laboratory            735 rows
    ///   02 X-ray / CT scan       174 rows
    ///   03 Ultrasound             59 rows
    ///   04 Dental                 46 rows (extraction, root canal, dentures)
    ///   06 Operation Theatre     292 rows (major / medium / minor operation)
    ///   07 Room &amp; bed charges  215 rows (hospitalization)
    ///   08 Ward procedures       104 rows (CPR, dressing, POP - hospitalization)
    ///   11 Visits                131 rows (consultancy &amp; procedures)
    /// Each row carries exactly one DEPT code, so the catalogue rows are
    /// visible to exactly one owning department.
    ///
    /// Developer : [TAJWER JALAL]
    /// Created   : 22-Oct-2026 16:40
    /// Change Log: 22-Oct-2026 16:40 - created; first scope: X-ray services.
    ///             22-Oct-2026 18:40 - lab scope added after [ITEM] lab purge.
    ///             22-Oct-2026 19:05 - hospitalization scope added.
    ///             22-Oct-2026 19:40 - department map completed (dental, OT,
    ///                                 consultancy, ultrasound) and every module
    ///                                 service page wired to its own scope.
    /// </summary>
    public static class DeptServiceScopes
    {
        /// <summary>Legacy DEPT code of the X-RAY/CT SCAN department.</summary>
        public const string XrayDept = "02";

        /// <summary>Legacy DEPT code of the LABORATORY department.</summary>
        public const string LabDept = "01";

        /// <summary>Legacy DEPT code of the ULTRASOUND department.</summary>
        public const string UsDept = "03";

        /// <summary>Legacy DEPT codes of the DENTAL department.</summary>
        public static readonly string[] DentalDepts = { "04" };

        /// <summary>
        /// Single legacy DEPT code of the DENTAL bank - used by /dental/indent.
        ///
        /// Modified by : AI Assistant | 27-Oct-2026 10:20
        /// What changed: added for the dental indent page migration to SERVICES.
        /// </summary>
        public const string DentalDept = "04";

        /// <summary>Legacy DEPT codes of the OPERATION THEATRE department.</summary>
        public static readonly string[] OtDepts = { "06" };

        /// <summary>
        /// Single legacy DEPT code of the OPERATION THEATRE bank - used by
        /// /ot/indent.
        ///
        /// Modified by : AI Assistant | 27-Oct-2026 10:20
        /// What changed: added for the OT indent page migration to SERVICES.
        /// </summary>
        public const string OtDept = "06";

        /// <summary>
        /// Legacy DEPT codes of the HOSPITALIZATION (inpatient) department:
        /// 07 room &amp; bed charges, 08 ward procedures.
        /// </summary>
        public static readonly string[] HospitalDepts = { "07", "08" };

        /// <summary>
        /// Legacy DEPT codes of the CONSULTANCY &amp; PROCEDURES department:
        /// 11 visits (consultation / subsequent visit / procedure charges).
        /// </summary>
        public static readonly string[] ConsultDepts = { "11" };

        /// <summary>
        /// Single legacy DEPT code of the CONSULTANCY &amp; PROCEDURES bank.
        /// Kept as a constant (in addition to <see cref="ConsultDepts"/>) so the
        /// /cnp/indent page can scope one query without allocating an array.
        ///
        /// Modified by : AI Assistant | 27-Oct-2026 10:05
        /// What changed: added for the CNP indent page migration to SERVICES.
        /// </summary>
        public const string CnpDept = "11";

        /// <summary>
        /// X-ray/CT services of the shared master (DEPT = '02'/'X-ray' rows
        /// only). Use for every X-ray screen, order or report lookup.
        /// </summary>
        public static IQueryable<DeptService> XrServices(this ApplicationDbContext db)
            => db.DeptServices.Where(s => s.Dept == XrayDept || s.Dept == "XR");

        /// <summary>
        /// Laboratory services of the shared master (DEPT = '01' rows only).
        /// Backs /lab/TestSetup and every lab order/report lookup - the lab
        /// tests no longer live on [ITEM] (ItemType 1/2 rows were dropped
        /// 22-Oct-2026 in the LabItemsPurge cleanup; Tajwer Jalal).
        /// </summary>
        public static IQueryable<DeptService> LabServices(this ApplicationDbContext db)
            => db.DeptServices.Where(s => s.Dept == LabDept || s.Dept == "LB");

        /// <summary>
        /// Ultrasound services of the shared master (DEPT = '03' rows only).
        /// </summary>
        public static IQueryable<DeptService> UsServices(this ApplicationDbContext db)
            => db.DeptServices.Where(s => s.Dept == UsDept || s.Dept == "US");

        /// <summary>
        /// Hospitalization services - the inpatient umbrella of the shared
        /// master (DEPT 07 room charges + 08 ward procedures). Backs
        /// /hospital/Services and the hospital indent screens.
        /// </summary>
        public static IQueryable<DeptService> HospitalServices(this ApplicationDbContext db)
            => db.DeptServices.Where(s => s.Dept == "07" || s.Dept == "08");

        /// <summary>Dental services of the shared master (DEPT = '04').</summary>
        public static IQueryable<DeptService> DentalServices(this ApplicationDbContext db)
            => db.DeptServices.Where(s => s.Dept == "04");

        /// <summary>Operation theatre services of the shared master (DEPT = '06').</summary>
        public static IQueryable<DeptService> OtServices(this ApplicationDbContext db)
            => db.DeptServices.Where(s => s.Dept == "06");

        /// <summary>
        /// Consultancy &amp; procedures services of the shared master
        /// (DEPT = '11' visits).
        /// </summary>
        public static IQueryable<DeptService> ConsultServices(this ApplicationDbContext db)
            => db.DeptServices.Where(s => s.Dept == "11");

        /// <summary>
        /// Display name of a legacy DEPT code (used by the module service
        /// pages and the department pickers).
        /// </summary>
        public static string HospitalDeptName(string dept) => dept switch
        {
            "01" => "Laboratory",
            "02" => "X-Ray / CT Scan",
            "03" => "Ultrasound",
            "04" => "Dental",
            "06" => "Operation Theatre",
            "07" => "Room & Bed Charges",
            "08" => "Ward Procedures",
            "11" => "Visits / Consultancy",
            _ => "Hospitalization"
        };

        /// <summary>All legacy department codes of the shared master.</summary>
        public static readonly string[] AllDepts = { "01", "02", "03", "04", "06", "07", "08", "11" };

        /// <summary>
        /// Billing mirror prefix per department code - used to work out which
        /// department a mirrored [ITEM] row belongs to.
        /// </summary>
        public static readonly (string Prefix, string Dept)[] MirrorPrefixes =
        {
            ("INV-LB-", "01"), ("INV-XR-", "02"), ("INV-US-", "03"),
            ("INV-DNT-", "04"), ("INV-OT-", "06"), ("INV-HSP-", "07"),
            ("INV-CNP-", "11")
        };
    }
}
