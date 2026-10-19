// ============================================================================
// Developer : Tajwer Jalal
// Date      : 19-Oct-2026
// Time      : 11:55
// Purpose   : The pharmacy double-entry ledger and the small legacy masters
//             it depends on, ported from BASEMENT\CURRENT\CHART.DBF,
//             MAINHEAD.DBF, MAINDET.DBF, GENERIC.DBF and CATALOG.DBF.
// ============================================================================
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models.Pharmacy
{
    /// <summary>
    /// Chart of accounts (legacy CHART.dbf, 636 rows).
    ///
    /// This is what makes "open balance" meaningful for a store. A balance
    /// derived from PHITEMSTOCK is a stock valuation; a balance derived from
    /// here is how much the store owes, or is owed. Both are exposed, they are
    /// not the same figure.
    ///
    /// YEARDEBIT / YEARCREDIT are the legacy's own year-to-date totals (YDR /
    /// YCR), stored verbatim so the migrated balance can be compared against
    /// the legacy figure rather than silently replacing it.
    /// </summary>
    public class PhAccount
    {
        public int Id { get; set; }

        [Required, StringLength(20)]
        public string Code { get; set; } = "";

        [Required, StringLength(200)]
        public string Name { get; set; } = "";

        /// <summary>Legacy ACTYPE - 'G' group, etc.</summary>
        [StringLength(1)]
        public string? AccountType { get; set; }

        public int Level { get; set; } = 1;

        [StringLength(20)]
        public string? ParentCode { get; set; }

        /// <summary>Legacy OPBAL.</summary>
        public decimal OpeningBalance { get; set; }

        /// <summary>Legacy YDR.</summary>
        public decimal YearDebit { get; set; }

        /// <summary>Legacy YCR.</summary>
        public decimal YearCredit { get; set; }
    }

    /// <summary>
    /// Voucher header (legacy MAINHEAD.DBF, 80,168 rows).
    /// DocType 'PV' = purchase voucher (48,087), 'RV' = sales voucher (32,081).
    /// PhPurchase.GLNO / PhSale.GLNO hold this VoucherId as text, which is how
    /// a document ties back to its accounting entry.
    /// </summary>
    public class PhVoucher
    {
        public int Id { get; set; }

        /// <summary>PV | RV.</summary>
        [Required, StringLength(2)]
        public string DocType { get; set; } = "";

        [Required, StringLength(30)]
        public string VoucherNo { get; set; } = "";

        public DateTime VoucherDate { get; set; }

        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<PhVoucherLine> Lines { get; set; } = new List<PhVoucherLine>();
    }

    /// <summary>
    /// Voucher line (legacy MAINDET.DBF, 179,893 rows).
    /// NARRATION is the legacy DESC and is the only place the counterparty is
    /// recorded - 64,191 lines read "Medicine sale on credit to HMC Satellite",
    /// which is how the main store's receivable from the sub store is found.
    /// </summary>
    public class PhVoucherLine
    {
        public int Id { get; set; }

        public int VoucherId { get; set; }
        public PhVoucher? Voucher { get; set; }

        [StringLength(20)]
        public string? AccountCode { get; set; }

        [StringLength(200)]
        public string? Narration { get; set; }

        public decimal Amount { get; set; }

        [StringLength(50)]
        public string? ChequeNo { get; set; }

        [StringLength(100)]
        public string? Bank { get; set; }
    }

    /// <summary>Generic (molecule) name - legacy GENERIC.DBF, 1,193 rows.</summary>
    public class PhGeneric
    {
        public int Id { get; set; }

        [Required, StringLength(20)]
        public string Code { get; set; } = "";

        [Required, StringLength(200)]
        public string Name { get; set; } = "";

        public bool Retired { get; set; }

        [NotMapped]
        public ICollection<PhItem> Items { get; set; } = new List<PhItem>();
    }

    /// <summary>Item catalogue - legacy CATALOG.DBF, 19 rows.</summary>
    public class PhCategory
    {
        public int Id { get; set; }

        [Required, StringLength(20)]
        public string Code { get; set; } = "";

        [Required, StringLength(200)]
        public string Name { get; set; } = "";

        public bool Retired { get; set; }

        [NotMapped]
        public ICollection<PhItem> Items { get; set; } = new List<PhItem>();
    }
}
