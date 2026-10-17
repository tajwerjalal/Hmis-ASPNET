using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models.Legacy
{
    /// <summary>
    /// LEGACY INPATIENT BILL HEADER - one row per department indent (slip).
    /// Source table <c>IBIL_MST</c>, migrated from <c>E:\Projects\HMC\billsys\ibil_mst.dbf</c>.
    ///
    /// Business rules
    ///   * Key used by VFP is (HOS_CODE, DEPT, SLIPNO) but it is NOT unique in the
    ///     data (72 duplicate groups) - address rows by the surrogate <see cref="Id"/>.
    ///   * <c>TYPE</c> = 'N' normal, 'R' return (return must be subtracted in totals:
    ///     SUM(IIF(TYPE&lt;&gt;'R', NTOTAL, NTOTAL*-1)) - see VFP BS0110.updbuttons).
    ///   * <c>BILL</c> is stamped by the generator (VFP GENBILLI / GenerateIpdBill):
    ///     NULL or 0 = unbilled, otherwise the IPDBILL.BILLNO that consumed it.
    ///     -1 is a legacy sentinel for masked VFP values ('******'), i.e. NOT unbilled.
    ///   * DETAIL lines live in <see cref="IbilDet"/> joined on (DEPT, SLIPNO).
    ///   * Side effects: written by the slip entry pages and by the bill generator;
    ///     never hard-deleted by this application.
    ///
    /// Developer: Tajwer Jalal | Date: 12-Oct-2026 | Time: 12:06
    /// </summary>
    [Table("IBIL_MST")]
    public class IbilMst
    {
        /// <summary>Surrogate row identity (added by migration 01). Use this for edits/deletes.</summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>Registration number (= REGISTER.REGNO for TYPE='I').</summary>
        [Column("HOS_CODE")]
        [MaxLength(16)]
        public string HosCode { get; set; }

        /// <summary>Legacy 2-char department code (see LegacyDepartment).</summary>
        [Column("DEPT")]
        [MaxLength(2)]
        public string Dept { get; set; }

        /// <summary>Slip (indent) number, sequence per department - not globally unique.</summary>
        [Column("SLIPNO")]
        [MaxLength(10)]
        public string SlipNo { get; set; }

        [Column("SLIPDT")]
        public DateTime? SlipDt { get; set; }

        /// <summary>'N' normal, 'R' return.</summary>
        [Column("TYPE")]
        [MaxLength(1)]
        public string Type { get; set; }

        public decimal? NTotal { get; set; }

        [Column("STAT")]
        [MaxLength(1)]
        public string Stat { get; set; }

        /// <summary>Consuming bill no (NULL/0 = unbilled; -1 = legacy masked sentinel).</summary>
        public long? Bill { get; set; }

        public DateTime? BDate { get; set; }

        [MaxLength(16)]
        public string UserId { get; set; }

        /// <summary>
        /// Last-update date. DB column <c>IBIL_MST.UDATE</c> is a <c>date</c>, so this
        /// must be a <see cref="DateTime"/>; a string mapping throws InvalidCastException
        /// as soon as a row with a non-null value is materialised.
        /// Developer: Tajwer Jalal | Date: 15-Oct-2026 | Time: 15:30
        /// </summary>
        [Column("UDATE")]
        public DateTime? UDate { get; set; }

        [MaxLength(8)]
        public string UTime { get; set; }

        /// <summary>Signed net value: returns subtracted. Used by the department summary.</summary>
        [NotMapped]
        public decimal SignedTotal => (Type == "R" ? -(NTotal ?? 0) : (NTotal ?? 0));
    }
}
