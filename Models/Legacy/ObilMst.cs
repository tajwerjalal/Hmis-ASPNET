using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models.Legacy
{
    /// <summary>
    /// LEGACY OUTDOOR PATIENT BILL HEADER - one row per department indent (slip).
    /// Source table <c>OBIL_MST</c>, migrated from <c>E:\Projects\HMC\billsys\obil_mst.dbf</c>.
    ///
    /// Business rules
    ///   * Key used by VFP is (HOS_CODE, DEPT, SLIPNO) but it is NOT unique in the
    ///     data - address rows by the surrogate <see cref="Id"/> (added by migration).
    ///   * <c>TYPE</c> = 'N' normal, 'R' return (return must be subtracted in totals:
    ///     SUM(IIF(TYPE<>'R', NTOTAL, NTOTAL*-1)) - see VFP BS0101.updbuttons).
    ///   * <c>BILL</c> is stamped by the generator (VFP GENBILL / GenerateOpdBill):
    ///     NULL or 0 = unbilled, otherwise the OPDBILL.BILLNO that consumed it.
    ///   * <c>STAT</c> = 'E' entered, 'P' printed/billed.
    ///   * DETAIL lines live in <see cref="ObilDet"/> joined on (DEPT, SLIPNO).
    ///
    /// Developer: Tajwer Jalal | Date: 20-Oct-2026 | Time: 14:30
    /// </summary>
    [Table("OBIL_MST")]
    public class ObilMst
    {
        /// <summary>Surrogate key added by migration (BIGINT IDENTITY).</summary>
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>Hospital/registration code (REGISTER.REGNO for TYPE='O').</summary>
        [Column("HOS_CODE")]
        public string HosCode { get; set; }

        /// <summary>Legacy 2-char department code.</summary>
        [Column("DEPT")]
        public string Dept { get; set; }

        /// <summary>Slip/indent number within the department (9-digit string).</summary>
        [Column("SLIPNO")]
        public string SlipNo { get; set; }

        /// <summary>Slip date.</summary>
        [Column("SLIPDT")]
        public DateTime? SlipDt { get; set; }

        /// <summary>Slip type: 'N' normal, 'R' return (subtracted from totals).</summary>
        [Column("TYPE")]
        public string Type { get; set; }

        /// <summary>Net total of the slip (NTOTAL == SUM(NAMT) of its lines).</summary>
        [Column("NTOTAL")]
        public decimal? NTotal { get; set; }

        /// <summary>Bill status: 'E' entered, 'P' printed/billed.</summary>
        [Column("STAT")]
        public string Stat { get; set; }

        /// <summary>Bill number stamped by GENBILL. 0/NULL = unbilled.</summary>
        [Column("BILL")]
        public long? Bill { get; set; }

        /// <summary>Bill date (stamped when generated).</summary>
        [Column("BDATE")]
        public DateTime? BDate { get; set; }

        /// <summary>VFP audit: user id.</summary>
        [Column("USERID")]
        public string UserId { get; set; }

        /// <summary>VFP audit: update date.</summary>
        [Column("UDATE")]
        public DateTime? UDate { get; set; }

        /// <summary>VFP audit: update time.</summary>
        [Column("UTIME")]
        public string UTime { get; set; }
    }
}