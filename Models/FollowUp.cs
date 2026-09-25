using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("FOLLOWUP")]
    public class FollowUp
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long? PatientEncounterId { get; set; }
        public long? PatientId { get; set; }
        public long? VisitId { get; set; }
        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }

        public DateTime? NextVisitDate { get; set; }
        public string TreatmentPlan { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public bool Completed { get; set; }
        public DateTime? CompletedAt { get; set; }

        public long? DoctorId { get; set; }
        public long? RecommendedDoctorId { get; set; }
        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; } = string.Empty;

        [ForeignKey("PatientEncounterId")]
        public virtual PatientEncounter? PatientEncounter { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }
        [ForeignKey("InstitutionId")]
        public virtual Institution? Institution { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }
        [ForeignKey("DoctorId")]
        public virtual Doctor? Doctor { get; set; }
        [ForeignKey("RecommendedDoctorId")]
        public virtual Doctor? RecommendedDoctor { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser? Creater { get; set; }
    }
}
