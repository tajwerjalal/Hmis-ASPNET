using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    /// <summary>
    /// One stay of a patient in a room / bed.
    ///
    /// The registration screens now link the stay to [WARD] and [BED_POS] as
    /// well, so a movement can be traced from the bed master instead of only
    /// through the legacy [ROOM] master.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 22-Oct-2026
    /// Time      : 16:20
    /// Change Log: 22-Oct-2026 - WardId / BedPositionId added.
    /// </summary>
    [Table("PATIENTROOM")]
    public class PatientRoom
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public DateTime? FromTime { get; set; }
        public DateTime? ToTime { get; set; }
        public bool Discharged { get; set; }
        public DateTime? DischargedAt { get; set; }
        public long? RoomId { get; set; }
        public long? PatientEncounterId { get; set; }
        public long? AdmissionId { get; set; }
        public long? WardId { get; set; }
        public long? BedPositionId { get; set; }
        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }
        [ForeignKey("RoomId")]
        public virtual Room Room { get; set; }

        /// <summary>Ward the patient is staying in (legacy [WARD]).</summary>
        [ForeignKey("WardId")]
        public virtual Ward Ward { get; set; }

        /// <summary>Bed position the patient is staying in (legacy [BED_POS]).</summary>
        [ForeignKey("BedPositionId")]
        public virtual BedPosition BedPosition { get; set; }
        [ForeignKey("PatientEncounterId")]
        public virtual PatientEncounter PatientEncounter { get; set; }
        [ForeignKey("AdmissionId")]
        public virtual Admission Admission { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
    }
}
