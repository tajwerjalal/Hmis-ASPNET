using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("USERNOTIFICATION")]
    public class UserNotification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long? NotificationId { get; set; }
        public long? WebUserId { get; set; }
        public bool Seen { get; set; }

        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; }
        public long? ToDepartmentId { get; set; }

        [ForeignKey("NotificationId")]
        public virtual Notification Notification { get; set; }

        [ForeignKey("WebUserId")]
        public virtual WebUser WebUser { get; set; }

        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }

        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }

        [ForeignKey("ToDepartmentId")]
        public virtual Department ToDepartment { get; set; }
    }
}
