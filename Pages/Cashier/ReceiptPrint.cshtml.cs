using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Models;

namespace HmisWeb.Pages.Cashier
{
    /*
     * ============================================================================
     * File........: Pages/Cashier/ReceiptPrint.cshtml.cs
     * Purpose.....: Standalone thermal (80 mm) receipt for one CASHCNT/CRCVD
     *               receipt, opened automatically after a successful payment.
     * Inputs......: trNo query string; print=1 opens the browser print dialog.
     * Outputs.....: CashReceipt with its CRCVDDET lines, rendered by
     *               ReceiptPrint.cshtml.
     * Business
     *   rules.....: 1. Only the receipt identified by TrNo is printed.
     *               2. The page is standalone (Layout = null) for clean printing.
     *               3. print=1 opens the browser print dialog automatically.
     * Developer...: Tajwer Jalal
     * Date/Time...: 2026-11-04 10:00
     * ============================================================================
     */
    public class ReceiptPrintModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public ReceiptPrintModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)] public string TrNo { get; set; }
        [BindProperty(SupportsGet = true)] public string Print { get; set; }

        public CashReceipt Receipt { get; set; }
        public string PatientName { get; set; } = "";
        public string ErrorMessage { get; set; }

        public bool AutoPrint =>
            string.Equals(Print, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Print, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Print, "yes", StringComparison.OrdinalIgnoreCase);

        public async Task OnGetAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TrNo))
                {
                    ErrorMessage = "No receipt number was supplied.";
                    return;
                }

                Receipt = await _db.CashReceipts
                    .AsNoTracking()
                    .Include(r => r.Details)
                    .FirstOrDefaultAsync(r => r.TrNo == TrNo.Trim());

                if (Receipt == null)
                {
                    ErrorMessage = "Receipt not found: " + TrNo;
                    return;
                }

                if (Receipt.RegNo.HasValue)
                {
                    var patient = await _db.Patients
                        .AsNoTracking()
                        .Include(p => p.Person)
                        .FirstOrDefaultAsync(p => p.PatientId == Receipt.RegNo.Value);

                    PatientName = patient?.Person?.NameWithInitials
                               ?? patient?.Person?.Name
                               ?? "";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Could not load the receipt: " + ex.Message;
            }
        }
    }
}
