using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("AGENTHISTORY")]
    public class AgentHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public double BalanceBeforeTransaction { get; set; }
        public double BalanceAfterTransaction { get; set; }
        public double TransactionValue { get; set; }
        public double? AdjustmentToAgencyBalance { get; set; }
        public double? PaidAmountToAgency { get; set; }
        public double? PaidAmountByAgency { get; set; }
        public double CompanyTransactionValue { get; set; }
        public double CompanyBalanceBefore { get; set; }
        public double CompanyBalanceAfter { get; set; }
        public double AgentTransactionValue { get; set; }
        public double AgentBalanceBefore { get; set; }
        public double AgentBalanceAfter { get; set; }
        public double StaffTrasnactionValue { get; set; }
        public double CollectingCentertransactionValue { get; set; }
        public HistoryType? HistoryType { get; set; }
        public string ReferenceNumber { get; set; }
        public string Comment { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; }

        public long? BillItemId { get; set; }
        public long? BillId { get; set; }
        public long? BillSessionId { get; set; }
        public long? CreaterId { get; set; }
        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }
        public long? AgencyId { get; set; }
        public long? RetirerId { get; set; }

        [ForeignKey("BillItemId")]
        public virtual BillItem BillItem { get; set; }
        [ForeignKey("BillId")]
        public virtual Bill Bill { get; set; }
        [ForeignKey("BillSessionId")]
        public virtual BillSession BillSession { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }
        [ForeignKey("AgencyId")]
        public virtual Institution Agency { get; set; }
        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }
    }
}
