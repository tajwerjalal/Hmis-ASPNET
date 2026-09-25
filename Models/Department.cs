using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("DEPARTMENT")]
    public class Department
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Name { get; set; }
        public string Sname { get; set; }
        public string Code { get; set; }
        public string PrintingName { get; set; }
        public string DepartmentAddress { get; set; }
        public string DepartmentPhone { get; set; }
        public string DepartmentFax { get; set; }
        public string DepartmentEmail { get; set; }

        public DepartmentType? DepartmentType { get; set; }

        public long? InstitutionId { get; set; }
        public long? SiteId { get; set; }
        public long? LabInstitutionId { get; set; }
        public long? SuperDepartmentId { get; set; }
        public long? SampleDepartmentId { get; set; }
        public long? LabDepartmentId { get; set; }

        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? RetiredAt { get; set; }
        public bool Retired { get; set; }
        public string RetireComments { get; set; }
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }

        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }

        [ForeignKey("SiteId")]
        public virtual Institution Site { get; set; }

        [ForeignKey("LabInstitutionId")]
        public virtual Institution LabInstitution { get; set; }

        [ForeignKey("SuperDepartmentId")]
        public virtual Department SuperDepartment { get; set; }

        [ForeignKey("SampleDepartmentId")]
        public virtual Department SampleDepartment { get; set; }

        [ForeignKey("LabDepartmentId")]
        public virtual Department LabDepartment { get; set; }

        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }

        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }

        [ForeignKey("EditerId")]
        public virtual WebUser Editer { get; set; }
    }
}