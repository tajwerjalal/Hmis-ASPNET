using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models
{
    /// <summary>
    /// Audit trail for laboratory indent payment-slip prints.
    /// One row per print action; PrintCount accumulates the number of
    /// duplicate (or original) prints issued for a given indent.
    /// </summary>
    [Table("INDENT_PRINT_LOG")]
    public class IndentPrintLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>FK to BILL.Id - the indent (payment slip) being reprinted.</summary>
        public long BillId { get; set; }

        /// <summary>0 = original slip, 1 = duplicate copy.</summary>
        public bool Duplicate { get; set; }

        /// <summary>User who performed the print (WEBUSER.Id).</summary>
        public long PrintedByUserId { get; set; }

        /// <summary>Server-side timestamp of the print action.</summary>
        public DateTime PrintedAt { get; set; }

        /// <summary>How many times this (bill, duplicate) combination has been printed.</summary>
        public int PrintCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
