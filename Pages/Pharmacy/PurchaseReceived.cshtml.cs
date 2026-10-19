// ============================================================================
// Developer : Tajwer Jalal
// Date      : 19-Oct-2026
// Time      : 16:55
// Purpose   : Purchase Received - port of legacy BASEMENT\SPR\PURCHASE.SPR.
//             The first transaction screen that actually moves stock.
//
// WHAT THIS WRITES
//   One PHPURCHASE header, its PHPURCHASELINE rows, and one PHSTOCKMOVEMENT
//   row per line. The movement is the source of truth: PHITEMSTOCK.ONHANDQTY
//   and AVGCOST are updated from it, and every on-hand / valuation / open
//   balance figure is derived from that ledger. Writing the header alone
//   would leave the document visible but the stock unchanged.
//
// AVERAGE COST
//   Weighted moving average, the same basis the legacy S-AVGCST used and the
//   same one the migration stored in PHITEMSTOCK.AVGCOST:
//       new = (oldQty * oldAvg + receivedQty * lineCost) / (oldQty + receivedQty)
//   Using the line's actual cost rather than the master trade cost is what
//   keeps the valuation honest when a supplier's price changes.
// ============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HmisWeb.Models.Pharmacy;
using HmisWeb.Services.Pharmacy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Pharmacy
{
    public class PurchaseReceivedModel : PageModel
    {
        private readonly PhService _ph;
        private readonly HmisWeb.Data.ApplicationDbContext _db;

        public PurchaseReceivedModel(PhService ph, HmisWeb.Data.ApplicationDbContext db)
        {
            _ph = ph;
            _db = db;
        }

        [BindProperty(SupportsGet = true)] public int StoreId { get; set; }
        [BindProperty(SupportsGet = true)] public string? Search { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? From { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? To { get; set; }

        [BindProperty] public HeaderModel Head { get; set; } = new();

        /// <summary>Posted back as a JSON string by the grid's hidden field.</summary>
        [BindProperty] public string? LinesJson { get; set; }

        [TempData] public string? Message { get; set; }
        [TempData] public string? Error { get; set; }

        public List<PhPurchaseRow> Rows { get; set; } = new();
        public List<PhStore> Stores { get; set; } = new();
        public List<PhSupplier> Suppliers { get; set; } = new();

        public class HeaderModel
        {
            public int Id { get; set; }
            public string DocNo { get; set; } = "";
            public DateTime DocDate { get; set; } = DateTime.Today;
            public int StoreId { get; set; }
            public string? VendorCode { get; set; }
            public string? VendorName { get; set; }
            public string? VendorInvoiceNo { get; set; }
            public decimal DiscountAmount { get; set; }
        }

        /// <summary>A line as posted by the grid.</summary>
        public class LineModel
        {
            public int ItemId { get; set; }
            public decimal Qty { get; set; }
            public decimal Rate { get; set; }
            public decimal TaxRate { get; set; }
            public decimal DiscountAmount { get; set; }
            public string? BatchNo { get; set; }
            public DateTime? ExpiryDate { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            Stores = await _ph.GetStoresAsync();
            Suppliers = await _ph.GetSupplierPickListAsync();
            if (StoreId == 0 && Stores.Count > 0) StoreId = Stores[0].Id;
            Rows = await _ph.GetPurchasesAsync(StoreId, Search, From, To);
        }

        /// <summary>Item lookup for the grid's picker. Returns the master
        /// detail - code, name, pack, trade cost, retail, on-hand.</summary>
        public async Task<IActionResult> OnGetItemLookupAsync(string? term)
        {
            var store = StoreId > 0 ? StoreId : (await _ph.GetStoresAsync()).FirstOrDefault()?.Id ?? 0;
            var items = await _ph.GetItemLookupAsync(store, term);
            return new JsonResult(items);
        }

        /// <summary>Next document number, so the operator never has to guess.</summary>
        public async Task<IActionResult> OnGetNextDocNoAsync(int storeId)
        {
            return new JsonResult(new { docNo = await _ph.NextPurchaseDocNoAsync(storeId) });
        }

        /// <summary>Reopen one saved document with its lines.</summary>
        public async Task<IActionResult> OnGetLinesAsync(int id)
        {
            var head = await _db.PhPurchases.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (head == null) return NotFound();

            var lines = await _ph.GetPurchaseLinesAsync(id);
            return new JsonResult(new
            {
                head = new
                {
                    id = head.Id,
                    docNo = head.DocNo,
                    docDate = head.DocDate.ToString("yyyy-MM-dd"),
                    storeId = head.StoreId,
                    vendorCode = head.VendorCode ?? "",
                    vendorName = head.VendorName ?? "",
                    vendorInvoiceNo = head.VendorInvoiceNo ?? "",
                    discountAmount = head.DiscountAmount,
                    grossAmount = head.GrossAmount,
                    taxAmount = head.TaxAmount,
                    netAmount = head.NetAmount,
                    isPosted = head.IsPosted
                },
                lines
            });
        }

        /// <summary>
        /// Save the document and post its stock.
        ///
        /// All of it is one transaction: if any line fails, no stock moves and
        /// no header is written. A half-posted purchase would leave on-hand
        /// disagreeing with the document that caused it.
        /// </summary>
        public async Task<IActionResult> OnPostSaveAsync()
        {
            List<LineModel>? lines = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(LinesJson))
                {
                    lines = System.Text.Json.JsonSerializer.Deserialize<List<LineModel>>(
                        LinesJson,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            catch (Exception ex)
            {
                Error = "The line grid could not be read: " + ex.Message;
            }

            if (string.IsNullOrWhiteSpace(Head.DocNo))
                Error = "Document number is required.";
            else if (Head.StoreId == 0)
                Error = "Receiving store is required.";
            else if (lines == null || lines.Count == 0)
                Error = "Add at least one medicine line.";
            else if (lines.Any(l => l.ItemId <= 0))
                Error = "Every line needs a medicine.";
            else if (lines.Any(l => l.Qty <= 0))
                Error = "Every line needs a quantity greater than zero.";
            else if (Head.Id == 0 && await _db.PhPurchases.AnyAsync(p => p.StoreId == Head.StoreId && p.DocNo == Head.DocNo))
                Error = $"Document '{Head.DocNo}' already exists in this store.";

            if (Error != null)
            {
                await LoadAsync();
                return Page();
            }

            var storeId = Head.StoreId;
            var docDate = Head.DocDate == default ? DateTime.Today : Head.DocDate;

            // ---- recompute the money server-side; never trust the grid ----
            decimal gross = 0, tax = 0, disc = 0;
            foreach (var l in lines!)
            {
                var lineGross = l.Qty * l.Rate;
                var lineTax = lineGross * l.TaxRate / 100m;
                gross += lineGross;
                tax += lineTax;
                disc += l.DiscountAmount;
            }
            var net = gross + tax - disc - Head.DiscountAmount;

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                PhPurchase head;
                if (Head.Id == 0)
                {
                    head = new PhPurchase
                    {
                        DocNo = Head.DocNo.Trim(),
                        DocDate = docDate,
                        StoreId = storeId,
                        VendorCode = Head.VendorCode,
                        VendorName = Head.VendorName,
                        VendorInvoiceNo = Head.VendorInvoiceNo,
                        GrossAmount = gross,
                        DiscountAmount = disc + Head.DiscountAmount,
                        TaxAmount = tax,
                        NetAmount = net,
                        IsPosted = true
                    };
                    _db.PhPurchases.Add(head);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    head = await _db.PhPurchases.FirstOrDefaultAsync(p => p.Id == Head.Id)
                           ?? throw new InvalidOperationException("Document not found.");
                    if (head.IsPosted)
                        throw new InvalidOperationException(
                            "This document is already posted. Reverse it instead of editing it.");

                    head.DocNo = Head.DocNo.Trim();
                    head.DocDate = docDate;
                    head.StoreId = storeId;
                    head.VendorCode = Head.VendorCode;
                    head.VendorName = Head.VendorName;
                    head.VendorInvoiceNo = Head.VendorInvoiceNo;
                    head.GrossAmount = gross;
                    head.DiscountAmount = disc + Head.DiscountAmount;
                    head.TaxAmount = tax;
                    head.NetAmount = net;
                    await _db.SaveChangesAsync();
                }

                foreach (var l in lines)
                {
                    var lineGross = l.Qty * l.Rate;
                    var lineTax = lineGross * l.TaxRate / 100m;

                    _db.PhPurchaseLines.Add(new PhPurchaseLine
                    {
                        PurchaseId = head.Id,
                        ItemId = l.ItemId,
                        Qty = l.Qty,
                        Rate = l.Rate,
                        TaxRate = l.TaxRate,
                        TaxAmount = lineTax,
                        DiscountAmount = l.DiscountAmount,
                        Amount = lineGross + lineTax - l.DiscountAmount,
                        BatchNo = l.BatchNo,
                        ExpiryDate = l.ExpiryDate
                    });

                    // ---- the stock ledger ----
                    _db.PhStockMovements.Add(new PhStockMovement
                    {
                        StoreId = storeId,
                        ItemId = l.ItemId,
                        MoveDate = docDate,
                        DocType = "PURCHASE",
                        DocId = head.Id,
                        DocNo = head.DocNo,
                        QtyIn = l.Qty,
                        QtyOut = 0,
                        UnitCost = l.Rate,
                        Narration = string.IsNullOrWhiteSpace(Head.VendorName)
                            ? "Purchase received"
                            : "Purchase received from " + Head.VendorName
                    });

                    await ApplyToStockAsync(storeId, l.ItemId, l.Qty, l.Rate);
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                Error = "Nothing was saved. " + ex.Message;
                await LoadAsync();
                return Page();
            }

            Message = $"Purchase '{Head.DocNo}' saved and posted to stock ({lines.Count} line(s)).";
            return RedirectToPage(new { StoreId, Search, From, To });
        }

        /// <summary>
        /// Update the per-store quantity and weighted average cost. Creates the
        /// (item, store) row if this drug has never been stocked here.
        /// </summary>
        private async Task ApplyToStockAsync(int storeId, int itemId, decimal qtyIn, decimal unitCost)
        {
            var st = await _db.PhItemStocks.FirstOrDefaultAsync(s => s.ItemId == itemId && s.StoreId == storeId);
            if (st == null)
            {
                st = new PhItemStock
                {
                    ItemId = itemId,
                    StoreId = storeId,
                    OnHandQty = qtyIn,
                    AvgCost = unitCost
                };
                _db.PhItemStocks.Add(st);
                return;
            }

            var oldQty = st.OnHandQty;
            var oldAvg = st.AvgCost;
            var newQty = oldQty + qtyIn;

            if (newQty > 0)
            {
                // Weighted moving average. When the old quantity is negative or
                // zero the old value cannot be weighted, so the incoming cost
                // becomes the new average.
                st.AvgCost = oldQty > 0
                    ? ((oldQty * oldAvg) + (qtyIn * unitCost)) / newQty
                    : unitCost;
            }

            st.OnHandQty = newQty;
        }

        /// <summary>
        /// Take a received quantity back out of stock and unwind the weighted
        /// average cost that the receipt introduced.
        ///
        /// Simply subtracting the quantity is not enough: a receipt changes the
        /// average, so removing the goods must restore the average that applied
        /// before they arrived. The value still held is
        /// (qty now x avg now) - (qty removed x its unit cost), and dividing that
        /// by the quantity left gives the prior average back.
        ///
        /// Developer: Tajwer Jalal
        /// Created: 19-Oct-2026 16:05
        /// </summary>
        private async Task ReverseFromStockAsync(int storeId, int itemId, decimal qtyOut, decimal unitCost)
        {
            var st = await _db.PhItemStocks.FirstOrDefaultAsync(s => s.ItemId == itemId && s.StoreId == storeId);
            if (st == null) return;

            var qtyBefore = st.OnHandQty;
            var avgBefore = st.AvgCost;
            var qtyAfter = qtyBefore - qtyOut;

            if (qtyAfter > 0)
            {
                var remainingValue = (qtyBefore * avgBefore) - (qtyOut * unitCost);
                // A legacy document can store an Amount below Qty x Rate, which
                // would drive the remaining value negative. Zero is the floor;
                // a negative average would corrupt every later valuation.
                st.AvgCost = remainingValue > 0 ? remainingValue / qtyAfter : 0;
            }

            st.OnHandQty = qtyAfter;
        }

        /// <summary>
        /// Reverse a posted purchase: write a negative movement for every line
        /// and back the quantity out of stock. The document is retired, not
        /// deleted, so the audit trail keeps both the original and the reversal.
        /// </summary>
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var head = await _db.PhPurchases.FirstOrDefaultAsync(p => p.Id == id);
            if (head == null) return NotFound();

            var lines = await _db.PhPurchaseLines
                .Where(l => l.PurchaseId == id && !l.Retired)
                .ToListAsync();

            if (lines.Count == 0)
            {
                head.Retired = true;
                await _db.SaveChangesAsync();
                Message = $"Purchase '{head.DocNo}' deleted.";
                return RedirectToPage(new { StoreId, Search, From, To });
            }

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                foreach (var l in lines)
                {
                    _db.PhStockMovements.Add(new PhStockMovement
                    {
                        StoreId = head.StoreId,
                        ItemId = l.ItemId,
                        MoveDate = DateTime.Today,
                        DocType = "PURCHASE",
                        DocId = head.Id,
                        DocNo = head.DocNo,
                        QtyIn = 0,
                        QtyOut = l.Qty,
                        UnitCost = l.Rate,
                        Narration = "Reversal of purchase " + head.DocNo
                    });

                    await ReverseFromStockAsync(head.StoreId, l.ItemId, l.Qty, l.Rate);
                }

                head.Retired = true;
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                Error = "Could not reverse the purchase. " + ex.Message;
                return RedirectToPage(new { StoreId, Search, From, To });
            }

            Message = $"Purchase '{head.DocNo}' reversed and removed from stock.";
            return RedirectToPage(new { StoreId, Search, From, To });
        }
    }
}
