// ============================================================================
// Developer : Tajwer Jalal
// Date      : 19-Oct-2026
// Time      : 11:06
// Purpose   : The single stock ledger that every on-hand quantity, open
//             balance and ending inventory figure in the pharmacy module is
//             derived from. Legacy S-MOVE.PRG only ever recomputed INVMAST.ONHAND
//             in place, which is why the legacy system could not answer
//             "what was on hand on date X" for more than one store.
// ============================================================================
using System;
using System.ComponentModel.DataAnnotations;

namespace HmisWeb.Models.Pharmacy
{
    /// <summary>
    /// One row per stock movement per store per drug.
    ///
    /// DOCTYPE is one of:
    ///   OPENING      - migrated opening position (legacy OP_BAL / ONHAND)
    ///   PURCHASE     - receipt in (supplier purchase or main-store issue)
    ///   SALE         - sale out
    ///   SALERETURN   - retail sale return in
    ///   PURBASERETURN- purchase return out
    ///   ADJUST       - manual stock correction
    ///
    /// A movement to a sub store is written as TWO rows: a PURCHASE into the
    /// receiving store and, when the issuing store is tracked, a matching
    /// SALE-like issue out of the issuing store.
    /// </summary>
    public class PhStockMovement
    {
        public int Id { get; set; }

        public int StoreId { get; set; }
        public PhStore? Store { get; set; }

        public int ItemId { get; set; }
        public PhItem? Item { get; set; }

        public DateTime MoveDate { get; set; }

        /// <summary>OPENING | PURCHASE | SALE | SALERETURN | PURBASERETURN | ADJUST.
        /// VARCHAR(20) - "PURBASERETURN" is 13 characters.</summary>
        [Required, StringLength(20)]
        public string DocType { get; set; } = "";

        /// <summary>Header id in the originating document table.</summary>
        public int? DocId { get; set; }

        [StringLength(30)]
        public string? DocNo { get; set; }

        public decimal QtyIn { get; set; }
        public decimal QtyOut { get; set; }
        public decimal UnitCost { get; set; }

        [StringLength(200)]
        public string? Narration { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
