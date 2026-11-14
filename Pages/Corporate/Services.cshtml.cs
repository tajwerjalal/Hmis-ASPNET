using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Corporate
{
    public class ServicesModel : Shared.PagedPageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _appSession;

        public ServicesModel(ApplicationDbContext db, IAppSession appSession)
        {
            _db = db;
            _appSession = appSession;
        }

        public List<ServiceCard> Services { get; set; } = new();
        public List<SelectListItem> DepartmentList { get; set; } = new();
        public List<SelectListItem> ItemTypeList { get; set; } = new();
        public int FilteredCount { get; set; }

        public string AutoCode { get; set; }
        public Dictionary<string, int> NextCodeMap { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public long? FilterDepartmentId { get; set; }

        /// <summary>
        /// Department filter on the shared master (legacy DEPT code). The old
        /// ITEM.DepartmentId filter matched nothing, because the migrated and
        /// the mirrored rows carry no department id.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string FilterDept { get; set; }

        /// <summary>Department options of the shared master (code + name).</summary>
        public List<SelectListItem> DeptFilterList { get; set; } = new();

        /// <summary>All billable services (the unfiltered counter).</summary>
        public int AllCount { get; set; }

        [BindProperty(SupportsGet = true)]
        public string FilterItemType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Q { get; set; }

        [BindProperty]
        public ItemInputModel FormItem { get; set; } = new();

        [BindProperty]
        public long EditId { get; set; }

        /// <summary>Service code to legacy department code (shared master).</summary>
        private Dictionary<string, string> deptByCode = new(StringComparer.OrdinalIgnoreCase);

        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }

        public class ServiceCard
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public string Code { get; set; }
            public string DepartmentName { get; set; }
            public long? DepartmentId { get; set; }
            public string ItemType { get; set; }
            public double? BaseRate { get; set; }
            public double? StaffRate { get; set; }
            public double? UnitRate { get; set; }
            public bool? ConsultantRequired { get; set; }
            public bool? CorporateActive { get; set; }
            public string Requirements { get; set; }
            public string MinDuration { get; set; }
            public string MaxDuration { get; set; }
            public string Source { get; set; }
            public bool DiscountAllowed { get; set; }
            public int CompanyRateCount { get; set; }
        }

        public class ItemInputModel
        {
            public string Name { get; set; }
            public string Sname { get; set; }
            public string Code { get; set; }
            public double? Total { get; set; }
            public double? StaffRate { get; set; }
            public double? UnitRate { get; set; }
            public string Requirements { get; set; }
            public bool? ConsultantRequired { get; set; }
            public string MinDuration { get; set; }
            public string MaxDuration { get; set; }
            public string Source { get; set; }
            public bool? CorporateActive { get; set; }
            public ItemType? ItemType { get; set; }
            public long? DepartmentId { get; set; }
            public bool DiscountAllowed { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (string.IsNullOrWhiteSpace(FormItem.Name))
            {
                ErrorMessage = "Service name is required.";
                await LoadAsync();
                return Page();
            }

            var nextCode = await GenerateServiceCodeAsync(FormItem.DepartmentId, FormItem.ItemType);

            var entity = new Item
            {
                Name = FormItem.Name?.Trim() ?? "",
                Sname = FormItem.Sname?.Trim(),
                Code = nextCode,
                Total = FormItem.Total ?? 0,
                StaffRate = FormItem.StaffRate,
                UnitRate = FormItem.UnitRate,
                Requirements = FormItem.Requirements?.Trim(),
                ConsultantRequired = FormItem.ConsultantRequired,
                MinDuration = FormItem.MinDuration?.Trim(),
                MaxDuration = FormItem.MaxDuration?.Trim(),
                Source = FormItem.Source?.Trim(),
                CorporateActive = FormItem.CorporateActive,
                ItemType = FormItem.ItemType,
                DepartmentId = FormItem.DepartmentId,
                DiscountAllowed = FormItem.DiscountAllowed,
                Billable = true,
                InstitutionId = _appSession.LoggedSite?.Id,
                CreaterId = _appSession.WebUserId,
                CreatedAt = DateTime.Now,
                Retired = false
            };

            _db.Items.Add(entity);
            await _db.SaveChangesAsync();

            SuccessMessage = $"Service \"{entity.Name}\" created with code {nextCode}.";
            FormItem = new ItemInputModel();
            await LoadAsync();
            return Page();
        }

        private async Task<string> GenerateServiceCodeAsync(long? departmentId, ItemType? itemType)
        {
            string prefix;
            if (itemType == ItemType.Investigation)
            {
                if (departmentId.HasValue)
                {
                    var dept = await _db.Departments.FindAsync(departmentId.Value);
                    var deptName = (dept?.Name ?? "").ToUpper();
                    if (deptName.Contains("RADIO") || deptName.Contains("X-RAY") || deptName.Contains("XRAY"))
                        prefix = "RAD";
                    else if (deptName.Contains("ULTRASOUND") || deptName.Contains("USG") || deptName.Contains(" US"))
                        prefix = "USG";
                    else if (deptName.Contains("CT"))
                        prefix = "CTS";
                    else if (deptName.Contains("MRI"))
                        prefix = "MRI";
                    else if (deptName.Contains("ECG") || deptName.Contains("EEG"))
                        prefix = "ECG";
                    else
                        prefix = "INV";
                }
                else
                    prefix = "INV";
            }
            else if (itemType == ItemType.Service)
                prefix = "SER";
            else
                prefix = "ITM";

            var lastCode = await _db.Items
                .Where(i => i.Code != null && i.Code.StartsWith(prefix + "-") && !i.Retired)
                .OrderByDescending(i => i.Code)
                .Select(i => i.Code)
                .FirstOrDefaultAsync();

            if (lastCode != null && int.TryParse(lastCode.Substring(prefix.Length + 1), out var lastNum))
                return $"{prefix}-{(lastNum + 1):D3}";

            return $"{prefix}-001";
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            var item = await _db.Items
                .FirstOrDefaultAsync(i => i.Id == EditId && !i.Retired);

            if (item == null)
            {
                ErrorMessage = "Service not found.";
                await LoadAsync();
                return Page();
            }

            if (string.IsNullOrWhiteSpace(FormItem.Name))
            {
                FormItem.Code = item.Code;
                ErrorMessage = "Service name is required.";
                await LoadAsync();
                return Page();
            }

            item.Name = FormItem.Name?.Trim() ?? item.Name;
            item.Sname = FormItem.Sname?.Trim();
            item.Total = FormItem.Total ?? item.Total;
            item.StaffRate = FormItem.StaffRate;
            item.UnitRate = FormItem.UnitRate;
            item.Requirements = FormItem.Requirements?.Trim();
            item.ConsultantRequired = FormItem.ConsultantRequired;
            item.MinDuration = FormItem.MinDuration?.Trim();
            item.MaxDuration = FormItem.MaxDuration?.Trim();
            item.Source = FormItem.Source?.Trim();
            item.CorporateActive = FormItem.CorporateActive;
            item.ItemType = FormItem.ItemType;
            item.DepartmentId = FormItem.DepartmentId;
            item.DiscountAllowed = FormItem.DiscountAllowed;
            item.EditerId = _appSession.WebUserId;
            item.EditedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            SuccessMessage = $"Service \"{item.Name}\" updated successfully.";
            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostRetireAsync(long id)
        {
            var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == id && !i.Retired);
            if (item == null)
            {
                ErrorMessage = "Service not found.";
                await LoadAsync();
                return Page();
            }

            item.Retired = true;
            item.RetiredAt = DateTime.Now;
            item.RetirerId = _appSession.WebUserId;
            item.RetireComments = "Retired from Corporate Services page.";

            await _db.SaveChangesAsync();

            SuccessMessage = $"Service \"{item.Name}\" has been retired.";
            await LoadAsync();
            return Page();
        }

        private async Task LoadAsync()
        {
            await LoadNextCodesAsync();

            DepartmentList = await _db.Departments
                .Where(d => !d.Retired)
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .ToListAsync();

            DeptFilterList = DeptServiceScopes.AllDepts
                .Select(d => new SelectListItem
                {
                    Value = d,
                    Text = $"{DeptServiceScopes.HospitalDeptName(d)} ({d})"
                })
                .ToList();
            ItemTypeList = Enum.GetValues(typeof(ItemType))
                .Cast<ItemType>()
                .Select(t => new SelectListItem { Value = t.ToString(), Text = t.ToString() })
                .ToList();

            var query = _db.Items
                .Where(i => i.Billable && !i.Retired);

            // department filter on the shared master: the rows of a department
            // are the legacy service codes of that DEPT plus their billing
            // mirrors (INV-LB-01001 belongs to department 01, and so on)
            if (!string.IsNullOrWhiteSpace(FilterDept) && DeptServiceScopes.AllDepts.Contains(FilterDept))
            {
                // the department rows are the legacy service codes of that DEPT
                // plus their billing mirrors (INV-LB-01001 -> department 01).
                // The codes are resolved in memory and matched with an IN list:
                // a correlated EXISTS over the unindexable ITEM.Code column
                // times out on the 6,800 row master.
                var dept = FilterDept;
                var prefix = DeptBillingMirrorService.PrefixFor(dept);
                var plainCodes = await _db.DeptServices
                    .Where(s => s.Dept == dept && s.Code != null)
                    .Select(s => s.Code)
                    .ToListAsync();

                var allowed = plainCodes
                    .Select(c => c)
                    .Concat(plainCodes.Select(c => prefix + c))
                    .ToList();

                query = query.Where(i => i.Code != null && allowed.Contains(i.Code));
            }

            if (FilterDepartmentId.HasValue)
                query = query.Where(i => i.DepartmentId == FilterDepartmentId.Value);
            if (!string.IsNullOrWhiteSpace(FilterItemType) && Enum.TryParse<ItemType>(FilterItemType, out var parsedType))
                query = query.Where(i => i.ItemType == parsedType);
            if (!string.IsNullOrWhiteSpace(Q))
            {
                var term = Q.Trim().ToLower();
                query = query.Where(i =>
                    (i.Name != null && i.Name.ToLower().Contains(term))
                    || (i.Code != null && i.Code.ToLower().Contains(term))
                    || (i.Sname != null && i.Sname.ToLower().Contains(term)));
            }
            AllCount = await _db.Items.CountAsync(i => i.Billable && !i.Retired);
            FilteredCount = await query.CountAsync();
            TotalCount = FilteredCount;
            ClampPaging();

            var list = await ApplyPaging(query
                .OrderBy(i => i.Department != null ? i.Department.Name : "")
                .ThenBy(i => i.Name))
                .ToListAsync();

            // department of a row, resolved from the shared master (the plain
            // service code or the code behind the billing mirror)
            deptByCode = (await _db.DeptServices
                .Where(s => s.Code != null)
                .Select(s => new { s.Code, s.Dept })
                .ToListAsync())
                .GroupBy(s => s.Code)
                .ToDictionary(g => g.Key, g => g.First().Dept, StringComparer.OrdinalIgnoreCase);

            var itemIds = list.Select(i => (long?)i.Id).ToList();

            var rateCounts = await _db.CompanyServiceRates
                .Where(r => itemIds.Contains(r.ItemId) && !r.Retired)
                .GroupBy(r => r.ItemId)
                .Select(g => new { ItemId = g.Key, Count = g.Count() })
                .ToListAsync();

            var rateMap = rateCounts.ToDictionary(r => r.ItemId, r => r.Count);

            Services = list.Select(i => new ServiceCard
            {
                Id = i.Id,
                Name = i.Name ?? "",
                Code = i.Code ?? "",
                DepartmentName = ResolveDeptName(i.Code, i.Department?.Name),
                DepartmentId = i.DepartmentId,
                ItemType = i.ItemType?.ToString() ?? "-",
                BaseRate = i.Total,
                StaffRate = i.StaffRate,
                UnitRate = i.UnitRate,
                ConsultantRequired = i.ConsultantRequired,
                CorporateActive = i.CorporateActive,
                Requirements = i.Requirements ?? "",
                MinDuration = i.MinDuration ?? "",
                MaxDuration = i.MaxDuration ?? "",
                Source = i.Source ?? "",
                DiscountAllowed = i.DiscountAllowed == true,
                CompanyRateCount = rateMap.ContainsKey(i.Id) ? rateMap[i.Id] : 0
            }).ToList();

            FilteredCount = Services.Count;
        }

        /// <summary>
        /// Department of a service row: the plain legacy service code or the
        /// code behind the billing mirror (INV-LB-01001 -> 01) is looked up in
        /// the shared master.
        /// </summary>
        private string ResolveDeptName(string code, string fallback)
        {
            if (string.IsNullOrWhiteSpace(code)) return fallback ?? "-";

            if (deptByCode.TryGetValue(code, out var dept))
                return $"{DeptServiceScopes.HospitalDeptName(dept)} ({dept})";

            foreach (var (prefix, _) in DeptServiceScopes.MirrorPrefixes)
            {
                if (code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    var rest = code.Substring(prefix.Length);
                    if (deptByCode.TryGetValue(rest, out var mirrorDept))
                        return $"{DeptServiceScopes.HospitalDeptName(mirrorDept)} ({mirrorDept})";
                    break;
                }
            }

            return fallback ?? "-";
        }
        private async Task LoadNextCodesAsync()
        {
            var prefixes = new[] { "INV", "SER", "ITM", "RAD", "USG", "CTS", "MRI", "ECG" };
            foreach (var prefix in prefixes)
            {
                var lastCode = await _db.Items
                    .Where(i => i.Code != null && i.Code.StartsWith(prefix + "-") && !i.Retired)
                    .OrderByDescending(i => i.Code)
                    .Select(i => i.Code)
                    .FirstOrDefaultAsync();

                var nextNum = 1;
                if (lastCode != null && int.TryParse(lastCode.Substring(prefix.Length + 1), out var lastNum))
                    nextNum = lastNum + 1;

                NextCodeMap[prefix] = nextNum;
            }

            AutoCode = $"ITM-{NextCodeMap["ITM"]:D3}";
        }
    }
}
