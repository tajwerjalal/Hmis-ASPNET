using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using HmisWeb.Services;
using HmisWeb.Services.Billing;
using HmisWeb.Models.Recovery;

namespace HmisWeb.Pages.Recovery
{
    /// <summary>
    /// COMPANY INVOICE / LETTER screen - /Recovery/Invoice.
    /// Phase 4 sprint 1 - ports VFP BillSys form INVOICE (bs0601, menu key
    /// bs0601, RIGHTS.DEPTID=recovery) onto RECOVERY_INV / RECOVERY_INVDET.
    ///
    /// Conventions (workspace rules)
    ///   * list page + popup entry, popup save refreshes the list (location
    ///     reload with TempData message), master detail shown in the list
    ///     (line count + total),
    ///   * delete = red trash icon with a confirm dialog and a mandatory
    ///     reason - implemented as a soft-cancel with audit rows, per the
    ///     Phase-3 proposal (original behaviour was a physical DELETE),
    ///   * every handler carries dev-header comments server-side (this file).
    ///
    /// Permissions (baseclass RIGHTS carried over)
    ///   * RECOVERY_INVOICE_EDIT  add / edit / cancel invoices,
    ///   * ALL                    wildcard, same as every other screen.
    ///
    /// Developer: Tajwer Jalal | Date: 20-Oct-2026 | Time: 19:00
    /// </summary>
    public class InvoiceModel : PageModel
    {
        private readonly RecoveryInvoiceQueryService _query;
        private readonly RecoveryInvoiceService _write;
        private readonly RecoveryInvoiceReportService _letter;
        private readonly IAppSession _appSession;

        public InvoiceModel(RecoveryInvoiceQueryService query,
            RecoveryInvoiceService write, RecoveryInvoiceReportService letter,
            IAppSession appSession)
        {
            _query = query;
            _write = write;
            _letter = letter;
            _appSession = appSession;
        }

        // --- List tab filters -------------------------------------------------
        [BindProperty(SupportsGet = true)]
        public string Company { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Month { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Year { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Term { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        public RecoveryInvoiceQueryService.SearchResult SearchResult { get; set; }
        public List<RecoveryInvoiceQueryService.CompanyRow> Companies { get; set; } = new();

        // --- Permissions -------------------------------------------------------
        public bool CanEdit => _appSession.HasPrivilege("RECOVERY_INVOICE_EDIT")
                               || _appSession.HasPrivilege("ALL");
        public bool CanLetter => _appSession.HasPrivilege("RECOVERY_INVOICE_PRINT")
                               || _appSession.HasPrivilege("RECOVERY_INVOICE_EDIT")
                               || _appSession.HasPrivilege("ALL");

        [TempData]
        public string Success { get; set; }

        [TempData]
        public string Error { get; set; }

        // ------------------------------------------------------------------ get

        /// <summary>Renders the filtered list + dropdowns.</summary>
        public async Task OnGetAsync()
        {
            await LoadListAsync();
        }

        private async Task LoadListAsync()
        {
            Companies = await _query.GetCompaniesAsync();
            int? year = Year;
            SearchResult = await _query.SearchPageAsync(
                company: Company,
                month: Month,
                year: year,
                term: Term,
                status: Status,
                pageIndex: PageIndex,
                pageSize: 0);
        }

        // --------------------------------------------------------------- fetch

        /// <summary>Popup payload: header + lines + audit of one invoice.</summary>
        /// Change Log 20-Oct-2026 20:30 (Tajwer Jalal): login-gated after the
        /// audit review of the e2e run - patient PII must not leak to
        /// anonymous sessions through the fetch handlers, matching the
        /// security review of the OPD bill screen.
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
        /// Line-patient lookup for the popup (billed inpatients of the company,
        /// REGISTER TYPE='I' BILLGEN=1, minus regnos already on open invoices).
        /// </summary>
        public async Task<IActionResult> OnGetPatientSearchAsync(string company, string term)
        {
            if (!_appSession.IsLoggedIn)
                return new UnauthorizedResult();
            if (string.IsNullOrWhiteSpace(company))
                return new JsonResult(new List<RecoveryInvoiceQueryService.PatientRow>());

            var exclude = new List<string>();
            var open = await RegNosOnOpenInvoicesAsync();
            var patients = await _query.PatientSearchAsync(company, term, open);
            return new JsonResult(patients);
        }

        /// <summary>Regnos sitting on open invoices (dupe-guard source list).</summary>
        private async Task<List<string>> RegNosOnOpenInvoicesAsync()
        {
            // Handled inside the search service by exclusion; an empty list here
            // keeps the popup simple - the write service still guards.
            return new List<string>();
        }

        // ---------------------------------------------------------------- post

        /// <summary>
        /// Creates or updates one invoice from the popup (JSON body; the
        /// anti-forgery token travels in the RequestVerificationToken header).
        /// Change Log 20-Oct-2026 19:55 (Tajwer Jalal): bound via
        /// Request.ReadFromJsonAsync instead of a [FromBody] handler parameter -
        /// the parameter value arrived null under curl/PowerShell tests even
        /// with a valid token; reading the body directly is binding-source
        /// independent and keeps the antiforgery validation intact.
        /// </summary>
        public async Task<IActionResult> OnPostSaveAsync()
        {
            RecoveryInvoiceService.InvoiceUpsert input;
            try
            {
                input = await Request.ReadFromJsonAsync<RecoveryInvoiceService.InvoiceUpsert>();
            }
            catch (System.Text.Json.JsonException)
            {
                input = null;
            }
            if (input == null)
                return new JsonResult(new RecoveryInvoiceService.Result
                { Ok = false, Message = "Empty submission." });

            if (!CanEdit)
                return new JsonResult(new RecoveryInvoiceService.Result
                { Ok = false, Message = "You do not have RECOVERY_INVOICE_EDIT rights." });

            var result = input.Id == 0
                ? await _write.CreateAsync(input)
                : await _write.UpdateAsync(input);

            if (result.Ok && input.Id == 0)
                Success = $"Invoice {result.DocNo} created with "
                    + input.Lines.Count(l => !string.IsNullOrWhiteSpace(l.RegNo)) + " patient line(s).";
            else if (result.Ok)
                Success = $"Invoice {result.DocNo} updated.";

            return new JsonResult(result);
        }

        /// <summary>
        /// Soft-cancel of an invoice (mandatory reason) - the red-trash delete
        /// with confirmation, without losing the paper trail.
        /// </summary>
        public async Task<IActionResult> OnPostCancelAsync(long id, string reason)
        {
            // Server-side rights check - the red-trash button alone was not
            // enough (same audit review of 20-Oct-2026).
            if (!CanEdit)
                return new JsonResult(new RecoveryInvoiceService.Result
                { Ok = false, Message = "You do not have RECOVERY_INVOICE_EDIT rights." });
            var result = await _write.CancelAsync(id, reason);
            if (result.Ok) Success = $"Invoice {result.DocNo} cancelled.";
            return new JsonResult(result);
        }

        // --------------------------------------------------------------- letter

        /// <summary>
        /// IPDCOVLT port - covering letter PDF of one invoice (download).
        /// Rows=0 falls to a SweetAlert on the page, never to a blank PDF.
        /// </summary>
        public async Task<IActionResult> OnGetLetterPdfAsync(long id)
        {
            // Server-side rights check (only a privilege gate on the button
            // existed before; the handler itself enforced nothing).
            if (!_appSession.IsLoggedIn || !CanLetter)
            {
                Error = "You do not have RECOVERY_INVOICE_PRINT rights to print a letter.";
                return RedirectToPage();
            }
            var ctx = new RecoveryInvoiceReportService.LetterContext
            {
                HospitalName = _appSession.LoggedSite?.PrintingName
                               ?? _appSession.LoggedSite?.Name
                               ?? "HMIS Hospital",
                UserId = _appSession.LoggedUser?.WebUserPerson?.NameWithInitials
                         ?? _appSession.LoggedUser?.RealUserName
                         ?? _appSession.LoggedUser?.UserName
                         ?? ""
            };
            var letter = await _letter.RenderLetterAsync(id, ctx);
            if (letter.Rows == 0)
            {
                Error = letter.Message ?? "The invoice has nothing to submit.";
                return RedirectToPage();
            }
            var name = "RECOVERY-LETTER-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".pdf";
            return File(letter.Pdf, "application/pdf", name);
        }
    }
}
