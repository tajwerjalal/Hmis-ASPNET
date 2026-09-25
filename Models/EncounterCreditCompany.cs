using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("ENCOUNTERCREDITCOMPANY")]
    public class EncounterCreditCompany
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long? PatientEncounterId { get; set; }
        public long? InstitutionId { get; set; }

        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; }
        public double CreditLimit { get; set; }
        public string Descreption { get; set; }
        public string PolicyNo { get; set; }
        public string ReferanceNo { get; set; }

        [ForeignKey("PatientEncounterId")]
        public virtual PatientEncounter PatientEncounter { get; set; }

        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }

        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }

        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }
    }
}
