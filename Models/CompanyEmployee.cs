using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("COMPANYEMPLOYEE")]
    public class CompanyEmployee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public long CompanyId { get; set; }

        public string EmployeeCode { get; set; }
        public long? PersonId { get; set; }
        public string Name { get; set; }
        public string Designation { get; set; }
        public string Nic { get; set; }
        public string Mobile { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }

        public bool Retired { get; set; }
        public string RetireComments { get; set; }
        public long? CreaterId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? EditerId { get; set; }
        public DateTime? EditedAt { get; set; }

        [ForeignKey("CompanyId")]
        public virtual Institution Company { get; set; }

        [ForeignKey("PersonId")]
        public virtual Person Person { get; set; }

        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }

        [ForeignKey("EditerId")]
        public virtual WebUser Editer { get; set; }
    }
}