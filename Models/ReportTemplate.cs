using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("REPORTTEMPLATE")]
    public class ReportTemplate
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public ReportTemplateType? ReportTemplateType { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public int OrderNo { get; set; }
        public string Keywords { get; set; }
        public long? CategoryId { get; set; }
        public string Filters { get; set; }
        public string BillTypes { get; set; }
        public string Columns { get; set; }
        public string Totals { get; set; }
        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }
    }
}
