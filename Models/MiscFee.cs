using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("MISCFEE")]
    public class MiscFee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long? PatientId { get; set; }
        public long? VisitId { get; set; }
        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }

        public string BuyerType { get; set; } = "Patient";
        public string BuyerName { get; set; } = string.Empty;
        public string FeeHead { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Amount { get; set; }
        public string AmountInWords { get; set; } = string.Empty;

        public bool Paid { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public double? CashReceived { get; set; }
        public double? CashReturn { get; set; }
        public string? ReferenceNo { get; set; }
        public DateTime? PaidAt { get; set; }
        public long? ReceivedById { get; set; }

        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; } = string.Empty;

        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }
        [ForeignKey("VisitId")]
        public virtual PatientEncounter? Visit { get; set; }
        [ForeignKey("InstitutionId")]
        public virtual Institution? Institution { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }
        [ForeignKey("ReceivedById")]
        public virtual WebUser? ReceivedBy { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser? Creater { get; set; }
    }
}
