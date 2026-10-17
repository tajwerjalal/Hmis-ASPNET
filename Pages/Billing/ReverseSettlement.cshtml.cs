using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Billing
{
    /// <summary>
    /// Reverse Settlement - private (non-corporate) patient bill settlement reversal and
    /// discount change. Migrated from the VFP Billsys form <c>unsettle</c> (unsettle.dbf).
    /// <para>
    /// Legacy rules: the registration must be found, must be a <b>private</b> patient
    /// (no credit company) and must already be settled. A reverse (CHG_TYPE = 1)
    /// clears the settled flag so the bill can be re-generated; CHG_TYPE = 2 records
    /// a discount change against the bill.
    /// </para>
    /// </summary>
    public class ReverseSettlementModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public ReverseSettlementModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db; _session = session;
        }

        [BindProperty(SupportsGet = true)] public string PatientSearch { get; set; }

        // Request form
        [BindProperty] public long EncounterId { get; set; }
        [BindProperty] public int ChgType { get; set; } = 1;
        [BindProperty] public long? BillId { get; set; }
        [BindProperty] public double? NewDiscAmt { get; set; }
        [BindProperty] public string Remarks { get; set; }

        public List<PatientEncounter> Encounters { get; set; } = new();
        public List<UnsettleRequest> History { get; set; } = new();

        public PatientEncounter Selected { get; set; }
        public List<Bill> CandidateBills { get; set; } = new();

        public string Success { get; set; }
        public string ErrorMessage { get; set; }

        public string SelectedRegno { get; set; }
        public string SelectedPatientName { get; set; }
        public bool SelectedIsPrivate { get; set; }
        public bool SelectedSettled { get; set; }

        public async Task OnGetAsync() => await LoadAsync();

        public async Task<IActionResult> OnPostLoadAsync()
        {
            await LoadSelectedAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSubmitAsync()
        {
            var enc = await _db.PatientEncounters
                .Include(e => e.CreditCompany)
                .FirstOrDefaultAsync(e => e.Id == EncounterId && !e.Retired);

            if (enc == null)
            {
                ErrorMessage = "Registration not found.";
                await LoadAsync();
                return Page();
            }

            // Legacy validation: private patient only.
            if (enc.CreditCompanyId.HasValue)
            {
                ErrorMessage = "Reverse settlement is allowed only for private (non-corporate) patients.";
                await LoadSelectedAsync();
                return Page();
            }

            // Legacy validation: must already be settled.
            if (!enc.PaymentFinalized)
            {
                ErrorMessage = "This registration is not settled; nothing to reverse.";
                await LoadSelectedAsync();
                return Page();
            }

            var now = DateTime.Now;
            var regno = enc.BhtNo ?? enc.ReferenceNo;

            Bill bill = null;
            if (BillId.HasValue && BillId.Value > 0)
            {
                bill = await _db.Bills.FirstOrDefaultAsync(b =>
                    b.Id == BillId.Value && b.PatientEncounterId == enc.Id && !b.Retired);
            }

            if (ChgType == 2 && !NewDiscAmt.HasValue)
            {
                ErrorMessage = "Enter the new discount amount for a discount change.";
                await LoadSelectedAsync();
                return Page();
            }

            var reqNo = "USR" + now.ToString("yyMMddHHmmss");

            var req = new UnsettleRequest
            {
                ReqNo = reqNo,
                ReqDate = now.Date,
                Regno = regno,
                EncounterId = enc.Id,
                ChgType = ChgType,
                OldBillNo = bill?.ReferenceNumber,
                OldBillAmt = bill?.NetTotal,
                ChgDate = now.Date,
                ChgBy = _session.WebUserId?.ToString(),
                CreatedAt = now,
                CreaterId = _session.WebUserId,
                Reset = false
            };

            if (ChgType == 1)
            {
                // Reverse the settlement: clear the settled flag and close the bill.
                enc.PaymentFinalized = false;
                enc.EditedAt = now;
                enc.EditerId = _session.WebUserId;

                if (bill != null)
                {
                    bill.BillPaymentCompletelySettled = false;
                    bill.Paid = false;
                    bill.BillClosed = false;
                    bill.EditedAt = now;
                    bill.EditorId = _session.WebUserId;
                }

                req.Reset = true;
                req.NewDiscBy = Remarks;
            }
            else
            {
                req.NewDiscAmt = NewDiscAmt;
                req.NewDiscBy = Remarks;

                if (bill != null)
                {
                    var newNet = Math.Max(0, bill.NetTotal - NewDiscAmt.Value);
                    bill.Discount = NewDiscAmt.Value;
                    bill.NetTotal = newNet;
                    bill.Balance = newNet - bill.PaidAmount;
                    bill.EditedAt = now;
                    bill.EditorId = _session.WebUserId;
                }
            }

            _db.UnsettleRequests.Add(req);
            await _db.SaveChangesAsync();

            Success = ChgType == 1
                ? $"Settlement reversed for {regno} (request {reqNo})."
                : $"Discount updated for {regno} (request {reqNo}).";

            await LoadAsync();
            return Page();
        }

        private async Task LoadSelectedAsync()
        {
            if (EncounterId <= 0) return;
            Selected = await _db.PatientEncounters
                .Include(e => e.CreditCompany)
                .Include(e => e.Patient).ThenInclude(p => p.Person)
                .FirstOrDefaultAsync(e => e.Id == EncounterId && !e.Retired);
            await ResolveSelectedAsync();
        }

        private async Task ResolveSelectedAsync()
        {
            if (Selected == null) return;

            SelectedRegno = Selected.BhtNo ?? Selected.ReferenceNo;
            SelectedPatientName = Selected.Patient?.Person?.Name;
            SelectedIsPrivate = !Selected.CreditCompanyId.HasValue;
            SelectedSettled = Selected.PaymentFinalized;

            CandidateBills = await _db.Bills
                .Where(b => b.PatientEncounterId == Selected.Id && !b.Retired && !b.Cancelled)
                .OrderByDescending(b => b.Id)
                .Take(50)
                .ToListAsync();
        }

        private async Task LoadAsync()
        {
            Success = TempData["RsSuccess"] as string;
            ErrorMessage ??= TempData["RsError"] as string;

            if (EncounterId <= 0)
            {
                var q = _db.PatientEncounters
                    .Include(e => e.Patient).ThenInclude(p => p.Person)
                    .Include(e => e.CreditCompany)
                    .Where(e => !e.Retired && e.PaymentFinalized);

                if (!string.IsNullOrWhiteSpace(PatientSearch))
                {
                    var t = PatientSearch.Trim();
                    q = q.Where(e =>
                        (e.BhtNo != null && e.BhtNo.Contains(t))
                        || (e.ReferenceNo != null && e.ReferenceNo.Contains(t))
                        || (e.Patient.Phn != null && e.Patient.Phn.Contains(t))
                        || (e.Patient.Person.Name != null && e.Patient.Person.Name.Contains(t)));
                }

                Encounters = await q.OrderByDescending(e => e.Id).Take(25).ToListAsync();
            }

            await LoadSelectedAsync();

            History = await _db.UnsettleRequests
                .Where(u => !u.Retired)
                .OrderByDescending(u => u.Id)
                .Take(50)
                .ToListAsync();
        }
    }
}