using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("PAYMENT")]
    public class Payment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public DateTime? WrittenAt { get; set; }
        public DateTime? ToRealizeAt { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public bool Realized { get; set; }
        public DateTime? RealizedAt { get; set; }
        public string RealizeComments { get; set; }
        public string Comments { get; set; }
        public string ChequeRefNo { get; set; }
        public DateTime? ChequeDate { get; set; }
        public string CreditCardRefNo { get; set; }
        public double PaidValue { get; set; }
        public int CreditDurationInDays { get; set; }
        public string CurrencyDenominationsJson { get; set; }
        public string ReferenceNo { get; set; }
        public bool CashbookEntryStated { get; set; }
        public bool CashbookEntryCompleted { get; set; }

        public long? BillId { get; set; }
        public long? BankId { get; set; }
        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }
        public long? RealizerId { get; set; }
        public long? CreaterId { get; set; }
        public long? RetirerId { get; set; }
        public long? HandoverCreatedBillId { get; set; }
        public long? HandoverAcceptBillId { get; set; }
        public long? CashbookEntryId { get; set; }
        public long? CashbookId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; }

        [ForeignKey("BillId")]
        public virtual Bill Bill { get; set; }
        [ForeignKey("BankId")]
        public virtual Institution Bank { get; set; }
        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }
        [ForeignKey("RealizerId")]
        public virtual WebUser Realizer { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }
        [ForeignKey("HandoverCreatedBillId")]
        public virtual Bill HandoverCreatedBill { get; set; }
        [ForeignKey("HandoverAcceptBillId")]
        public virtual Bill HandoverAcceptBill { get; set; }
        [ForeignKey("CashbookId")]
        public virtual CashBook Cashbook { get; set; }
        [ForeignKey("CashbookEntryId")]
        public virtual CashBookEntry CashbookEntry { get; set; }
    }
}
