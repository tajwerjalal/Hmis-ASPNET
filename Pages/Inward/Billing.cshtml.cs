using System;
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

namespace HmisWeb.Pages.Inward
{
    /// <summary>
    /// INDOOR PATIENT (IPD BILL) form - /inward/Billing.
    /// Replaces the legacy VFP form BS0110 (frmipdtran) and the previous
    /// bill-entry page that duplicated bill generation.
    ///
    /// Tabs
    ///   Update : a single inpatient file - header, department summary grid and
    ///            the department detail grid (editable while the file is Open).
    ///   List   : searchable list of inpatient files with a status filter.
    ///
    /// Workflow and roles (privileges checked in code, buttons disabled in the UI)
    ///   Admitted -> Discharged -> Bill Updating -> Bill Generated -> Settled
    ///   Executive : IPD_BILL_EDIT  (line add/edit/retire while status 1 or 2)
    ///   Officer   : IPD_BILL_GENERATE / IPD_BILL_REVERSE / IPD_BILL_UNSETTLE
    ///   Cashier   : IPD_BILL_SETTLE
    ///   Admin     : IPD_BILL_ADMIN (all of the above)
    ///
    /// Side effects: line edits write IBIL_DET / IBIL_MST and IpdBillAudit rows;
    /// generate writes BCONTROL / IPDBILL / IBIL_MST / REGISTER; every write is
    /// transactional and status-guarded at the SQL level.
    ///
    /// Developer: Tajwer Jalal | Date: 12-Oct-2026 | Time: 12:10
    /// </summary>
    public class BillingModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;
        private readonly ICommonService _common;
        private readonly IpdBillQueryService _query;
        private readonly IpdBillLineService _lines;
        private readonly IpdBillWorkflowService _workflow;

        public BillingModel(ApplicationDbContext db, IAppSession appSession, ICommonService common,
            IpdBillQueryService query, IpdBillLineService lines, IpdBillWorkflowService workflow)
        {
            _db = db;
            _appSession = appSession;
            _common = common;
            _query = query;
            _lines = lines;
            _workflow = workflow;
        }

        // ---- query / navigation ----
        [BindProperty(SupportsGet = true)] public string Tab { get; set; } = "update";
        [BindProperty(SupportsGet = true)] public string RegNo { get; set; }
        // NOTE: left empty on purpose. "01" as a default made an explicit ?Dept=02
        // indistinguishable from "no department supplied", so the LoadFileAsync
        // fallback below would always snap a dept-less request back to Laboratory.
        // Empty means "caller did not choose"; LoadFileAsync then picks the first
        // department that actually has indents on this file.
        [BindProperty(SupportsGet = true)] public string Dept { get; set; } = "";

        // ---- list tab ----
        [BindProperty(SupportsGet = true)] public string SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)] public byte? StatusFilter { get; set; }
        [BindProperty(SupportsGet = true)] public int ListPageIndex { get; set; } = 1;

        // ---- line editor ----
        [BindProperty] public long? LineId { get; set; }
        [BindProperty] public string SlipNo { get; set; }
        [BindProperty] public DateTime? SlipDate { get; set; }
        [BindProperty] public string ItemCode { get; set; }
        [BindProperty] public string Description { get; set; }
        [BindProperty] public decimal? Qty { get; set; }
        [BindProperty] public decimal? Rate { get; set; }
        [BindProperty] public string Remarks { get; set; }
        [BindProperty] public string ServiceSearch { get; set; }

        // ---- workflow ----
        [BindProperty] public DateTime BillDate { get; set; } = DateTime.Today;
        [BindProperty] public DateTime? PeriodStart { get; set; }
        [BindProperty] public DateTime? PeriodEnd { get; set; }
        [BindProperty] public string Reason { get; set; }

        public RegisterLegacy Header { get; set; }
        public IpdBillStatus? Status { get; set; }
        public string StatusText { get; set; }
        public List<IpdBillQueryService.DeptTotal> DeptTotals { get; set; } = new();
        public List<IpdBillQueryService.BillLine> Lines { get; set; } = new();
        public List<IpdBillHeader> BillHeaders { get; set; } = new();
        public List<IpdBillAudit> Audit { get; set; } = new();
        public List<IpdBillQueryService.FileRow> Files { get; set; } = new();
        public IpdBillQueryService.SearchResult ListResult { get; set; } = new();
        public int ListTotalRows => ListResult.TotalRows;
        public int ListTotalPages => ListResult.TotalPages;
        public int ListFirstRow => ListResult.FirstRowOnPage;
        public int ListLastRow => ListResult.LastRowOnPage;
        public List<SelectListItem> DeptList { get; set; } = new();
        public string PatientName { get; set; }
        /// <summary>
        /// The PATIENT row's surrogate key (PATIENT.Id), NOT the RegNo and NOT the
        /// (unused, normally NULL) PATIENT.PatientId column. Resolved in
        /// <see cref="LoadFileAsync"/> by joining REGISTER.REGNO to PATIENT.Phn.
        /// Historically only the removed "Lab Indent" link used it
        /// (/lab/indent?patientId=...); it is retained here as a model binding for
        /// that link and any future cross-page navigation.
        /// </summary>
        public long? PatientId { get; set; }
        public string Message { get; set; }
        public string Success { get; set; }

        public decimal UsedAmount { get; set; }
        public decimal Receipts { get; set; }
        public decimal Payments { get; set; }
        public decimal Discount { get; set; }
        public decimal BilledAmount { get; set; }
        public decimal AmountDue { get; set; }

        /// <summary>
        /// True when the file carries a billed total but the legacy indent tables
        /// hold no slips for it. ~37k of 100k inpatient files are in this state, so
        /// the grids must say so rather than render as 0.00 with no explanation.
        /// </summary>
        public bool IndentDetailMissing => BilledAmount != 0 && !DeptTotals.Any();

        public bool CanEdit => Status == IpdBillStatus.Discharged || Status == IpdBillStatus.BillUpdating;

        /// <summary>
        /// True when a NEW indent (slip line) may be added: the file has not been
        /// billed yet (not generated) and is not settled. Admitted (0), Discharged (1)
        /// and Bill Updating (2) files accept new indents; generated (3) and settled (4)
        /// files are locked. Editing / retiring existing lines stays on <see cref="CanEdit"/>.
        /// </summary>
        public bool CanAddIndent => Status.HasValue
            && Status != IpdBillStatus.BillGenerated
            && Status != IpdBillStatus.Settled;
        public bool CanGenerate => CanEdit && (UserHas("IPD_BILL_GENERATE") || UserHas("IPD_BILL_ADMIN"));
        public bool CanReverse => Status == IpdBillStatus.BillGenerated && (UserHas("IPD_BILL_REVERSE") || UserHas("IPD_BILL_ADMIN"));
        public bool CanSettle => Status == IpdBillStatus.BillGenerated && (UserHas("IPD_BILL_SETTLE") || UserHas("IPD_BILL_ADMIN"));
        public bool CanUnsettle => Status == IpdBillStatus.Settled && (UserHas("IPD_BILL_UNSETTLE") || UserHas("IPD_BILL_ADMIN"));

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadDepartmentsAsync();
            if (string.Equals(Tab, "list", StringComparison.OrdinalIgnoreCase))
            {
                await LoadListAsync();
                return Page();
            }
            await LoadFileAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostListAsync()
        {
            Tab = "list";
            await LoadDepartmentsAsync();
            await LoadListAsync();
            return Page();
        }

        /// <summary>
        /// Loads one page of the List tab. Re-runs the query when the requested
        /// page is past the end so the grid clamps instead of rendering empty.
        /// </summary>
        private async Task LoadListAsync()
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                ListResult = await _query.SearchPageAsync(SearchTerm, StatusFilter, ListPageIndex);
                Files = ListResult.Rows;
                if (ListResult.PageIndex == ListPageIndex) break;
                // Service clamped the index; adopt it and re-query once.
                ListPageIndex = ListResult.PageIndex;
            }
        }

        public async Task<IActionResult> OnPostAddLineAsync()
        {
            await LoadDepartmentsAsync();
            // Auto-generate slip number if missing
            if (string.IsNullOrWhiteSpace(SlipNo) && !string.IsNullOrWhiteSpace(Dept))
            {
                SlipNo = await GetNextSlipNoAsync(Dept);
            }
            try
            {
                await _lines.AddLineAsync(RegNo, Dept, SlipNo, SlipDate ?? DateTime.Today,
                    ItemCode, Description, Qty ?? 1, Rate ?? 0, Remarks,
                    _appSession.WebUserId?.ToString() ?? "system", _appSession.WebUserId);
                Success = "Line added.";
            }
            catch (InvalidOperationException ex) { Message = ex.Message; }
            await LoadFileAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostEditLineAsync()
        {
            await LoadDepartmentsAsync();
            try
            {
                await _lines.EditLineAsync(LineId ?? 0, Qty ?? 1, Rate ?? 0, Description, Remarks,
                    RegNo, _appSession.WebUserId?.ToString() ?? "system", _appSession.WebUserId);
                Success = "Line updated.";
            }
            catch (InvalidOperationException ex) { Message = ex.Message; }
            await LoadFileAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostRetireLineAsync(long retireLineId)
        {
            await LoadDepartmentsAsync();
            try
            {
                await _lines.RetireLineAsync(retireLineId, RegNo, _appSession.WebUserId?.ToString() ?? "system", _appSession.WebUserId);
                Success = "Line retired.";
            }
            catch (InvalidOperationException ex) { Message = ex.Message; }
            await LoadFileAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostGenerateAsync()
        {
            await LoadDepartmentsAsync();
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

        public async Task<IActionResult> OnPostReverseAsync(long billNo)
        {
            await LoadDepartmentsAsync();
            try
            {
                await _workflow.ReverseAsync(RegNo, billNo, Reason, _appSession.WebUserId?.ToString() ?? "system", _appSession.WebUserId);
                Success = $"Bill {billNo} reversed.";
            }
            catch (InvalidOperationException ex) { Message = ex.Message; }
            await LoadFileAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSettleAsync()
        {
            await LoadDepartmentsAsync();
            try
            {
                await _workflow.SettleAsync(RegNo, _appSession.WebUserId?.ToString() ?? "system", _appSession.WebUserId);
                Success = "Bill settled.";
            }
            catch (InvalidOperationException ex) { Message = ex.Message; }
            await LoadFileAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostUnsettleAsync()
        {
            await LoadDepartmentsAsync();
            try
            {
                await _workflow.UnsettleAsync(RegNo, Reason, _appSession.WebUserId?.ToString() ?? "system", _appSession.WebUserId);
                Success = "Bill unsettled.";
            }
            catch (InvalidOperationException ex) { Message = ex.Message; }
            await LoadFileAsync();
            return Page();
        }

        /// <summary>
        /// Loads everything the Update tab shows for the file in <see cref="RegNo"/>.
        /// </summary>
        /// <remarks>
        /// Inputs  : RegNo (query), Dept (query; an EXPLICIT value - even a department
        ///           with no indents yet on this file - is honored so the service picker
        ///           follows the chosen department. When Dept is empty the first
        ///           department that actually has indents is auto-selected),
        ///           SlipNo (auto-seeded when the indent editor may be opened).
        /// Returns : nothing; sets Header/Status/DeptTotals/Lines/BillHeaders/Audit,
        ///           the money fields, PatientName and PatientId on the model.
        /// Logic   : header -&gt; status -&gt; department totals (REGNO keyed) -&gt; money
        ///           (AmountDue falls back to BilledAmount when no slips exist) -&gt;
        ///           lines for the selected dept -&gt; bills -&gt; last 50 audit rows -&gt;
        ///           next slip no -&gt; patient name by joining REGISTER.REGNO to
        ///           PATIENT.Phn (RegNo is the universal key; PCODE is only a hint).
        ///           Sets Message and returns early when the file is not found.
        /// Note    : Fixed 16-Oct-2026 - previously any Dept not present in DeptTotals
        ///           was silently replaced by DeptTotals.First(), so selecting X-Ray /
        ///           CT Scan (02) on a lab-only file snapped back to Laboratory (01)
        ///           and the popup kept listing lab services.
        /// Developer: Tajwer Jalal | Date: 16-Oct-2026 | Time: 12:45
        /// </remarks>
        private async Task LoadFileAsync()
        {
            if (string.IsNullOrWhiteSpace(RegNo)) return;
            Header = await _query.GetHeaderAsync(RegNo);
            if (Header == null) { Message = "Registration not found."; return; }

            Status = (IpdBillStatus?)Header.BillStatus;
            StatusText = Status?.ToString() ?? "Unknown";

            DeptTotals = await _query.GetDeptTotalsAsync(RegNo);
            UsedAmount = DeptTotals.Sum(d => d.Amount);
            Receipts = Header.TRcvd ?? 0;
            Payments = Header.TPymt ?? 0;
            Discount = Header.CashDisc ?? 0;
            BilledAmount = Header.TBillAmt ?? 0;
            // Used Amount follows the indents that actually exist. When the file has
            // a billed total but no slips, fall back to it so Amt. Due is not 0.00.
            AmountDue = (IndentDetailMissing ? BilledAmount : UsedAmount) - Receipts + Payments - Discount;

            // Honor an EXPLICIT department selection coming from the summary row or the
            // header dropdown - including a department that has no indents yet on this
            // file, because the user may be about to ADD the first one (e.g. picking
            // X-Ray / CT Scan on a file that so far only has Laboratory indents).
            // Only auto-pick when no department was supplied at all (first load), so
            // the grid is never shown for a department that does not exist here.
            if (string.IsNullOrWhiteSpace(Dept))
                Dept = DeptTotals.FirstOrDefault()?.Dept ?? "01";

            Lines = await _query.GetLinesAsync(RegNo, Dept);
            BillHeaders = await _query.GetBillHeadersAsync(RegNo);
            Audit = await _query.GetAuditAsync(RegNo, 50);

            // Auto-generate the next indent (slip) number for the entry form.
            if (CanAddIndent && string.IsNullOrWhiteSpace(SlipNo))
                SlipNo = await GetNextSlipNoAsync(Dept);

            // Resolve the patient display name (and its surrogate key).
            //
            // REGNO is the universal key on this screen: every IBIL_MST/IBIL_DET/
            // IpdBill row is keyed by it, and REGNO == PATIENT.Phn for 103,608 of
            // 103,620 inpatient files (99.99%). REGISTER.PCODE == PATIENT.Code
            // matches only 103,614 and actively DISAGREES on some legacy rows
            // (e.g. REGNO 83705 -> PCODE 220101057 but PATIENT.Code 2210101316),
            // so PCODE is only a secondary hint. This block no longer uses the
            // loose Person.Name.Contains(...) match, which could return an
            // unrelated patient.
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
                    .OrderByDescending(p => p.Phn == regKey)   // REGNO/Phn match wins
                    .Select(p => new { p.Id, Name = p.Person != null ? p.Person.Name : null })
                    .FirstOrDefaultAsync();

                PatientName = pat?.Name ?? "-";
                PatientId = pat?.Id;
            }
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
        /// <param name="dept">Legacy 2-char department code (IBIL_MST.DEPT).</param>
        /// <returns>The next slip number as a string, or null when no department is supplied.</returns>
        /// <remarks>
        /// A slip number must be unique per DEPARTMENT across the WHOLE data set, not just
        /// this registration: IBIL_DET is keyed by (DEPT, SLIPNO) alone and has no
        /// registration / HOS_CODE column, so if two files reuse the same (DEPT, SLIPNO) the
        /// grid join shows (and the two files effectively share) each other's detail lines.
        /// The previous implementation scanned only the CURRENT file's slips, so it could hand
        /// out a number another registration already used - observed live when a new line was
        /// given slip "1" in dept 01, colliding with an existing dept-01 slip "1".
        /// Therefore: take the global numeric maximum for the department and add one. Legacy
        /// slip numbers are 9-digit; TRY_CAST ignores any non-numeric slip values.
        /// Developer: Tajwer Jalal | Date: 16-Oct-2026 | Time: 13:10
        /// </remarks>
        private async Task<string> GetNextSlipNoAsync(string dept)
        {
            if (string.IsNullOrWhiteSpace(dept)) return null;

            var maxSlip = await _db.Database
                .SqlQueryRaw<long?>(
                    "SELECT MAX(TRY_CAST(SLIPNO AS bigint)) AS [Value] FROM dbo.IBIL_MST " +
                    "WHERE DEPT = {0} AND SLIPNO IS NOT NULL AND LTRIM(RTRIM(SLIPNO)) <> ''",
                    dept)
                .FirstOrDefaultAsync();

            return ((maxSlip ?? 0) + 1).ToString();
        }

        public async Task<IActionResult> OnGetDeptServicesAsync(string dept, string search)
        {
            var q = _db.Items.Where(i => !i.Retired && i.Billable);

            // Scope the picker to the selected legacy department's services, mirroring the
            // slip-entry pages: legacy CatCode -> Category.Code -> Item.CategoryId. This is
            // precise per department (Lab, X-Ray and Ultra Sound are distinct categories).
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
        /// Department Summary grid (e.g. "01" -&gt; "Laboratory"). Reads the already
        /// loaded <see cref="DeptList"/> ("01 - Laboratory"), splitting on the first
        /// " - ". Falls back to the code itself when it cannot be resolved.
        /// </summary>
        /// <param name="code">Legacy department code (IBIL_MST.DEPT / DeptTotal.Dept).</param>
        /// <returns>The department name, or the code when no lookup is possible.</returns>
        /// <remarks>Developer: Tajwer Jalal | Date: 15-Oct-2026 | Time: 15:00</remarks>
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
