using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("INVESTIGATIONORDER")]
    public class InvestigationOrder
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

        public string InvestigationName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Precautions { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public bool Ordered { get; set; }
        public bool Completed { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Result { get; set; } = string.Empty;
        public string ResultNotes { get; set; } = string.Empty;

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
