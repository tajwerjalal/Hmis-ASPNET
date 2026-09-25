using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("AREA")]
    public class Area
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string Name { get; set; }
        public string Sname { get; set; }
        public string Code { get; set; }
        public long? InstitutionId { get; set; }
        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? RetiredAt { get; set; }
        public bool Retired { get; set; }
        public string RetireComments { get; set; }
        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }
    }
}
