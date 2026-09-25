using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("BILLFEEPAYMENT")]
    public class BillFeePayment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public double Amount { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; }

        public long? CreaterId { get; set; }
        public long? RetirerId { get; set; }
        public long? BillFeeId { get; set; }
        public long? PaymentId { get; set; }
        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }

        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }
        [ForeignKey("BillFeeId")]
        public virtual BillFee BillFee { get; set; }
        [ForeignKey("PaymentId")]
        public virtual Payment Payment { get; set; }
        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }
    }
}
