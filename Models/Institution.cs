using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("INSTITUTION")]
    public class Institution
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Name { get; set; }
        public string Sname { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Attention { get; set; }
        public string AttentionDesignation { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string PrintingName { get; set; }
        public string Code { get; set; }
        public string RegNo { get; set; }
        public string PharmacyCode { get; set; }
        public string SiName { get; set; }

        public InstitutionType? InstitutionType { get; set; }

        public long? InstitutionId { get; set; }
        public long? BankBranchId { get; set; }
        public long? ParentInstitutionId { get; set; }
        public long? LabInstitutionId { get; set; }
        public long? ContactPersonId { get; set; }
        public long? LabDepartmentId { get; set; }
        public long? FeeListTypeId { get; set; }
        public long? RouteId { get; set; }

        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? RetiredAt { get; set; }
        public bool Retired { get; set; }
        public string RetireComments { get; set; }
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }

        [ForeignKey("InstitutionId")]
        public virtual Institution Parent { get; set; }

        [ForeignKey("BankBranchId")]
        public virtual Institution BankBranch { get; set; }

        [ForeignKey("ParentInstitutionId")]
        public virtual Institution ParentInstitution { get; set; }

        [ForeignKey("LabInstitutionId")]
        public virtual Institution LabInstitution { get; set; }

        [ForeignKey("ContactPersonId")]
        public virtual Person ContactPerson { get; set; }

        [ForeignKey("LabDepartmentId")]
        public virtual Department LabDepartment { get; set; }

        [ForeignKey("FeeListTypeId")]
        public virtual Category FeeListType { get; set; }

        [ForeignKey("RouteId")]
        public virtual Route Route { get; set; }

        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }

        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }

        [ForeignKey("EditerId")]
        public virtual WebUser Editer { get; set; }
    }
}