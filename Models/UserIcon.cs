using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("USERICON")]
    public class UserIcon
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long? WebUserId { get; set; }
        public long? WebUserRoleId { get; set; }
        public Icon? Icon { get; set; }
        public bool Retired { get; set; }
        public double OrderNumber { get; set; }
        public long? DepartmentId { get; set; }

        [ForeignKey("WebUserId")]
        public virtual WebUser WebUser { get; set; }

        [ForeignKey("WebUserRoleId")]
        public virtual WebUserRole WebUserRole { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }
    }
}
