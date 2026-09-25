using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    [Table("BILLENTRY")]
    public class BillEntry
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public double? TotDiscount { get; set; }
        public double? TotCash { get; set; }
        public double? TotCredit { get; set; }
        public double? TotCreditCard { get; set; }
        public double? TotVat { get; set; }
    }
}
