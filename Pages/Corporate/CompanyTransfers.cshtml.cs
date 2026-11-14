using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Corporate
{
    /// <summary>
    /// Company Transfer - /corporate/CompanyTransfers.
    ///
    /// Replaces the legacy VFP screen cmptrans.scx: it moves an inpatient or
    /// outpatient stay from one panel company to another, updating the credit
    /// company, employee, reference and card data on the stay and keeping the
    /// movement history in [COMPANYTRANSFER]. Pending (unsettled) bills of the
    /// stay can optionally be re-priced to the new company.
    ///
    /// List + popup entry, server-side paging, soft-retire with a reason
    /// (SweetAlert), and login / CORPORATE_COMPANY_TRANSFER_EDIT gates.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 22-Oct-2026
    /// Time      : 18:00
    /// Change Log: 22-Oct-2026 - created on COMPANY + INSTITUTION + COMPANYTRANSFER.
    /// </summary>
    public class CompanyTransfersModel : Shared.PagedPageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public CompanyTransfersModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        public List<CompanyTransfer> Transfers { get; set; } = new();
        public List<SelectListItem> CompanyList { get; set; } = new();
        public List<SelectListItem> WardList { get; set; } = new();
        public List<SelectListItem> EmployeeList { get; set; } = new();
        public List<SelectListItem> RelationList { get; set; } = new();
        public List<PatientEncounter> SearchResults { get; set; } = new();
        public PatientEncounter SelectedEncounter { get; set; }
        public Institution FromCompanyInstitution { get; set; }
        public string FromCompanyCode { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Q { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Type { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool ShowRetired { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? SelectedEncounterId { get; set; }

        // transfer inputs
        [BindProperty]
        public long? TransferEncounterId { get; set; }

        [BindProperty]
        public string ToCompanyCode { get; set; }

        [BindProperty]
        public string EmployeeCode { get; set; }

        [BindProperty]
        public string EmployeeName { get; set; }

        [BindProperty]
        public string EmployeeDesignation { get; set; }

        [BindProperty]
        public string EmployeeRelation { get; set; }

        [BindProperty]
        public string ReferenceNo { get; set; }

        [BindProperty]
        public DateTime? ReferenceValid { get; set; }

        [BindProperty]
        public string CardNo { get; set; }

        [BindProperty]
        public string BedEntitlement { get; set; }

        [BindProperty]
        public bool UpdateRates { get; set; }

        [BindProperty]
        public DateTime? TransferDate { get; set; }

        [TempData]
        public string Success { get; set; }

        [TempData]
        public string Error { get; set; }

        public bool CanEdit => _session.HasPrivilege("CORPORATE_COMPANY_TRANSFER_EDIT")
                               || _session.HasPrivilege("ALL");

        public async Task<IActionResult> OnGetAsync()
        {
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");

            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSearchAsync()
        {
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");

            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostTransferAsync()
        {
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");
            if (!CanEdit)
            {
                Error = "You do not have the right to transfer companies.";
                return RedirectToPage(new { Q, Type, ShowRetired, SearchTerm, SelectedEncounterId });
            }

            var encounter = await _db.PatientEncounters
                .Include(e => e.Patient).ThenInclude(p => p.Person)
                .FirstOrDefaultAsync(e => e.Id == TransferEncounterId && !e.Retired);
            if (encounter == null)
            {
                Error = "The selected stay was not found.";
                return RedirectToPage(new { Q, Type, ShowRetired, SearchTerm, SelectedEncounterId });
            }

            if (string.IsNullOrWhiteSpace(ToCompanyCode))
            {
                Error = "The target company is required.";
                return RedirectToPage(new { Q, Type, ShowRetired, SearchTerm, SelectedEncounterId });
            }

            // resolve company code -> institution
            var toCompanyMaster = await _db.CompanyMasters.FirstOrDefaultAsync(c => c.Id == ToCompanyCode.Trim());
            if (toCompanyMaster == null)
            {
                Error = "The target company was not found.";
                return RedirectToPage(new { Q, Type, ShowRetired, SearchTerm, SelectedEncounterId });
            }

            var toInstitution = await _db.Institutions.FirstOrDefaultAsync(i => i.Code == toCompanyMaster.Id);
            if (toInstitution == null)
            {
                Error = "The target company is not linked to a billing institution.";
                return RedirectToPage(new { Q, Type, ShowRetired, SearchTerm, SelectedEncounterId });
            }

            var fromCode = encounter.CreditCompanyId.HasValue
                ? (await _db.Institutions.FirstOrDefaultAsync(i => i.Id == encounter.CreditCompanyId.Value))?.Code
                : null;

            // from company code (legacy COMPANY.ID) when available
            var fromCompanyMaster = string.IsNullOrWhiteSpace(fromCode)
                ? null
                : await _db.CompanyMasters.FirstOrDefaultAsync(c => c.Id == fromCode);

            // capture the old company before the stay is updated
            var oldCreditCompanyId = encounter.CreditCompanyId;
            var oldCreditCompanyCode = fromCode;

            var transferAt = TransferDate ?? DateTime.Now;

            // update the stay
            encounter.CreditCompanyId = toInstitution.Id;
            encounter.EmployeeCode = string.IsNullOrWhiteSpace(EmployeeCode) ? encounter.EmployeeCode : EmployeeCode.Trim();
            encounter.EmployeeName = string.IsNullOrWhiteSpace(EmployeeName) ? encounter.EmployeeName : EmployeeName.Trim();
            encounter.EmployeeRelation = string.IsNullOrWhiteSpace(EmployeeRelation) ? encounter.EmployeeRelation : EmployeeRelation.Trim();
            encounter.ReferenceNo = string.IsNullOrWhiteSpace(ReferenceNo) ? encounter.ReferenceNo : ReferenceNo.Trim();
            encounter.RefValid = ReferenceValid ?? encounter.RefValid;
            encounter.CardNo = string.IsNullOrWhiteSpace(CardNo) ? encounter.CardNo : CardNo.Trim();
            encounter.BedEntitlement = string.IsNullOrWhiteSpace(BedEntitlement) ? encounter.BedEntitlement : BedEntitlement.Trim();
            encounter.EditerId = _session.WebUserId;
            encounter.EditedAt = DateTime.Now;

            var transfer = new CompanyTransfer
            {
                Type = encounter.EncounterType == EncounterType.Inward ? "I" : "O",
                RegNo = encounter.BhtNo ?? encounter.Id.ToString(),
                PatientEncounterId = encounter.Id,
                FromCompanyId = oldCreditCompanyId,
                ToCompanyId = toInstitution.Id,
                FromCompanyCode = fromCompanyMaster?.Id ?? oldCreditCompanyCode,
                ToCompanyCode = toCompanyMaster.Id,
                EmployeeCode = EmployeeCode?.Trim(),
                EmployeeName = EmployeeName?.Trim(),
                EmployeeDesignation = EmployeeDesignation?.Trim(),
                EmployeeRelation = EmployeeRelation?.Trim(),
                ReferenceNo = ReferenceNo?.Trim(),
                ReferenceValid = ReferenceValid,
                CardNo = CardNo?.Trim(),
                BedEntitlement = BedEntitlement?.Trim(),
                UpdateRates = UpdateRates,
                TransferDate = transferAt,
                CreaterId = _session.WebUserId,
                CreatedAt = DateTime.Now,
                Retired = false,
                UserId = _session.LoggedUser?.UserName ?? "SYSTEM",
                UDate = DateTime.Now.Date,
                UTime = DateTime.Now.ToString("HH:mm")
            };

            _db.CompanyTransfers.Add(transfer);

            // optionally re-price pending (unsettled) bills of the stay
            if (UpdateRates)
            {
                var bills = await _db.Bills
                    .Include(b => b.BillItems)
                    .Where(b => b.PatientEncounterId == encounter.Id && !b.Retired && !b.Paid)
                    .ToListAsync();

                foreach (var bill in bills)
                {
                    bill.CreditCompanyId = toInstitution?.Id ?? bill.CreditCompanyId;
                    bill.PaymentMethod = PaymentMethod.Credit;

                    foreach (var item in bill.BillItems.Where(i => !i.Retired && i.ItemId1.HasValue))
                    {
                        var rate = await _db.CompanyServiceRates
                            .Where(r => r.CompanyId == toInstitution.Id && r.ItemId == item.ItemId1 && !r.Retired)
                            .OrderByDescending(r => r.Id)
                            .FirstOrDefaultAsync();
                        if (rate != null && rate.Rate > 0)
                        {
                            item.Rate = (double)rate.Rate;
                            item.NetRate = (double)rate.Rate;
                            var qty = item.Qty ?? 1;
                            item.GrossValue = qty * (double)rate.Rate;
                            item.NetValue = qty * (double)rate.Rate;
                        }
                    }

                    var total = bill.BillItems.Where(i => !i.Retired).Sum(i => i.NetValue);
                    bill.Total = total;
                    bill.NetTotal = total;
                    bill.BillTotal = total;
                    bill.GrantTotal = total;
                    bill.Balance = total;
                }
            }

            await _db.SaveChangesAsync();

            Success = "Company transfer saved.";
            return RedirectToPage(new { Q, Type, ShowRetired, SearchTerm, SelectedEncounterId });
        }

        public async Task<IActionResult> OnPostRetireAsync(long id, string reason)
        {
            if (!_session.IsLoggedIn)
                return RedirectToPage("/Account/Login");
            if (!CanEdit)
            {
                Error = "You do not have the right to retire company transfers.";
                return RedirectToPage(new { Q, Type, ShowRetired });
            }

            var transfer = await _db.CompanyTransfers.FirstOrDefaultAsync(t => t.Id == id && !t.Retired);
            if (transfer == null)
            {
                Error = "Transfer not found.";
                return RedirectToPage(new { Q, Type, ShowRetired });
            }

            transfer.Retired = true;
            transfer.RetireComments = string.IsNullOrWhiteSpace(reason) ? "Retired from company transfer." : reason.Trim();
            transfer.RetiredAt = DateTime.Now;
            transfer.RetirerId = _session.WebUserId;
            transfer.EditedAt = DateTime.Now;
            transfer.EditerId = _session.WebUserId;

            // When this is the latest live transfer of the stay, put the stay
            // back on the company it came from. Older transfers leave the stay
            // untouched, because a newer transfer already owns it.
            var newer = await _db.CompanyTransfers.AnyAsync(t =>
                t.PatientEncounterId == transfer.PatientEncounterId && !t.Retired && t.Id > transfer.Id);
            if (!newer && transfer.PatientEncounterId.HasValue)
            {
                var encounter = await _db.PatientEncounters
                    .FirstOrDefaultAsync(e => e.Id == transfer.PatientEncounterId.Value && !e.Retired);
                if (encounter != null)
                {
                    encounter.CreditCompanyId = transfer.FromCompanyId;
                    encounter.EditerId = _session.WebUserId;
                    encounter.EditedAt = DateTime.Now;
                }
            }

            await _db.SaveChangesAsync();

            Success = "Company transfer retired.";
            return RedirectToPage(new { Q, Type, ShowRetired });
        }

        private async Task LoadAsync()
        {
            // companies (legacy COMPANY master)
            var companies = await _db.CompanyMasters.AsNoTracking()
                .Where(c => c.Status == null || c.Status != "I")
                .OrderBy(c => c.Name)
                .ToListAsync();

            CompanyList = companies.Select(c => new SelectListItem
            {
                Value = c.Id,
                Text = $"{c.Id} - {c.Name}"
            }).ToList();

            WardList = await _db.Wards.AsNoTracking()
                .Where(w => !w.Retired)
                .OrderBy(w => w.Code)
                .Select(w => new SelectListItem { Value = w.Code, Text = $"{w.Code} - {w.Name}" })
                .ToListAsync();

            RelationList = await _db.Categories.AsNoTracking()
                .OfType<Relation>()
                .Where(r => !r.Retired)
                .OrderBy(r => r.Name)
                .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                .ToListAsync();

            // employees (all active, filtered client-side by company)
            var employees = await _db.CompanyEmployees.AsNoTracking()
                .Where(e => !e.Retired)
                .OrderBy(e => e.EmployeeCode)
                .ToListAsync();
            // map company id (institution) -> company code
            var instMap = await _db.Institutions.AsNoTracking()
                .Where(i => i.Code != null)
                .ToDictionaryAsync(i => i.Id, i => i.Code);
            EmployeeList = employees.Select(e =>
            {
                var code = instMap.TryGetValue(e.CompanyId, out var c) ? c : "";
                return new SelectListItem
                {
                    Value = e.EmployeeCode ?? "",
                    Text = $"{e.EmployeeCode} - {e.Name} ({code})"
                };
            }).ToList();

            // patient search
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim();
                SearchResults = await _db.PatientEncounters
                    .Include(e => e.Patient).ThenInclude(p => p.Person)
                    .Where(e => !e.Retired
                                && e.DateOfDischarge == null
                                && ((e.BhtNo != null && e.BhtNo.Contains(term))
                                    || (e.Patient != null && e.Patient.Person != null
                                        && e.Patient.Person.Name != null
                                        && e.Patient.Person.Name.Contains(term))))
                    .OrderByDescending(e => e.DateOfAdmission)
                    .Take(20)
                    .ToListAsync();
            }

            if (SelectedEncounterId.HasValue)
            {
                SelectedEncounter = await _db.PatientEncounters
                    .Include(e => e.Patient).ThenInclude(p => p.Person)
                    .FirstOrDefaultAsync(e => e.Id == SelectedEncounterId.Value && !e.Retired);
                if (SelectedEncounter != null)
                {
                    TransferEncounterId = SelectedEncounter.Id;
                    TransferDate = TransferDate ?? DateTime.Today;
                    // from company code
                    if (SelectedEncounter.CreditCompanyId.HasValue)
                    {
                        var inst = await _db.Institutions.FirstOrDefaultAsync(i => i.Id == SelectedEncounter.CreditCompanyId.Value);
                        FromCompanyCode = inst?.Code;
                        FromCompanyInstitution = inst;
                    }
                }
            }

            // transfer list
            var query = _db.CompanyTransfers.AsNoTracking().AsQueryable();

            if (!ShowRetired)
                query = query.Where(t => !t.Retired);

            if (!string.IsNullOrWhiteSpace(Type))
                query = query.Where(t => t.Type == Type.Trim());

            if (!string.IsNullOrWhiteSpace(Q))
            {
                var term = Q.Trim();
                query = query.Where(t =>
                    (t.RegNo != null && t.RegNo.Contains(term))
                    || (t.FromCompanyCode != null && t.FromCompanyCode.Contains(term))
                    || (t.ToCompanyCode != null && t.ToCompanyCode.Contains(term))
                    || (t.EmployeeCode != null && t.EmployeeCode.Contains(term)));
            }

            TotalCount = await query.CountAsync();
            ClampPaging();

            Transfers = await ApplyPaging(query
                .OrderByDescending(t => t.TransferDate)
                .ThenByDescending(t => t.Id))
                .ToListAsync();
        }
    }
}
