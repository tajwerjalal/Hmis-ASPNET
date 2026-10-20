using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models.Legacy
{
    /// <summary>
    /// LEGACY GENERATED OPD BILL HEADER - one row per generated outpatient bill.
    /// Source table <c>OPDBILL</c>, migrated from <c>E:\Projects\HMC\billsys\opdbill.dbf</c>.
    /// Written by VFP <c>GENBILL</c> and by <c>/billing/GenerateOpdBill</c>.
    ///
    /// Business rules
    ///   * BILLNO comes from <c>BCONTROL.OPDBILL</c> (allocated with UPDLOCK, ROWLOCK,
    ///     incremented by 1) - never reuse a number.
    ///   * NTOT is the sum of the stamped unbilled slips (returns subtracted).
    ///   * A reverse (Officer, reason mandatory) marks OPDBILL.STAT = 'X' and clears
    ///     OBIL_MST.BILL for the same HOS_CODE and bill number.
    ///
    /// Developer: Tajwer Jalal | Date: 20-Oct-2026 | Time: 14:40
    /// </summary>
    [Table("OPDBILL")]
    public class OpdBillHeader
    {
        /// <summary>Bill number (from BCONTROL.OPDBILL).</summary>
        [Key]
        [Column("BILLNO")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long BillNo { get; set; }

        /// <summary>Bill date (header).</summary>
        [Column("BDATE")]
        public DateTime? BDate { get; set; }

        /// <summary>Period start date (from GENBILL date range filter).</summary>
        [Column("SDATE")]
        public DateTime? SDate { get; set; }

        /// <summary>Period end date (from GENBILL date range filter).</summary>
        [Column("EDATE")]
        public DateTime? EDate { get; set; }

        /// <summary>Hospital/registration code (REGISTER.REGNO for TYPE='O').</summary>
        [Column("HOS_CODE")]
        public string HosCode { get; set; }

        /// <summary>Patient code (REGISTER.PCODE).</summary>
        [Column("PCODE")]
        public string PCode { get; set; }

        /// <summary>Company/consultant code (REGISTER.CID).</summary>
        [Column("CID")]
        public string Cid { get; set; }

        /// <summary>Net total of the bill (sum of slips, returns subtracted).</summary>
        [Column("NTOT")]
        public decimal? NTot { get; set; }

        /// <summary>Record date (stamp).</summary>
        [Column("RDATE")]
        public DateTime? RDate { get; set; }

        /// <summary>Bill status: '' (empty) = active, 'X' = reversed/cancelled.</summary>
        [Column("STAT")]
        public string Stat { get; set; }

        /// <summary>VFP audit: user id.</summary>
        [Column("USERID")]
        public string UserId { get; set; }

        /// <summary>VFP audit: update date.</summary>
        [Column("UDATE")]
        public DateTime? UDate { get; set; }

        /// <summary>VFP audit: update time.</summary>
        [Column("UTIME")]
        public string UTime { get; set; }

        /// <summary>VFP audit: reversing user.</summary>
        [Column("RUSERID")]
        public string RUserId { get; set; }

        /// <summary>VFP audit: reversal time.</summary>
        [Column("RTIME")]
        public string RTime { get; set; }
    }
}