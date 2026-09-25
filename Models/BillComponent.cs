using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("BILLCOMPONENT")]
    public class BillComponent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Name { get; set; }
        public string DeptId { get; set; }
        public string InsId { get; set; }
        public string CatId { get; set; }
        public string SessionId { get; set; }
        public double? ComponentValue { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; }

        public long? PackegeId { get; set; }
        public long? ItemId { get; set; }
        public long? CreaterId { get; set; }
        public long? RetirerId { get; set; }
        public long? BillId { get; set; }
        public long? BillItemId { get; set; }
        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }
        public long? StaffId { get; set; }
        public long? SpecialityId { get; set; }
        public long? PaymentMethodValueId { get; set; }

        [ForeignKey("PackegeId")]
        public virtual Packege Packege { get; set; }
        [ForeignKey("ItemId")]
        public virtual Item Item { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }
        [ForeignKey("BillId")]
        public virtual Bill Bill { get; set; }
        [ForeignKey("BillItemId")]
        public virtual BillItem BillItem { get; set; }
        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }
        [ForeignKey("StaffId")]
        public virtual Staff Staff { get; set; }
        [ForeignKey("SpecialityId")]
        public virtual Speciality Speciality { get; set; }
    }
}
