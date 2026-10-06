using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("USORDER")]
    public class UsOrder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long? PatientEncounterId { get; set; }
        public long? PatientId { get; set; }
        public long? VisitId { get; set; }
        public long? UsServiceId { get; set; }
        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }
        public long? BillId { get; set; }
        public int Qty { get; set; } = 1;

        public string OrderName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public bool Ordered { get; set; }
        public bool SampleCollected { get; set; }
        public DateTime? SampleCollectedAt { get; set; }
        public long? SampleCollectedById { get; set; }
        public string SampleType { get; set; }
        public string SampleCondition { get; set; }
        public string Barcode { get; set; }
        public string BarcodeLabel { get; set; }
        public long? CollectorId { get; set; }
        public DateTime? CollectionDateTime { get; set; }
        public string CollectionNotes { get; set; }

        public bool Completed { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Result { get; set; } = string.Empty;
        public string ResultNotes { get; set; } = string.Empty;
        public string ImagesJson { get; set; }  // Store image paths as JSON

        // Rich-text report body produced by the Quill editor on the Result Entry workspace.
        // "Result" stays the plain-text version used by the register report and search.
        public string ResultHtml { get; set; } = string.Empty;

        // Draft / Saved / Finalized lifecycle for the rich-text report.
        public UsReportStatus ReportStatus { get; set; } = UsReportStatus.Draft;
        public DateTime? FinalizedAt { get; set; }
        public long? FinalizedById { get; set; }
        public DateTime? LastSavedAt { get; set; }
        public int WordCount { get; set; }
        public int CharCount { get; set; }

        public long? ReportTemplateId { get; set; }
        public virtual UsReportTemplate? ReportTemplate { get; set; }
        public virtual WebUser? FinalizedBy { get; set; }

        public long? DoctorId { get; set; }
        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; } = string.Empty;
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }

        [ForeignKey("PatientEncounterId")]
        public virtual PatientEncounter? PatientEncounter { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }
        [ForeignKey("UsServiceId")]
        public virtual UsService? UsService { get; set; }
        [ForeignKey("InstitutionId")]
        public virtual Institution? Institution { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }
        [ForeignKey("DoctorId")]
        public virtual Doctor? Doctor { get; set; }
        [ForeignKey("BillId")]
        public virtual Bill? Bill { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser? Creater { get; set; }
        [ForeignKey("CollectorId")]
        public virtual WebUser? Collector { get; set; }
        [ForeignKey("SampleCollectedById")]
        public virtual WebUser? SampleCollectedBy { get; set; }
    }
}