using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("PRICEMATRIX")]
    public class PriceMatrix
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string Name { get; set; }
        public double? Fee { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public long? ItemId { get; set; }
        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }
        public long? CreaterId { get; set; }
        public long? RetirerId { get; set; }
        public long? EditerId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public DateTime? RetiredAt { get; set; }
        public bool Retired { get; set; }
        public string RetireComments { get; set; }
        [ForeignKey("ItemId")]
        public virtual Item Item { get; set; }
        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }
        [ForeignKey("EditerId")]
        public virtual WebUser Editer { get; set; }
    }
}
