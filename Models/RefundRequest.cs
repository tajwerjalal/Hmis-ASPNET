using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("REFUNDREQUEST")]
    public class RefundRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long? BillId { get; set; }
        public long? PatientId { get; set; }
        public long? VisitId { get; set; }
        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }

        public string PatientName { get; set; } = string.Empty;
        public string FeeType { get; set; } = string.Empty;
        public double FeeAmount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;

        public bool Approved { get; set; }
        public long? ApprovedById { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public bool Refunded { get; set; }
        public long? RefundedById { get; set; }
        public DateTime? RefundedAt { get; set; }

        public bool Cancelled { get; set; }
        public long? CancelledById { get; set; }
        public DateTime? CancelledAt { get; set; }

        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; } = string.Empty;

        [ForeignKey("BillId")]
        public virtual Bill? Bill { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }
        [ForeignKey("VisitId")]
        public virtual PatientEncounter? Visit { get; set; }
        [ForeignKey("InstitutionId")]
        public virtual Institution? Institution { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }
        [ForeignKey("ApprovedById")]
        public virtual WebUser? ApprovedBy { get; set; }
        [ForeignKey("RefundedById")]
        public virtual WebUser? RefundedBy { get; set; }
        [ForeignKey("CancelledById")]
        public virtual WebUser? CancelledBy { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser? Creater { get; set; }
    }
}
