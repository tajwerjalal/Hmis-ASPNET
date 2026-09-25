using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("PATIENT")]
    public class Patient
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long? PatientPhoneNumber { get; set; }
        public long? PatientMobileNumber { get; set; }

        public long? PersonId { get; set; }

        [ForeignKey("PersonId")]
        public virtual Person Person { get; set; }

        public int? PinNo { get; set; }
        public int? HinNo { get; set; }
        public long? CreatedInstitutionId { get; set; }
        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool Retired { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; }

        [ForeignKey("CreatedInstitutionId")]
        public virtual Institution CreatedInstitution { get; set; }

        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }

        [ForeignKey("EditerId")]
        public virtual WebUser Editer { get; set; }

        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }

        public byte[] BaImage { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string Code { get; set; }
        public string Comments { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        [MaxLength(15)]
        public string Phn { get; set; }

        public bool? HasAnAccount { get; set; }
        public double? RunningBalance { get; set; }
        public double? CreditLimit { get; set; }
        public long? PatientId { get; set; }
        public bool? CardIssues { get; set; }
        public DateTime? CardIssuedDate { get; set; }
    }
}