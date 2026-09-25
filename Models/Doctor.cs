using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    public class Doctor : Staff
    {
        public string SltRegNo { get; set; }
        public string PmcNumber { get; set; }
        public bool CarryDefaultSMS { get; set; }
        public bool CarryOwnStaff { get; set; }
        public long? ChannelingDestinationInstitutionId { get; set; }
        public long? ChannelingDestinationDepartmentId { get; set; }
        [ForeignKey("ChannelingDestinationInstitutionId")]
        public virtual Institution ChannelingDestinationInstitution { get; set; }
        [ForeignKey("ChannelingDestinationDepartmentId")]
        public virtual Department ChannelingDestinationDepartment { get; set; }
    }
}
