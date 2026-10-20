using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models.Legacy
{
    /// <summary>
    /// LEGACY OUTDOOR PATIENT BILL DETAIL - one row per item/service line of an indent.
    /// Source table <c>OBIL_DET</c>, migrated from <c>E:\Projects\HMC\billsys\obil_det.dbf</c>.
    ///
    /// Business rules
    ///   * NO HOS_CODE column - the parent link is the pair (DEPT, SLIPNO) which points
    ///     at <see cref="ObilMst"/>. (DEPT,SLIPNO,CODE) repeats in the data,
    ///     so edits/deletes must target the surrogate <see cref="Id"/>.
    ///   * <c>CODE</c> is the 6-char legacy item code; <c>QTY</c>, <c>RATE</c>, <c>NAMT</c>
    ///     are the line quantities/amounts (NAMT = QTY * RATE).
    ///   * <c>REM</c> is the free-text remark shown in the detail grid.
    ///   * <c>FLAG</c> and <c>CONCODE</c> relate to consultant commission splits (legacy).
    ///   * <c>CONPAY</c> is the consultant payable amount for the line.
    ///   * Soft delete only: Retired=1 hides a line from totals (no hard delete).
    ///
    /// Developer: Tajwer Jalal | Date: 20-Oct-2026 | Time: 14:35
    /// </summary>
    [Table("OBIL_DET")]
    public class ObilDet
    {
        /// <summary>Surrogate key added by migration (BIGINT IDENTITY).</summary>
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>Legacy 2-char department code (matches OBIL_MST.DEPT).</summary>
        [Column("DEPT")]
        public string Dept { get; set; }

        /// <summary>Slip/indent number within the department.</summary>
        [Column("SLIPNO")]
        public string SlipNo { get; set; }

        /// <summary>Legacy 6-char item/service code.</summary>
        [Column("CODE")]
        public string Code { get; set; }

        /// <summary>Quantity.</summary>
        [Column("QTY")]
        public decimal? Qty { get; set; }

        /// <summary>Rate per unit.</summary>
        [Column("RATE")]
        public decimal? Rate { get; set; }

        /// <summary>Net amount (QTY * RATE).</summary>
        [Column("NAMT")]
        public decimal? NAmt { get; set; }

        /// <summary>Reference ID (legacy).</summary>
        [Column("RID")]
        public string Rid { get; set; }

        /// <summary>Company/consultant code.</summary>
        [Column("CID")]
        public string Cid { get; set; }

        /// <summary>Description / service name.</summary>
        [Column("DESC")]
        public string Desc { get; set; }

        /// <summary>Remarks / free text.</summary>
        [Column("REM")]
        public string Rem { get; set; }

        /// <summary>Consultant code (legacy commission split).</summary>
        [Column("CONCODE")]
        public string ConCode { get; set; }

        /// <summary>Flag (legacy).</summary>
        [Column("FLAG")]
        public string Flag { get; set; }

        /// <summary>Consultant payable amount (legacy).</summary>
        [Column("CONPAY")]
        public decimal? ConPay { get; set; }

        /// <summary>VFP audit: user id.</summary>
        [Column("USERID")]
        public string UserId { get; set; }

        /// <summary>VFP audit: update date.</summary>
        [Column("UDATE")]
        public DateTime? UDate { get; set; }

        /// <summary>VFP audit: update time.</summary>
        [Column("UTIME")]
        public string UTime { get; set; }

        /// <summary>Soft delete flag: true = line is retired and excluded from totals.</summary>
        [Column("Retired")]
        public bool? Retired { get; set; }
    }
}