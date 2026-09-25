using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    public class Appointment : PatientEncounter
    {
        public DateTime? AppointmentDate { get; set; }
        public DateTime? AppointmentTimeFrom { get; set; }
        public DateTime? AppointmentTimeTo { get; set; }

        public long? BillId { get; set; }
        public long? PatientEncounterId { get; set; }

        [ForeignKey("BillId")]
        public virtual Bill Bill { get; set; }
        [ForeignKey("PatientEncounterId")]
        public virtual PatientEncounter PatientEncounter { get; set; }
    }
}
