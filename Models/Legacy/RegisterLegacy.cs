using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models.Legacy
{
    /// <summary>
    /// LEGACY REGISTER (INPATIENT FILE) - the patient's billing file, one row per
    /// registration.  Source table <c>REGISTER</c>, migrated from
    /// <c>E:\Projects\HMC\billsys\register.dbf</c>.  This is the exact field set of
    /// VFP form <c>BS0110</c> ("Indoor Patient") and is the header source for
    /// <c>/inward/Billing</c>.
    ///
    /// Business rules
    ///   * TYPE 'I' = inpatient, 'O' = outpatient.  Only 'I' rows appear on this page.
    ///   * BILLGEN 1 = the file has been billed (VFP .T.); SETTLE 1 = settled.
    ///   * BILLSTATUS (added by migration 01) drives the lifecycle:
    ///       0 Admitted | 1 Discharged | 2 Bill Updating | 3 Bill Generated | 4 Settled
    ///     Edits are allowed only while BILLSTATUS IN (1,2).
    ///   * Financial counters are maintained by the CASH module (VFP register.prg):
    ///       TRCVD receipts, TPYMT payments made on the patient's behalf,
    ///       CASHDISC settlement discount, TBILLAMT billed amount.
    ///   * Amount due (VFP BS0110.updbuttons):
    ///       nused - TRCVD + TPYMT - CASHDISC
    ///   * REGNO is the join key to IBIL_MST.HOS_CODE and to app PatientEncounter.
    ///
    /// Developer: Tajwer Jalal | Date: 12-Oct-2026 | Time: 12:06
    /// </summary>
    [Table("REGISTER")]
    public class RegisterLegacy
    {
        /// <summary>'I' inpatient, 'O' outpatient.</summary>
        [Column("TYPE")]
        [MaxLength(1)]
        public string Type { get; set; }

        /// <summary>Registration number - primary business key with TYPE.</summary>
        [Column("REGNO")]
        [MaxLength(16)]
        public string RegNo { get; set; }

        public DateTime? RDate { get; set; }

        [Column("RTIME")]
        [MaxLength(8)]
        public string RTime { get; set; }

        [Column("PCODE")]
        [MaxLength(16)]
        public string PCode { get; set; }

        /// <summary>Guardian / attendant name.</summary>
        [MaxLength(80)]
        public string GName { get; set; }

        [MaxLength(40)]
        public string GRelation { get; set; }

        [MaxLength(200)]
        public string Diagnose { get; set; }

        [MaxLength(20)]
        public string BedNo { get; set; }

        [MaxLength(40)]
        public string BedEntitle { get; set; }

        /// <summary>Discharge date - NULL while admitted.  IPD bill generation filters on it.</summary>
        public DateTime? DDate { get; set; }

        [Column("DTIME")]
        [MaxLength(8)]
        public string DTime { get; set; }

        /// <summary>Company id (credit company code).</summary>
        [MaxLength(8)]
        public string Cid { get; set; }

        /// <summary>Employee code (company employee).</summary>
        [MaxLength(16)]
        public string ECode { get; set; }

        [MaxLength(40)]
        public string ERelation { get; set; }

        /// <summary>Consultant code.</summary>
        [MaxLength(8)]
        public string ConCode { get; set; }

        [MaxLength(16)]
        public string RefNo { get; set; }

        [MaxLength(16)]
        public string RefValid { get; set; }

        [MaxLength(20)]
        public string CardNo { get; set; }

        /// <summary>Company/insurance status code ('A' active etc.).</summary>
        [MaxLength(2)]
        public string CStat { get; set; }

        /// <summary>1 = bill generated for this file (VFP BILLGEN .T.).</summary>
        public bool? BillGen { get; set; }

        /// <summary>Receipts received from the patient.</summary>
        public decimal? TRcvd { get; set; }

        /// <summary>Payments made on the patient's behalf (added back to the due).</summary>
        public decimal? TPymt { get; set; }

        /// <summary>Billed amount (total of all bills raised on this file).</summary>
        public decimal? TBillAmt { get; set; }

        /// <summary>Settlement discount.</summary>
        public decimal? CashDisc { get; set; }

        [MaxLength(16)]
        public string DiscBy { get; set; }

        /// <summary>1 = settled.</summary>
        public bool? Settle { get; set; }

        public DateTime? SettleDt { get; set; }

        [MaxLength(16)]
        public string UserId { get; set; }

        [MaxLength(8)]
        public string UDate { get; set; }

        [MaxLength(8)]
        public string UTime { get; set; }

        // ---- added by migration 01 (nullable, legacy imports untouched) ----

        /// <summary>0 Admitted | 1 Discharged | 2 Bill Updating | 3 Bill Generated | 4 Settled.</summary>
        public byte? BillStatus { get; set; }

        public DateTime? BillStatusDt { get; set; }

        [MaxLength(16)]
        public string BillStatusBy { get; set; }
    }
}
