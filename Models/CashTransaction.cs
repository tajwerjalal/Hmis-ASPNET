using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("CASHTRANSACTION")]
    public class CashTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public double? PayingAmount { get; set; }
        public double? ChangeAmount { get; set; }
        public double? TotalPaidAmount { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? DrawerId { get; set; }
        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }
        public long? CreaterId { get; set; }
        [ForeignKey("DrawerId")]
        public virtual Drawer Drawer { get; set; }
        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
    }
}
