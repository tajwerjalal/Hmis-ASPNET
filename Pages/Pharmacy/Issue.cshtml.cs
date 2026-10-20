// ============================================================================
// Developer : Tajwer Jalal
// Date      : 19-Oct-2026
// Time      : 17:30
// Purpose   : Issue - port of legacy sal_sate / sal_hipd / sal_hopd / sal_ward.
//             One screen with a destination picker, because all four legacy
//             screens were the same operation with a different destination.
//
// WHAT THIS WRITES
//   Stock goes OUT of the source store. Where the document is recorded
//   depends on the destination:
//     * Satellite Store -> PHPURCHASE with SourceStoreId (store-to-store)
//     * HMC IPD/OPD/Ward -> PHSALE with PatientType I/O/W
//
//   In both cases a PHSTOCKMOVEMENT row with QtyOut is written and
//   PHITEMSTOCK.ONHANDQTY is reduced. For store-to-store, a matching
//   QtyIn movement is written at the receiving store.
//
//   All of it is one transaction: if any line fails, no stock moves and
//   no header is written.
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
    public class IssueModel : PageModel
    {
        private readonly PhService _ph;
        private readonly HmisWeb.Data.ApplicationDbContext _db;

        public IssueModel(PhService ph, HmisWeb.Data.ApplicationDbContext db)
        {
            _ph = ph;
            _db = db;
        }

        [BindProperty(SupportsGet = true)] public int StoreId { get; set; }
        [BindProperty(SupportsGet = true)] public string? Search { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? From { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? To { get; set; }

        [BindProperty] public HeaderModel Head { get; set; } = new();

        [BindProperty] public string? LinesJson { get; set; }

        /// <summary>Full grid payload (master detail included), echoed back
        /// when validation fails so the popup reopens with the typed values.
        /// Developer: Tajwer Jalal 20-Oct-2026 10:05</summary>
        [BindProperty] public string? LinesDetailJson { get; set; }

        [TempData] public string? Message { get; set; }
        [TempData] public string? Error { get; set; }

        public List<PhIssueRow> Rows { get; set; } = new();
        public List<PhStore> Stores { get; set; } = new();

        public class HeaderModel
        {
            public int Id { get; set; }
            public string DocNo { get; set; } = "";
            public DateTime DocDate { get; set; } = DateTime.Today;
            public int StoreId { get; set; }
            public string Destination { get; set; } = "";
            public string? PatientName { get; set; }
            public decimal DiscountAmount { get; set; }
        }

        public class LineModel
        {
            public int ItemId { get; set; }
            public decimal Qty { get; set; }
            public decimal Rate { get; set; }
            public decimal TaxRate { get; set; }
            public decimal DiscountAmount { get; set; }
            public string? BatchNo { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            Stores = await _ph.GetStoresAsync();
            if (StoreId == 0 && Stores.Count > 0) StoreId = Stores[0].Id;
            Rows = await _ph.GetIssuesAsync(StoreId, Search, From, To);
        }

        public async Task<IActionResult> OnGetItemLookupAsync(string? term)
        {
            var store = StoreId > 0 ? StoreId : (await _ph.GetStoresAsync()).FirstOrDefault()?.Id ?? 0;
            var items = await _ph.GetItemLookupAsync(store, term);
            return new JsonResult(items);
        }

        public async Task<IActionResult> OnGetNextDocNoAsync(int storeId)
        {
            return new JsonResult(new { docNo = await _ph.NextIssueDocNoAsync(storeId) });
        }

        public async Task<IActionResult> OnGetLinesAsync(int id, bool isStore)
        {
            PhIssueHead head;
            List<PhIssueLineRow> lines;

            if (isStore)
            {
                var h = await _db.PhPurchases.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                if (h == null) return NotFound();
                head = new PhIssueHead
                {
                    id = h.Id,
                    docNo = h.DocNo,
                    docDate = h.DocDate.ToString("yyyy-MM-dd"),
                    // For a store-to-store issue the ISSUING store is
                    // SourceStoreId; StoreId is the receiving store. Show the
                    // issuing store so the popup reads the way the document
                    // was written.
                    storeId = h.SourceStoreId ?? h.StoreId,
                    destination = "SATL",
                    patientName = h.VendorName ?? "",
                    discountAmount = h.DiscountAmount,
                    grossAmount = h.GrossAmount,
                    taxAmount = h.TaxAmount,
                    netAmount = h.NetAmount,
                    isPosted = h.IsPosted
                };
            }
            else
            {
                var h = await _db.PhSales.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
                if (h == null) return NotFound();
                var dest = h.PatientType switch
                {
                    "I" => "IPD",
                    "W" => "WARD",
                    _ => "OPD"
                };
                head = new PhIssueHead
                {
                    id = h.Id,
                    docNo = h.SlipNo,
                    docDate = h.SlipDate.ToString("yyyy-MM-dd"),
                    storeId = h.StoreId,
                    destination = dest,
                    patientName = h.PatientName ?? "",
                    discountAmount = h.DiscountAmount,
                    grossAmount = h.GrossAmount,
                    taxAmount = h.TaxAmount,
                    netAmount = h.NetAmount,
                    isPosted = h.IsPosted
                };
            }

            lines = await _ph.GetIssueLinesAsync(id, isStore);
            return new JsonResult(new { head, lines });
        }

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
                Error = "Source store is required.";
            else if (string.IsNullOrWhiteSpace(Head.Destination))
                Error = "Destination is required.";
            else if (lines == null || lines.Count == 0)
                Error = "Add at least one medicine line.";
            else if (lines.Any(l => l.ItemId <= 0))
                Error = "Every line needs a medicine.";
            else if (lines.Any(l => l.Qty <= 0))
                Error = "Every line needs a quantity greater than zero.";

            if (Error != null)
            {
                await LoadAsync();
                return Page();
            }

            var storeId = Head.StoreId;
            var docDate = Head.DocDate == default ? DateTime.Today : Head.DocDate;
            var isStoreIssue = Head.Destination == "SATL";

            // A store cannot issue to itself - that would write a zero-sum
            // pair of movements and confuse the stock ledger.
            if (isStoreIssue)
            {
                var subId = await _db.PhStores.AsNoTracking()
                    .Where(s => s.StoreType == "SUB" && s.IsActive)
                    .Select(s => (int?)s.Id)
                    .FirstOrDefaultAsync();

                if (subId == storeId)
                {
                    Error = "The source store is already the satellite store, so it cannot issue to itself.";
                    await LoadAsync();
                    return Page();
                }
            }

            // ---- never issue more than the source store actually holds ----
            // Without this an issue quietly drives PHITEMSTOCK.ONHANDQTY
            // negative, and every valuation derived from the ledger is then
            // wrong. The store is named in the message so the operator knows
            // which drawer to check.
            var itemIds = lines!.Select(l => l.ItemId).Distinct().ToList();
            var onHand = await _db.PhItemStocks.AsNoTracking()
                .Where(s => s.StoreId == storeId && itemIds.Contains(s.ItemId) && !s.Retired)
                .ToDictionaryAsync(s => s.ItemId, s => s.OnHandQty);

            var shortLines = lines
                .Where(l => l.Qty > (onHand.TryGetValue(l.ItemId, out var oh) ? oh : 0m))
                .ToList();

            if (shortLines.Count > 0)
            {
                var shortIds = shortLines.Select(l => l.ItemId).Distinct().ToList();
                var codes = await _db.PhItems.AsNoTracking()
                    .Where(i => shortIds.Contains(i.Id))
                    .ToDictionaryAsync(i => i.Id, i => i.Code);

                var detail = string.Join("; ", shortLines.Take(5).Select(l =>
                {
                    var code = codes.TryGetValue(l.ItemId, out var c) ? c : l.ItemId.ToString();
                    var have = onHand.TryGetValue(l.ItemId, out var q) ? q : 0m;
                    return $"{code} needs {l.Qty:0.##}, on hand {have:0.##}";
                }));

                var storeName = (await _db.PhStores.AsNoTracking()
                    .Where(s => s.Id == storeId).Select(s => s.Name).FirstOrDefaultAsync()) ?? "store";

                Error = $"Not enough stock at {storeName}: {detail}" +
                        (shortLines.Count > 5 ? $" (+{shortLines.Count - 5} more)" : "") +
                        ". Nothing was saved.";
                await LoadAsync();
                return Page();
            }

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
                if (isStoreIssue)
                {
                    // ---- store-to-store: PHPURCHASE with SourceStoreId ----
                    var destStore = await _db.PhStores.FirstOrDefaultAsync(s => s.StoreType == "SUB" && s.IsActive)
                                    ?? throw new InvalidOperationException("No active sub store found.");

                    PhPurchase head;
                    if (Head.Id == 0)
                    {
                        head = new PhPurchase
                        {
                            DocNo = Head.DocNo.Trim(),
                            DocDate = docDate,
                            StoreId = destStore.Id,
                            SourceStoreId = storeId,
                            VendorName = "Main store (Basement)",
                            VendorInvoiceNo = Head.PatientName,
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
                        head.StoreId = destStore.Id;
                        head.SourceStoreId = storeId;
                        head.VendorName = "Main store (Basement)";
                        head.VendorInvoiceNo = Head.PatientName;
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
                            BatchNo = l.BatchNo
                        });

                        // out of the source store
                        _db.PhStockMovements.Add(new PhStockMovement
                        {
                            StoreId = storeId,
                            ItemId = l.ItemId,
                            MoveDate = docDate,
                            DocType = "ISSUE",
                            DocId = head.Id,
                            DocNo = head.DocNo,
                            QtyIn = 0,
                            QtyOut = l.Qty,
                            UnitCost = l.Rate,
                            Narration = "Issued to " + destStore.Name
                        });

                        // into the receiving store
                        _db.PhStockMovements.Add(new PhStockMovement
                        {
                            StoreId = destStore.Id,
                            ItemId = l.ItemId,
                            MoveDate = docDate,
                            DocType = "PURCHASE",
                            DocId = head.Id,
                            DocNo = head.DocNo,
                            QtyIn = l.Qty,
                            QtyOut = 0,
                            UnitCost = l.Rate,
                            Narration = "Received from main store"
                        });

                        await ApplyToStockAsync(storeId, l.ItemId, -l.Qty, l.Rate);
                        await ApplyToStockAsync(destStore.Id, l.ItemId, l.Qty, l.Rate);
                    }
                }
                else
                {
                    // ---- department issue: PHSALE with PatientType ----
                    var patientType = Head.Destination switch
                    {
                        "IPD" => "I",
                        "WARD" => "W",
                        _ => "O"
                    };

                    PhSale head;
                    if (Head.Id == 0)
                    {
                        head = new PhSale
                        {
                            SlipNo = Head.DocNo.Trim(),
                            SlipDate = docDate,
                            StoreId = storeId,
                            PatientType = patientType,
                            PatientName = Head.PatientName,
                            GrossAmount = gross,
                            DiscountAmount = disc + Head.DiscountAmount,
                            TaxAmount = tax,
                            NetAmount = net,
                            PaidAmount = 0,
                            IsPosted = true
                        };
                        _db.PhSales.Add(head);
                        await _db.SaveChangesAsync();
                    }
                    else
                    {
                        head = await _db.PhSales.FirstOrDefaultAsync(s => s.Id == Head.Id)
                               ?? throw new InvalidOperationException("Document not found.");
                        if (head.IsPosted)
                            throw new InvalidOperationException(
                                "This document is already posted. Reverse it instead of editing it.");

                        head.SlipNo = Head.DocNo.Trim();
                        head.SlipDate = docDate;
                        head.StoreId = storeId;
                        head.PatientType = patientType;
                        head.PatientName = Head.PatientName;
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

                        _db.PhSaleLines.Add(new PhSaleLine
                        {
                            SaleId = head.Id,
                            ItemId = l.ItemId,
                            Qty = l.Qty,
                            Rate = l.Rate,
                            GrossAmount = lineGross,
                            DiscountAmount = l.DiscountAmount,
                            TaxAmount = lineTax,
                            NetAmount = lineGross + lineTax - l.DiscountAmount
                        });

                        _db.PhStockMovements.Add(new PhStockMovement
                        {
                            StoreId = storeId,
                            ItemId = l.ItemId,
                            MoveDate = docDate,
                            DocType = "SALE",
                            DocId = head.Id,
                            DocNo = head.SlipNo,
                            QtyIn = 0,
                            QtyOut = l.Qty,
                            UnitCost = l.Rate,
                            Narration = "Issue to " + Head.Destination
                        });

                        await ApplyToStockAsync(storeId, l.ItemId, -l.Qty, l.Rate);
                    }
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

            Message = $"Issue '{Head.DocNo}' saved and posted to stock ({lines.Count} line(s)).";
            return RedirectToPage(new { StoreId, Search, From, To });
        }

        /// <summary>
        /// Apply a quantity delta to one (item, store) stock row.
        ///
        /// Developer: Tajwer Jalal
        /// Created: 19-Oct-2026 17:30
        /// </summary>
        /// <param name="adjustAvgCost">
        /// True for a receipt, where the moving average must be recomputed.
        /// False for reversing an ISSUE: the issue never changed the average,
        /// so putting the quantity back must not change it either. Recomputing
        /// here would re-value the store at the document rate and leave a
        /// permanent drift in the valuation.
        /// </param>
        private async Task ApplyToStockAsync(
            int storeId, int itemId, decimal qty, decimal unitCost, bool adjustAvgCost = true)
        {
            var st = await _db.PhItemStocks.FirstOrDefaultAsync(s => s.ItemId == itemId && s.StoreId == storeId);
            if (st == null)
            {
                if (qty <= 0) return;
                st = new PhItemStock
                {
                    ItemId = itemId,
                    StoreId = storeId,
                    OnHandQty = qty,
                    AvgCost = unitCost
                };
                _db.PhItemStocks.Add(st);
                return;
            }

            var oldQty = st.OnHandQty;
            var newQty = oldQty + qty;

            if (adjustAvgCost && newQty > 0 && qty > 0)
            {
                st.AvgCost = oldQty > 0
                    ? ((oldQty * st.AvgCost) + (qty * unitCost)) / newQty
                    : unitCost;
            }

            st.OnHandQty = newQty;
        }

        /// <summary>
        /// Remove a quantity that was received at unitCost and unwind the
        /// moving average, the same way the posted-purchase reversal does
        /// (defect D10). Removing at the current average alone would leave the
        /// store valued at the blended rate for ever.
        ///
        /// Developer: Tajwer Jalal
        /// Created: 20-Oct-2026 11:20
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
                // Never allow a negative average; it would poison every later
                // valuation of this store.
                st.AvgCost = remainingValue > 0 ? remainingValue / qtyAfter : 0;
            }

            st.OnHandQty = qtyAfter;
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, bool isStore)
        {
            var storeId = StoreId > 0 ? StoreId : (await _ph.GetStoresAsync()).FirstOrDefault()?.Id ?? 0;

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                if (isStore)
                {
                    var head = await _db.PhPurchases.FirstOrDefaultAsync(p => p.Id == id);
                    if (head == null)
                    {
                        Error = "Issue not found.";
                        return RedirectToPage(new { StoreId, Search, From, To });
                    }

                    // Migrated documents were carried in as opening balances,
                    // not as ledger movements. Reversing one would invent
                    // stock that was never taken out, so retire it without
                    // touching the ledger.
                    var moveCount = await _db.PhStockMovements
                        .CountAsync(m => m.DocNo == head.DocNo);
                    if (moveCount == 0)
                    {
                        head.Retired = true;
                        await _db.SaveChangesAsync();
                        await tx.CommitAsync();
                        Message = $"'{head.DocNo}' is a migrated record with no stock movements, " +
                                  "so it was retired without changing stock.";
                        return RedirectToPage(new { StoreId, Search, From, To });
                    }

                    var lines = await _db.PhPurchaseLines.Where(l => l.PurchaseId == id && !l.Retired).ToListAsync();

                    if (lines.Count == 0)
                    {
                        head.Retired = true;
                        await _db.SaveChangesAsync();
                        Message = $"Issue '{head.DocNo}' deleted.";
                        return RedirectToPage(new { StoreId, Search, From, To });
                    }

                    var destStoreId = head.StoreId;
                    var sourceStoreId = head.SourceStoreId ?? storeId;

                    foreach (var l in lines)
                    {
                        // put it back in the source store
                        _db.PhStockMovements.Add(new PhStockMovement
                        {
                            StoreId = sourceStoreId,
                            ItemId = l.ItemId,
                            MoveDate = DateTime.Today,
                            DocType = "ISSUE",
                            DocId = head.Id,
                            DocNo = head.DocNo,
                            QtyIn = l.Qty,
                            QtyOut = 0,
                            UnitCost = l.Rate,
                            Narration = "Reversal of issue " + head.DocNo
                        });

                        // take it back out of the destination store
                        _db.PhStockMovements.Add(new PhStockMovement
                        {
                            StoreId = destStoreId,
                            ItemId = l.ItemId,
                            MoveDate = DateTime.Today,
                            DocType = "PURCHASE",
                            DocId = head.Id,
                            DocNo = head.DocNo,
                            QtyIn = 0,
                            QtyOut = l.Qty,
                            UnitCost = l.Rate,
                            Narration = "Reversal of issue " + head.DocNo
                        });

                        await ApplyToStockAsync(sourceStoreId, l.ItemId, l.Qty, l.Rate, adjustAvgCost: false);
                        await ReverseFromStockAsync(destStoreId, l.ItemId, l.Qty, l.Rate);
                    }

                    head.Retired = true;
                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                else
                {
                    var head = await _db.PhSales.FirstOrDefaultAsync(s => s.Id == id);
                    if (head == null)
                    {
                        Error = "Issue not found.";
                        return RedirectToPage(new { StoreId, Search, From, To });
                    }

                    // Same guard as the store-to-store branch: the 3,521
                    // migrated satellite slips have no stock movements, so
                    // reversing one must not add stock back.
                    var saleMoveCount = await _db.PhStockMovements
                        .CountAsync(m => m.DocNo == head.SlipNo);
                    if (saleMoveCount == 0)
                    {
                        head.Retired = true;
                        await _db.SaveChangesAsync();
                        await tx.CommitAsync();
                        Message = $"'{head.SlipNo}' is a migrated record with no stock movements, " +
                                  "so it was retired without changing stock.";
                        return RedirectToPage(new { StoreId, Search, From, To });
                    }

                    var lines = await _db.PhSaleLines.Where(l => l.SaleId == id && !l.Retired).ToListAsync();

                    if (lines.Count == 0)
                    {
                        head.Retired = true;
                        await _db.SaveChangesAsync();
                        Message = $"Issue '{head.SlipNo}' deleted.";
                        return RedirectToPage(new { StoreId, Search, From, To });
                    }

                    foreach (var l in lines)
                    {
                        _db.PhStockMovements.Add(new PhStockMovement
                        {
                            StoreId = head.StoreId,
                            ItemId = l.ItemId,
                            MoveDate = DateTime.Today,
                            DocType = "SALERETURN",
                            DocId = head.Id,
                            DocNo = head.SlipNo,
                            QtyIn = l.Qty,
                            QtyOut = 0,
                            UnitCost = l.Rate,
                            Narration = "Reversal of issue " + head.SlipNo
                        });

                        await ApplyToStockAsync(head.StoreId, l.ItemId, l.Qty, l.Rate, adjustAvgCost: false);
                    }

                    head.Retired = true;
                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();
                }
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                Error = "Could not reverse the issue. " + ex.Message;
                return RedirectToPage(new { StoreId, Search, From, To });
            }

            Message = "Issue reversed and stock restored.";
            return RedirectToPage(new { StoreId, Search, From, To });
        }
    }

    public class PhIssueHead
    {
        public int id { get; set; }
        public string docNo { get; set; } = "";
        public string docDate { get; set; } = "";
        public int storeId { get; set; }
        public string destination { get; set; } = "";
        public string patientName { get; set; } = "";
        public decimal discountAmount { get; set; }
        public decimal grossAmount { get; set; }
        public decimal taxAmount { get; set; }
        public decimal netAmount { get; set; }
        public bool isPosted { get; set; }
    }
}
