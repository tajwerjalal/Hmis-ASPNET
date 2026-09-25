using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("OPDSESSION")]
    public class OpdSession
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long? ConsultantId { get; set; }
        public long? SpecialityId { get; set; }
        public DateTime SessionDate { get; set; }
        public string FromTime { get; set; }
        public string ToTime { get; set; }
        public string RoomNo { get; set; }
        public double Fee { get; set; }
        public double HospitalFee { get; set; }
        public int TokenStartingNo { get; set; } = 1;
        public int TokenMax { get; set; } = 50;
        public int TokenIncrement { get; set; } = 1;
        public OpdSessionStatus Status { get; set; } = OpdSessionStatus.Scheduled;
        public string Note { get; set; }

        public long? DepartmentId { get; set; }
        public long? InstitutionId { get; set; }
        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool Retired { get; set; }

        [ForeignKey("ConsultantId")]
        public virtual Staff Consultant { get; set; }
        [ForeignKey("SpecialityId")]
        public virtual Speciality Speciality { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }
        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
    }
}