using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models.Legacy
{
    /// <summary>
    /// LEGACY INPATIENT BILL DETAIL - one row per item/service line of an indent.
    /// Source table <c>IBIL_DET</c>, migrated from <c>E:\Projects\HMC\billsys\ibil_det.dbf</c>.
    ///
    /// Business rules
    ///   * NO HOS_CODE column - the parent link is the pair (DEPT, SLIPNO) which points
    ///     at <see cref="IbilMst"/>.  (DEPT,SLIPNO,CODE) repeats 43,551 times in the data,
    ///     so edits/deletes must target the surrogate <see cref="Id"/>.
    ///   * <c>CODE</c> is the 6-char legacy item code; <c>QTY</c>, <c>RATE</c>, <c>NAMT</c>
    ///     are the line quantities/amounts (NAMT = QTY * RATE).
    ///   * <c>REM</c> is the free-text remark shown in the detail grid.
    ///   * Soft delete only: Retired=1 hides a line from totals (no hard delete).
    ///
    /// Developer: Tajwer Jalal | Date: 12-Oct-2026 | Time: 12:06
    /// </summary>
    [Table("IBIL_DET")]
    public class IbilDet
    {
        /// <summary>Surrogate row identity (added by migration 01).</summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Column("DEPT")]
        [MaxLength(2)]
        public string Dept { get; set; }

        [Column("SLIPNO")]
        [MaxLength(10)]
        public string SlipNo { get; set; }

        /// <summary>Legacy 6-char item code.</summary>
        [Column("CODE")]
        [MaxLength(12)]
        public string Code { get; set; }

        public decimal? Qty { get; set; }

        public decimal? Rate { get; set; }

        /// <summary>Net amount for the line (Qty * Rate).</summary>
        public decimal? NAmt { get; set; }

        /// <summary>Resource id (department equipment / room) when applicable. Legacy is a free-text code.</summary>
        [Column("RID")]
        [MaxLength(20)]
        public string Rid { get; set; }

        /// <summary>Company id (credit company) when the line was billed to a company.</summary>
        [Column("CID")]
        [MaxLength(8)]
        public string Cid { get; set; }

        [Column("DESC")]
        [MaxLength(200)]
        public string Desc { get; set; }

        /// <summary>Free-text remark shown in the detail grid.</summary>
        [Column("REM")]
        [MaxLength(200)]
        public string Rem { get; set; }

        /// <summary>Consultant code owning the line (when consultant-driven).</summary>
        [Column("CONCODE")]
        [MaxLength(8)]
        public string ConCode { get; set; }

        [Column("WCODE")]
        [MaxLength(8)]
        public string WCode { get; set; }

        [Column("FLAG")]
        [MaxLength(1)]
        public string Flag { get; set; }

        /// <summary>Consultant payable portion of the line.</summary>
        public decimal? ConPay { get; set; }

        [MaxLength(16)]
        public string UserId { get; set; }

        /// <summary>
        /// Last-update date. DB column <c>IBIL_DET.UDATE</c> is a <c>date</c>, so this
        /// must be a <see cref="DateTime"/>; a string mapping throws InvalidCastException
        /// as soon as a row with a non-null value is materialised.
        /// Developer: Tajwer Jalal | Date: 15-Oct-2026 | Time: 15:30
        /// </summary>
        [Column("UDATE")]
        public DateTime? UDate { get; set; }

        [MaxLength(8)]
        public string UTime { get; set; }

        /// <summary>
        /// Application-level soft delete added by the IPD Bill form.
        /// 0/null = visible, 1 = retired (excluded from totals, never physically deleted).
        /// </summary>
        public bool? Retired { get; set; }
    }
}
