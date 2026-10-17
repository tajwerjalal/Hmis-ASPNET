using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Billing
{
    /// <summary>
    /// Department slip entry - mirrors the VFP Billsys per-department transaction forms
    /// (bs010101..150 outdoor / bs011001..070 indoor). One page parameterised by
    /// <c>Type</c> (opd|inward) and <c>Dept</c> (legacy 2-char department code).
    /// Creates and modifies PreBill slips (BILL / BILLITEM) that the OPD / IPD batch
    /// pages later aggregate into the final bill. Fields follow the VFP forms
    /// (patient code/name, registration no., company, slip no, slip date, accept/return,
    /// net amount; grid = service, description, resource, quantity, rate, amount).
    /// </summary>
    public class SlipEntryModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ICommonService _common;
        private readonly IAppSession _appSession;

        public SlipEntryModel(ApplicationDbContext db, ICommonService common, IAppSession appSession)
        {
            _db = db;
            _common = common;
            _appSession = appSession;
        }

        // Legacy department code -> display name + category code used to scope the item lookup.
        private static readonly Dictionary<string, (string Name, string CatCode)> Departments = new()
        {
            ["01"] = ("Laboratory", "LAB"),
            ["02"] = ("X-Ray / CT Scan", "02"),
            ["03"] = ("Ultra Sound", "03"),
            ["04"] = ("Dental Clinic", "04"),
            ["06"] = ("Operation Theatre", "06"),
            ["07"] = ("Hospitalization", "07"),
            ["10"] = ("Pharmacy", "10"),
            ["11"] = ("Consultancy & Procedure", "11"),
            ["12"] = ("Misc Expenses", "12"),
        };

        [BindProperty(SupportsGet = true)] public string Type { get; set; } = "opd";
        [BindProperty(SupportsGet = true)] public string Dept { get; set; } = "01";
        [BindProperty(SupportsGet = true)] public long? EncounterId { get; set; }

        [BindProperty] public string PatientSearch { get; set; }
        [BindProperty] public string ItemSearch { get; set; }
        [BindProperty] public string SlipNo { get; set; }
        [BindProperty] public DateTime? SlipDate { get; set; }
        [BindProperty] public string SlipType { get; set; }
        [BindProperty] public string Comments { get; set; }
        [BindProperty] public long? AddItemId { get; set; }
        [BindProperty] public long? AddResourceId { get; set; }

        public string DeptName { get; set; }
        public long? CategoryId { get; set; }
        public bool IsPharmacy => Dept == "10";
        public bool IsInward => string.Equals(Type, "inward", StringComparison.OrdinalIgnoreCase);

        public PatientEncounter Encounter { get; set; }
        public Bill EditingBill { get; set; }
        public List<PatientEncounter> EncounterResults { get; set; } = new();
        public List<Bill> SlipList { get; set; } = new();
        public List<DeptSummary> DeptSummaries { get; set; } = new();
        public List<Item> ItemResults { get; set; } = new();
        public List<Resource> Resources { get; set; } = new();
        public List<SlipLine> Lines { get; set; } = new();
        public string Message { get; set; }
        public string Success { get; set; }
        public double NetAmount => Math.Round(Lines.Sum(l => l.Qty * l.Rate), 2);

        public List<SelectListItem> DeptList => Departments
            .Select(d => new SelectListItem { Value = d.Key, Text = d.Value.Name, Selected = d.Key == Dept }).ToList();

        private string StateKey => $"SlipEntry_{Type}_{Dept}_{EncounterId}";

        public async Task OnGetAsync()
        {
            await LoadAsync();
            if (EditingBill != null)
            {
                SlipNo = EditingBill.ReferenceNumber;
                SlipDate = EditingBill.BillDate;
                SlipType = (EditingBill.BillType == BillType.OpdReturn || EditingBill.BillType == BillType.InwardReturn) ? "R" : "A";
                Comments = EditingBill.Comments;
            }
            else
            {
                SlipDate ??= DateTime.Today;
                SlipType ??= "A";
            }
        }

        public async Task<IActionResult> OnPostSearchPatientsAsync()
        {
            await LoadAsync();
            await SearchEncountersAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSelectEncounterAsync(long encounterId)
        {
            // Switch patient/encounter: start a fresh slip for the new context.
            HttpContext.Session.Remove($"SlipEntry_{Type}_{Dept}_{encounterId}");
            return RedirectToPage(new { Type, Dept, EncounterId = encounterId });
        }

        public async Task<IActionResult> OnPostSearchItemsAsync()
        {
            await LoadAsync();
            await SearchItemsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAddItemAsync()
        {
            await LoadAsync();
            if (AddItemId.HasValue)
            {
                var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == AddItemId.Value && !i.Retired);
                if (item != null)
                {
                    var companyId = await ResolveCompanyIdAsync();
                    var rate = await _common.GetAppliedRateAsync(item, companyId, Encounter?.OpdDoctorId, AddResourceId);
                    var state = GetState();
                    var line = state.Lines.FirstOrDefault(l => l.ItemId == item.Id && l.ResourceId == AddResourceId);
                    if (line != null)
                    {
                        line.Qty += 1;
                    }
                    else
                    {
                        state.Lines.Add(new SlipLine
                        {
                            ItemId = item.Id,
                            Code = item.Code,
                            ItemName = string.IsNullOrWhiteSpace(item.PrintName) ? item.Name : item.PrintName,
                            Rate = rate,
                            Qty = 1,
                            ResourceId = AddResourceId,
                            ResourceName = AddResourceId.HasValue
                                ? Resources.FirstOrDefault(r => r.Id == AddResourceId.Value)?.Name
                                : null
                        });
                    }
                    SetState(state);
                }
            }
            await SearchItemsAsync();
            ApplyState(GetState());
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateLineAsync(long itemId, long? resourceId, double qty, double rate)
        {
            await LoadAsync();
            var state = GetState();
            var line = state.Lines.FirstOrDefault(l => l.ItemId == itemId && l.ResourceId == resourceId);
            if (line != null)
            {
                line.Qty = qty > 0 ? qty : 1;
                line.Rate = rate >= 0 ? rate : 0;
                SetState(state);
            }
            ApplyState(state);
            return Page();
        }

        public async Task<IActionResult> OnPostRemoveLineAsync(long itemId, long? resourceId)
        {
            await LoadAsync();
            var state = GetState();
            state.Lines.RemoveAll(l => l.ItemId == itemId && l.ResourceId == resourceId);
            SetState(state);
            ApplyState(state);
            return Page();
        }

        public async Task<IActionResult> OnPostNewAsync()
        {
            await LoadAsync();
            SetState(new SlipState());
            return RedirectToPage(new { Type, Dept, EncounterId });
        }

        public async Task<IActionResult> OnPostEditAsync(long slipId)
        {
            await LoadAsync();
            var bill = await _db.Bills.Include(b => b.BillItems)
                .FirstOrDefaultAsync(b => b.Id == slipId && !b.Retired);
            if (bill == null)
                return RedirectToPage(new { Type, Dept, EncounterId });

            var ids = bill.BillItems.Where(i => !i.Retired && i.ItemId1.HasValue)
                .Select(i => i.ItemId1.Value).Distinct().ToList();
            var names = await _db.Items.Where(i => ids.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => string.IsNullOrWhiteSpace(i.PrintName) ? i.Name : i.PrintName);

            var state = new SlipState { EditingBillId = bill.Id };
            foreach (var it in bill.BillItems.Where(i => !i.Retired).OrderBy(i => i.SearialNo))
            {
                state.Lines.Add(new SlipLine
                {
                    ItemId = it.ItemId1 ?? 0,
                    Code = it.ItemId,
                    ItemName = !string.IsNullOrWhiteSpace(it.Descreption)
                        ? it.Descreption
                        : (it.ItemId1.HasValue ? names.GetValueOrDefault(it.ItemId1.Value) : null),
                    Qty = it.Qty ?? 1,
                    Rate = it.Rate
                });
            }
            SetState(state);
            return RedirectToPage(new { Type, Dept, EncounterId });
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            await LoadAsync();
            var state = GetState();

            if (Encounter == null)
            {
                Message = "Select a patient encounter first.";
                return Page();
            }
            if (string.IsNullOrWhiteSpace(SlipNo))
            {
                Message = "Please enter slip No.";
                return Page();
            }
            if (SlipDate == null)
            {
                Message = "Please enter slip date.";
                return Page();
            }
            if (state.Lines.Count == 0)
            {
                Message = "Add at least one item to the slip.";
                return Page();
            }
            var slipType = string.IsNullOrWhiteSpace(SlipType) ? "A" : SlipType;

            var now = DateTime.Now;
            var userId = _appSession.WebUserId;

            Bill bill = null;
            if (state.EditingBillId.HasValue)
            {
                bill = await _db.Bills.Include(b => b.BillItems)
                    .FirstOrDefaultAsync(b => b.Id == state.EditingBillId.Value
                        && b.PatientEncounterId == Encounter.Id && !b.Retired);
            }
            if (bill == null)
            {
                bill = new Bill { CreatedAt = now, CreaterId = userId };
                _db.Bills.Add(bill);
            }

            var code = string.Equals(Type, "inward", StringComparison.OrdinalIgnoreCase);
            bill.BillClassType = BillClassType.PreBill;
            bill.BillType = code
                ? (slipType == "R" ? BillType.InwardReturn : BillType.InwardBill)
                : (slipType == "R" ? BillType.OpdReturn : BillType.OpdBill);
            bill.PatientEncounterId = Encounter.Id;
            bill.PatientId = Encounter.PatientId;
            bill.PersonId = Encounter.Patient?.PersonId;
            bill.InstitutionId = _appSession.LoggedSite?.Id ?? Encounter.InstitutionId;
            bill.DepartmentId = _appSession.LoggedDepartment?.Id ?? Encounter.DepartmentId;
            bill.CreditCompanyId = Encounter.CreditCompanyId;
            bill.DeptId = Dept;
            bill.IpOpOrCc = code ? "IPD" : "OPD";
            bill.ReferenceNumber = SlipNo.Trim();
            bill.BillDate = SlipDate;
            bill.Comments = Comments;
            bill.BillTime = now;
            bill.EditedAt = now;
            bill.EditorId = userId;
            bill.Paid = false;
            bill.BillPaymentCompletelySettled = false;

            var total = Math.Round(state.Lines.Sum(l => l.Qty * l.Rate), 2);
            bill.Total = total;
            bill.NetTotal = total;
            bill.BillTotal = total;
            bill.GrantTotal = total;
            bill.Balance = total;

            await _db.SaveChangesAsync();

            foreach (var old in bill.BillItems.Where(i => !i.Retired).ToList())
            {
                old.Retired = true;
                old.RetiredAt = now;
                old.RetirerId = userId;
            }

            int serial = 1;
            foreach (var l in state.Lines)
            {
                var lineTotal = Math.Round(l.Qty * l.Rate, 2);
                _db.BillItems.Add(new BillItem
                {
                    BillId = bill.Id,
                    ItemId1 = l.ItemId > 0 ? l.ItemId : (long?)null,
                    ItemId = l.Code,
                    Descreption = l.ItemName,
                    Qty = l.Qty,
                    Rate = l.Rate,
                    NetRate = l.Rate,
                    GrossValue = lineTotal,
                    NetValue = lineTotal,
                    SessionDate = SlipDate?.Date,
                    SearialNo = serial++,
                    CreatedAt = now,
                    CreaterId = userId
                });
            }
            await _db.SaveChangesAsync();

            TempData["SlipSuccess"] = $"Slip {bill.ReferenceNumber} saved ({DeptName}, Rs {total:N2}).";
            SetState(new SlipState());
            return RedirectToPage(new { Type, Dept, EncounterId });
        }

        public async Task<IActionResult> OnPostDeleteAsync(long slipId)
        {
            await LoadAsync();
            var bill = await _db.Bills.Include(b => b.BillItems)
                .FirstOrDefaultAsync(b => b.Id == slipId && !b.Retired);
            if (bill != null)
            {
                var now = DateTime.Now;
                var userId = _appSession.WebUserId;
                bill.Retired = true;
                bill.Cancelled = true;
                bill.RetiredAt = now;
                bill.EditedAt = now;
                bill.RetirerId = userId;
                bill.RetireComments = "Deleted from department slip entry";
                foreach (var it in bill.BillItems.Where(i => !i.Retired))
                {
                    it.Retired = true;
                    it.RetiredAt = now;
                    it.RetirerId = userId;
                }
                await _db.SaveChangesAsync();
                TempData["SlipSuccess"] = $"Slip {bill.ReferenceNumber} deleted.";
            }
            var state = GetState();
            if (state.EditingBillId == slipId)
                SetState(new SlipState());
            return RedirectToPage(new { Type, Dept, EncounterId });
        }

        // ----- loading -----

        private async Task LoadAsync()
        {
            if (!Departments.ContainsKey(Dept ?? "")) Dept = "01";
            DeptName = Departments[Dept].Name;

            var catCode = Departments[Dept].CatCode;
            CategoryId = await _db.Categories
                .Where(c => !c.Retired && c.Id > 116 && c.Code == catCode)
                .Select(c => (long?)c.Id)
                .FirstOrDefaultAsync();

            Resources = IsPharmacy
                ? new List<Resource>()
                : await _db.Resources.Where(r => !r.Retired && r.IsActive).OrderBy(r => r.Name).ToListAsync();

            if (EncounterId.HasValue)
            {
                var expectedType = IsInward ? EncounterType.Inward : EncounterType.Opd;
                Encounter = await _db.PatientEncounters
                    .Include(e => e.Patient).ThenInclude(p => p.Person)
                    .Include(e => e.CreditCompany)
                    .FirstOrDefaultAsync(e => e.Id == EncounterId.Value && !e.Retired && e.EncounterType == expectedType);

                if (Encounter != null)
                {
                    SlipList = await _db.Bills
                        .Where(b => b.PatientEncounterId == Encounter.Id && !b.Retired
                            && b.BillClassType == BillClassType.PreBill && b.DeptId == Dept)
                        .OrderByDescending(b => b.Id)
                        .ToListAsync();

                    if (GetState().EditingBillId is long editId)
                        EditingBill = SlipList.FirstOrDefault(b => b.Id == editId);

                    var grouped = await _db.Bills
                        .Where(b => b.PatientEncounterId == Encounter.Id && !b.Retired
                            && b.BillClassType == BillClassType.PreBill)
                        .GroupBy(b => b.DeptId)
                        .Select(g => new { Dept = g.Key, Cnt = g.Count(), Amt = g.Sum(x => x.NetTotal) })
                        .ToListAsync();

                    DeptSummaries = Departments.Select(d =>
                    {
                        var g = grouped.FirstOrDefault(x => string.Equals(x.Dept, d.Key, StringComparison.OrdinalIgnoreCase));
                        return new DeptSummary
                        {
                            Code = d.Key,
                            Name = d.Value.Name,
                            SlipCount = g?.Cnt ?? 0,
                            Amount = g?.Amt ?? 0
                        };
                    }).ToList();
                }
            }

            Success = TempData["SlipSuccess"] as string;
            ApplyState(GetState());
        }

        private async Task SearchEncountersAsync()
        {
            var term = PatientSearch?.Trim();
            if (string.IsNullOrWhiteSpace(term)) return;

            var expectedType = IsInward ? EncounterType.Inward : EncounterType.Opd;
            var q = _db.PatientEncounters
                .Include(e => e.Patient).ThenInclude(p => p.Person)
                .Where(e => !e.Retired && e.EncounterType == expectedType);

            q = q.Where(e =>
                (e.BhtNo != null && e.BhtNo.Contains(term))
                || (e.ReferenceNo != null && e.ReferenceNo.Contains(term))
                || (e.Patient != null && e.Patient.Phn != null && e.Patient.Phn.Contains(term))
                || (e.Patient != null && e.Patient.Person != null && (
                    (e.Patient.Person.Name != null && e.Patient.Person.Name.Contains(term))
                    || (e.Patient.Person.NameWithInitials != null && e.Patient.Person.NameWithInitials.Contains(term))
                    || (e.Patient.Person.Nic != null && e.Patient.Person.Nic.Contains(term))
                    || (e.Patient.Person.Mobile != null && e.Patient.Person.Mobile.Contains(term)))));

            EncounterResults = await q.OrderByDescending(e => e.Id).Take(25).ToListAsync();
        }

        private async Task SearchItemsAsync()
        {
            var term = ItemSearch?.Trim();
            if (string.IsNullOrWhiteSpace(term) || CategoryId == null)
            {
                ItemResults = new List<Item>();
                return;
            }

            ItemResults = await _db.Items
                .Where(i => !i.Retired && i.CategoryId == CategoryId
                    && ((i.Name != null && i.Name.Contains(term))
                        || (i.Sname != null && i.Sname.Contains(term))
                        || (i.PrintName != null && i.PrintName.Contains(term))
                        || (i.Code != null && i.Code.Contains(term))))
                .OrderBy(i => i.Name)
                .Take(30)
                .ToListAsync();
        }

        private async Task<long?> ResolveCompanyIdAsync()
        {
            if (Encounter == null) return null;
            return Encounter.CreditCompanyId
                ?? await _common.GetPatientCompanyIdAsync(Encounter.Patient?.PersonId ?? 0);
        }

        // ----- state -----

        private SlipState GetState()
        {
            var raw = HttpContext.Session.GetString(StateKey);
            if (string.IsNullOrWhiteSpace(raw)) return new SlipState();
            try { return JsonSerializer.Deserialize<SlipState>(raw) ?? new SlipState(); }
            catch { return new SlipState(); }
        }

        private void SetState(SlipState state) => HttpContext.Session.SetString(StateKey, JsonSerializer.Serialize(state));

        private void ApplyState(SlipState state)
        {
            Lines = state.Lines ?? new List<SlipLine>();
        }

        public class DeptSummary
        {
            public string Code { get; set; }
            public string Name { get; set; }
            public int SlipCount { get; set; }
            public double Amount { get; set; }
            public bool HasSlips => SlipCount > 0;
        }

        public class SlipLine
        {
            public long ItemId { get; set; }
            public string Code { get; set; }
            public string ItemName { get; set; }
            public double Qty { get; set; }
            public double Rate { get; set; }
            public long? ResourceId { get; set; }
            public string ResourceName { get; set; }
        }

        private class SlipState
        {
            public long? EditingBillId { get; set; }
            public List<SlipLine> Lines { get; set; } = new();
        }
    }
}
