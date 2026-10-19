// ============================================================================
// Developer : Tajwer Jalal
// Date      : 19-Oct-2026
// Time      : 11:03
// Purpose   : The SINGLE pharmacy drug master, merged from both legacy
//             INVMAST copies, plus the one-copy supplier and manufacturer
//             masters.
// ============================================================================
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HmisWeb.Models.Pharmacy
{
    /// <summary>
    /// One row per drug, stored once for the whole organisation.
    ///
    /// Legacy source, merged (the Satell copy is a superset of the Basement
    /// copy - it adds ACTIVE and TOP):
    ///   BASEMENT\CURRENT\INVMAST.DBF  4,449 rows x 22 fields
    ///   Satell\CURRENT\invmast.dbf    4,929 rows x 24 fields
    ///
    /// Fields that were genuinely per-store (ONHAND, OP_BAL, OP_TRADE,
    /// OP_RETAIL, and the store's own pricing) were deliberately NOT brought
    /// in here - they live on PhItemStock, one row per store.
    /// </summary>
    public class PhItem
    {
        public int Id { get; set; }

        /// <summary>Legacy ITCODE.</summary>
        [Required, StringLength(20)]
        public string Code { get; set; } = "";

        /// <summary>Legacy ITNAME.</summary>
        [Required, StringLength(200)]
        public string Name { get; set; } = "";

        /// <summary>Legacy IT_TYPE - 'M' medicine, 'G' goods, ...</summary>
        [StringLength(1)]
        public string? ItemType { get; set; }

        /// <summary>Legacy SPCODE. Held as a code, not an id, because the legacy
        /// supplier master was never migrated for the main store.</summary>
        [StringLength(20)]
        public string? SupplierCode { get; set; }
        [NotMapped]
        public PhSupplier? Supplier { get; set; }

        /// <summary>Legacy MFCODE.</summary>
        [StringLength(20)]
        public string? ManufacturerCode { get; set; }
        [NotMapped]
        public PhManufacturer? Manufacturer { get; set; }

        /// <summary>Legacy CACODE - item catalogue (e.g. ANTIBIOTIC).</summary>
        [StringLength(20)]
        public string? CategoryCode { get; set; }
        [NotMapped]
        public PhCategory? Category { get; set; }

        /// <summary>Legacy GECODE - generic (molecule) name.</summary>
        [StringLength(20)]
        public string? GenericCode { get; set; }
        [NotMapped]
        public PhGeneric? Generic { get; set; }

        /// <summary>Legacy LCODE - rack/shelf location.</summary>
        [StringLength(20)]
        public string? LocationCode { get; set; }

        /// <summary>Legacy PK_TYPE - how the pack is billed.</summary>
        [StringLength(1)]
        public string? PackType { get; set; }

        /// <summary>Legacy PK_UNIT - units contained in one pack.</summary>
        public decimal? PackUnit { get; set; }

        /// <summary>Legacy TRADE - purchase/trade price per pack.</summary>
        public decimal TradeCost { get; set; }

        /// <summary>Legacy TP - tax-inclusive trade price. Differs from
        /// TradeCost once S_TAX_TP is non-zero, so both are kept.</summary>
        public decimal TradePrice { get; set; }

        /// <summary>Legacy S_TAX_TP - tax component above trade price.</summary>
        public decimal TaxAboveTrade { get; set; }

        /// <summary>Legacy S_TAX_RP - tax component above retail price.</summary>
        public decimal TaxAboveRetail { get; set; }

        /// <summary>Legacy RP / RETAIL - selling price per pack.</summary>
        public decimal RetailPrice { get; set; }

        /// <summary>Legacy S_TAX_RATE.</summary>
        public decimal TaxRate { get; set; }

        /// <summary>Legacy UNT_COST - cost of a single unit.</summary>
        public decimal UnitCost { get; set; }

        /// <summary>Legacy HOSP_COST - cost basis used for hospital billing.</summary>
        public decimal HospitalCost { get; set; }

        public decimal MinStock { get; set; }
        public decimal MaxStock { get; set; }

        /// <summary>Legacy REORDER.</summary>
        public decimal ReorderLevel { get; set; }

        /// <summary>Legacy ACTIVE (Satell-only field).</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Legacy TOP (Satell-only field) - flagged as a fast mover.</summary>
        public bool IsTop { get; set; }

        public bool Retired { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<PhItemStock> ItemStocks { get; set; } = new List<PhItemStock>();
    }

    /// <summary>Legacy supplier.dbf - migrated once, not once per store.</summary>
    public class PhSupplier
    {
        public int Id { get; set; }

        [Required, StringLength(20)]
        public string Code { get; set; } = "";

        [Required, StringLength(150)]
        public string Name { get; set; } = "";

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(80)]
        public string? City { get; set; }

        /// <summary>Legacy SNAME - the short name the pharmacy bills on.</summary>
        [StringLength(100)]
        public string? ShortName { get; set; }

        /// <summary>Legacy S_TYPE - 'M' medicine, etc.</summary>
        [StringLength(1)]
        public string? SupplierType { get; set; }

        /// <summary>Legacy ACTIVE.</summary>
        public bool Active { get; set; } = true;

        /// <summary>Legacy BALANCE - the supplier's outstanding balance.</summary>
        public decimal Balance { get; set; }

        public bool Retired { get; set; }

        [NotMapped]
        public ICollection<PhItem> Items { get; set; } = new List<PhItem>();
    }

    /// <summary>Legacy manufac.dbf - migrated once, not once per store.</summary>
    public class PhManufacturer
    {
        public int Id { get; set; }

        [Required, StringLength(20)]
        public string Code { get; set; } = "";

        [Required, StringLength(150)]
        public string Name { get; set; } = "";

        /// <summary>Legacy SNAME.</summary>
        [StringLength(100)]
        public string? ShortName { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        public bool Retired { get; set; }

        [NotMapped]
        public ICollection<PhItem> Items { get; set; } = new List<PhItem>();
    }
}
