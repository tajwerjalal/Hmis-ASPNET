using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("WEBUSERROLEUSER")]
    public class WebUserRoleUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public long? WebUserRoleId { get; set; }
        public long? WebUserId { get; set; }
        public bool Retired { get; set; }
        public DateTime? CreatedAt { get; set; }
        [ForeignKey("WebUserRoleId")]
        public virtual WebUserRole WebUserRole { get; set; }
        [ForeignKey("WebUserId")]
        public virtual WebUser WebUser { get; set; }
    }
}
