using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("PATIENTDEPOSITHISTORY")]
    public class PatientDepositHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long? BillItemId { get; set; }
        public long? BillId { get; set; }
        public long? BillSessionId { get; set; }
        public long? PatientDepositId { get; set; }
        public double BalanceBeforeTransaction { get; set; }
        public double BalanceAfterTransaction { get; set; }
        public double TransactionValue { get; set; }
        public long? CreaterId { get; set; }
        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }
        public HistoryType? HistoryType { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; }
        public string ReferenceNumber { get; set; }
        public string Comment { get; set; }

        [ForeignKey("BillItemId")]
        public virtual BillItem BillItem { get; set; }
        [ForeignKey("BillId")]
        public virtual Bill Bill { get; set; }
        [ForeignKey("BillSessionId")]
        public virtual BillSession BillSession { get; set; }
        [ForeignKey("PatientDepositId")]
        public virtual PatientDeposit PatientDeposit { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }
        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }
    }
}
