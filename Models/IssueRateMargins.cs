using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("ISSUERATEMARGINS")]
    public class IssueRateMargins
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Name { get; set; }
        public DateTime? CreatedAt { get; set; }
        public long? CreaterId { get; set; }
        public bool Retired { get; set; }
        public DateTime? RetiredAt { get; set; }
        public long? RetirerId { get; set; }

        public long? FromDepartmentId { get; set; }
        public long? ToDepartmentId { get; set; }
        public long? FromInstitutionId { get; set; }
        public long? ToInstitutionId { get; set; }

        public double RateForPharmaceuticals { get; set; }
        public double RateForConsumables { get; set; }
        public double RateForInventory { get; set; }
        public bool ShowRates { get; set; }
        public bool AtPurchaseRate { get; set; }

        [ForeignKey("CreaterId")]
        public virtual WebUser Creater { get; set; }

        [ForeignKey("RetirerId")]
        public virtual WebUser Retirer { get; set; }

        [ForeignKey("FromDepartmentId")]
        public virtual Department FromDepartment { get; set; }

        [ForeignKey("ToDepartmentId")]
        public virtual Department ToDepartment { get; set; }

        [ForeignKey("FromInstitutionId")]
        public virtual Institution FromInstitution { get; set; }

        [ForeignKey("ToInstitutionId")]
        public virtual Institution ToInstitution { get; set; }
    }
}
