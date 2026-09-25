using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("FEEVALUE")]
    public class FeeValue
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long? ItemId { get; set; }
        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }
        public long? CategoryId { get; set; }

        public double? TotalValueForLocals { get; set; }
        public double? TotalValueForForeigners { get; set; }

        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; }

        [ForeignKey("ItemId")]
        public virtual Item Item { get; set; }

        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }

        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }
    }
}
