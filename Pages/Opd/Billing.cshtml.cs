using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Models.Legacy;
using HmisWeb.Services;
using HmisWeb.Services.Billing;

namespace HmisWeb.Pages.Opd
{
    /// <summary>
    /// OUTDOOR PATIENT (OPD BILL) form - /opd/Billing.
    /// Mirrors the IPD Billing screen (/inward/Billing) for outpatient files.
    /// Replaces the legacy VFP form BS0101 (frmopdtran). Batch generation stays
    /// on /billing/GenerateOpdBill (legacy) and /opd/BillingBatch (new schema).
    ///
    /// Tabs
    ///   Update : a single outpatient file - header, department summary grid and
    ///            the department detail grid (editable while the file is not billed).
    ///   List   : searchable list of outpatient files with a status filter.
    ///
    /// Workflow and roles (privileges checked in code, buttons disabled in the UI)
    ///   Open -> Generated -> Reversed (Officer) / Settled (Cashier, display only)
    ///   Executive : OPD_BILL_GENERATE, OPD_BILL_REVERSE
    ///   Cashier   : OPD_BILL_SETTLE (view only)
    ///
    /// Developer: Tajwer Jalal | Date: 20-Oct-2026 | Time: 16:00
    /// </summary>
    public class BillingModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ICommonService _common;
        private readonly IAppSession _appSession;
        private readonly OpdBillQueryService _query;
        private readonly OpdBillLineService _lines;
        private readonly OpdBillWorkflowService _workflow;

        public BillingModel(ApplicationDbContext db, ICommonService common, IAppSession appSession,
            OpdBillQueryService query, OpdBillLineService lines, OpdBillWorkflowService workflow)
        {
            _db = db;
            _common = common;
            _appSession = appSession;
            _query = query;
            _lines = lines;
            _workflow = workflow;
        }

        // --- Route / Query ---
        [BindProperty(SupportsGet = true)]
        public string Tab { get; set; } = "update";

        [BindProperty(SupportsGet = true)]
        public string RegNo { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Dept { get; set; }

        // --- Update tab header ---
        public RegisterLegacy Header { get; set; }
        public string StatusText { get; set; }
        public decimal UsedAmount { get; set; }
        public decimal Receipts { get; set; }
        public decimal Payments { get; set; }
        public decimal Discount { get; set; }
        public decimal BilledAmount { get; set; }
        public decimal AmountDue { get; set; }
        public bool IndentDetailMissing { get; set; }
        public string PatientName { get; set; }
        public long? PatientId { get; set; }

        // --- Update tab grids ---
        public List<OpdBillQueryService.DeptTotal> DeptTotals { get; set; } = new();
        public List<OpdBillQueryService.BillLine> Lines { get; set; } = new();
        public List<OpdBillHeader> BillHeaders { get; set; } = new();
        public List<OpdBillAudit> Audit { get; set; } = new();
        public List<SelectListItem> DeptList { get; set; } = new();

        // --- Inline entry form ---
        [BindProperty]
        public string SlipNo { get; set; }

        [BindProperty]
        public DateTime? SlipDate { get; set; }

        [BindProperty]
        public string ItemCode { get; set; }

        [BindProperty]
        public string Description { get; set; }

        [BindProperty]
        public decimal Qty { get; set; }

        [BindProperty]
        public decimal Rate { get; set; }

        [BindProperty]
        public string Remarks { get; set; }

        [BindProperty]
        public long LineId { get; set; }

        // --- Generate / Reverse modals ---
        [BindProperty]
        public DateTime BillDate { get; set; } = DateTime.Today;

        [BindProperty]
        public DateTime? PeriodStart { get; set; }

        [BindProperty]
        public DateTime? PeriodEnd { get; set; }

        [BindProperty]
        public string Reason { get; set; }

        [BindProperty]
        public long ReverseBillNo { get; set; }

        // --- List tab ---
        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        public OpdBillQueryService.SearchResult SearchResult { get; set; }

        // --- Messages ---
        [TempData]
        public string Success { get; set; }

        [TempData]
        public string Message { get; set; }

        // --- Permissions ---
        public bool CanAddIndent => _appSession.HasPrivilege("OPD_BILL_EDIT") || _appSession.HasPrivilege("ALL");
        public bool CanGenerate => _appSession.HasPrivilege("OPD_BILL_GENERATE") || _appSession.HasPrivilege("ALL");
        public bool CanReverse => _appSession.HasPrivilege("OPD_BILL_REVERSE") || _appSession.HasPrivilege("ALL");

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadDepartmentsAsync();

            if (Tab == "list")
            {
                await LoadListAsync();
                return Page();
            }

            if (!string.IsNullOrWhiteSpace(RegNo))
            {
                await LoadFileAsync();
            }
            else
            {
                Message = "Enter a registration number to load a file.";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAddLineAsync()
        {
            await LoadDepartmentsAsync();
            if (!CanAddIndent) { Message = "Insufficient privilege."; await LoadFileAsync(); return Page(); }

            try
            {
                await _lines.AddLineAsync(RegNo, Dept, SlipNo, SlipDate ?? DateTime.Today,
                    ItemCode, Description, Qty, Rate, Remarks,
                    _appSession.WebUserId?.ToString() ?? "system", _appSession.WebUserId);
                Success = "Indent line added.";
                // Reset so the next Add re-generates the next free slip number
                // (the global per-department MAX + 1) instead of reusing the posted one.
                SlipNo = null;
            }
            catch (InvalidOperationException ex) { Message = ex.Message; }

            await LoadFileAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostEditLineAsync()
        {
            await LoadDepartmentsAsync();
            if (!CanAddIndent) { Message = "Insufficient privilege."; await LoadFileAsync(); return Page(); }

            try
            {
                await _lines.EditLineAsync(LineId, Qty, Rate, Description, Remarks, RegNo,
                    _appSession.WebUserId?.ToString() ?? "system", _appSession.WebUserId);
                Success = "Indent line updated.";
                // Clear so the entry form returns to Add mode with a fresh slip number.
                SlipNo = null;
                LineId = 0;
            }
            catch (InvalidOperationException ex) { Message = ex.Message; }

            await LoadFileAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostRetireLineAsync(long retireLineId)
        {
            await LoadDepartmentsAsync();
            if (!CanAddIndent) { Message = "Insufficient privilege."; await LoadFileAsync(); return Page(); }

            try
            {
                await _lines.RetireLineAsync(retireLineId, RegNo,
                    _appSession.WebUserId?.ToString() ?? "system", _appSession.WebUserId);
                Success = "Indent line retired.";
            }
            catch (InvalidOperationException ex) { Message = ex.Message; }

            await LoadFileAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostGenerateAsync()
        {
            await LoadDepartmentsAsync();
            if (!CanGenerate) { Message = "Insufficient privilege."; await LoadFileAsync(); return Page(); }

            try
            {
                var billNo = await _workflow.GenerateAsync(RegNo, BillDate, PeriodStart, PeriodEnd,
                    _appSession.WebUserId?.ToString() ?? "system", _appSession.WebUserId);
                Success = $"Bill {billNo} generated.";
            }
            catch (InvalidOperationException ex) { Message = ex.Message; }

            await LoadFileAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostReverseAsync()
        {
            await LoadDepartmentsAsync();
            if (!CanReverse) { Message = "Insufficient privilege."; await LoadFileAsync(); return Page(); }

            try
            {
                await _workflow.ReverseAsync(RegNo, ReverseBillNo, Reason,
                    _appSession.WebUserId?.ToString() ?? "system", _appSession.WebUserId);
                Success = $"Bill {ReverseBillNo} reversed.";
            }
            catch (InvalidOperationException ex) { Message = ex.Message; }

            await LoadFileAsync();
            return Page();
        }

        /// <summary>
        /// Loads everything the Update tab shows for the file in <see cref="RegNo"/>.
        /// </summary>
        private async Task LoadFileAsync()
        {
            if (string.IsNullOrWhiteSpace(RegNo)) return;
            Header = await _query.GetHeaderAsync(RegNo);
            if (Header == null) { Message = "Registration not found."; return; }

            // Derived status text for OPD
            if (Header.Settle == true) StatusText = "Settled";
            else if (Header.BillGen == true)
            {
                var hasReversed = await _db.OpdBillHeaders
                    .AnyAsync(h => h.HosCode == RegNo && h.Stat == "X");
                StatusText = hasReversed ? "Reversed" : "Generated";
            }
            else StatusText = "Open";

            DeptTotals = await _query.GetDeptTotalsAsync(RegNo);
            UsedAmount = DeptTotals.Sum(d => d.Amount);
            Receipts = Header.TRcvd ?? 0;
            Payments = Header.TPymt ?? 0;
            Discount = Header.CashDisc ?? 0;
            BilledAmount = Header.TBillAmt ?? 0;
            // Used Amount follows the indents that actually exist. When the file has
            // a billed total but no slips, fall back to it so Amt. Due is not 0.00.
            IndentDetailMissing = BilledAmount != 0 && DeptTotals.Sum(d => d.SlipCount) == 0;
            AmountDue = (IndentDetailMissing ? BilledAmount : UsedAmount) - Receipts + Payments - Discount;

            // Honor an EXPLICIT department selection coming from the summary row or the
            // header dropdown - including a department that has no indents yet on this
            // file, because the user may be about to ADD the first one.
            // Only auto-pick when no department was supplied at all (first load).
            if (string.IsNullOrWhiteSpace(Dept))
                Dept = DeptTotals.FirstOrDefault()?.Dept ?? "01";

            Lines = await _query.GetLinesAsync(RegNo, Dept);
            BillHeaders = await _query.GetBillHeadersAsync(RegNo);
            Audit = await _query.GetAuditAsync(RegNo, 50);

            // Auto-generate the next indent (slip) number for the entry form.
            if (CanAddIndent && string.IsNullOrWhiteSpace(SlipNo))
                SlipNo = await GetNextSlipNoAsync(Dept);

            // Resolve the patient display name.
            var regKey = (RegNo ?? "").Trim();
            var pcodeKey = (Header.PCode ?? "").Trim();

            if (regKey.Length > 0 || pcodeKey.Length > 0)
            {
                var pq = _db.Patients.Where(p => !p.Retired);
                if (regKey.Length > 0 && pcodeKey.Length > 0)
                    pq = pq.Where(p => p.Phn == regKey || p.Code == pcodeKey);
                else if (regKey.Length > 0)
                    pq = pq.Where(p => p.Phn == regKey);
                else
                    pq = pq.Where(p => p.Code == pcodeKey);

                var pat = await pq
                    .OrderByDescending(p => p.Phn == regKey)
                    .Select(p => new { p.Id, Name = p.Person != null ? p.Person.Name : null })
                    .FirstOrDefaultAsync();

                PatientName = pat?.Name ?? "-";
                PatientId = pat?.Id;
            }
        }

        private async Task LoadListAsync()
        {
            SearchResult = await _query.SearchPageAsync(SearchTerm, StatusFilter, PageIndex);
        }

        private async Task LoadDepartmentsAsync()
        {
            DeptList = await _db.LegacyDepartments.Where(d => !d.Retired)
                .OrderBy(d => d.Code)
                .Select(d => new SelectListItem { Value = d.Code, Text = d.Code + " - " + d.Name })
                .ToListAsync();
        }

        /// <summary>
        /// Computes the next indent (slip) number to offer in the entry form for a department.
        /// </summary>
        /// <param name="dept">Legacy 2-char department code (OBIL_MST.DEPT).</param>
        /// <returns>The next slip number as a string, or null when no department is supplied.</returns>
        /// <remarks>
        /// A slip number must be unique per DEPARTMENT across the WHOLE data set, not just
        /// this registration: OBIL_DET is keyed by (DEPT, SLIPNO) alone and has no
        /// registration / HOS_CODE column, so if two files reuse the same (DEPT, SLIPNO) the
        /// grid join shows (and the two files effectively share) each other's detail lines.
        /// The previous implementation scanned only the CURRENT file's slips, so it could hand
        /// out a number another registration already used. Therefore: take the global numeric
        /// maximum for the department and add one. Legacy slip numbers are 9-digit; TRY_CAST
        /// ignores any non-numeric slip values.
        /// Developer: Tajwer Jalal | Date: 20-Oct-2026 | Time: 16:30
        /// </remarks>
        private async Task<string> GetNextSlipNoAsync(string dept)
        {
            if (string.IsNullOrWhiteSpace(dept)) return null;

            var maxSlip = await _db.Database
                .SqlQueryRaw<long?>(
                    "SELECT MAX(TRY_CAST(SLIPNO AS bigint)) AS [Value] FROM dbo.OBIL_MST " +
                    "WHERE DEPT = {0} AND SLIPNO IS NOT NULL AND LTRIM(RTRIM(SLIPNO)) <> ''",
                    dept)
                .FirstOrDefaultAsync();

            return ((maxSlip ?? 0) + 1).ToString();
        }

        public async Task<IActionResult> OnGetDeptServicesAsync(string dept, string search)
        {
            var q = _db.Items.Where(i => !i.Retired && i.Billable);

            // Scope the picker to the selected legacy department's services, mirroring the
            // slip-entry pages: legacy CatCode -> Category.Code -> Item.CategoryId.
            // If no category is found, fall back to items linked directly to a Department.
            if (!string.IsNullOrWhiteSpace(dept))
            {
                var legacy = await _db.LegacyDepartments
                    .Where(d => !d.Retired && d.Code == dept)
                    .Select(d => new { d.CatCode, d.Name })
                    .FirstOrDefaultAsync();

                if (legacy != null)
                {
                    long? catId = null;
                    if (!string.IsNullOrWhiteSpace(legacy.CatCode))
                    {
                        var catCode = legacy.CatCode;
                        catId = await _db.Categories
                            .Where(c => !c.Retired && c.Id > 116 && c.Code == catCode)
                            .Select(c => (long?)c.Id)
                            .FirstOrDefaultAsync();
                    }

                    long? depId = null;
                    var type = MapLegacyDeptType(legacy.Name);
                    if (type.HasValue)
                    {
                        var t = type.Value;
                        depId = await _db.Departments
                            .Where(d => !d.Retired && d.DepartmentType == t)
                            .OrderBy(d => d.Id)
                            .Select(d => (long?)d.Id)
                            .FirstOrDefaultAsync();
                    }

                    if (catId.HasValue)
                        q = q.Where(i => i.CategoryId == catId.Value);
                    else if (depId.HasValue)
                        q = q.Where(i => i.DepartmentId == depId.Value);
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(i => (i.Name != null && i.Name.Contains(s))
                    || (i.Code != null && i.Code.Contains(s))
                    || (i.PrintName != null && i.PrintName.Contains(s))
                    || (i.Sname != null && i.Sname.Contains(s)));
            }

            var list = await q.OrderBy(i => i.Name).Take(100).Select(i => new
            {
                code = i.Code,
                name = i.PrintName != null && i.PrintName != "" ? i.PrintName : i.Name,
                rate = i.Total ?? i.UnitRate ?? i.StaffRate ?? 0
            }).ToListAsync();

            return new JsonResult(list);
        }

        /// <summary>
        /// Maps a legacy department name to the corresponding new <see cref="Data.Enums.DepartmentType"/>
        /// so the service picker can also pick up items linked directly to a Department.
        /// </summary>
        private static Data.Enums.DepartmentType? MapLegacyDeptType(string legacyName)
        {
            var n = (legacyName ?? "").ToLowerInvariant();
            if (n.Contains("laborator") || n.Contains("lab")) return Data.Enums.DepartmentType.Laboratory;
            if (n.Contains("x-ray") || n.Contains("xray") || n.Contains("ct") || n.Contains("radiolog") || n.Contains("ultra"))
                return Data.Enums.DepartmentType.Radiology;
            if (n.Contains("dental")) return Data.Enums.DepartmentType.Dental;
            if (n.Contains("operation")) return Data.Enums.DepartmentType.OperationTheater;
            if (n.Contains("pharmac")) return Data.Enums.DepartmentType.Pharmacy;
            if (n.Contains("hospital") || n.Contains("ward")) return Data.Enums.DepartmentType.Ward;
            if (n.Contains("consult") || n.Contains("clinic")) return Data.Enums.DepartmentType.Consultant;
            return null;
        }

        /// <summary>
        /// Resolves a legacy 2-char department CODE to its display NAME only for the
        /// Department Summary grid (e.g. "01" -> "Laboratory").
        /// </summary>
        public string DeptDisplay(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "-";
            var text = DeptList?.FirstOrDefault(x => x.Value == code)?.Text;
            if (string.IsNullOrWhiteSpace(text)) return code;
            var sep = text.IndexOf(" - ", StringComparison.Ordinal);
            return sep >= 0 ? text.Substring(sep + 3) : text;
        }

        private bool UserHas(string privilege) => _appSession.HasPrivilege(privilege);
    }
}