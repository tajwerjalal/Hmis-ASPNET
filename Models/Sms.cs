using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("SMS")]
    public class Sms
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long? PatientInvestigationId { get; set; }
        public long? BillId { get; set; }
        public long? PatientReportId { get; set; }
        public string SendingMessage { get; set; }
        public string ReceivedMessage { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }
        public string SendingUrl { get; set; }
        public string Otp { get; set; }
        public string ReceipientNumber { get; set; }
        public MessageType? SmsType { get; set; }
        public bool? SentSuccessfully { get; set; }
        public bool Pending { get; set; }
        public DateTime? SentAt { get; set; }

        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; }

        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }

        [ForeignKey("BillId")]
        public virtual Bill Bill { get; set; }

        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }

        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }

        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }
    }
}
