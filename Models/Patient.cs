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

        // ---- Legacy PATIENT columns -------------------------------------
        // The PATIENT table is shared with the VFP patient master. CODE is the
        // permanent patient code (PCODE / MRNo) and never changes. REGISTER is
        // the visit table: a new REGISTER.REGNO is issued on every visit and
        // PATIENT.CODE stays the same for the patient's lifetime.
        //
        // These columns were previously only read through raw SQL; mapping them
        // lets the registration forms keep the legacy master in step with the
        // app-side PERSON row without a second patient table.

        [Column("NAME", TypeName = "varchar(50)")]
        public string LegacyName { get; set; }

        [Column("SEX", TypeName = "varchar(15)")]
        public string LegacySex { get; set; }

        [Column("AGE", TypeName = "varchar(3)")]
        public string LegacyAge { get; set; }

        [Column("ATYPE", TypeName = "varchar(10)")]
        public string AType { get; set; }

        [Column("ADDRESS", TypeName = "varchar(100)")]
        public string Address { get; set; }

        [Column("PHONE1", TypeName = "varchar(25)")]
        public string Phone1 { get; set; }

        [Column("PHONE2", TypeName = "varchar(25)")]
        public string Phone2 { get; set; }

        [Column("PHONE3", TypeName = "varchar(25)")]
        public string Phone3 { get; set; }

        [Column("RDATE", TypeName = "varchar(100)")]
        public string RDate { get; set; }

        [Column("NIC", TypeName = "varchar(15)")]
        public string Nic { get; set; }

        [Column("REGDATE", TypeName = "varchar(100)")]
        public string RegDate { get; set; }

        [Column("CASTE", TypeName = "varchar(15)")]
        public string Caste { get; set; }

        [Column("USERID", TypeName = "varchar(15)")]
        public string UserId { get; set; }

        [Column("UDATE", TypeName = "datetime")]
        public DateTime UDate { get; set; } = DateTime.Now;

        [Column("UTIME", TypeName = "varchar(5)")]
        public string UTime { get; set; }

        [NotMapped]
        public int PendingTestCount { get; set; }
    }
}