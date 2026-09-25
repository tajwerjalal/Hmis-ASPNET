using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("WEBUSERPRIVILEGE")]
    public class WebUserPrivilege
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string Privilege { get; set; }
        public long? WebUserRoleId { get; set; }
        public bool Retired { get; set; }
        [ForeignKey("WebUserRoleId")]
        public virtual WebUserRole WebUserRole { get; set; }
    }
}
