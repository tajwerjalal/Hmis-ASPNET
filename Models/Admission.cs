using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("ADMISSION")]
    public class Admission
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public DateTime? AdmissionDate { get; set; }
        public DateTime? DischargeDate { get; set; }
        public long? PatientEncounterId { get; set; }
        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }
        [ForeignKey("PatientEncounterId")]
        public virtual PatientEncounter PatientEncounter { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
    }
}
