using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("CONSULTANTSETTLEMENT")]
    public class ConsultantSettlement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long? ConsultantId { get; set; }
        public DateTime SettlementDate { get; set; }
        public int TokenCount { get; set; }
        public int BillCount { get; set; }
        public double AmountHospital { get; set; }
        public double AmountHandedOver { get; set; }
        public double AmountTotal { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public long? ReceivedById { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public string Note { get; set; }

        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }

        [ForeignKey("ConsultantId")]
        public virtual Staff Consultant { get; set; }
        [ForeignKey("ReceivedById")]
        public virtual Staff ReceivedBy { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
    }
}