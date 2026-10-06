using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

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

        // Sample Collection fields
        public bool SampleCollected { get; set; }
        public DateTime? SampleCollectedAt { get; set; }
        public long? SampleCollectedById { get; set; }
        public string SampleType { get; set; }
        public string SampleContainer { get; set; }
        public string SampleVolume { get; set; }
        public string SampleCondition { get; set; }
        public string Barcode { get; set; }
        public string BarcodeLabel { get; set; }
        public long? CollectorId { get; set; }
        public DateTime? CollectionDateTime { get; set; }
        public string CollectionNotes { get; set; }

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
        [ForeignKey("CollectorId")]
        public virtual WebUser? Collector { get; set; }
    }
}
