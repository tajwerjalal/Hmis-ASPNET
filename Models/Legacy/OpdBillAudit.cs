using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models.Legacy
{
    /// <summary>
    /// OPD BILL AUDIT - one immutable row per audited action on an outpatient file.
    /// Created by migration OPD bill schema. Rows are only ever INSERTed.
    ///
    /// Business rules
    ///   * Action values: LINE_ADD, LINE_EDIT, LINE_DEL, GENERATE, REVERSE, STATUS.
    ///   * Entity: OBIL_DET / OBIL_MST / OPDBILL / REGISTER.
    ///   * EntityKey: row Id for OBIL_DET / OBIL_MST, otherwise the business key
    ///     ('dept|slipno' or the bill/reg number).
    ///   * Reason is mandatory (enforced in code) for REVERSE.
    ///   * OldValue / NewValue hold the field-level before/after pair.
    ///
    /// Developer: Tajwer Jalal | Date: 20-Oct-2026 | Time: 14:45
    /// </summary>
    [Table("OPDBILL_AUDIT")]
    public class OpdBillAudit
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>Registration number (REGISTER.REGNO, TYPE='O').</summary>
        [Column("RegNo")]
        [StringLength(16)]
        public string RegNo { get; set; }

        /// <summary>Action performed: LINE_ADD, LINE_EDIT, LINE_DEL, GENERATE, REVERSE, STATUS.</summary>
        [Column("Action")]
        [StringLength(32)]
        public string Action { get; set; }

        /// <summary>Entity affected: OBIL_DET, OBIL_MST, OPDBILL, REGISTER.</summary>
        [Column("Entity")]
        [StringLength(32)]
        public string Entity { get; set; }

        /// <summary>Business key of the affected entity (row Id or dept|slipno or billno).</summary>
        [Column("EntityKey")]
        [StringLength(64)]
        public string EntityKey { get; set; }

        /// <summary>Field name for field-level changes.</summary>
        [Column("Field")]
        [StringLength(64)]
        public string Field { get; set; }

        /// <summary>Old value (before change).</summary>
        [Column("OldValue")]
        public string OldValue { get; set; }

        /// <summary>New value (after change).</summary>
        [Column("NewValue")]
        public string NewValue { get; set; }

        /// <summary>Reason for the change (mandatory for REVERSE).</summary>
        [Column("Reason")]
        public string Reason { get; set; }

        /// <summary>User name who performed the action.</summary>
        [Column("UserName")]
        [StringLength(64)]
        public string UserName { get; set; }

        /// <summary>Web user id (FK to WebUser).</summary>
        [Column("WebUserId")]
        public long? WebUserId { get; set; }

        /// <summary>UTC timestamp of the action.</summary>
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}