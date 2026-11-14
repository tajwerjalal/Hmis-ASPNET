using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HmisWeb.Models.Lab;
using HmisWeb.Services.Lab;

namespace HmisWeb.Pages.Lab
{
    /*
     * ============================================================================
     * File........: Pages/Lab/TestResultPrint.cshtml.cs
     * Purpose.....: Printable laboratory test report for one TESTTR line
     *               (READTR results plus the culture block), opened from the
     *               Save success dialog or the Entry tab Print button.
     * Inputs......: labNo/tid/testNo query string; print=1 auto-opens the
     *               browser print dialog.
     * Outputs.....: LabEntryView rendered by TestResultPrint.cshtml.
     * Business
     *   rules.....: 1. Uses the same LabResultService.GetEntryAsync data as the
     *                  entry screen, so the printed values are exactly what was
     *                  saved.
     *               2. Only READTR rows with a non-empty VALUE are printed.
     *               3. Culture is printed only when growth/species/medicine data
     *                  exists.
     *               4. The page is standalone (Layout = null) for clean printing.
     * Developer...: Tajwer Jalal
     * Date/Time...: 2026-11-03 12:16
     * ============================================================================
     */
    public class TestResultPrintModel : PageModel
    {
        private readonly LabResultService _lab;

        public TestResultPrintModel(LabResultService lab)
        {
            _lab = lab;
        }

        [BindProperty(SupportsGet = true)] public string LabNo { get; set; }
        [BindProperty(SupportsGet = true)] public string Tid { get; set; }
        [BindProperty(SupportsGet = true)] public string TestNo { get; set; }
        [BindProperty(SupportsGet = true)] public string Print { get; set; }

        public LabEntryView Entry { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime PrintedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Accepts print=1, print=true or print=yes because the Entry tab and
        /// the Save dialog both pass print=1.
        /// </summary>
        public bool AutoPrint =>
            string.Equals(Print, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Print, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Print, "yes", StringComparison.OrdinalIgnoreCase);

        public async Task OnGetAsync()
        {
            try
            {
                Entry = await _lab.GetEntryAsync(LabNo, Tid, TestNo);
                if (Entry == null || Entry.Header == null)
                {
                    ErrorMessage = Entry?.Error ?? "Indent not found.";
                    return;
                }

                if (Entry.SelectedTest == null)
                {
                    ErrorMessage = "No test selected for this indent.";
                    return;
                }

                // GetEntryAsync falls back to the first pending test when the
                // requested key is missing; never print a different test.
                if (!string.IsNullOrWhiteSpace(Tid) &&
                    !string.Equals(Entry.SelectedTest.Tid, Tid, StringComparison.OrdinalIgnoreCase))
                {
                    ErrorMessage = "The requested test was not found on this indent.";
                    return;
                }
                if (!string.IsNullOrWhiteSpace(TestNo) &&
                    !string.Equals(Entry.SelectedTest.TestNo ?? "", TestNo,
                        StringComparison.OrdinalIgnoreCase))
                {
                    ErrorMessage = "The requested test was not found on this indent.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Could not load the report: " + ex.Message;
            }
        }
    }
}
