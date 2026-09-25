using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("PRESCRIPTION")]
    public class Prescription
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long? PatientEncounterId { get; set; }
        public long? PatientId { get; set; }
        public long? VisitId { get; set; }
        public long? ItemId { get; set; }
        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }

        public string MedicineName { get; set; } = string.Empty;
        public string Dose { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public int Qty { get; set; }
        public bool Dispensed { get; set; }
        public DateTime? DispensedAt { get; set; }
        public bool RequiredAction { get; set; }

        public long? DoctorId { get; set; }
        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; } = string.Empty;

        [ForeignKey("PatientEncounterId")]
        public virtual PatientEncounter? PatientEncounter { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }
        [ForeignKey("ItemId")]
        public virtual Item? Item { get; set; }
        [ForeignKey("InstitutionId")]
        public virtual Institution? Institution { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }
        [ForeignKey("DoctorId")]
        public virtual Doctor? Doctor { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser? Creater { get; set; }
    }
}
