using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("APPEMAIL")]
    public class AppEmail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public MessageType? SmsType { get; set; }
        public string ReceipientEmail { get; set; }
        public string MessageSubject { get; set; }
        public string MessageBody { get; set; }
        public string SenderUsername { get; set; }
        public string SenderPassword { get; set; }
        public string SenderEmail { get; set; }
        public string Attachment1 { get; set; }
        public string Attachment2 { get; set; }
        public string Attachment3 { get; set; }
        public string Attachment4 { get; set; }
        public bool? SentSuccessfully { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; }

        public long? PatientReportId { get; set; }
        public long? PatientInvestigationId { get; set; }
        public long? BillId { get; set; }
        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }
        public long? CreaterId { get; set; }
        public long? RetirerId { get; set; }

        [ForeignKey("BillId")]
        public virtual Bill Bill { get; set; }
        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }
    }
}
