using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("WEBUSERDEPARTMENT")]
    public class WebUserDepartment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public long? WebUserId { get; set; }
        public long? DepartmentId { get; set; }
        public bool Retired { get; set; }
        [ForeignKey("WebUserId")]
        public virtual WebUser WebUser { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }
    }
}
