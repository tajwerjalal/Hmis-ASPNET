using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("WEBCONTENT")]
    public class WebContent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Name { get; set; }
        public bool Retired { get; set; }
        public string Sinhala { get; set; }
        public string Tamil { get; set; }
        public string English { get; set; }
        public double OrderNo { get; set; }
        public long? WebLanguageId { get; set; }
        public WebContentType? Type { get; set; }
        public string ShortContext { get; set; }
        public string LongContext { get; set; }
        public long? ParentId { get; set; }

        [ForeignKey("WebLanguageId")]
        public virtual WebLanguage WebLanguage { get; set; }

        [ForeignKey("ParentId")]
        public virtual WebContent Parent { get; set; }
    }
}
