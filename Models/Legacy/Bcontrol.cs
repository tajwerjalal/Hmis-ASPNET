using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models.Legacy
{
    /// <summary>
    /// LEGACY BILL COUNTER - single row (Id = 1) holding the next bill numbers.
    /// Source table <c>bcontrol.dbf</c> (VFP GENBILL / GENBILLI).
    ///
    /// Business rules
    ///   * OPDBILL = last allocated OPD bill no; IPDBILL = last allocated IPD bill no.
    ///   * Allocation is <c>value + 1</c> under UPDLOCK, ROWLOCK inside a transaction
    ///     (VFP table-buffered +1 was not atomic - this is a deliberate improvement).
    ///   * Never decrement; numbers are never reused.
    ///
    /// Developer: Tajwer Jalal | Date: 12-Oct-2026 | Time: 12:06
    /// </summary>
    [Table("BCONTROL")]
    public class Bcontrol
    {
        /// <summary>Always 1 (single counter row, unique).</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>Last allocated OPD bill number.</summary>
        public long? OpdBill { get; set; }

        /// <summary>Last allocated IPD bill number.</summary>
        public long? IpdBill { get; set; }
    }
}
