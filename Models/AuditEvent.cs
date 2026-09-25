using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("AUDITEVENT")]
    public class AuditEvent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public DateTime? EventDataTime { get; set; }
        public DateTime? EventEndTime { get; set; }
        public long? WebUserId { get; set; }
        public string Uuid { get; set; }
        public string Url { get; set; }
        public string EventTrigger { get; set; }
        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }
        public string BeforeEdit { get; set; }
        public string AfterEdit { get; set; }
        public long? EventDuration { get; set; }
        public string EventStatus { get; set; }
        public string IpAddress { get; set; }
    }
}
