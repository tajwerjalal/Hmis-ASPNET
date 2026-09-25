using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("MAPPING")]
    public class Mapping
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long? Item1Id { get; set; }
        public long? Item2Id { get; set; }
        public string Descreption { get; set; }

        [ForeignKey("Item1Id")]
        public virtual Item Item1 { get; set; }

        [ForeignKey("Item2Id")]
        public virtual Item Item2 { get; set; }
    }
}
