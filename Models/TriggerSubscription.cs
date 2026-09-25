using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("TRIGGERSUBSCRIPTION")]
    public class TriggerSubscription
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public TriggerType? TriggerType { get; set; }

        public long? WebUserId { get; set; }
        public long? DepartmentId { get; set; }
        public long? WebUserRoleId { get; set; }
        public double OrderNumber { get; set; }

        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; }
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }

        [ForeignKey("WebUserId")]
        public virtual WebUser WebUser { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }

        [ForeignKey("WebUserRoleId")]
        public virtual WebUserRole WebUserRole { get; set; }

        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }

        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }

        [ForeignKey("EditerId")]
        public virtual WebUser Editer { get; set; }
    }
}
