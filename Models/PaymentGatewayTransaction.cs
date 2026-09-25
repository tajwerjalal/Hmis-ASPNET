using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("PAYMENTGATEWAYTRANSACTION")]
    public class PaymentGatewayTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }
        public string OrderId { get; set; }
        public double? OrderAmount { get; set; }
        public string SessionId { get; set; }
        public string OrderStatus { get; set; }
        public string TransactionId { get; set; }
        public long? SessionInstanceId { get; set; }
        public long? BillId { get; set; }
        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; }

        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }
        [ForeignKey("BillId")]
        public virtual Bill Bill { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }
    }
}
