using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HmisWeb.Data.Enums;

namespace HmisWeb.Models
{
    [Table("BILLNUMBER")]
    public class BillNumber
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long? LastBillNumber { get; set; }
        public BillType? BillType { get; set; }
        public BillClassType? BillClassType { get; set; }
        public int? BillYear { get; set; }
        public bool Retired { get; set; }
        public DateTime? RetiredAt { get; set; }

        public long? DepartmentId { get; set; }
        public long? InstitutionId { get; set; }
        public long? ToDepartmentId { get; set; }
        public long? RetirerId { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }
        [ForeignKey("InstitutionId")]
        public virtual Institution Institution { get; set; }
        [ForeignKey("ToDepartmentId")]
        public virtual Department ToDepartment { get; set; }
        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }
    }
}
