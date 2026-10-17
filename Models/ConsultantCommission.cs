using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    /// <summary>
    /// Consultant commission matrix - hospital commission percentage per
    /// (company, consultant, service-code-prefix). Migrated from the VFP master
    /// <c>concomm.dbf</c>. Consulted by recalcon/orecalcon: a consultant's pay for
    /// a line is <c>netAmount * (100 - HmcPer) / 100</c> unless the patient is
    /// owned by him (see <see cref="PatientOwner"/>), in which case he gets 100%.
    /// </summary>
    [Table("CONCOMM")]
    public class ConsultantCommission
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>Legacy company code (register.CID).</summary>
        [MaxLength(8)]
        public string CompanyCode { get; set; }

        /// <summary>Resolved company (INSTITUTION) if the code matched.</summary>
        public long? CompanyId { get; set; }

        /// <summary>Legacy consultant code (consult.ID).</summary>
        [MaxLength(8)]
        public string ConCode { get; set; }

        /// <summary>Resolved consultant (STAFF) if the code matched.</summary>
        public long? StaffId { get; set; }

        /// <summary>Service code prefix (first 5 chars of the item code).</summary>
        [MaxLength(16)]
        public string SCode { get; set; }

        /// <summary>Hospital commission percentage kept from the line amount.</summary>
        public double HmcPer { get; set; }

        /// <summary>Legacy department code.</summary>
        [MaxLength(4)]
        public string Dept { get; set; }

        public bool Retired { get; set; }
        public string RetireComments { get; set; }
        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }
    }
}
