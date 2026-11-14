using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HmisWeb.Pages.Recovery
{
    /// <summary>
    /// Form/Page/Method : Recovery module - 'form under migration' status page
    /// Module           : Billing - Recovery - HMC
    /// Purpose          : With the full legacy VFP pad (BSMENU.MPR, pad bs06
    ///                    'Recovery', 11 bars) now wired into the sidebar, every
    ///                    pending bar points here instead of nowhere.  The page
    ///                    shows which legacy form the bar maps to, its Phase-1
    ///                    review status, and the sprint it is scheduled in the
    ///                    approved Phase-3 rollout, so the menu is never a dead
    ///                    link while each form is rebuilt one-per-sprint.
    /// Parameters       : k (query, string) - the legacy form key: recpt,
    ///                    oinvoice, orecpt, obilvalue, invoicer, recptr,
    ///                    recledger, ostreport.  Unknown/absent falls back to a
    ///                    generic card.
    /// Returns          : html status card
    /// Tables Used      : none (read-only static page)
    /// Business Rules   : none - informational only; the live screens enforce
    ///                    their own RECOVERY_INVOICE_EDIT/PRINT privileges.
    ///                    Bars 1 (invoice) of the pad is the live screen at
    ///                    /Recovery/Invoice and does not route here.
    /// Developer        : [TAJWER JALAL]
    /// Created/Modified : 2026-10-21 10:20
    /// Change Log       : 2026-10-21 10:20 - created to complete the full
    ///                    BSMENU.MPR Recovery pad wiring under Billing.
    /// </summary>
    public class ComingSoonModel : PageModel
    {
        /// <summary>Status row resolved from the query key.</summary>
        public class PadInfo
        {
            public int Bar { get; set; }
            public string Title { get; set; }
            public string Form { get; set; }
            public string OneLiner { get; set; }
            public string Planned { get; set; }
            public bool Next { get; set; }
        }

        /// <summary>All non-built bars of the pad, in menu order.</summary>
        /// Change Log 21-Oct-2026 15:05 (Tajwer Jalal): recpt moved to BUILT
        /// (sprint 2, /Recovery/Receipt live) and removed from this map;
        /// oinvoice/orecpt are now flagged next.
        public static readonly Dictionary<string, PadInfo> Pad = new(
            StringComparer.OrdinalIgnoreCase)
        {
            ["oinvoice"] = new PadInfo
            {
                Bar = 3, Title = "Invoice/Letter (OPD)", Form = "oinvoice (oinvoicef)",
                OneLiner = "Byte-level clone of the IPD invoice rebound to ORCPT/ORCPTDET-era OPD data; one identical screen with DocType 'O'.",
                Planned = "Sprint 3 - reuses the same RECOVERY_INV tables with DocType 'O' (no duplicate table pair)."
            },
            ["orecpt"] = new PadInfo
            {
                Bar = 4, Title = "Receipts / Cheques (OPD)", Form = "orecpt (orecptf)",
                OneLiner = "OPD receipts, same engine as the IPD receipt screen with the OPD file filter.",
                Planned = "Sprint 4 - reuses RECOVERY_RCPT / RECOVERY_RCPT_ALLOC with DocType 'O' on the shared receipt engine."
            },
            ["obilvalue"] = new PadInfo
            {
                Bar = 6, Title = "Bill Value Correction", Form = "obilvalue (obilvaluef)",
                OneLiner = "Legacy form silently overwrites OLDBILL values with no trail - reviewed CRITICAL (data-integrity).",
                Planned = "Rebuilds as a privileged correction screen: mandatory reason, before/after audit rows into RECOVERY_AUDIT, and role gate instead of an overwrite."
            },
            ["invoicer"] = new PadInfo
            {
                Bar = 8, Title = "Invoice Register (IPD)", Form = "invoicer",
                OneLiner = "Register/listing of raised invoices per company and period.",
                Planned = "Registers phase - collapses with the other registers over one canonical ledger view, PDF to spec (A4, navy/teal, Page X of Y)."
            },
            ["recptr"] = new PadInfo
            {
                Bar = 9, Title = "Receipt Register (IPD)", Form = "recptr",
                OneLiner = "Register/listing of receipts with the cheque columns.",
                Planned = "Registers phase - same shared engine as the Invoice Register."
            },
            ["recledger"] = new PadInfo
            {
                Bar = 10, Title = "Recovery Ledger", Form = "recledger",
                OneLiner = "The reconciliation backbone - invoices vs receipts vs outstanding per party.",
                Planned = "Registers/ledger phase - one canonical vw_RecoveryLedger feeding the registers, ledger and outstanding PDFs so they can never disagree (G-x gaps closed at source)."
            },
            ["ostreport"] = new PadInfo
            {
                Bar = 11, Title = "Outstanding Receipts", Form = "ostreport",
                OneLiner = "Outstanding/aging report per company with follow-up.",
                Planned = "Final report sprint - 0-30/31-60/61-90/90+ aging buckets per panel/party with follow-up tracking."
            }
        };

        /// <summary>
        /// Form/Page/Method : OnGetAsync
        /// Purpose          : Resolve the 'k' key into the pad card to show.
        /// Parameters       : k (query, string) - legacy form key or empty.
        /// Returns          : void; Info carries the card data (null for a
        ///                    generic card when unknown).
        /// Tables Used      : none
        /// Business Rules   : unknown keys do not error - a generic card is
        ///                    shown; this page never writes.
        /// Developer        : [TAJWER JALAL]
        /// Created/Modified : 2026-10-21 10:20
        /// Change Log       : 2026-10-21 10:20 - created.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string K { get; set; }

        public PadInfo Info { get; private set; }

        public void OnGet()
        {
            if (!string.IsNullOrWhiteSpace(K) && Pad.TryGetValue(K.Trim(), out var row))
                Info = row;
        }
    }
}
