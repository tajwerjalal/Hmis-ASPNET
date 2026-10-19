// ============================================================================
// Developer : Tajwer Jalal
// Date      : 19-Oct-2026
// Time      : 11:05
// Purpose   : Purchase/receipt and retail sale headers with their lines.
//             Ported from legacy pur_mast/pur_det and sal_mast/sal_det.
// ============================================================================
using System;
using System.ComponentModel.DataAnnotations;

namespace HmisWeb.Models.Pharmacy
{
    /// <summary>
    /// A stock receipt. This ONE table covers both cases the legacy system
    /// spread across stores:
    ///
    ///   * Supplier purchase into the MAIN store
    ///       SourceStoreId = null
    ///
    ///   * Main store (Basement) ISSUING stock to a sub store
    ///       StoreId = receiving sub store, SourceStoreId = main store
    ///
    /// The legacy system had no table for the second case at all - it wrote a
    /// pur_mast/pur_det row into the RECEIVING store's own file and tagged it
    /// SPCODE='BASE', INVC='Basement' (all 65 Satell receipts carry exactly
    /// that). StoreId/SourceStoreId makes the same information explicit.
    /// </summary>
    public class PhPurchase
    {
        public int Id { get; set; }

        [Required, StringLength(30)]
        public string DocNo { get; set; } = "";

        public DateTime DocDate { get; set; }

        /// <summary>Receiving store.</summary>
        public int StoreId { get; set; }
        public PhStore? Store { get; set; }

        /// <summary>Issuing store, or null when purchased from a supplier.</summary>
        public int? SourceStoreId { get; set; }
        public PhStore? SourceStore { get; set; }

        [StringLength(20)]
        public string? VendorCode { get; set; }

        [StringLength(150)]
        public string? VendorName { get; set; }

        [StringLength(50)]
        public string? VendorInvoiceNo { get; set; }

        /// <summary>Legacy GLNO - joins the document to its PHVOUCHER row.</summary>
        [StringLength(30)]
        public string? GLNo { get; set; }

        /// <summary>Who this receipt is from, read off the matching ledger line.</summary>
        [StringLength(200)]
        public string? Counterparty { get; set; }

        public decimal GrossAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }

        /// <summary>Legacy POST flag.</summary>
        public bool IsPosted { get; set; } = true;

        public bool Retired { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<PhPurchaseLine> Lines { get; set; } = new List<PhPurchaseLine>();
    }

    /// <summary>One drug line on a PhPurchase.</summary>
    public class PhPurchaseLine
    {
        public int Id { get; set; }

        public int PurchaseId { get; set; }
        public PhPurchase? Purchase { get; set; }

        public int ItemId { get; set; }
        public PhItem? Item { get; set; }

        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Amount { get; set; }

        [StringLength(50)]
        public string? BatchNo { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public bool Retired { get; set; }
    }

    /// <summary>
    /// A retail sale slip. PatientType 'I' rows already carried a HOS_CODE -
    /// an HMIS inpatient registration number - in the legacy data, so pharmacy
    /// sales can be reconciled back against HMIS patients.
    /// </summary>
    public class PhSale
    {
        public int Id { get; set; }

        [Required, StringLength(30)]
        public string SlipNo { get; set; } = "";

        public DateTime SlipDate { get; set; }

        public int StoreId { get; set; }
        public PhStore? Store { get; set; }

        /// <summary>Legacy PT_TYPE - 'I' inpatient, 'O' other/walk-in.</summary>
        [Required, StringLength(5)]
        public string PatientType { get; set; } = "O";

        /// <summary>Legacy HOS_CODE - HMIS inpatient registration number.</summary>
        public int? HosCode { get; set; }

        [StringLength(100)]
        public string? PatientName { get; set; }

        /// <summary>Legacy GLNO - joins the slip to its PHVOUCHER row.</summary>
        [StringLength(30)]
        public string? GLNo { get; set; }

        /// <summary>Who the goods went to. On the main store's issues to the
        /// satellite this reads "HMC Satellite", which is how the inter-store
        /// receivable is identified.</summary>
        [StringLength(200)]
        public string? Counterparty { get; set; }

        public decimal GrossAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal PaidAmount { get; set; }

        public bool IsPosted { get; set; } = true;

        public bool Retired { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<PhSaleLine> Lines { get; set; } = new List<PhSaleLine>();
    }

    /// <summary>One drug line on a PhSale.</summary>
    public class PhSaleLine
    {
        public int Id { get; set; }

        public int SaleId { get; set; }
        public PhSale? Sale { get; set; }

        public int ItemId { get; set; }
        public PhItem? Item { get; set; }

        public decimal Qty { get; set; }

        /// <summary>Quantity handed back at the time of billing.</summary>
        public decimal ReturnQty { get; set; }

        public decimal Rate { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }

        public bool Retired { get; set; }
    }
}
