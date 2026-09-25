using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("COMPANYSERVICERATE")]
    public class CompanyServiceRate
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long CompanyId { get; set; }

        [Required]
        public long ItemId { get; set; }

        [Required]
        public double Rate { get; set; }

        public double? StaffRate { get; set; }

        public double? UnitRate { get; set; }

        public long? DepartmentId { get; set; }

        public double? DiscountPercent { get; set; }

        public DateTime? EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public bool Retired { get; set; }

        public string RetireComments { get; set; }

        public long? CreaterId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public long? EditerId { get; set; }

        public DateTime? EditedAt { get; set; }

        [ForeignKey(nameof(CompanyId))]
        public Institution Company { get; set; }

        [ForeignKey(nameof(ItemId))]
        public Item Item { get; set; }
    }
}
