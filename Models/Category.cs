using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("CATEGORY")]
    public class Category
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Name { get; set; }
        public string Sname { get; set; }
        public string Code { get; set; }
        public string PrintingName { get; set; }
        public string Description { get; set; }

        public SymanticType? SymanticType { get; set; }

        public long? ParentCategoryId { get; set; }
        public long? PaymentSchemeId { get; set; }
        public long? InstitutionId { get; set; }

        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? RetiredAt { get; set; }
        public bool Retired { get; set; }
        public string RetireComments { get; set; }
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }

        public bool DiscountAllowed { get; set; }
        public bool DepartmentLimitToDepartment { get; set; }

        [ForeignKey("ParentCategoryId")]
        public virtual Category ParentCategory { get; set; }

        [ForeignKey("PaymentSchemeId")]
        public virtual PaymentScheme PaymentScheme { get; set; }

        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }

        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }

        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }

        [ForeignKey("EditerId")]
        public virtual WebUser Editer { get; set; }

        [Column("DTYPE")]
        public string Discriminator { get; set; }
    }
}