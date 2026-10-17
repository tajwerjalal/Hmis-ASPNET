using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    /// <summary>
    /// Reverse Settlement request - patient (private) settlement reversal or
    /// discount change request. Migrated from the VFP table <c>unsettle.dbf</c>
    /// used by form <c>unsettle</c> (Reverse Settlement, Pvt. Patients).
    /// A reverse-type request resets the encounter's settled flag.
    /// </summary>
    [Table("UNSETTLE")]
    public class UnsettleRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [MaxLength(16)]
        public string ReqNo { get; set; }

        public DateTime? ReqDate { get; set; }

        [MaxLength(16)]
        public string Regno { get; set; }

        public long? EncounterId { get; set; }

        /// <summary>1 = reverse settlement, 2 = change discount.</summary>
        public int? ChgType { get; set; }

        [MaxLength(16)]
        public string OldBillNo { get; set; }

        public double? OldBillAmt { get; set; }

        public DateTime? ChgDate { get; set; }

        [MaxLength(16)]
        public string ChgBy { get; set; }

        /// <summary>Bill reset flag (request processed/reversed).</summary>
        public bool Reset { get; set; }

        [MaxLength(16)]
        public string NewBillNo { get; set; }

        public double? NewBillAmt { get; set; }

        public DateTime? NchgDate { get; set; }

        [MaxLength(16)]
        public string NchgBy { get; set; }

        public double? OldDiscAmt { get; set; }

        [MaxLength(16)]
        public string OldDiscBy { get; set; }

        public double? NewDiscAmt { get; set; }

        [MaxLength(16)]
        public string NewDiscBy { get; set; }

        public bool Retired { get; set; }
        public string RetireComments { get; set; }
        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }

        [ForeignKey("EncounterId")]
        public virtual PatientEncounter Encounter { get; set; }
    }
}
