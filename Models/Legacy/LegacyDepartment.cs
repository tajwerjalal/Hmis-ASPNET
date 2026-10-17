using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models.Legacy
{
    /// <summary>
    /// LEGACY DEPARTMENT MASTER - the 8 legacy billing departments.
    /// Created by migration 01_ipd_bill_schema.sql (seeded from the hard-coded
    /// dictionary previously held in Pages/Billing/SlipEntry.cshtml.cs).
    ///
    /// Business rules
    ///   * Code is the 2-char legacy department code used by IBIL_MST / IBIL_DET
    ///     and by the slip entry pages (01 Lab, 02 X-Ray, 03 Ultra-Sound, 04 Dental,
    ///     06 O.T., 07 Hospitalization, 10 Pharmacy, 11 Consultancy & Procedure).
    ///   * CatCode is the category code used to scope the item lookup on slip entry.
    ///   * Retired departments stay in the table (no hard delete); they are filtered
    ///     out of pickers but still resolve for historical rows.
    ///
    /// Developer: Tajwer Jalal | Date: 12-Oct-2026 | Time: 12:06
    /// </summary>
    [Table("LegacyDepartment")]
    public class LegacyDepartment
    {
        [Key]
        public int Id { get; set; }

        /// <summary>2-char legacy department code.</summary>
        [MaxLength(2)]
        public string Code { get; set; }

        [MaxLength(40)]
        public string Name { get; set; }

        /// <summary>Category code used to scope the item lookup.</summary>
        [MaxLength(10)]
        public string CatCode { get; set; }

        public bool Retired { get; set; }
    }
}
