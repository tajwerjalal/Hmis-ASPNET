using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("BILLITEM")]
    public class BillItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public double? Qty { get; set; }
        public string Descreption { get; set; }
        public double RemainingQty { get; set; }
        public double Rate { get; set; }
        public double DiscountRate { get; set; }
        public double MarginRate { get; set; }
        public double NetRate { get; set; }
        public double GrossValue { get; set; }
        public double Discount { get; set; }
        public double Vat { get; set; }
        public double NetValue { get; set; }
        public double VatPlusNetValue { get; set; }
        public double MarginValue { get; set; }
        public double AdjustedValue { get; set; }
        public double HospitalFee { get; set; }
        public double CollectingCentreFee { get; set; }
        public double StaffFee { get; set; }
        public double OtherFee { get; set; }
        public double FeeValue { get; set; }
        public int SearialNo { get; set; }

        public bool? Refunded { get; set; }
        public bool Retired { get; set; }
        public string InsId { get; set; }
        public string DeptId { get; set; }
        public string CatId { get; set; }
        public string SessionId { get; set; }
        public string ItemId { get; set; }
        /// <summary>Legacy consultant code for the line (ibil_det/obil_det.CONCODE).</summary>
        [MaxLength(8)]
        public string Concode { get; set; }
        /// <summary>Legacy status flag (bil_det.FLAG): 'B','C','P' (posted). NULL for new rows.</summary>
        [MaxLength(2)]
        public string LegacyFlag { get; set; }
        public string AgentRefNo { get; set; }

        public DateTime? FromTime { get; set; }
        public DateTime? ToTime { get; set; }
        public DateTime? SessionDate { get; set; }
        public DateTime? BillTime { get; set; }

        public InwardChargeType? InwardChargeType { get; set; }
        public Priority? Priority { get; set; }
        public BillItemStatus? BillItemStatus { get; set; }

        public long? BillId { get; set; }
        public long? ExpenseBillId { get; set; }
        public long? ItemId1 { get; set; }
        public long? ParentBillItemId { get; set; }
        public long? ReferanceBillItemId { get; set; }
        public long? PaidForBillFeeId { get; set; }
        public long? PatientEncounterId { get; set; }
        public long? ReferenceBillId { get; set; }
        public long? PriceMatrixId { get; set; }
        public long? CreaterId { get; set; }
        public long? EditorId { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; }

        [ForeignKey("BillId")]
        public virtual Bill Bill { get; set; }
        [ForeignKey("ExpenseBillId")]
        public virtual Bill ExpenseBill { get; set; }
        [ForeignKey("ItemId1")]
        public virtual Item Item { get; set; }
        [ForeignKey("ParentBillItemId")]
        public virtual BillItem ParentBillItem { get; set; }
        [ForeignKey("ReferanceBillItemId")]
        public virtual BillItem ReferanceBillItem { get; set; }
        [ForeignKey("PaidForBillFeeId")]
        public virtual BillFee PaidForBillFee { get; set; }
        [ForeignKey("PatientEncounterId")]
        public virtual PatientEncounter PatientEncounter { get; set; }
        [ForeignKey("ReferenceBillId")]
        public virtual Bill ReferenceBill { get; set; }
        [ForeignKey("PriceMatrixId")]
        public virtual PriceMatrix PriceMatrix { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
        [ForeignKey("EditorId")]
        public virtual WebUser Editor { get; set; }
        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }

        public virtual PharmaceuticalBillItem PharmaceuticalBillItem { get; set; }
        public virtual BillSession BillSession { get; set; }

        public virtual ICollection<BillFee> BillFees { get; set; } = new List<BillFee>();
        public virtual ICollection<BillFee> ProFees { get; set; } = new List<BillFee>();
        public virtual ICollection<BillItem> ChiledBillItems { get; set; } = new List<BillItem>();
    }
}
