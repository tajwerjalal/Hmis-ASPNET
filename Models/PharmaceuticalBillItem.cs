using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("PHARMACEUTICALBILLITEM")]
    public class PharmaceuticalBillItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public double? Qty { get; set; }
        public double? FreeQty { get; set; }
        public double? SellingPrice { get; set; }
        public double? PurchasePrice { get; set; }
        public long? BillItemId { get; set; }
        public long? ItemBatchId { get; set; }
        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        [ForeignKey("BillItemId")]
        public virtual BillItem BillItem { get; set; }
        [ForeignKey("ItemBatchId")]
        public virtual ItemBatch ItemBatch { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
    }
}
