using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("WEBUSER")]
    public class WebUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string UserName { get; set; }
        public string Password { get; set; }
        public string RealUserName { get; set; }
        public string Email { get; set; }
        public string WebUserPass { get; set; }

        public long? WebUserPersonId { get; set; }
        public long? WebThemeId { get; set; }
        public long? WebUserRoleUserId { get; set; }
        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }
        public long? StaffId { get; set; }
        public long? CreaterId { get; set; }
        public long? RetirerId { get; set; }
        public long? ActivatorId { get; set; }
        public long? SelfRoleUserId { get; set; }

        public bool Retired { get; set; }
        public bool Activated { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? RetiredAt { get; set; }
        public DateTime? ActivatedAt { get; set; }

        [ForeignKey("WebUserPersonId")]
        public virtual Person WebUserPerson { get; set; }

        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }

        [ForeignKey("StaffId")]
        public virtual Staff Staff { get; set; }

        [ForeignKey("WebUserRoleUserId")]
        public virtual WebUserRole WebUserRole { get; set; }

        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }

        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }

        [ForeignKey("ActivatorId")]
        public virtual WebUser Activator { get; set; }

        [ForeignKey("SelfRoleUserId")]
        public virtual WebUserRole SelfRole { get; set; }
    }
}