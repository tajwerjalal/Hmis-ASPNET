using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models.Legacy
{
    /// <summary>
    /// LEGACY GENERATED IPD BILL HEADER - one row per generated inpatient bill.
    /// Source table <c>IPDBILL</c>, migrated from <c>E:\Projects\HMC\billsys\ipdbill.dbf</c>.
    /// Written by VFP <c>GENBILLI</c> and by <c>/billing/GenerateIpdBill</c>.
    ///
    /// Business rules
    ///   * BILLNO comes from <c>BCONTROL.IPDBILL</c> (allocated with UPDLOCK, ROWLOCK,
    ///     incremented by 1) - never reuse a number.
    ///   * NTOT is the sum of the stamped unbilled slips (returns subtracted).
    ///   * A reverse (Officer, reason mandatory) deletes this header and clears
    ///     IBIL_MST.BILL for the same HOS_CODE.
    ///
    /// Developer: Tajwer Jalal | Date: 12-Oct-2026 | Time: 12:06
    /// </summary>
    [Table("IPDBILL")]
    public class IpdBillHeader
    {
        /// <summary>Bill number (from BCONTROL.IPDBILL).</summary>
        [Column("BILLNO")]
        public long BillNo { get; set; }

        /// <summary>Bill date (user supplied).</summary>
        public DateTime? BDate { get; set; }

        /// <summary>Period start used when the bill was raised.</summary>
        public DateTime? SDate { get; set; }

        /// <summary>Period end used when the bill was raised.</summary>
        public DateTime? EDate { get; set; }

        /// <summary>Registration number.</summary>
        [Column("HOS_CODE")]
        [MaxLength(16)]
        public string HosCode { get; set; }

        [Column("PCODE")]
        [MaxLength(16)]
        public string PCode { get; set; }

        [Column("CID")]
        [MaxLength(8)]
        public string Cid { get; set; }

        /// <summary>Billed total.</summary>
        public decimal? NTot { get; set; }

        public DateTime? RDate { get; set; }

        [Column("STAT")]
        [MaxLength(1)]
        public string Stat { get; set; }

        [MaxLength(16)]
        public string UserId { get; set; }

        /// <summary>
        /// Last-update date. DB column <c>IPDBILL.UDATE</c> is a <c>date</c>, so this
        /// must be a <see cref="DateTime"/>; a string mapping throws InvalidCastException
        /// as soon as a row with a non-null value is materialised.
        /// Developer: Tajwer Jalal | Date: 15-Oct-2026 | Time: 15:30
        /// </summary>
        [Column("UDATE")]
        public DateTime? UDate { get; set; }

        [MaxLength(8)]
        public string UTime { get; set; }

        [MaxLength(16)]
        public string RUserId { get; set; }

        [MaxLength(8)]
        public string RTime { get; set; }
    }
}
