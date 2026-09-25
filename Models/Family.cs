using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("FAMILY")]
    public class Family
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long? MembershipCardNo { get; set; }
        public string PhoneNo { get; set; }
        public string Comments { get; set; }

        public long? MembershipSchemeId { get; set; }
        public long? CreatedInstitutionId { get; set; }
        public long? CreatedDepartmentId { get; set; }

        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool Retired { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? RetiredAt { get; set; }
        public string RetireComments { get; set; }

        [ForeignKey("MembershipSchemeId")]
        public virtual MembershipScheme MembershipScheme { get; set; }

        [ForeignKey("CreatedInstitutionId")]
        public virtual Institution CreatedInstitution { get; set; }

        [ForeignKey("CreatedDepartmentId")]
        public virtual Department CreatedDepartment { get; set; }

        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }

        [ForeignKey("EditerId")]
        public virtual WebUser Editer { get; set; }

        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }
    }
}
