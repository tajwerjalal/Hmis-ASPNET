using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    /// <summary>
    /// Patient ownership record - the consultant who "owns" a patient registration.
    /// Migrated from the VFP master <c>powner.dbf</c> used by form <c>patowner</c>
    /// (Assign Patient Ownership). During recalculation (recalcon/orecalcon) the
    /// owning consultant receives 100% of his line's net amount as consultant pay.
    /// </summary>
    [Table("POWNER")]
    public class PatientOwner
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>Registration type: 'I' (IPD) or 'O' (OPD).</summary>
        [MaxLength(1)]
        public string Type { get; set; } = "I";

        /// <summary>Legacy registration number.</summary>
        [MaxLength(16)]
        public string Regno { get; set; }

        public long? EncounterId { get; set; }

        /// <summary>Legacy consultant code (consult.ID) that owns the patient.</summary>
        [MaxLength(8)]
        public string OwnerCode { get; set; }

        /// <summary>Resolved owner consultant (STAFF) if the code matched a staff record.</summary>
        public long? OwnerId { get; set; }

        public bool Retired { get; set; }
        public string RetireComments { get; set; }
        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }

        [ForeignKey("EncounterId")]
        public virtual PatientEncounter Encounter { get; set; }

        [ForeignKey("OwnerId")]
        public virtual Staff Owner { get; set; }
    }
}
