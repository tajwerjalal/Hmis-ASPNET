using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("STAFFSALARY")]
    public class StaffSalary
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string Name { get; set; }
        public double? GrossSalary { get; set; }
        public double? NetSalary { get; set; }
        public double? EpfEmployee { get; set; }
        public double? EpfEmployer { get; set; }
        public double? Etf { get; set; }
        public double? Payee { get; set; }
        public double? BasicSalary { get; set; }
        public double? Allowance { get; set; }
        public DateTime? SalaryDate { get; set; }
        public long? StaffId { get; set; }
        public long? InstitutionId { get; set; }
        public long? DepartmentId { get; set; }
        public long? CreaterId { get; set; }
        public long? EditerId { get; set; }
        public long? RetirerId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public DateTime? RetiredAt { get; set; }
        public bool Retired { get; set; }
        public string RetireComments { get; set; }
        [ForeignKey("StaffId")]
        public virtual Staff Staff { get; set; }
        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }
        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }
        [ForeignKey("EditerId")]
        public virtual WebUser Editer { get; set; }
        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }
    }
}