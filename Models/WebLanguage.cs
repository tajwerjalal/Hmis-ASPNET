using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("WEBLANGUAGE")]
    public class WebLanguage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Name { get; set; }
        public string Code { get; set; }
        public bool Retired { get; set; }
        public double OrderNo { get; set; }
        public bool DefaultLanguage { get; set; }
    }
}
