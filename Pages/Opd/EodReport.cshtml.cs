using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HmisWeb.Pages.Opd
{
    /// <summary>
    /// Legacy URL shim for the EOD collection report.
    ///
    /// The report moved to the Cashier module at /cashier/eodreport. This page
    /// keeps /opd/eodreport working (old bookmarks / printed links) by
    /// redirecting to the new route and carrying the selected date across.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 23-Oct-2026
    /// Time      : 11:15
    /// Change Log: 23-Oct-2026 - report moved to Cashier; this page now only
    ///             redirects to /cashier/eodreport.
    /// </summary>
    public class EodReportRedirectModel : PageModel
    {
        /// <summary>Redirect to the Cashier report, preserving ?sessionDate=.</summary>
        public IActionResult OnGet(DateTime? sessionDate)
        {
            if (sessionDate == null)
                return RedirectToPage("/Cashier/EodReport");

            return RedirectToPage("/Cashier/EodReport", new
            {
                sessionDate = sessionDate.Value.ToString("yyyy-MM-dd")
            });
        }
    }
}
