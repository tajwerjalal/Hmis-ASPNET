using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("PATIENTITEM")]
    public class PatientItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Name { get; set; }
        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; }
        public long? PatientId { get; set; }
        public long? PatientEncounterId { get; set; }
        public long? BillId { get; set; }
        public long? BillItemId { get; set; }
        public long? PackegeId { get; set; }
        public long? ItemId { get; set; }
        public DateTime? FromTime { get; set; }
        public DateTime? ToTime { get; set; }
        public double? ServiceValue { get; set; }
        public double Discount { get; set; }
        public double AdjustedValue { get; set; }

        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; }
        [ForeignKey("PatientEncounterId")]
        public virtual PatientEncounter PatientEncounter { get; set; }
        [ForeignKey("BillId")]
        public virtual Bill Bill { get; set; }
        [ForeignKey("BillItemId")]
        public virtual BillItem BillItem { get; set; }
        [ForeignKey("PackegeId")]
        public virtual Packege Packege { get; set; }
        [ForeignKey("ItemId")]
        public virtual Item Item { get; set; }
    }
}
