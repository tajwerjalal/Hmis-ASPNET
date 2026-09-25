using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("WEBUSERROLEPRIVILEGE")]
    public class WebUserRolePrivilege
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        public long? WebUserRoleId { get; set; }
        public Privileges? Privilege { get; set; }
        public long? DepartmentId { get; set; }

        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }

        public bool Retired { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; }
        public string Sname { get; set; }
        public string Tname { get; set; }

        [ForeignKey("WebUserRoleId")]
        public virtual WebUserRole WebUserRole { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }

        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }

        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }
    }
}
