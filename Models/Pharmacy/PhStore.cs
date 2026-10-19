// ============================================================================
// Developer : Tajwer Jalal
// Date      : 19-Oct-2026
// Time      : 11:02
// Purpose   : Pharmacy multi-store registry and per-store stock, ported from
//             the legacy VFP6.0 systems BASEMENT (main store) + Satell
//             (sub store) under E:\Projects\HMC\PHARSYS.
// ============================================================================
using System;
using System.ComponentModel.DataAnnotations;

namespace HmisWeb.Models.Pharmacy
{
    /// <summary>
    /// A physical pharmacy store. In the legacy system a store was a whole
    /// FOLDER with its own copy of every DBF; here a store is one row and the
    /// data lives once. STORETYPE is 'MAIN' for the purchase/issue store
    /// (Basement) and 'SUB' for a retail counter (Satell).
    /// </summary>
    public class PhStore
    {
        public int Id { get; set; }

        [Required, StringLength(10)]
        public string Code { get; set; } = "";

        [Required, StringLength(100)]
        public string Name { get; set; } = "";

        /// <summary>MAIN or SUB.</summary>
        [Required, StringLength(10)]
        public string StoreType { get; set; } = "SUB";

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? OpeningDate { get; set; }

        public bool Retired { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<PhItemStock> ItemStocks { get; set; } = new List<PhItemStock>();
    }

    /// <summary>
    /// Per-store quantity and value for one drug. This child row is what
    /// replaces the legacy duplication where every store kept its own
    /// INVMAST row for the same drug (own ONHAND / OP_BAL / OP_TRADE /
    /// OP_RETAIL). Unique on (ItemId, StoreId).
    /// </summary>
    public class PhItemStock
    {
        public int Id { get; set; }

        public int ItemId { get; set; }
        public PhItem? Item { get; set; }

        public int StoreId { get; set; }
        public PhStore? Store { get; set; }

        /// <summary>Current on-hand quantity, maintained from PHSTOCKMOVEMENT.</summary>
        public decimal OnHandQty { get; set; }

        /// <summary>Legacy OP_BAL - quantity carried in at migration.</summary>
        public decimal OpeningQty { get; set; }

        /// <summary>Legacy OP_TRADE - trade value of the opening quantity.</summary>
        public decimal OpeningCost { get; set; }

        /// <summary>Legacy OP_RETAIL - retail value of the opening quantity.</summary>
        public decimal OpeningRetail { get; set; }

        /// <summary>Moving average cost at this store (legacy S-AVGCST).</summary>
        public decimal AvgCost { get; set; }

        public bool Retired { get; set; }
    }
}
