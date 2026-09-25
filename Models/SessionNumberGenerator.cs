using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    public class SessionNumberGenerator : Category
    {
        public long? SpecialityId { get; set; }
        public long? StaffId { get; set; }

        [ForeignKey("SpecialityId")]
        public virtual Speciality Speciality { get; set; }

        [ForeignKey("StaffId")]
        public virtual Staff Staff { get; set; }
    }
}
