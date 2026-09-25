using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("BILL")]
    public class Bill
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public BillClassType? BillClassType { get; set; }
        public BillType? BillType { get; set; }
        public BillTypeAtomic? BillTypeAtomic { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public SurgeryBillType? SurgeryBillType { get; set; }
        public PatientInvestigationStatus? Status { get; set; }

        public string Comments { get; set; }
        public string CreditCardRefNo { get; set; }
        public string ChequeRefNo { get; set; }
        public int CreditDuration { get; set; }
        public DateTime? ChequeDate { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string QutationNumber { get; set; }
        public string ReferenceNumber { get; set; }
        public double Margin { get; set; }
        public double Total { get; set; }
        public double NetTotal { get; set; }
        public double Discount { get; set; }
        public double Vat { get; set; }
        public double VatPlusNetTotal { get; set; }
        public double DiscountPercent { get; set; }
        public double BillTotal { get; set; }
        public double PaidAmount { get; set; }
        public double RefundAmount { get; set; }
        public double Balance { get; set; }
        public double ServiceCharge { get; set; }
        public double? Tax { get; set; }
        public double? CashPaid { get; set; }
        public double? CashBalance { get; set; }
        public double SaleValue { get; set; }
        public double FreeValue { get; set; }
        public double PerformInstitutionFee { get; set; }
        public double StaffFee { get; set; }
        public double BillerFee { get; set; }
        public double GrantTotal { get; set; }
        public double ExpenseTotal { get; set; }
        public double GrnNetTotal { get; set; }
        public double HospitalFee { get; set; }
        public double ProfessionalFee { get; set; }
        public double Qty { get; set; }
        public double TenderedAmount { get; set; }
        public double TotalHospitalFee { get; set; }
        public double TotalCenterFee { get; set; }
        public double TotalStaffFee { get; set; }
        public double ClaimableTotal { get; set; }

        public string DeptId { get; set; }
        public string InsId { get; set; }
        public string CatId { get; set; }
        public string SessionId { get; set; }
        public string BookingId { get; set; }
        public string InvoiceNumber { get; set; }
        public string AgentRefNo { get; set; }
        public string IpOpOrCc { get; set; }
        public string LocalNumber { get; set; }

        public bool Paid { get; set; }
        public bool Cancelled { get; set; }
        public bool Refunded { get; set; }
        public bool Reactivated { get; set; }
        public bool Printed { get; set; }
        public bool DuplicatedPrinted { get; set; }
        public bool BillClosed { get; set; }
        public bool BillPaymentCompletelySettled { get; set; }
        public bool? Smsed { get; set; }

        public DateTime? BillDate { get; set; }
        public DateTime? BillTime { get; set; }
        public DateTime? AppointmentAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public DateTime? ApproveAt { get; set; }
        public DateTime? PrintedAt { get; set; }
        public DateTime? DuplicatePrintedAt { get; set; }
        public DateTime? SmsedAt { get; set; }

        public long? ItemId { get; set; }
        public long? MembershipSchemeId { get; set; }
        public long? CategoryId { get; set; }
        public long? CashTransactionId { get; set; }
        public long? BankId { get; set; }
        public long? ApproveUserId { get; set; }
        public long? PaymentSchemeInstitutionId { get; set; }
        public long? CollectingCentreId { get; set; }
        public long? InstitutionId { get; set; }
        public long? FromInstitutionId { get; set; }
        public long? ToInstitutionId { get; set; }
        public long? CreditCompanyId { get; set; }
        public long? ReferenceInstitutionId { get; set; }
        public long? ReferringDepartmentId { get; set; }
        public long? DepartmentId { get; set; }
        public long? FromDepartmentId { get; set; }
        public long? ToDepartmentId { get; set; }
        public long? BilledBillId { get; set; }
        public long? CancelledBillId { get; set; }
        public long? RefundedBillId { get; set; }
        public long? ReactivatedBillId { get; set; }
        public long? ReferenceBillId { get; set; }
        public long? ForwardReferenceBillId { get; set; }
        public long? BackwardReferenceBillId { get; set; }
        public long? StaffId { get; set; }
        public long? FromStaffId { get; set; }
        public long? ToStaffId { get; set; }
        public long? CreaterId { get; set; }
        public long? EditorId { get; set; }
        public long? CheckedById { get; set; }
        public long? RetirerId { get; set; }
        public long? PaymentSchemeId { get; set; }
        public long? PersonId { get; set; }
        public long? PatientId { get; set; }
        public long? ReferredById { get; set; }
        public long? PatientEncounterId { get; set; }
        public long? ProcedureId { get; set; }
        public long? BillPackegeId { get; set; }
        public long? SingleBillItemId { get; set; }
        public long? SingleBillSessionId { get; set; }
        public long? ReferredByInstitutionId { get; set; }
        public long? PaidBillId { get; set; }
        public long? ConsultantSettlementId { get; set; }
        public long? ToWebUserId { get; set; }
        public long? FromWebUserId { get; set; }
        public long? SmsedUserId { get; set; }
        public long? PrintedUserId { get; set; }
        public long? DuplicatePrintedUserId { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public DateTime? CheckeAt { get; set; }
        public bool Retired { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; }

        [ForeignKey("ItemId")]
        public virtual Item Item { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; }
        [ForeignKey("PersonId")]
        public virtual Person Person { get; set; }
        [ForeignKey("StaffId")]
        public virtual Staff Staff { get; set; }
        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
        [ForeignKey("EditorId")]
        public virtual WebUser Editor { get; set; }
        [ForeignKey("CheckedById")]
        public virtual WebUser CheckedBy { get; set; }
        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }
        [ForeignKey("BilledBillId")]
        public virtual Bill BilledBill { get; set; }
        [ForeignKey("CancelledBillId")]
        public virtual Bill CancelledBill { get; set; }
        [ForeignKey("RefundedBillId")]
        public virtual Bill RefundedBill { get; set; }
        [ForeignKey("ReferenceBillId")]
        public virtual Bill ReferenceBill { get; set; }
        [ForeignKey("ForwardReferenceBillId")]
        public virtual Bill ForwardReferenceBill { get; set; }
        [ForeignKey("BackwardReferenceBillId")]
        public virtual Bill BackwardReferenceBill { get; set; }
        [ForeignKey("FromInstitutionId")]
        public virtual Institution FromInstitution { get; set; }
        [ForeignKey("ToInstitutionId")]
        public virtual Institution ToInstitution { get; set; }
        [ForeignKey("CreditCompanyId")]
        public virtual Institution CreditCompany { get; set; }
        [ForeignKey("FromDepartmentId")]
        public virtual Department FromDepartment { get; set; }
        [ForeignKey("ToDepartmentId")]
        public virtual Department ToDepartment { get; set; }
        [ForeignKey("PatientEncounterId")]
        public virtual PatientEncounter PatientEncounter { get; set; }
        [ForeignKey("PaymentSchemeId")]
        public virtual PaymentScheme PaymentScheme { get; set; }
        [ForeignKey("CollectingCentreId")]
        public virtual Institution CollectingCentre { get; set; }
        [ForeignKey("PaymentSchemeInstitutionId")]
        public virtual Institution PaymentSchemeInstitution { get; set; }
        [ForeignKey("ReferredByInstitutionId")]
        public virtual Institution ReferredByInstitution { get; set; }
        [ForeignKey("ReferenceInstitutionId")]
        public virtual Institution ReferenceInstitution { get; set; }
        [ForeignKey("CashTransactionId")]
        public virtual CashTransaction CashTransaction { get; set; }
        [ForeignKey("ReferredById")]
        public virtual Doctor ReferredBy { get; set; }
        [ForeignKey("MembershipSchemeId")]
        public virtual MembershipScheme MembershipScheme { get; set; }
        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }
        [ForeignKey("BankId")]
        public virtual Institution Bank { get; set; }
        [ForeignKey("PaidBillId")]
        public virtual Bill PaidBill { get; set; }
        [ForeignKey("ConsultantSettlementId")]
        public virtual ConsultantSettlement ConsultantSettlement { get; set; }

        public virtual ICollection<BillItem> BillItems { get; set; } = new List<BillItem>();
        public virtual ICollection<BillFee> BillFees { get; set; } = new List<BillFee>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

        [Column("DTYPE")]
        public string Discriminator { get; set; }
    }
}
