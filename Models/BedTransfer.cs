using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    /// <summary>
    /// One hospital bed movement, migrated from the legacy VFP file
    /// [HMIS].[dbo].[BEDTRANS] (ward transfer / admission bed assignment):
    ///
    ///   TYPE      C(1)   'I' = Inpatient admission / transfer, 'O' = Outpatient
    ///   REGNO     C(8)   Registration number (the BHT of the stay)
    ///   BEDNO     C(5)   Bed the patient was moved to / admitted to ("PW102")
    ///   BEDNOOLD  C(5)   Bed the patient was in before the move (empty for a
    ///                  fresh admission)
    ///   INDATE    D      Transfer / admission date
    ///   INTIME    C(5)   Transfer / admission time
    ///   SLIPNO    C(30)  Hospitalisation service indent generated on transfer
    ///   SLIPDT    D      Indent date
    ///
    /// The application keeps the columns it needs: the movement itself
    /// (from bed -> to bed, transfer date), the indent it may have generated and
    /// the soft-retire (with reason) that the list screen exposes. The legacy
    /// edit stamp is kept (USERID/UDATE/UTIME) alongside the app stamps.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 22-Oct-2026
    /// Time      : 16:20
    /// Change Log: 22-Oct-2026 - created from the bed_trans code of the
    ///             In-Patient / Ward Transfer screens (bedtrans.scx).
    /// </summary>
    [Table("BEDTRANSFER")]
    public class BedTransfer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>Movement type: 'I' = inpatient, 'O' = outpatient.</summary>
        [Column(TypeName = "varchar(1)")]
        [MaxLength(1)]
        public string Type { get; set; } = "I";

        /// <summary>Registration number (BHT) of the stay.</summary>
        [Column(TypeName = "varchar(30)")]
        [MaxLength(30)]
        public string RegNo { get; set; }

        public long? PatientEncounterId { get; set; }

        public long? AdmissionId { get; set; }

        public long? FromBedPositionId { get; set; }

        public long? ToBedPositionId { get; set; }

        [ForeignKey("PatientEncounterId")]
        public virtual PatientEncounter PatientEncounter { get; set; }

        [ForeignKey("AdmissionId")]
        public virtual Admission Admission { get; set; }

        /// <summary>Bed the patient moved from.</summary>
        [ForeignKey("FromBedPositionId")]
        public virtual BedPosition FromBedPosition { get; set; }

        /// <summary>Bed the patient moved to.</summary>
        [ForeignKey("ToBedPositionId")]
        public virtual BedPosition ToBedPosition { get; set; }

        [Column(TypeName = "varchar(10)")]
        [MaxLength(10)]
        public string FromBedCode { get; set; }

        [Column(TypeName = "varchar(10)")]
        [MaxLength(10)]
        public string ToBedCode { get; set; }

        /// <summary>Transfer / admission date and time.</summary>
        public DateTime? TransferDate { get; set; }

        /// <summary>Indent number generated on the transfer, if any.</summary>
        [Column(TypeName = "varchar(30)")]
        [MaxLength(30)]
        public string IndentNo { get; set; }

        /// <summary>Indent date.</summary>
        [Column(TypeName = "date")]
        public DateTime? IndentDate { get; set; }

        public bool Retired { get; set; }

        [Column(TypeName = "varchar(500)")]
        [MaxLength(500)]
        public string RetireComments { get; set; }

        public long? CreaterId { get; set; }

        public DateTime? CreatedAt { get; set; }

        public long? EditerId { get; set; }

        public DateTime? EditedAt { get; set; }

        public long? RetirerId { get; set; }

        public DateTime? RetiredAt { get; set; }

        /// <summary>Last edit user (legacy USERID stamp).</summary>
        [Column(TypeName = "varchar(15)")]
        [MaxLength(15)]
        public string UserId { get; set; }

        /// <summary>Last edit date (legacy UDATE stamp).</summary>
        [Column(TypeName = "date")]
        public DateTime? UDate { get; set; }

        /// <summary>Last edit time (legacy UTIME stamp).</summary>
        [Column(TypeName = "varchar(5)")]
        [MaxLength(5)]
        public string UTime { get; set; }

        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }

        [ForeignKey("EditerId")]
        public virtual WebUser Editer { get; set; }

        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }
    }
}
