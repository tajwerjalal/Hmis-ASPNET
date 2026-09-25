using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("VMP")]
    public class Vmp
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Strength { get; set; }
        public long? VtmId { get; set; }
        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }
        [ForeignKey("VtmId")]
        public virtual Vtm Vtm { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
    }
}