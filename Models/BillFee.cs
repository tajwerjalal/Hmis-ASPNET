using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("BILLFEE")]
    public class BillFee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public double FeeValue { get; set; }
        public double? FeeGrossValue { get; set; }
        public double FeeDiscount { get; set; }
        public double FeeVat { get; set; }
        public double FeeVatPlusValue { get; set; }
        public double FeeMargin { get; set; }
        public double FeeAdjusted { get; set; }
        public double PaidValue { get; set; }
        public double SettleValue { get; set; }
        public int OrderNo { get; set; }
        public DateTime? FeeAt { get; set; }

        public long? FeeId { get; set; }
        public long? PatientId { get; set; }
        public long? PatienEncounterId { get; set; }
        public long? ChildEncounterId { get; set; }
        public long? StaffId { get; set; }
        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }
        public long? SpecialityId { get; set; }
        public long? ReferenceBillFeeId { get; set; }
        public long? PatientItemId { get; set; }
        public long? PriceMatrixId { get; set; }
        public long? BillItemId { get; set; }
        public long? BillId { get; set; }
        public long? ReferenceBillItemId { get; set; }
        public long? ReferencePatientRoomId { get; set; }
        public long? CreaterId { get; set; }
        public long? EditorId { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool Retired { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; }

        [ForeignKey("FeeId")]
        public virtual Fee Fee { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; }
        [ForeignKey("PatienEncounterId")]
        public virtual PatientEncounter PatienEncounter { get; set; }
        [ForeignKey("ChildEncounterId")]
        public virtual PatientEncounter ChildEncounter { get; set; }
        [ForeignKey("StaffId")]
        public virtual Staff Staff { get; set; }
        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }
        [ForeignKey("SpecialityId")]
        public virtual Speciality Speciality { get; set; }
        [ForeignKey("ReferenceBillFeeId")]
        public virtual BillFee ReferenceBillFee { get; set; }
        [ForeignKey("BillItemId")]
        public virtual BillItem BillItem { get; set; }
        [ForeignKey("ReferenceBillItemId")]
        public virtual BillItem ReferenceBillItem { get; set; }
        [ForeignKey("BillId")]
        public virtual Bill Bill { get; set; }
        [ForeignKey("PriceMatrixId")]
        public virtual PriceMatrix PriceMatrix { get; set; }
        [ForeignKey("ReferencePatientRoomId")]
        public virtual PatientRoom ReferencePatientRoom { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
        [ForeignKey("EditorId")]
        public virtual WebUser Editor { get; set; }
        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }
    }
}
