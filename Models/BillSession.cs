using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("BILLSESSION")]
    public class BillSession
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public DateTime? SessionDate { get; set; }
        public string SessionNumber { get; set; }
        public long? BillId { get; set; }
        public long? BillItemId { get; set; }
        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }

        [ForeignKey("BillId")]
        public virtual Bill Bill { get; set; }
        [ForeignKey("BillItemId")]
        public virtual BillItem BillItem { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
    }
}
