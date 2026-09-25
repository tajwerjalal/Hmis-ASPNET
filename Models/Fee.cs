using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("FEE")]
    public class Fee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string Name { get; set; }
        public string EndorserAddress { get; set; }
        public FeeType? FeeType { get; set; }
        public double FeeValue { get; set; }
        public double? FeeGrossValue { get; set; }
        public double FeeDiscount { get; set; }
        public double FeeVat { get; set; }
        public double FeeMargin { get; set; }
        public double FeeAdjusted { get; set; }
        public double VatPercentage { get; set; }
        public long? ItemId { get; set; }
        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }
        public long? SpecialityId { get; set; }
        public long? StaffId { get; set; }
        public long? CreaterId { get; set; }
        public long? EditerId { get; set; }
        public long? RetirerId { get; set; }
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
        [ForeignKey("SpecialityId")]
        public virtual Speciality Speciality { get; set; }
        [ForeignKey("StaffId")]
        public virtual Staff Staff { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
        [ForeignKey("EditerId")]
        public virtual WebUser Editer { get; set; }
        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }
    }
}
