using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("CONFIGOPTION")]
    public class ConfigOption
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string Name { get; set; }
        public string OptionValue { get; set; }
        public OptionScope? OptionScope { get; set; }
        public OptionValueType? OptionType { get; set; }
        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }
        public bool Retired { get; set; }
        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
    }
}