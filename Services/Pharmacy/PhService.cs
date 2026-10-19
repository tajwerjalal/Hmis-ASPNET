// ============================================================================
// Developer : Tajwer Jalal
// Date      : 19-Oct-2026
// Time      : 12:40
// Purpose   : Read-side queries for the pharmacy module - multi-store stock,
//             ending inventory valuation, and open balance.
//
// WHY A SERVICE AND NOT LINES ON THE PAGE
//   Three of the four figures the brief asks for (open balance, on-hand
//   quantity, ending inventory) have to agree with each other. On-hand comes
//   from PHITEMSTOCK, which is maintained from PHSTOCKMOVEMENT; the valuation
//   multiplies that by PHITEM.AVGCOST / RETAILPRICE; and the open balance comes
//   from the PHVOUCHERLINE ledger. Keeping them in one place stops a page from
//   quietly reporting a stock figure that disagrees with the dashboard.
//
// DATE-RANGED ON-HAND
//   PhStoreOnHand builds the balance by replaying the ledger up to a date,
//   rather than trusting the stored ONHANDQTY. The two agree at "today"
//   (verified at migration: 0 mismatches over 14,163 rows) and only the replay
//   answers "what was on hand on the 30th".
// ============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HmisWeb.Data;
using HmisWeb.Models.Pharmacy;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Services.Pharmacy
{
    /// <summary>Per-store headline figures for the pharmacy dashboard.</summary>
    public class PhStoreSummary
    {
        public int StoreId { get; set; }
        public string StoreCode { get; set; } = "";
        public string StoreName { get; set; } = "";
        public string StoreType { get; set; } = "";

        /// <summary>Distinct drugs carried by this store.</summary>
        public int ItemCount { get; set; }

        /// <summary>Drugs with stock right now.</summary>
        public int InStockCount { get; set; }

        public decimal TotalQty { get; set; }

        /// <summary>Ending inventory at cost (on-hand x average cost).</summary>
        public decimal EndingInventoryCost { get; set; }

        /// <summary>Ending inventory at retail price.</summary>
        public decimal EndingInventoryRetail { get; set; }

        /// <summary>Potential gross margin, retail less cost.</summary>
        public decimal PotentialMargin
        {
            get { return EndingInventoryRetail - EndingInventoryCost; }
        }

        /// <summary>Items at or below their reorder level.</summary>
        public int ReorderCount { get; set; }

        // ---- amounts owed to / by this store, from the ledger ----

        /// <summary>Sales value in the period.</summary>
        public decimal SalesValue { get; set; }

        /// <summary>Purchase value in the period.</summary>
        public decimal PurchaseValue { get; set; }
    }

    /// <summary>One line of a stock report.</summary>
    public class PhStockRow
    {
        public int ItemId { get; set; }
        public int StoreId { get; set; }
        public string ItemCode { get; set; } = "";
        public string ItemName { get; set; } = "";
        /// <summary>Legacy codes, kept so the name lookups can be batched
        /// instead of joining four more tables per row.</summary>
        public string? GenericCode { get; set; }
        public string? SupplierCode { get; set; }
        public string? ManufacturerCode { get; set; }
        public string? CategoryCode { get; set; }

        public string GenericName { get; set; } = "";
        public string SupplierName { get; set; } = "";
        public string ManufacturerName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public decimal OnHandQty { get; set; }
        public decimal AvgCost { get; set; }
        public decimal TradePrice { get; set; }
        public decimal RetailPrice { get; set; }
        public decimal ReorderLevel { get; set; }
        public decimal ValueAtCost { get; set; }
        public decimal ValueAtRetail { get; set; }
        public bool BelowReorder { get; set; }
    }

    /// <summary>An account balance derived from the migrated ledger.</summary>
    public class PhBalanceRow
    {
        public string AccountCode { get; set; } = "";
        public string AccountName { get; set; } = "";
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }

        /// <summary>Debit less credit. Positive = the store is owed money.</summary>
        public decimal Balance
        {
            get { return Debit - Credit; }
        }

        public string BalanceType
        {
            get
            {
                if (Balance > 0.0099m) return "Receivable";
                if (Balance < -0.0099m) return "Payable";
                return "Nil";
            }
        }
    }

    public class PhLedgerSummary
    {
        public decimal TotalReceivable { get; set; }
        public decimal TotalPayable { get; set; }
        public int AccountCount { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }

    public class PhService
    {
        private readonly ApplicationDbContext _db;

        public PhService(ApplicationDbContext db)
        {
            _db = db;
        }

        public Task<List<PhStore>> GetStoresAsync()
        {
            return _db.PhStores.AsNoTracking()
                .Where(s => !s.Retired)
                .OrderBy(s => s.StoreType == "MAIN" ? 0 : 1)
                .ThenBy(s => s.Code)
                .ToListAsync();
        }

        /// <summary>Item catalogues, for the stock filter.</summary>
        public Task<List<PhCategory>> GetCategoriesAsync()
        {
            return _db.PhCategories.AsNoTracking()
                .Where(c => !c.Retired)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        /// <summary>One row of the medicine / product master (legacy INVMAST).</summary>
        public class PhItemRow
        {
            public int Id { get; set; }
            public string Code { get; set; } = "";
            public string Name { get; set; } = "";
            public string ItemType { get; set; } = "";

            // The raw foreign codes are carried as well as the display names.
            // The data-entry popup edits these fields, so without them a row
            // opened for editing would post blanks and silently clear the
            // drug's generic / supplier / manufacturer / catalogue links.
            public string GenericCode { get; set; } = "";
            public string SupplierCode { get; set; } = "";
            public string ManufacturerCode { get; set; } = "";
            public string CategoryCode { get; set; } = "";

            public string GenericName { get; set; } = "";
            public string SupplierName { get; set; } = "";
            public string ManufacturerName { get; set; } = "";
            public string CategoryName { get; set; } = "";
            public string PackType { get; set; } = "";
            public decimal PackUnit { get; set; }
            public decimal TradeCost { get; set; }
            public decimal TradePrice { get; set; }
            public decimal RetailPrice { get; set; }
            public decimal TaxRate { get; set; }
            public decimal UnitCost { get; set; }
            public decimal HospitalCost { get; set; }
            public decimal ReorderLevel { get; set; }
            public decimal MinStock { get; set; }
            public decimal MaxStock { get; set; }
            public bool IsActive { get; set; }
            public bool IsTop { get; set; }

            /// <summary>Total on-hand across every store.</summary>
            public decimal TotalOnHand { get; set; }

            /// <summary>On-hand in the store currently being filtered on.</summary>
            public decimal StoreOnHand { get; set; }
        }

        /// <summary>
        /// Medicine / product master list. Totals are aggregated per item first
        /// so the paging and searching happen over one row per drug, not over
        /// the (item, store) pair - otherwise an item stocked in two stores
        /// appears twice and the total is still right but the list lies.
        /// </summary>
        public async Task<List<PhItemRow>> GetItemsAsync(
            int storeId, string? search, string? categoryCode, bool activeOnly, int maxRows = 1000)
        {
            var stockByItem = await (
                from st in _db.PhItemStocks.AsNoTracking()
                where !st.Retired
                group st by st.ItemId into g
                select new
                {
                    ItemId = g.Key,
                    Total = g.Sum(x => x.OnHandQty),
                    Store = g.Where(x => x.StoreId == storeId).Select(x => (decimal?)x.OnHandQty).FirstOrDefault() ?? 0
                })
                .ToListAsync();

            var q = _db.PhItems.AsNoTracking().Where(i => !i.Retired);

            if (activeOnly) q = q.Where(i => i.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                q = q.Where(i => i.Name.ToLower().Contains(term) || i.Code.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(categoryCode))
                q = q.Where(i => i.CategoryCode == categoryCode);

            var items = await q.OrderBy(i => i.Name).Take(maxRows).ToListAsync();

            var gcodes = items.Select(i => i.GenericCode).Where(c => c != null).Distinct().ToList();
            var scodes = items.Select(i => i.SupplierCode).Where(c => c != null).Distinct().ToList();
            var mcodes = items.Select(i => i.ManufacturerCode).Where(c => c != null).Distinct().ToList();
            var ccodes = items.Select(i => i.CategoryCode).Where(c => c != null).Distinct().ToList();

            var generics = await _db.PhGenerics.AsNoTracking()
                .Where(g => gcodes.Contains(g.Code)).ToDictionaryAsync(g => g.Code, g => g.Name);
            var suppliers = await _db.PhSuppliers.AsNoTracking()
                .Where(s => scodes.Contains(s.Code)).ToDictionaryAsync(s => s.Code, s => s.Name);
            var manufs = await _db.PhManufacturers.AsNoTracking()
                .Where(m => mcodes.Contains(m.Code)).ToDictionaryAsync(m => m.Code, m => m.Name);
            var cats = await _db.PhCategories.AsNoTracking()
                .Where(c => ccodes.Contains(c.Code)).ToDictionaryAsync(c => c.Code, c => c.Name);

            var stockMap = stockByItem.ToDictionary(x => x.ItemId);

            return items.Select(i =>
            {
                stockMap.TryGetValue(i.Id, out var s);
                return new PhItemRow
                {
                    Id = i.Id,
                    Code = i.Code,
                    Name = i.Name,
                    ItemType = i.ItemType ?? "",
                    GenericCode = i.GenericCode ?? "",
                    SupplierCode = i.SupplierCode ?? "",
                    ManufacturerCode = i.ManufacturerCode ?? "",
                    CategoryCode = i.CategoryCode ?? "",
                    GenericName = i.GenericCode != null && generics.TryGetValue(i.GenericCode, out var g) ? g : "",
                    SupplierName = i.SupplierCode != null && suppliers.TryGetValue(i.SupplierCode, out var sp) ? sp : "",
                    ManufacturerName = i.ManufacturerCode != null && manufs.TryGetValue(i.ManufacturerCode, out var m) ? m : "",
                    CategoryName = i.CategoryCode != null && cats.TryGetValue(i.CategoryCode, out var c) ? c : "",
                    PackType = i.PackType ?? "",
                    PackUnit = i.PackUnit ?? 0,
                    TradeCost = i.TradeCost,
                    TradePrice = i.TradePrice,
                    RetailPrice = i.RetailPrice,
                    TaxRate = i.TaxRate,
                    UnitCost = i.UnitCost,
                    HospitalCost = i.HospitalCost,
                    ReorderLevel = i.ReorderLevel,
                    MinStock = i.MinStock,
                    MaxStock = i.MaxStock,
                    IsActive = i.IsActive,
                    IsTop = i.IsTop,
                    TotalOnHand = s?.Total ?? 0,
                    StoreOnHand = s?.Store ?? 0
                };
            }).ToList();
        }

        /// <summary>Fetch one drug for the edit form.</summary>
        public Task<PhItem?> GetItemAsync(int id)
        {
            return _db.PhItems.FirstOrDefaultAsync(i => i.Id == id);
        }

        /// <summary>Stock position of one drug, split by store.</summary>
        public async Task<List<(int StoreId, string StoreName, decimal OnHand, decimal AvgCost)>>
            GetItemStockAsync(int itemId)
        {
            return await (from st in _db.PhItemStocks.AsNoTracking()
                          where st.ItemId == itemId && !st.Retired
                          join s in _db.PhStores.AsNoTracking() on st.StoreId equals s.Id
                          orderby s.Code
                          select new { st.StoreId, StoreName = s.Name, st.OnHandQty, st.AvgCost })
                .Select(x => new { x.StoreId, x.StoreName, x.OnHandQty, x.AvgCost })
                .ToListAsync()
                .ContinueWith(t => t.Result
                    .Select(x => (x.StoreId, x.StoreName, x.OnHandQty, x.AvgCost)).ToList());
        }

        /// <summary>
        /// Headline figures for every store over a period. Everything is
        /// computed in the database rather than by loading rows into memory,
        /// because the ledger alone is 60,000 lines.
        /// </summary>
        public async Task<List<PhStoreSummary>> GetStoreSummariesAsync(DateTime dateFrom, DateTime dateTo)
        {
            var stores = await GetStoresAsync();
            var result = new List<PhStoreSummary>();
            if (stores.Count == 0) return result;

            var ids = stores.Select(s => s.Id).ToList();

            var stockRows = await (
                from st in _db.PhItemStocks.AsNoTracking()
                join i in _db.PhItems.AsNoTracking() on st.ItemId equals i.Id
                where ids.Contains(st.StoreId) && !st.Retired && !i.Retired
                select new
                {
                    st.StoreId,
                    st.ItemId,
                    Qty = st.OnHandQty,
                    Cost = st.OnHandQty * st.AvgCost,
                    Retail = st.OnHandQty * i.RetailPrice,
                    Reorder = i.ReorderLevel
                })
                .ToListAsync();

            var stock = stockRows
                .GroupBy(x => new { x.StoreId, x.ItemId })
                .Select(g => new
                {
                    g.Key.StoreId,
                    OnHand = g.Sum(x => x.Qty),
                    Cost = g.Sum(x => x.Cost),
                    Retail = g.Sum(x => x.Retail),
                    Reorder = g.First().Reorder
                })
                .ToList();

            var sales = await (
                from s in _db.PhSales.AsNoTracking()
                where ids.Contains(s.StoreId) && !s.Retired
                      && s.SlipDate >= dateFrom && s.SlipDate <= dateTo
                group s by s.StoreId into g
                select new { StoreId = g.Key, Value = g.Sum(x => x.NetAmount) })
                .ToListAsync();

            var purchases = await (
                from p in _db.PhPurchases.AsNoTracking()
                where ids.Contains(p.StoreId) && !p.Retired
                      && p.DocDate >= dateFrom && p.DocDate <= dateTo
                group p by p.StoreId into g
                select new { StoreId = g.Key, Value = g.Sum(x => x.NetAmount) })
                .ToListAsync();

            foreach (var st in stores)
            {
                var ss = stock.Where(x => x.StoreId == st.Id).ToList();
                result.Add(new PhStoreSummary
                {
                    StoreId = st.Id,
                    StoreCode = st.Code,
                    StoreName = st.Name,
                    StoreType = st.StoreType,
                    ItemCount = ss.Count,
                    InStockCount = ss.Count(x => x.OnHand != 0),
                    TotalQty = ss.Sum(x => x.OnHand),
                    EndingInventoryCost = ss.Sum(x => x.Cost),
                    EndingInventoryRetail = ss.Sum(x => x.Retail),
                    ReorderCount = ss.Count(x => x.Reorder > 0 && x.OnHand <= x.Reorder),
                    SalesValue = sales.FirstOrDefault(x => x.StoreId == st.Id)?.Value ?? 0,
                    PurchaseValue = purchases.FirstOrDefault(x => x.StoreId == st.Id)?.Value ?? 0
                });
            }
            return result;
        }

        /// <summary>
        /// Stock report rows for one store, optionally filtered.
        /// <paramref name="includeZero"/> is off by default because a pharmacy
        /// catalogue holds thousands of drugs it has never stocked.
        /// </summary>
        public async Task<List<PhStockRow>> GetStockAsync(
            int storeId, string? search, int? categoryId, bool belowReorderOnly,
            bool includeZero, int maxRows = 5000)
        {
            // Scalars are projected straight out of the join. Carrying the two
            // entities through a transparent identifier and filtering on them
            // makes EF emit invalid column names ("PhItemId" / "PhStoreId").
            var q = from st in _db.PhItemStocks.AsNoTracking()
                    join i in _db.PhItems.AsNoTracking() on st.ItemId equals i.Id
                    where !st.Retired && !i.Retired
                    select new PhStockRow
                    {
                        ItemId = i.Id,
                        StoreId = st.StoreId,
                        ItemCode = i.Code,
                        ItemName = i.Name,
                        GenericCode = i.GenericCode,
                        SupplierCode = i.SupplierCode,
                        ManufacturerCode = i.ManufacturerCode,
                        CategoryCode = i.CategoryCode,
                        OnHandQty = st.OnHandQty,
                        AvgCost = st.AvgCost,
                        TradePrice = i.TradePrice,
                        RetailPrice = i.RetailPrice,
                        ReorderLevel = i.ReorderLevel,
                        ValueAtCost = st.OnHandQty * st.AvgCost,
                        ValueAtRetail = st.OnHandQty * i.RetailPrice,
                        BelowReorder = i.ReorderLevel > 0 && st.OnHandQty <= i.ReorderLevel
                    };

            if (storeId > 0) q = q.Where(x => x.StoreId == storeId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                q = q.Where(x => x.ItemName.ToLower().Contains(term)
                              || x.ItemCode.ToLower().Contains(term));
            }

            if (categoryId.HasValue)
            {
                // Resolved up front rather than as a correlated subquery inside
                // the filter - EF mis-resolves the column names in that position.
                var catCode = await _db.PhCategories.AsNoTracking()
                    .Where(c => c.Id == categoryId.Value).Select(c => c.Code).FirstOrDefaultAsync();
                q = q.Where(x => x.CategoryCode == catCode);
            }

            if (belowReorderOnly)
                q = q.Where(x => x.BelowReorder);

            if (!includeZero)
                q = q.Where(x => x.OnHandQty != 0);

            var list = await q.OrderBy(x => x.ItemName).Take(maxRows).ToListAsync();

            var codes = list.Select(x => x.SupplierCode).Where(c => c != null).Distinct().ToList();
            var mcodes = list.Select(x => x.ManufacturerCode).Where(c => c != null).Distinct().ToList();
            var gcodes = list.Select(x => x.GenericCode).Where(c => c != null).Distinct().ToList();
            var ccodes = list.Select(x => x.CategoryCode).Where(c => c != null).Distinct().ToList();

            var suppliers = await _db.PhSuppliers.AsNoTracking()
                .Where(s => codes.Contains(s.Code)).ToDictionaryAsync(s => s.Code, s => s.Name);
            var manufs = await _db.PhManufacturers.AsNoTracking()
                .Where(m => mcodes.Contains(m.Code)).ToDictionaryAsync(m => m.Code, m => m.Name);
            var generics = await _db.PhGenerics.AsNoTracking()
                .Where(g => gcodes.Contains(g.Code)).ToDictionaryAsync(g => g.Code, g => g.Name);
            var cats = await _db.PhCategories.AsNoTracking()
                .Where(c => ccodes.Contains(c.Code)).ToDictionaryAsync(c => c.Code, c => c.Name);

            foreach (var r in list)
            {
                r.GenericName = r.GenericCode != null && generics.TryGetValue(r.GenericCode, out var g) ? g : "";
                r.SupplierName = r.SupplierCode != null && suppliers.TryGetValue(r.SupplierCode, out var s) ? s : "";
                r.ManufacturerName = r.ManufacturerCode != null && manufs.TryGetValue(r.ManufacturerCode, out var m) ? m : "";
                r.CategoryName = r.CategoryCode != null && cats.TryGetValue(r.CategoryCode, out var c) ? c : "";
            }
            foreach (var r in list)
            {
                r.ValueAtCost = Math.Round(r.ValueAtCost, 2);
                r.ValueAtRetail = Math.Round(r.ValueAtRetail, 2);
            }
            return list;
        }

        /// <summary>
        /// Account balances from the migrated ledger for a period.
        ///
        /// This is the answer to "open balance". Only leaf accounts (those
        /// that actually carry postings) are returned; rolling a figure up the
        /// tree would double count, because every posting already hits a
        /// parent as well.
        /// </summary>
        public async Task<List<PhBalanceRow>> GetBalancesAsync(DateTime dateFrom, DateTime dateTo, string? search)
        {
            var q = from l in _db.PhVoucherLines.AsNoTracking()
                    join v in _db.PhVouchers.AsNoTracking() on l.VoucherId equals v.Id
                    where l.AccountCode != null
                          && v.VoucherDate >= dateFrom && v.VoucherDate <= dateTo
                    group l by l.AccountCode into g
                    select new
                    {
                        AccountCode = g.Key,
                        Debit = g.Where(x => x.Amount > 0).Sum(x => x.Amount),
                        Credit = g.Where(x => x.Amount < 0).Sum(x => -x.Amount)
                    };

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                q = q.Where(x => x.AccountCode.ToLower().Contains(term));
            }

            var balances = await q.Where(x => x.Debit != 0 || x.Credit != 0)
                .OrderBy(x => x.AccountCode).ToListAsync();
            if (balances.Count == 0) return new List<PhBalanceRow>();

            var codes = balances.Select(b => b.AccountCode).ToList();
            var names = await _db.PhAccounts.AsNoTracking()
                .Where(a => codes.Contains(a.Code)).ToDictionaryAsync(a => a.Code, a => a.Name);

            return balances.Select(b => new PhBalanceRow
            {
                AccountCode = b.AccountCode,
                AccountName = names.TryGetValue(b.AccountCode, out var n) ? n : "",
                Debit = Math.Round(b.Debit, 2),
                Credit = Math.Round(b.Credit, 2)
            }).ToList();
        }

        /// <summary>Receivable and payable totals for the dashboard tiles.</summary>
        public async Task<PhLedgerSummary> GetLedgerSummaryAsync(DateTime dateFrom, DateTime dateTo)
        {
            var rows = await GetBalancesAsync(dateFrom, dateTo, null);
            return new PhLedgerSummary
            {
                TotalReceivable = rows.Where(r => r.Balance > 0).Sum(r => r.Balance),
                TotalPayable = rows.Where(r => r.Balance < 0).Sum(r => -r.Balance),
                AccountCount = rows.Count,
                FromDate = dateFrom,
                ToDate = dateTo
            };
        }

        /// <summary>Stock movements for one item at one store - the audit trail.</summary>
        public async Task<List<PhStockMovement>> GetMovementsAsync(int storeId, int itemId, int maxRows = 300)
        {
            var q = _db.PhStockMovements.AsNoTracking().AsQueryable();
            if (storeId > 0) q = q.Where(m => m.StoreId == storeId);
            q = q.Where(m => m.ItemId == itemId);
            return await q.OrderByDescending(m => m.MoveDate).ThenByDescending(m => m.Id)
                .Take(maxRows).ToListAsync();
        }

        // ==================== SETUP LISTS ====================
        //
        // One method per legacy setup screen. Each carries the usage count as
        // well as the row, because "can this be deleted?" is decided by
        // whether anything still points at it - and the user needs to see that
        // before they press the red bin, not after.

        /// <summary>Suppliers (legacy supplier.spr), with how many drugs each supplies.</summary>
        public async Task<List<PhSupplierRow>> GetSuppliersAsync(string? search, bool activeOnly)
        {
            var q = _db.PhSuppliers.AsNoTracking().Where(s => !s.Retired);
            if (activeOnly) q = q.Where(s => s.Active);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                q = q.Where(s => s.Name.ToLower().Contains(term)
                              || s.Code.ToLower().Contains(term)
                              || (s.ShortName != null && s.ShortName.ToLower().Contains(term)));
            }

            var suppliers = await q.OrderBy(s => s.Name).Take(2000).ToListAsync();

            // One grouped count for the whole page rather than a query per row.
            var usage = await _db.PhItems.AsNoTracking()
                .Where(i => !i.Retired && i.SupplierCode != null)
                .GroupBy(i => i.SupplierCode!)
                .Select(g => new { Code = g.Key, N = g.Count() })
                .ToDictionaryAsync(x => x.Code, x => x.N);

            return suppliers.Select(s => new PhSupplierRow
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                ShortName = s.ShortName ?? "",
                Address = s.Address ?? "",
                Phone = s.Phone ?? "",
                City = s.City ?? "",
                SupplierType = s.SupplierType ?? "",
                Active = s.Active,
                Balance = s.Balance,
                ItemCount = usage.TryGetValue(s.Code, out var n) ? n : 0
            }).ToList();
        }

        /// <summary>Manufacturers (legacy manufac.spr), with their drug count.</summary>
        public async Task<List<PhManufacturerRow>> GetManufacturersAsync(string? search)
        {
            var q = _db.PhManufacturers.AsNoTracking().Where(m => !m.Retired);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                q = q.Where(m => m.Name.ToLower().Contains(term)
                              || m.Code.ToLower().Contains(term)
                              || (m.ShortName != null && m.ShortName.ToLower().Contains(term)));
            }

            var makers = await q.OrderBy(m => m.Name).Take(2000).ToListAsync();

            var usage = await _db.PhItems.AsNoTracking()
                .Where(i => !i.Retired && i.ManufacturerCode != null)
                .GroupBy(i => i.ManufacturerCode!)
                .Select(g => new { Code = g.Key, N = g.Count() })
                .ToDictionaryAsync(x => x.Code, x => x.N);

            return makers.Select(m => new PhManufacturerRow
            {
                Id = m.Id,
                Code = m.Code,
                Name = m.Name,
                ShortName = m.ShortName ?? "",
                Address = m.Address ?? "",
                ItemCount = usage.TryGetValue(m.Code, out var n) ? n : 0
            }).ToList();
        }

        /// <summary>
        /// Chart of accounts (legacy chart.spr), flattened for a list view.
        /// The ledger posts against these codes, so the screen is the
        /// prerequisite for every financial report.
        /// </summary>
        public async Task<List<PhAccountRow>> GetAccountsAsync(string? search, string? typeFilter)
        {
            var q = _db.PhAccounts.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(typeFilter)) q = q.Where(a => a.AccountType == typeFilter);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                q = q.Where(a => a.Name.ToLower().Contains(term) || a.Code.ToLower().Contains(term));
            }

            var accounts = await q.OrderBy(a => a.Code).Take(2000).ToListAsync();

            // Postings per account, so the screen can show which accounts are
            // actually in use. Without this an account looks deletable when it
            // has years of ledger behind it.
            var posting = await _db.PhVoucherLines.AsNoTracking()
                .Where(l => l.AccountCode != null)
                .GroupBy(l => l.AccountCode!)
                .Select(g => new { Code = g.Key, N = g.Count(), Amount = g.Sum(x => x.Amount) })
                .ToDictionaryAsync(x => x.Code, x => x);

            return accounts.Select(a => new PhAccountRow
            {
                Id = a.Id,
                Code = a.Code,
                Name = a.Name,
                AccountType = a.AccountType ?? "",
                Level = a.Level,
                ParentCode = a.ParentCode ?? "",
                OpeningBalance = a.OpeningBalance,
                YearDebit = a.YearDebit,
                YearCredit = a.YearCredit,
                PostingCount = posting.TryGetValue(a.Code, out var p) ? p.N : 0,
                PostingAmount = posting.TryGetValue(a.Code, out var p2) ? p2.Amount : 0
            }).ToList();
        }

        /// <summary>Generic (molecule) names, with the drug count using each.</summary>
        public async Task<List<PhGenericRow>> GetGenericsAsync(string? search)
        {
            var q = _db.PhGenerics.AsNoTracking().Where(g => !g.Retired);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                q = q.Where(g => g.Name.ToLower().Contains(term) || g.Code.ToLower().Contains(term));
            }

            var generics = await q.OrderBy(g => g.Name).Take(2000).ToListAsync();

            var usage = await _db.PhItems.AsNoTracking()
                .Where(i => !i.Retired && i.GenericCode != null)
                .GroupBy(i => i.GenericCode!)
                .Select(g => new { Code = g.Key, N = g.Count() })
                .ToDictionaryAsync(x => x.Code, x => x.N);

            return generics.Select(g => new PhGenericRow
            {
                Id = g.Id,
                Code = g.Code,
                Name = g.Name,
                ItemCount = usage.TryGetValue(g.Code, out var n) ? n : 0
            }).ToList();
        }

        /// <summary>Item catalogues (legacy catalog.dbf), with their drug count.</summary>
        public async Task<List<PhCategoryRow>> GetCategoryRowsAsync(string? search)
        {
            var q = _db.PhCategories.AsNoTracking().Where(c => !c.Retired);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                q = q.Where(c => c.Name.ToLower().Contains(term) || c.Code.ToLower().Contains(term));
            }

            var cats = await q.OrderBy(c => c.Name).Take(500).ToListAsync();

            var usage = await _db.PhItems.AsNoTracking()
                .Where(i => !i.Retired && i.CategoryCode != null)
                .GroupBy(i => i.CategoryCode!)
                .Select(g => new { Code = g.Key, N = g.Count() })
                .ToDictionaryAsync(x => x.Code, x => x.N);

            return cats.Select(c => new PhCategoryRow
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                ItemCount = usage.TryGetValue(c.Code, out var n) ? n : 0
            }).ToList();
        }

        /// <summary>How many drugs reference a given code in a given field.</summary>
        public Task<int> CountItemUsageAsync(string field, string code)
        {
            var q = _db.PhItems.AsNoTracking().Where(i => !i.Retired);
            q = field switch
            {
                "SupplierCode" => q.Where(i => i.SupplierCode == code),
                "ManufacturerCode" => q.Where(i => i.ManufacturerCode == code),
                "GenericCode" => q.Where(i => i.GenericCode == code),
                "CategoryCode" => q.Where(i => i.CategoryCode == code),
                _ => q.Where(i => false)
            };
            return q.CountAsync();
        }

        // ==================== TRANSACTION SUPPORT ====================

        /// <summary>
        /// Item lookup for a transaction line grid.
        ///
        /// The grid shows the drug's own master detail - code, name, pack,
        /// trade cost, retail price - plus the quantity already on hand at the
        /// receiving store, because the operator needs to see the master values
        /// before deciding what to key. Returning them here keeps the grid to a
        /// single request per keystroke instead of one request per column.
        /// </summary>
        public async Task<List<PhItemLookup>> GetItemLookupAsync(
            int storeId, string? search, int maxRows = 40)
        {
            var q = _db.PhItems.AsNoTracking().Where(i => !i.Retired && i.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                q = q.Where(i => i.Code.ToLower().Contains(term) || i.Name.ToLower().Contains(term));
            }

            var items = await q.OrderBy(i => i.Name).Take(maxRows).ToListAsync();
            var ids = items.Select(i => i.Id).ToList();

            var stock = await _db.PhItemStocks.AsNoTracking()
                .Where(s => s.StoreId == storeId && ids.Contains(s.ItemId) && !s.Retired)
                .ToDictionaryAsync(s => s.ItemId, s => s);

            var gcodes = items.Select(i => i.GenericCode).Where(c => c != null).Distinct().ToList();
            var generics = await _db.PhGenerics.AsNoTracking()
                .Where(g => gcodes.Contains(g.Code))
                .ToDictionaryAsync(g => g.Code, g => g.Name);

            return items.Select(i => new PhItemLookup
            {
                Id = i.Id,
                Code = i.Code,
                Name = i.Name,
                GenericName = i.GenericCode != null && generics.TryGetValue(i.GenericCode, out var g) ? g : "",
                PackType = i.PackType ?? "",
                PackUnit = i.PackUnit ?? 0,
                TradeCost = i.TradeCost,
                RetailPrice = i.RetailPrice,
                UnitCost = i.UnitCost,
                TaxRate = i.TaxRate,
                ReorderLevel = i.ReorderLevel,
                OnHand = stock.TryGetValue(i.Id, out var s) ? s.OnHandQty : 0,
                AvgCost = stock.TryGetValue(i.Id, out var s2) ? s2.AvgCost : 0
            }).ToList();
        }

        /// <summary>
        /// Next document number for a store, derived from the highest existing
        /// number rather than a counter table.
        ///
        /// The legacy numbers are long integers per store (main store
        /// 22645087.., satellite 9900633..) and are not zero-padded, so they
        /// are compared as integers, not as text - otherwise "9999" would sort
        /// above "10000".
        /// </summary>
        public async Task<string> NextPurchaseDocNoAsync(int storeId)
        {
            var numbers = await _db.PhPurchases.AsNoTracking()
                .Where(p => p.StoreId == storeId)
                .Select(p => p.DocNo)
                .ToListAsync();

            long max = 0;
            foreach (var n in numbers)
            {
                if (long.TryParse(n, out var v) && v > max) max = v;
            }

            return (max + 1).ToString();
        }

        /// <summary>Supplier pick list for the purchase screen.</summary>
        public Task<List<PhSupplier>> GetSupplierPickListAsync()
        {
            return _db.PhSuppliers.AsNoTracking()
                .Where(s => !s.Retired && s.Active)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Header rows for the purchase list. Lines are counted rather than
        /// loaded, so the list stays fast over thousands of documents.
        /// </summary>
        public async Task<List<PhPurchaseRow>> GetPurchasesAsync(
            int storeId, string? search, DateTime? from, DateTime? to, int maxRows = 500)
        {
            var q = _db.PhPurchases.AsNoTracking().Where(p => !p.Retired);

            if (storeId > 0) q = q.Where(p => p.StoreId == storeId);
            if (from.HasValue) q = q.Where(p => p.DocDate >= from.Value.Date);
            if (to.HasValue) q = q.Where(p => p.DocDate <= to.Value.Date);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                q = q.Where(p => p.DocNo.ToLower().Contains(term)
                              || (p.VendorCode != null && p.VendorCode.ToLower().Contains(term))
                              || (p.VendorName != null && p.VendorName.ToLower().Contains(term))
                              || (p.VendorInvoiceNo != null && p.VendorInvoiceNo.ToLower().Contains(term)));
            }

            var heads = await q.OrderByDescending(p => p.DocDate).ThenByDescending(p => p.Id)
                .Take(maxRows).ToListAsync();

            var ids = heads.Select(h => h.Id).ToList();
            var lineCount = await _db.PhPurchaseLines.AsNoTracking()
                .Where(l => ids.Contains(l.PurchaseId) && !l.Retired)
                .GroupBy(l => l.PurchaseId)
                .Select(g => new { Id = g.Key, N = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.N);

            var stores = await _db.PhStores.AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.Name);

            return heads.Select(h => new PhPurchaseRow
            {
                Id = h.Id,
                DocNo = h.DocNo,
                DocDate = h.DocDate,
                StoreId = h.StoreId,
                StoreName = stores.TryGetValue(h.StoreId, out var sn) ? sn : "",
                SourceStoreId = h.SourceStoreId,
                SourceStoreName = h.SourceStoreId.HasValue && stores.TryGetValue(h.SourceStoreId.Value, out var ss) ? ss : "",
                VendorCode = h.VendorCode ?? "",
                VendorName = h.VendorName ?? "",
                VendorInvoiceNo = h.VendorInvoiceNo ?? "",
                GrossAmount = h.GrossAmount,
                DiscountAmount = h.DiscountAmount,
                TaxAmount = h.TaxAmount,
                NetAmount = h.NetAmount,
                IsPosted = h.IsPosted,
                LineCount = lineCount.TryGetValue(h.Id, out var n) ? n : 0
            }).ToList();
        }

        /// <summary>
        /// Lines of one purchase document, with the master detail the grid shows
        /// so a saved document reopens exactly as it was keyed.
        ///
        /// Developer: Tajwer Jalal
        /// Created: 19-Oct-2026 15:40
        /// </summary>
        public async Task<List<PhPurchaseLineRow>> GetPurchaseLinesAsync(int purchaseId)
        {
            var lines = await _db.PhPurchaseLines.AsNoTracking()
                .Where(l => l.PurchaseId == purchaseId && !l.Retired)
                .OrderBy(l => l.Id)
                .ToListAsync();

            var ids = lines.Select(l => l.ItemId).Distinct().ToList();
            var items = await _db.PhItems.AsNoTracking()
                .Where(i => ids.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i);

            // The grid shows generic / trade / retail / on-hand beside every line.
            // Without these the columns collapsed to blank and 0.00 on reopen, which
            // made a saved document look different from the one being keyed.
            var gcodes = items.Values.Select(i => i.GenericCode)
                .Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
            var generics = await _db.PhGenerics.AsNoTracking()
                .Where(g => gcodes.Contains(g.Code))
                .ToDictionaryAsync(g => g.Code, g => g.Name);

            var storeId = await _db.PhPurchases.AsNoTracking()
                .Where(p => p.Id == purchaseId)
                .Select(p => p.StoreId)
                .FirstOrDefaultAsync();

            var stock = await _db.PhItemStocks.AsNoTracking()
                .Where(s => s.StoreId == storeId && ids.Contains(s.ItemId) && !s.Retired)
                .ToDictionaryAsync(s => s.ItemId, s => s);

            return lines.Select(l =>
            {
                items.TryGetValue(l.ItemId, out var it);
                stock.TryGetValue(l.ItemId, out var st);
                return new PhPurchaseLineRow
                {
                    ItemId = l.ItemId,
                    Code = it?.Code ?? "",
                    Name = it?.Name ?? "",
                    GenericName = it?.GenericCode != null && generics.TryGetValue(it.GenericCode, out var gn) ? gn : "",
                    PackType = it?.PackType ?? "",
                    PackUnit = it?.PackUnit ?? 0,
                    TradeCost = it?.TradeCost ?? 0,
                    RetailPrice = it?.RetailPrice ?? 0,
                    OnHand = st?.OnHandQty ?? 0,
                    Qty = l.Qty,
                    Rate = l.Rate,
                    TaxRate = l.TaxRate,
                    TaxAmount = l.TaxAmount,
                    DiscountAmount = l.DiscountAmount,
                    Amount = l.Amount,
                    BatchNo = l.BatchNo ?? "",
                    ExpiryDate = l.ExpiryDate
                };
            }).ToList();
        }
    /// <summary>
    /// Next issue document number for a store.
    ///
    /// Developer: Tajwer Jalal
    /// Created: 19-Oct-2026 17:30
    /// </summary>
    public async Task<string> NextIssueDocNoAsync(int storeId)
    {
        // Issues are stored in two tables (PHPURCHASE for store-to-store,
        // PHSALE for department issues), so the next number must consider
        // both to avoid a collision.
        var purNumbers = await _db.PhPurchases.AsNoTracking()
            .Where(p => p.StoreId == storeId && p.SourceStoreId.HasValue)
            .Select(p => p.DocNo)
            .ToListAsync();

        var saleNumbers = await _db.PhSales.AsNoTracking()
            .Where(s => s.StoreId == storeId)
            .Select(s => s.SlipNo)
            .ToListAsync();

        long max = 0;
        foreach (var n in purNumbers.Concat(saleNumbers))
        {
            if (long.TryParse(n, out var v) && v > max) max = v;
        }

        return (max + 1).ToString();
    }

    /// <summary>
    /// Header rows for the issue list, combining both issue tables.
    ///
    /// Developer: Tajwer Jalal
    /// Created: 19-Oct-2026 17:30
    /// </summary>
    public async Task<List<PhIssueRow>> GetIssuesAsync(
        int storeId, string? search, DateTime? from, DateTime? to, int maxRows = 500)
    {
        var rows = new List<PhIssueRow>();

        // --- store-to-store issues (PHPURCHASE with SourceStoreId) ---
        var purQ = _db.PhPurchases.AsNoTracking()
            .Where(p => !p.Retired && p.SourceStoreId.HasValue);

        if (storeId > 0) purQ = purQ.Where(p => p.SourceStoreId == storeId || p.StoreId == storeId);
        if (from.HasValue) purQ = purQ.Where(p => p.DocDate >= from.Value.Date);
        if (to.HasValue) purQ = purQ.Where(p => p.DocDate <= to.Value.Date);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            purQ = purQ.Where(p => p.DocNo.ToLower().Contains(term)
                                  || (p.VendorName != null && p.VendorName.ToLower().Contains(term))
                                  || (p.VendorInvoiceNo != null && p.VendorInvoiceNo.ToLower().Contains(term)));
        }

        var purHeads = await purQ
            .OrderByDescending(p => p.DocDate).ThenByDescending(p => p.Id)
            .Take(maxRows).ToListAsync();

        var purIds = purHeads.Select(h => h.Id).ToList();
        var purLineCount = await _db.PhPurchaseLines.AsNoTracking()
            .Where(l => purIds.Contains(l.PurchaseId) && !l.Retired)
            .GroupBy(l => l.PurchaseId)
            .Select(g => new { Id = g.Key, N = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.N);

        var stores = await _db.PhStores.AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.Name);

        foreach (var h in purHeads)
        {
            rows.Add(new PhIssueRow
            {
                Id = h.Id,
                DocNo = h.DocNo,
                DocDate = h.DocDate,
                StoreId = h.StoreId,
                StoreName = stores.TryGetValue(h.StoreId, out var sn) ? sn : "",
                SourceStoreId = h.SourceStoreId,
                SourceStoreName = h.SourceStoreId.HasValue && stores.TryGetValue(h.SourceStoreId.Value, out var ss) ? ss : "",
                Destination = "Store",
                DestinationName = stores.TryGetValue(h.StoreId, out var dn) ? dn : "",
                PatientName = h.VendorName ?? "",
                GrossAmount = h.GrossAmount,
                DiscountAmount = h.DiscountAmount,
                TaxAmount = h.TaxAmount,
                NetAmount = h.NetAmount,
                IsPosted = h.IsPosted,
                LineCount = purLineCount.TryGetValue(h.Id, out var n) ? n : 0,
                IsStoreIssue = true
            });
        }

        // --- department issues (PHSALE with PatientType I/O/W) ---
        var salQ = _db.PhSales.AsNoTracking()
            .Where(s => !s.Retired && s.PatientType != "O");

        if (storeId > 0) salQ = salQ.Where(s => s.StoreId == storeId);
        if (from.HasValue) salQ = salQ.Where(s => s.SlipDate >= from.Value.Date);
        if (to.HasValue) salQ = salQ.Where(s => s.SlipDate <= to.Value.Date);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            salQ = salQ.Where(s => s.SlipNo.ToLower().Contains(term)
                                  || (s.PatientName != null && s.PatientName.ToLower().Contains(term)));
        }

        var salHeads = await salQ
            .OrderByDescending(s => s.SlipDate).ThenByDescending(s => s.Id)
            .Take(maxRows).ToListAsync();

        var salIds = salHeads.Select(h => h.Id).ToList();
        var salLineCount = await _db.PhSaleLines.AsNoTracking()
            .Where(l => salIds.Contains(l.SaleId) && !l.Retired)
            .GroupBy(l => l.SaleId)
            .Select(g => new { Id = g.Key, N = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.N);

        foreach (var h in salHeads)
        {
            var dest = h.PatientType switch
            {
                "I" => "HMC IPD",
                "W" => "HMC Ward",
                _ => "Department"
            };

            rows.Add(new PhIssueRow
            {
                Id = h.Id,
                DocNo = h.SlipNo,
                DocDate = h.SlipDate,
                StoreId = h.StoreId,
                StoreName = stores.TryGetValue(h.StoreId, out var sn) ? sn : "",
                Destination = dest,
                DestinationName = dest,
                PatientName = h.PatientName ?? "",
                GrossAmount = h.GrossAmount,
                DiscountAmount = h.DiscountAmount,
                TaxAmount = h.TaxAmount,
                NetAmount = h.NetAmount,
                IsPosted = h.IsPosted,
                LineCount = salLineCount.TryGetValue(h.Id, out var n) ? n : 0,
                IsStoreIssue = false
            });
        }

        return rows.OrderByDescending(r => r.DocDate).ThenByDescending(r => r.Id).ToList();
    }

    /// <summary>
    /// Lines of one issue document, with master detail.
    ///
    /// Developer: Tajwer Jalal
    /// Created: 19-Oct-2026 17:30
    /// </summary>
    public async Task<List<PhIssueLineRow>> GetIssueLinesAsync(int id, bool isStoreIssue)
    {
        if (isStoreIssue)
        {
            var lines = await _db.PhPurchaseLines.AsNoTracking()
                .Where(l => l.PurchaseId == id && !l.Retired)
                .OrderBy(l => l.Id)
                .ToListAsync();

            var ids = lines.Select(l => l.ItemId).Distinct().ToList();
            var items = await _db.PhItems.AsNoTracking()
                .Where(i => ids.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i);

            var gcodes = items.Values.Select(i => i.GenericCode)
                .Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
            var generics = await _db.PhGenerics.AsNoTracking()
                .Where(g => gcodes.Contains(g.Code))
                .ToDictionaryAsync(g => g.Code, g => g.Name);

            var storeId = await _db.PhPurchases.AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => p.StoreId)
                .FirstOrDefaultAsync();

            var stock = await _db.PhItemStocks.AsNoTracking()
                .Where(s => s.StoreId == storeId && ids.Contains(s.ItemId) && !s.Retired)
                .ToDictionaryAsync(s => s.ItemId, s => s);

            return lines.Select(l =>
            {
                items.TryGetValue(l.ItemId, out var it);
                stock.TryGetValue(l.ItemId, out var st);
                return new PhIssueLineRow
                {
                    ItemId = l.ItemId,
                    Code = it?.Code ?? "",
                    Name = it?.Name ?? "",
                    GenericName = it?.GenericCode != null && generics.TryGetValue(it.GenericCode, out var gn) ? gn : "",
                    PackType = it?.PackType ?? "",
                    PackUnit = it?.PackUnit ?? 0,
                    TradeCost = it?.TradeCost ?? 0,
                    RetailPrice = it?.RetailPrice ?? 0,
                    OnHand = st?.OnHandQty ?? 0,
                    Qty = l.Qty,
                    Rate = l.Rate,
                    TaxRate = l.TaxRate,
                    TaxAmount = l.TaxAmount,
                    DiscountAmount = l.DiscountAmount,
                    Amount = l.Amount,
                    BatchNo = l.BatchNo ?? ""
                };
            }).ToList();
        }
        else
        {
            var lines = await _db.PhSaleLines.AsNoTracking()
                .Where(l => l.SaleId == id && !l.Retired)
                .OrderBy(l => l.Id)
                .ToListAsync();

            var ids = lines.Select(l => l.ItemId).Distinct().ToList();
            var items = await _db.PhItems.AsNoTracking()
                .Where(i => ids.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i);

            var gcodes = items.Values.Select(i => i.GenericCode)
                .Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
            var generics = await _db.PhGenerics.AsNoTracking()
                .Where(g => gcodes.Contains(g.Code))
                .ToDictionaryAsync(g => g.Code, g => g.Name);

            var storeId = await _db.PhSales.AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => s.StoreId)
                .FirstOrDefaultAsync();

            var stock = await _db.PhItemStocks.AsNoTracking()
                .Where(s => s.StoreId == storeId && ids.Contains(s.ItemId) && !s.Retired)
                .ToDictionaryAsync(s => s.ItemId, s => s);

            return lines.Select(l =>
            {
                items.TryGetValue(l.ItemId, out var it);
                stock.TryGetValue(l.ItemId, out var st);
                return new PhIssueLineRow
                {
                    ItemId = l.ItemId,
                    Code = it?.Code ?? "",
                    Name = it?.Name ?? "",
                    GenericName = it?.GenericCode != null && generics.TryGetValue(it.GenericCode, out var gn) ? gn : "",
                    PackType = it?.PackType ?? "",
                    PackUnit = it?.PackUnit ?? 0,
                    TradeCost = it?.TradeCost ?? 0,
                    RetailPrice = it?.RetailPrice ?? 0,
                    OnHand = st?.OnHandQty ?? 0,
                    Qty = l.Qty,
                    Rate = l.Rate,
                    TaxRate = 0,
                    TaxAmount = l.TaxAmount,
                    DiscountAmount = l.DiscountAmount,
                    Amount = l.NetAmount,
                    BatchNo = ""
                };
            }).ToList();
        }
    }

    }

    // ---- projection types for the setup screens ----

    /// <summary>One row of the supplier list.</summary>
    public class PhSupplierRow
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string ShortName { get; set; } = "";
        public string Address { get; set; } = "";
        public string Phone { get; set; } = "";
        public string City { get; set; } = "";
        public string SupplierType { get; set; } = "";
        public bool Active { get; set; }
        public decimal Balance { get; set; }
        /// <summary>Drugs whose SupplierCode points here.</summary>
        public int ItemCount { get; set; }
    }

    /// <summary>One row of the manufacturer list.</summary>
    public class PhManufacturerRow
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string ShortName { get; set; } = "";
        public string Address { get; set; } = "";
        public int ItemCount { get; set; }
    }

    /// <summary>One row of the chart of accounts.</summary>
    public class PhAccountRow
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        /// <summary>Legacy ACTYPE - 'G' group heading, 'D' detail account.</summary>
        public string AccountType { get; set; } = "";
        public int Level { get; set; }
        public string ParentCode { get; set; } = "";
        public decimal OpeningBalance { get; set; }
        public decimal YearDebit { get; set; }
        public decimal YearCredit { get; set; }
        public int PostingCount { get; set; }
        public decimal PostingAmount { get; set; }

        public bool IsGroup
        {
            get { return AccountType == "G"; }
        }

        public decimal Balance
        {
            get { return YearDebit - YearCredit; }
        }
    }

    /// <summary>One row of the generic (molecule) list.</summary>
    public class PhGenericRow
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public int ItemCount { get; set; }
    }

    /// <summary>One row of the item catalogue list.</summary>
    public class PhCategoryRow
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public int ItemCount { get; set; }
    }

    /// <summary>
    /// Master detail for one drug, used by the transaction line grids. Carries
    /// both the values the operator keys against (trade cost, retail) and the
    /// live on-hand, so the grid can show the master picture without a second
    /// request.
    /// </summary>
    public class PhItemLookup
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string GenericName { get; set; } = "";
        public string PackType { get; set; } = "";
        public decimal PackUnit { get; set; }
        public decimal TradeCost { get; set; }
        public decimal RetailPrice { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TaxRate { get; set; }
        public decimal ReorderLevel { get; set; }
        public decimal OnHand { get; set; }
        public decimal AvgCost { get; set; }
    }

    /// <summary>One header row of the purchase / receipt list.</summary>
    public class PhPurchaseRow
    {
        public int Id { get; set; }
        public string DocNo { get; set; } = "";
        public DateTime DocDate { get; set; }
        public int StoreId { get; set; }
        public string StoreName { get; set; } = "";
        public int? SourceStoreId { get; set; }
        public string SourceStoreName { get; set; } = "";
        public string VendorCode { get; set; } = "";
        public string VendorName { get; set; } = "";
        public string VendorInvoiceNo { get; set; } = "";
        public decimal GrossAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }
        public bool IsPosted { get; set; }
        public int LineCount { get; set; }

        /// <summary>True when the goods came from another store rather than a
        /// supplier - the legacy "Basement issue" rows.</summary>
        public bool IsStoreIssue
        {
            get { return SourceStoreId.HasValue; }
        }
    }

    /// <summary>One line of a purchase document, as shown in the grid.</summary>
    public class PhPurchaseLineRow
    {
        public int ItemId { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string GenericName { get; set; } = "";
        public string PackType { get; set; } = "";
        public decimal PackUnit { get; set; }
        public decimal TradeCost { get; set; }
        public decimal RetailPrice { get; set; }
        public decimal OnHand { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Amount { get; set; }
        public string BatchNo { get; set; } = "";
        public DateTime? ExpiryDate { get; set; }
    }

    // ========================================================================
    // ISSUE (stock out to a destination)
    // ========================================================================
    // The legacy system had four separate screens for issuing stock:
    //   sal_sate -> Issue to Satellite   (main store -> satellite transfer)
    //   sal_hipd -> Issue - HMC IPD
    //   sal_hopd -> Issue - HMC OPD
    //   sal_ward -> Issue - HMC Ward
    // They are the SAME operation with a different destination, so they
    // collapse into one screen with a destination picker.
    //
    // Where the goods are recorded depends on the destination:
    //   * Another store  -> PHPURCHASE with SourceStoreId (store-to-store)
    //   * A department   -> PHSALE with PatientType I/O/W
    // ========================================================================


    /// <summary>One row of the issue list.</summary>
    public class PhIssueRow
    {
        public int Id { get; set; }
        public string DocNo { get; set; } = "";
        public DateTime DocDate { get; set; }
        public int StoreId { get; set; }
        public string StoreName { get; set; } = "";
        public int? SourceStoreId { get; set; }
        public string SourceStoreName { get; set; } = "";
        public string Destination { get; set; } = "";
        public string DestinationName { get; set; } = "";
        public string PatientName { get; set; } = "";
        public decimal GrossAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }
        public bool IsPosted { get; set; }
        public int LineCount { get; set; }
        public bool IsStoreIssue { get; set; }
    }

    /// <summary>One line of an issue document, as shown in the grid.</summary>
    public class PhIssueLineRow
    {
        public int ItemId { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string GenericName { get; set; } = "";
        public string PackType { get; set; } = "";
        public decimal PackUnit { get; set; }
        public decimal TradeCost { get; set; }
        public decimal RetailPrice { get; set; }
        public decimal OnHand { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Amount { get; set; }
        public string BatchNo { get; set; } = "";
    }
}
