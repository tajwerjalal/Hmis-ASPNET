using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models.Legacy
{
    /// <summary>
    /// IPD BILL AUDIT - one immutable row per audited action on an inpatient file.
    /// Created by migration 01_ipd_bill_schema.sql.  Rows are only ever INSERTed.
    ///
    /// Business rules
    ///   * Action values: LINE_ADD, LINE_EDIT, LINE_DEL, GENERATE, REVERSE,
    ///     SETTLE, UNSETTLE, DISCOUNT, STATUS.
    ///   * Entity: IBIL_DET / IBIL_MST / IPDBILL / REGISTER.
    ///   * EntityKey: row Id for IBIL_DET / IBIL_MST, otherwise the business key
    ///     ('dept|slipno' or the bill/reg number).
    ///   * Reason is mandatory (enforced in code) for REVERSE, UNSETTLE, DISCOUNT.
    ///   * OldValue / NewValue hold the field-level before/after pair.
    ///
    /// Developer: Tajwer Jalal | Date: 12-Oct-2026 | Time: 12:06
    /// </summary>
    [Table("IpdBillAudit")]
    public class IpdBillAudit
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Column("Regno")]
        [MaxLength(16)]
        public string RegNo { get; set; }

        public long? EncounterId { get; set; }

        [MaxLength(20)]
        public string Action { get; set; }

        [MaxLength(20)]
        public string Entity { get; set; }

        [MaxLength(80)]
        public string EntityKey { get; set; }

        [MaxLength(40)]
        public string Field { get; set; }

        [MaxLength(200)]
        public string OldValue { get; set; }

        [MaxLength(200)]
        public string NewValue { get; set; }

        [MaxLength(300)]
        public string Reason { get; set; }

        public long? WebUserId { get; set; }

        [MaxLength(80)]
        public string UserName { get; set; }

        [MaxLength(64)]
        public string RoleName { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
