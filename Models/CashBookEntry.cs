using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("CASHBOOKENTRY")]
    public class CashBookEntry
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string Description { get; set; }
        public double CashIn { get; set; }
        public double CashOut { get; set; }
        public DateTime? CreatedAt { get; set; }
        public double CashBalance { get; set; }
        public long? CashBookId { get; set; }
        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }
        public long? CreaterId { get; set; }
        [ForeignKey("CashBookId")]
        public virtual CashBook CashBook { get; set; }
        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
    }
}
