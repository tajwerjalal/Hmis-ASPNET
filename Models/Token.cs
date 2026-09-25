using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("TOKEN")]
    public class Token
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string TokenId { get; set; }
        public string TokenNumber { get; set; }
        public DateTime? TokenDate { get; set; }
        public DateTime? TokenAt { get; set; }
        public TokenType? TokenType { get; set; }
        public long? CateroryId { get; set; }
        public long? BillId { get; set; }
        public DateTime? IssuedAt { get; set; }
        public bool Called { get; set; }
        public DateTime? CalledAt { get; set; }
        public bool InProgress { get; set; }
        public DateTime? StartedAt { get; set; }
        public bool Completed { get; set; }
        public DateTime? CompletedAt { get; set; }

        public long? PatientId { get; set; }
        public long? DepartmentId { get; set; }
        public long? FromDepartmentId { get; set; }
        public long? ToDepartmentId { get; set; }
        public long? InstitutionId { get; set; }
        public long? FromInstitutionId { get; set; }
        public long? ToInstitutionId { get; set; }
        public long? ServiceCounterId { get; set; }
        public long? CreatedById { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Retired { get; set; }
        public long? RetiredById { get; set; }

        public long? StaffId { get; set; }
        public long? FromStaffId { get; set; }
        public long? ToStaffId { get; set; }
        public long? PatientEncounterId { get; set; }
        public long? SessionId { get; set; }
        public TokenStatus? Status { get; set; }
        public long? CounterId { get; set; }
        public long? DoctorId { get; set; }
        public long? ReferaTokenId { get; set; }
        public bool RestartTokenServices { get; set; }

        [ForeignKey("CateroryId")]
        public virtual Category Caterory { get; set; }

        [ForeignKey("BillId")]
        public virtual Bill Bill { get; set; }

        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }

        [ForeignKey("FromDepartmentId")]
        public virtual Department FromDepartment { get; set; }

        [ForeignKey("ToDepartmentId")]
        public virtual Department ToDepartment { get; set; }

        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }

        [ForeignKey("FromInstitutionId")]
        public virtual Institution FromInstitution { get; set; }

        [ForeignKey("ToInstitutionId")]
        public virtual Institution ToInstitution { get; set; }

        [ForeignKey("ServiceCounterId")]
        public virtual Department ServiceCounter { get; set; }

        [ForeignKey("CreatedById")]
        public virtual WebUser CreatedBy { get; set; }

        [ForeignKey("RetiredById")]
        public virtual WebUser RetiredBy { get; set; }

        [ForeignKey("StaffId")]
        public virtual Staff Staff { get; set; }

        [ForeignKey("FromStaffId")]
        public virtual Staff FromStaff { get; set; }

        [ForeignKey("ToStaffId")]
        public virtual Staff ToStaff { get; set; }

        [ForeignKey("PatientEncounterId")]
        public virtual PatientEncounter PatientEncounter { get; set; }
        [ForeignKey("SessionId")]
        public virtual OpdSession Session { get; set; }

        [ForeignKey("CounterId")]
        public virtual Department Counter { get; set; }

        [ForeignKey("DoctorId")]
        public virtual Doctor Doctor { get; set; }

        [ForeignKey("ReferaTokenId")]
        public virtual Token ReferaToken { get; set; }
    }
}
