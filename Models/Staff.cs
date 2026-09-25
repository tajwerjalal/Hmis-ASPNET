using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("STAFF")]
    public class Staff
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Code { get; set; }
        public string StaffNumber { get; set; }
        public string CodeNumber { get; set; }
        public string SpecialityCode { get; set; }
        public string CpmdcCode { get; set; }

        public long? PersonId { get; set; }
        public long? DesignationId { get; set; }
        public long? GradeId { get; set; }
        public long? StaffCategoryId { get; set; }
        public long? RosterId { get; set; }
        public long? SpecialityId { get; set; }
        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }
        public long? WorkingDepartmentId { get; set; }

        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? RetiredAt { get; set; }
        public bool Retired { get; set; }
        public string RetireComments { get; set; }
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }

        [ForeignKey("PersonId")]
        public virtual Person Person { get; set; }

        [ForeignKey("DesignationId")]
        public virtual Category Designation { get; set; }

        [ForeignKey("GradeId")]
        public virtual Category Grade { get; set; }

        [ForeignKey("StaffCategoryId")]
        public virtual Category StaffCategory { get; set; }

        [ForeignKey("RosterId")]
        public virtual Roster Roster { get; set; }

        [ForeignKey("SpecialityId")]
        public virtual Speciality Speciality { get; set; }

        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }

        [ForeignKey("WorkingDepartmentId")]
        public virtual Department WorkingDepartment { get; set; }

        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }

        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }

        [ForeignKey("EditerId")]
        public virtual WebUser Editer { get; set; }
    }
}