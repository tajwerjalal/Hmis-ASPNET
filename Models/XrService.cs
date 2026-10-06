using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("XRSERVICE")]
    public class XrService
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Name { get; set; }
        public string Sname { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public string PrintName { get; set; }
        public string Requirements { get; set; }

        public double? Rate { get; set; }
        public double? RateForeigner { get; set; }
        public bool DiscountAllowed { get; set; }

        public long? CategoryId { get; set; }
        public long? DepartmentId { get; set; }
        public long? InstitutionId { get; set; }

        public ItemType? ItemType { get; set; }
        public Priority? Priority { get; set; }

        public bool Billable { get; set; } = true;
        public bool Active { get; set; } = true;

        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; }
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }

        [ForeignKey("CategoryId")]
        public virtual XrServiceCategory Category { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }

        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }

        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }

        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }

        [ForeignKey("EditerId")]
        public virtual WebUser Editer { get; set; }
    }
}