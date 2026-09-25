using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("STOCK")]
    public class Stock
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public double? StockQty { get; set; }
        public double? StockFreeQty { get; set; }
        public double? QuentityInUnit { get; set; }
        public double? QuentityInPack { get; set; }
        public double? LastPurchaseUnitPrice { get; set; }
        public double? LastPurchaseFreeQty { get; set; }
        public DateTime? LastPurchaseDate { get; set; }
        public long? ItemBatchId { get; set; }
        public long? ItemId { get; set; }
        public long? DepartmentId { get; set; }
        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool Retired { get; set; }
        [ForeignKey("ItemBatchId")]
        public virtual ItemBatch ItemBatch { get; set; }
        [ForeignKey("ItemId")]
        public virtual Item Item { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
        [ForeignKey("EditerId")]
        public virtual WebUser Editer { get; set; }
    }
}
