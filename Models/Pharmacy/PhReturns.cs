// ============================================================================
// Developer : Tajwer Jalal
// Date      : 19-Oct-2026
// Time      : 11:07
// Purpose   : Sales returns and purchase returns, ported from legacy
//             s_rn_mast / s_rn_det and p_rn_mast / p_rn_det.
// ============================================================================
using System;
using System.ComponentModel.DataAnnotations;

namespace HmisWeb.Models.Pharmacy
{
    /// <summary>A retail sale return (legacy s_rn_mast).</summary>
    public class PhSaleReturn
    {
        public int Id { get; set; }

        [Required, StringLength(30)]
        public string DocNo { get; set; } = "";

        public DateTime DocDate { get; set; }

        public int StoreId { get; set; }
        public PhStore? Store { get; set; }

        /// <summary>The original sale slip, where it can be identified.</summary>
        public int? SaleId { get; set; }
        public PhSale? Sale { get; set; }

        public decimal NetAmount { get; set; }

        public bool IsPosted { get; set; } = true;

        public bool Retired { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<PhSaleReturnLine> Lines { get; set; } = new List<PhSaleReturnLine>();
    }

    /// <summary>One drug line on a PhSaleReturn.</summary>
    public class PhSaleReturnLine
    {
        public int Id { get; set; }

        public int ReturnId { get; set; }
        public PhSaleReturn? SaleReturn { get; set; }

        public int ItemId { get; set; }
        public PhItem? Item { get; set; }

        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }

        public bool Retired { get; set; }
    }

    /// <summary>A purchase return (legacy p_rn_mast).</summary>
    public class PhPurchaseReturn
    {
        public int Id { get; set; }

        [Required, StringLength(30)]
        public string DocNo { get; set; } = "";

        public DateTime DocDate { get; set; }

        public int StoreId { get; set; }
        public PhStore? Store { get; set; }

        /// <summary>The original receipt, where it can be identified.</summary>
        public int? PurchaseId { get; set; }
        public PhPurchase? Purchase { get; set; }

        /// <summary>Store the goods go back to, for a main-store return.</summary>
        public int? SourceStoreId { get; set; }
        public PhStore? SourceStore { get; set; }

        public decimal NetAmount { get; set; }

        public bool IsPosted { get; set; } = true;

        public bool Retired { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<PhPurchaseReturnLine> Lines { get; set; } = new List<PhPurchaseReturnLine>();
    }

    /// <summary>One drug line on a PhPurchaseReturn.</summary>
    public class PhPurchaseReturnLine
    {
        public int Id { get; set; }

        public int ReturnId { get; set; }
        public PhPurchaseReturn? PurchaseReturn { get; set; }

        public int ItemId { get; set; }
        public PhItem? Item { get; set; }

        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }

        public bool Retired { get; set; }
    }
}
