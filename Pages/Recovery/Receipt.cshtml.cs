using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HmisWeb.Services;
using HmisWeb.Services.Billing;

namespace HmisWeb.Pages.Recovery
{
    /// <summary>
    /// RECEIPTS / CHEQUES (IPD) screen - /Recovery/Receipt.
    /// Phase 4 sprint 2 - ports VFP BillSys form RECPT (bs0603, menu key recpt,
    /// RIGHTS.DEPTID=recovery) onto RECOVERY_RCPT / RECOVERY_RCPT_ALLOC.
    ///
    /// Conventions (workspace rules, same as the Invoice screen)
    ///   * list page + popup entry; a save refreshes the list with a TempData
    ///     message; master detail opened from the row (allocation lines +
    ///     audit trail shown in the popup),
    ///   * red-trash cancel with a confirm dialog and a mandatory reason -
    ///     soft-cancel (no physical delete, approved drift, R-03/R-18),
    ///   * cheques carry a lifecycle the legacy screen never had:
    ///     'P'ending -> 'C'leared (Clear button) / 'B'ounced (Bounce button,
    ///     reason mandatory, allocations removed with an audit row per line),
    ///   * every write is privilege-gated server-side, not only by hiding the
    ///     button (lesson of the 20-Oct audit review).
    ///
    /// Permissions (baseclass RIGHTS carried over)
    ///   * RECOVERY_RECEIPT_EDIT  raise / clear / bounce / cancel receipts,
    ///   * ALL                    wildcard, same as every other screen.
    ///
    /// Developer: Tajwer Jalal | Date: 21-Oct-2026 | Time: 11:40
    /// </summary>
    public class ReceiptModel : PageModel
    {
        private readonly RecoveryReceiptQueryService _query;
        private readonly RecoveryReceiptService _write;
        private readonly RecoveryInvoiceQueryService _invoiceQuery;
        private readonly IAppSession _appSession;

        public ReceiptModel(RecoveryReceiptQueryService query,
            RecoveryReceiptService write, RecoveryInvoiceQueryService invoiceQuery,
            IAppSession appSession)
        {
            _query = query;
            _write = write;
            _invoiceQuery = invoiceQuery;
            _appSession = appSession;
        }

        // --- List tab filters -------------------------------------------------
        [BindProperty(SupportsGet = true)]
        public string Company { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Method { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Term { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        public RecoveryReceiptQueryService.SearchResult SearchResult { get; set; }
        public List<RecoveryInvoiceQueryService.CompanyRow> Companies { get; set; } = new();

        // --- Permissions -------------------------------------------------------
        public bool CanEdit => _appSession.HasPrivilege("RECOVERY_RECEIPT_EDIT")
                               || _appSession.HasPrivilege("ALL");

        [TempData]
        public string Success { get; set; }

        [TempData]
        public string Error { get; set; }

        // ------------------------------------------------------------------ get

        /// <summary>Renders the filtered receipt list + company dropdown.</summary>
        public async Task OnGetAsync()
        {
            await LoadListAsync();
        }

        private async Task LoadListAsync()
        {
            Companies = await _invoiceQuery.GetCompaniesAsync();
            SearchResult = await _query.SearchPageAsync(
                company: Company,
                method: Method,
                status: Status,
                term: Term,
                pageIndex: PageIndex,
                pageSize: 0);
        }

        // --------------------------------------------------------------- fetch

        /// <summary>Popup payload: header + allocation lines + audit.</summary>
        public async Task<IActionResult> OnGetDetailAsync(long id)
        {
            if (!_appSession.IsLoggedIn)
                return new UnauthorizedResult();
            var detail = await _query.GetDetailAsync(id);
            if (detail == null)
                return NotFound();
            return new JsonResult(detail);
        }

        /// <summary>
        /// Allocation-target lookup for the popup: the company's OPEN invoices
        /// with per-line remaining money after active receipts, plus patient
        /// names for the candidate lines.
        /// </summary>
        public async Task<IActionResult> OnGetOpenInvoicesAsync(string company)
        {
            if (!_appSession.IsLoggedIn)
                return new UnauthorizedResult();
            if (string.IsNullOrWhiteSpace(company))
                return new JsonResult(new { invoices = new List<RecoveryReceiptQueryService.InvoiceCandidate>(), patients = new Dictionary<string, string>() });

            var invoices = await _query.OpenInvoicesAsync(company.Trim());
            var regNos = invoices.SelectMany(i => i.Lines).Select(l => l.RegNo.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var patients = await _query.PatientNamesAsync(regNos);
            return new JsonResult(new { invoices, patients });
        }

        // ---------------------------------------------------------------- post

        /// <summary>
        /// Creates one receipt from the popup (JSON body; the anti-forgery
        /// token travels in the RequestVerificationToken header - same recipe
        /// that fixed the null-bound [FromBody] handler on the Invoice screen).
        /// </summary>
        public async Task<IActionResult> OnPostSaveAsync()
        {
            RecoveryReceiptService.ReceiptUpsert input;
            try
            {
                input = await Request.ReadFromJsonAsync<RecoveryReceiptService.ReceiptUpsert>();
            }
            catch (System.Text.Json.JsonException)
            {
                input = null;
            }
            if (input == null)
                return new JsonResult(new RecoveryReceiptService.Result
                { Ok = false, Message = "Empty submission." });

            if (!CanEdit)
                return new JsonResult(new RecoveryReceiptService.Result
                { Ok = false, Message = "You do not have RECOVERY_RECEIPT_EDIT rights." });

            var result = await _write.CreateAsync(input);
            if (result.Ok)
                Success = $"Receipt {result.ReceiptNo} recorded.";
            return new JsonResult(result);
        }

        /// <summary>Cheque arrived good: pending -> cleared.</summary>
        public async Task<IActionResult> OnPostClearAsync(long id)
        {
            if (!CanEdit)
                return new JsonResult(new RecoveryReceiptService.Result
                { Ok = false, Message = "You do not have RECOVERY_RECEIPT_EDIT rights." });
            var result = await _write.ClearAsync(id);
            if (result.Ok) Success = $"Receipt {result.ReceiptNo} cleared.";
            return new JsonResult(result);
        }

        /// <summary>
        /// Cheque bounced - allocations removed (invoices re-open), reason
        /// mandatory, everything audited.
        /// </summary>
        public async Task<IActionResult> OnPostBounceAsync(long id, string reason)
        {
            if (!CanEdit)
                return new JsonResult(new RecoveryReceiptService.Result
                { Ok = false, Message = "You do not have RECOVERY_RECEIPT_EDIT rights." });
            var result = await _write.BounceAsync(id, reason);
            if (result.Ok) Success = $"Receipt {result.ReceiptNo} marked bounced - its allocations were reversed.";
            return new JsonResult(result);
        }

        /// <summary>
        /// Soft-cancel (mandatory reason) - the red-trash delete with
        /// confirmation, without losing the paper trail.
        /// </summary>
        public async Task<IActionResult> OnPostCancelAsync(long id, string reason)
        {
            if (!CanEdit)
                return new JsonResult(new RecoveryReceiptService.Result
                { Ok = false, Message = "You do not have RECOVERY_RECEIPT_EDIT rights." });
            var result = await _write.CancelAsync(id, reason);
            if (result.Ok) Success = $"Receipt {result.ReceiptNo} cancelled - its allocations were reversed.";
            return new JsonResult(result);
        }
    }
}
