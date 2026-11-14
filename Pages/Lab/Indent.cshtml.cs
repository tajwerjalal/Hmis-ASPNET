using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Models.Legacy;
using HmisWeb.Services;

namespace HmisWeb.Pages.Lab
{
    public class IndentModel : PageModel
    {
        private const string CartKey = "LabIndentCart";
        private const string PatientKey = "LabIndentPatientId";
        private const string RegNoKey = "LabIndentRegNo";
        private const string LastBillKey = "LabIndentLastBillId";
        private readonly ApplicationDbContext _db;
        private readonly ICommonService _common;
        private readonly IAppSession _appSession;
        public IndentModel(ApplicationDbContext db, ICommonService common, IAppSession appSession) { _db = db; _common = common; _appSession = appSession; }

        [BindProperty] public string PatientSearch { get; set; }
        [BindProperty] public string Comments { get; set; }
        [BindProperty(SupportsGet = true)] public long? SelectedPatientId { get; set; }

        /// <summary>
        /// Set by the OPD registration page after a patient is added from the
        /// lab "Add New Patient" button. Selects the patient for a fresh indent.
        /// </summary>
        [BindProperty(SupportsGet = true)] public long? SelectPatientId { get; set; }
        [BindProperty] public List<IndentCartItem> Cart { get; set; } = new();

        public Patient SelectedPatient { get; set; }
        public PatientLegacyInfo LegacyPatient { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string Success { get; set; }
        public List<LegacyPatientRow> SearchResults { get; set; } = new();
        [BindProperty] public string SelectedRegNo { get; set; }
        public List<DeptService> DeptServiceResults { get; set; } = new();
        public List<Item> ItemResults { get; set; } = new();
        public List<IndentCartItem> DisplayCart { get; set; } = new();
        public List<PatientEncounter> ActiveEncounters { get; set; } = new();
        [BindProperty]
        public long? SelectedEncounterId { get; set; }

        // ===== Previous Indents state for the history card =====
        [BindProperty] public string HistoryDateFrom { get; set; }
        [BindProperty] public string HistoryDateTo { get; set; }
        [BindProperty(SupportsGet = true)] public string HistorySearch { get; set; }
        [BindProperty(SupportsGet = true)] public int HistoryPage { get; set; }

        public Bill SelectedBill { get; set; }
        public class LegacyIndentHistoryItem
        {
            public long Id { get; set; }
            public string LabNo { get; set; }
            public DateTime? BillDate { get; set; }
            public DateTime? CreatedAt { get; set; }
            public string PatientName { get; set; }
            public string RegNo { get; set; }
            public int ItemsCount { get; set; }
            public double NetTotal { get; set; }
            public bool Paid { get; set; }
        }

        public List<LegacyIndentHistoryItem> HistoryRows { get; set; } = new();
        public int HistoryTotalCount { get; set; }
        public int HistoryCurrentPage { get; set; } = 1;
        public int HistoryPageSize { get; set; } = 10;
        public bool HistoryHasPrevious { get; set; }
        public bool HistoryHasNext { get; set; }
        public string HistoryErrorMessage { get; set; }

        public PatientEncounter SelectedEncounter { get; set; }
        public List<Bill> UnpaidBills { get; set; } = new();
        public bool HasUnpaidBills => UnpaidBills.Any();
        /// <summary>Last saved indent Bill Id (session-cached) - enables the Print button.</summary>
        public long? LastBillId { get; set; }
        public bool HasSavedIndent => LastBillId.HasValue;
        public double Total { get; set; }
        public double DiscountTotal { get; set; }
        public double NetTotal { get; set; }
        public Department LabDepartment { get; set; }
        [BindProperty(SupportsGet = true)] public string ItemSearch { get; set; }
        [BindProperty(SupportsGet = true)] public bool OpenItemModal { get; set; }
        [BindProperty] public long? SelectedItemId { get; set; }
        [BindProperty] public long? ItemToRemove { get; set; }
        [BindProperty] public string NewName { get; set; }
        [BindProperty] public string NewNameWithInitials { get; set; }
        [BindProperty] public string NewNic { get; set; }
        [BindProperty] public string NewMobile { get; set; }
        [BindProperty] public string NewPhone { get; set; }
        [BindProperty] public Sex? NewSex { get; set; }
        public LabInvoiceModel Invoice { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Returned from OPD registration after adding a new patient: select
            // the patient and start a fresh indent.
            if (SelectPatientId.HasValue && SelectPatientId.Value > 0)
            {
                await SelectPatientForNewRegistrationAsync(SelectPatientId.Value);
                return RedirectToPage("/Lab/Indent");
            }

            await LoadStateAsync();
            return Page();
        }

        /// <summary>
        /// Selects a patient that was just created on the OPD registration
        /// screen and clears the previous indent.
        /// </summary>
        private async Task SelectPatientForNewRegistrationAsync(long patientId)
        {
            var patient = await _db.Patients
                .FirstOrDefaultAsync(p => p.Id == patientId && !p.Retired);
            if (patient == null) return;

            HttpContext.Session.SetString(PatientKey, patient.Id.ToString());
            HttpContext.Session.Remove(RegNoKey);
            HttpContext.Session.Remove(CartKey);
            HttpContext.Session.Remove(LastBillKey);
        }

        private void ComputeTotals()
        {
            Total = Math.Round(DisplayCart.Sum(c => c.GrossValue), 2);
            DiscountTotal = Math.Round(DisplayCart.Sum(c => c.Discount), 2);
            NetTotal = Math.Round(Total - DiscountTotal, 2);
            if (NetTotal < 0) NetTotal = 0;
        }

        private async Task LoadLookupsAsync()
        {
            LabDepartment = await _db.Departments.FirstOrDefaultAsync(d => !d.Retired && d.DepartmentType == DepartmentType.Laboratory);
            if (LabDepartment == null) LabDepartment = _appSession.LoggedDepartment ?? await _db.Departments.Where(d => !d.Retired).OrderBy(d => d.Name).FirstOrDefaultAsync();
            DeptServiceResults = await _db.DeptServices.Where(d => d.Dept == "01" && !d.Retired && d.Billable).OrderBy(d => d.Name).Take(100).ToListAsync();
        }

        private async Task LoadStateAsync()
        {
            await LoadLookupsAsync();
            SelectedPatient = await GetSelectedPatientAsync();
            DisplayCart = GetCart();
            Cart = DisplayCart;
            LastBillId = long.TryParse(HttpContext.Session.GetString(LastBillKey), out var lb) ? lb : (long?)null;
            if (SelectedPatient != null)
            {
                await _common.EnrichLegacyNamesAsync(new[] { SelectedPatient });
                LegacyPatient = await _common.GetPatientLegacyInfoAsync(SelectedPatient);
                if (string.IsNullOrWhiteSpace(SelectedRegNo))
                    SelectedRegNo = HttpContext.Session.GetString(RegNoKey);
                if (string.IsNullOrWhiteSpace(SelectedRegNo))
                    SelectedRegNo = LegacyPatient?.RegNo;
                UnpaidBills = await _db.Bills
                    .Where(b => b.PatientId == SelectedPatient.Id && !b.Retired && !b.Cancelled && !b.Paid)
                    .OrderByDescending(b => b.CreatedAt).ToListAsync();
            }
            else { LegacyPatient = null; UnpaidBills = new(); }
            ComputeTotals();
            await LoadHistoryStateAsync();
        }

        /// <summary>
        /// Loads previous indents (paid and unpaid) for the history card.
        /// Supports a free-text search over indent number, invoice number,
        /// patient registration number and patient name.
        /// </summary>
        private async Task LoadHistoryStateAsync()
        {
            HistoryCurrentPage = Math.Max(1, HistoryPage);
            using var conn = new Microsoft.Data.SqlClient.SqlConnection(_db.Database.GetDbConnection().ConnectionString);
            await conn.OpenAsync();

            // Build WHERE from search term
            var whereClauses = new List<string> { "1=1" };
            var parameters = new List<Microsoft.Data.SqlClient.SqlParameter>();

            if (!string.IsNullOrWhiteSpace(HistorySearch))
            {
                whereClauses.Add("(LABNO LIKE @search OR PCODE LIKE @search OR NAME LIKE @search OR NAME LIKE @search2)");
                parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@search", "%" + HistorySearch.Trim() + "%"));
                parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@search2", "%" + HistorySearch.Trim() + "%"));
            }

            if (DateTime.TryParse(HistoryDateFrom, out var fd))
            {
                whereClauses.Add("DATE >= @from");
                parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@from", fd.Date));
            }
            if (DateTime.TryParse(HistoryDateTo, out var td))
            {
                whereClauses.Add("DATE <= @to");
                parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@to", td.Date.AddDays(1).AddTicks(-1)));
            }

            var whereStr = string.Join(" AND ", whereClauses);

            // Count total
            var countSql = $"SELECT COUNT(*) FROM [HMIS].[dbo].[REPORT] WHERE {whereStr}";
            using var countCmd = new Microsoft.Data.SqlClient.SqlCommand(countSql, conn);
            countCmd.Parameters.AddRange(parameters.ToArray());
            HistoryTotalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

            var skip = (HistoryCurrentPage - 1) * HistoryPageSize;
            var sql = $@"SELECT LABNO, DATE, TIME, NAME, PCODE, NET, POSTED FROM [HMIS].[dbo].[REPORT] WHERE {whereStr} ORDER BY DATE DESC, LABNO DESC OFFSET @skip ROWS FETCH NEXT @pageSize ROWS ONLY";

            using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn);
            cmd.Parameters.AddRange(parameters.ToArray());
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@skip", skip));
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@pageSize", HistoryPageSize));

            // Load test counts per labno using TESTTR
            var rows = new List<LegacyIndentHistoryItem>();
            using var r = await cmd.ExecuteReaderAsync();
            var labNos = new List<string>();
            while (await r.ReadAsync())
            {
                var labNo = r.IsDBNull(0) ? null : r.GetString(0).Trim();
                if (string.IsNullOrWhiteSpace(labNo)) continue;
                labNos.Add(labNo);
                rows.Add(new LegacyIndentHistoryItem
                {
                    LabNo = labNo,
                    Id = long.TryParse(labNo, out var lid) ? lid : 0,
                    BillDate = r.IsDBNull(1) ? (DateTime?)null : r.GetDateTime(1),
                    CreatedAt = r.IsDBNull(1) ? (DateTime?)null : r.GetDateTime(1),
                    PatientName = r.IsDBNull(3) ? null : r.GetString(3).Trim(),
                    RegNo = r.IsDBNull(4) ? null : r.GetString(4).Trim(),
                    NetTotal = r.IsDBNull(5) ? 0 : Convert.ToDouble(r.GetValue(5)),
                    Paid = string.Equals(r.IsDBNull(6) ? null : r.GetString(6), "N", StringComparison.OrdinalIgnoreCase) == false
                });
            }

            // Count tests per labno from TESTTR
            if (labNos.Any())
            {
                var inList = string.Join(",", labNos.Select((_, i) => "@p" + i));
                var testSql = $"SELECT LABNO, COUNT(*) FROM [HMIS].[dbo].[TESTTR] WHERE LABNO IN ({inList}) GROUP BY LABNO";
                using var testCmd = new Microsoft.Data.SqlClient.SqlCommand(testSql, conn);
                for (int i = 0; i < labNos.Count; i++) testCmd.Parameters.AddWithValue("@p" + i, labNos[i]);
                using var tr = await testCmd.ExecuteReaderAsync();
                var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                while (await tr.ReadAsync())
                {
                    var ln = tr.IsDBNull(0) ? null : tr.GetString(0).Trim();
                    if (ln != null) counts[ln] = tr.IsDBNull(1) ? 0 : tr.GetInt32(1);
                }
                foreach (var item in rows)
                {
                    if (counts.TryGetValue(item.LabNo, out var c)) item.ItemsCount = c;
                }
            }

            HistoryRows = rows;
            HistoryHasPrevious = HistoryCurrentPage > 1;
            HistoryHasNext = (HistoryCurrentPage - 1) * HistoryPageSize < HistoryTotalCount;
        }

        public async Task<IActionResult> OnPostSearchPatientsAsync()
        {
            await LoadStateAsync();
            SearchResults = await _common.SearchLegacyPatientsAsync(PatientSearch);
            return Page();
        }

        public async Task<IActionResult> OnPostSelectPatientAsync()
        {
            if (SelectedPatientId == null || SelectedPatientId <= 0) return RedirectToPage();
            var patient = await _db.Patients.FirstOrDefaultAsync(p => p.Id == SelectedPatientId && !p.Retired);
            if (patient == null) return RedirectToPage();
            HttpContext.Session.SetString(PatientKey, patient.Id.ToString());
            if (!string.IsNullOrWhiteSpace(SelectedRegNo))
                HttpContext.Session.SetString(RegNoKey, SelectedRegNo.Trim());
            else HttpContext.Session.Remove(RegNoKey);
            return RedirectToPage("/Lab/Indent");
        }

        public async Task<IActionResult> OnPostAddItemAsync()
        {
            await LoadStateAsync();
            if (SelectedItemId == null || SelectedItemId <= 0) return RedirectToPage("/Lab/Indent");
            var svc = await _db.DeptServices.FirstOrDefaultAsync(d => d.Id == SelectedItemId && d.Dept == "01" && !d.Retired && d.Billable);
            if (svc == null) return RedirectToPage("/Lab/Indent");
            var cart = GetCart();
            cart.Add(new IndentCartItem
            {
                Code = svc.Code,
                ItemName = svc.Name ?? svc.Sname ?? svc.Code,
                Qty = 1,
                Rate = (double)(svc.Rate ?? svc.RateForeigner ?? 0),
                Discount = 0,
                FromDeptService = true
            });
            SaveCart(cart);
            return RedirectToPage("/Lab/Indent");
        }

        /// <summary>
        /// Loads an existing indent for viewing/editing by Bill Id.
        /// Navigates to /lab/SampleCollection where the indent can be viewed or edited
        /// if it's an unposted, unpaid indent.
        /// </summary>
        public async Task<IActionResult> OnPostViewBillAsync([FromForm] long BillId)
        {
            // Check if this indent can be edited (unposted, unpaid)
            var bill = await _db.Bills.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == BillId && !b.Retired);
            
            if (bill == null)
            {
                ErrorMessage = "Indent not found.";
                return RedirectToPage();
            }
            
            // Determine if this indent is editable
            // First check if it has legacy REPORT entry to see if it's posted
            using var conn = new Microsoft.Data.SqlClient.SqlConnection(_db.Database.GetDbConnection().ConnectionString);
            await conn.OpenAsync();
            using var cmd = new Microsoft.Data.SqlClient.SqlCommand("SELECT TOP 1 LABNO, POSTED FROM [HMIS].[dbo].[REPORT] WHERE LABNO = @labNo AND POSTED = 'N'", conn);
            cmd.Parameters.AddWithValue("@labNo", bill.LocalNumber ?? "");
            using var reader = await cmd.ExecuteReaderAsync();
            LegacyReportRow legacyReport = null;
            if (await reader.ReadAsync())
            {
                legacyReport = new LegacyReportRow
                {
                    LABNO = reader.IsDBNull(0) ? null : reader.GetString(0).Trim(),
                    POSTED = reader.IsDBNull(1) ? null : reader.GetString(1).Trim()
                };
            }
            
            var isEditable = !bill.Paid && legacyReport != null;
            
            // Navigate to SampleCollection page with billId and mode
            // The SampleCollection page will determine if it should be view-only or editable
            var mode = isEditable ? "edit" : "view";
            return RedirectToPage("/Lab/SampleCollection", new { 
                billId = BillId, 
                mode = mode 
            });
        }
        
        private class LegacyReportRow
        {
            public string LABNO { get; set; }
            public string POSTED { get; set; }
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            await LoadStateAsync();
            var patientId = GetSelectedPatientId();
            var cart = GetCart();
            if (patientId == null) { Message = "Please select a patient."; return Page(); }
            if (!cart.Any()) { Message = "Add at least one service."; return Page(); }
            var patient = await _db.Patients.Include(p => p.Person).FirstOrDefaultAsync(p => p.Id == patientId && !p.Retired);
            if (patient == null) { Message = "Patient not found."; return Page(); }
            double total = Math.Round(cart.Sum(c => c.Qty * c.Rate), 2);
            double itemDiscount = Math.Round(cart.Sum(c => c.Discount), 2);
            double netTotal = Math.Max(total - itemDiscount, 0);
            var bill = new Bill
            {
                BillType = BillType.OpdBill,
                PatientId = patient.Id,
                PersonId = patient.PersonId,
                CreaterId = _appSession.WebUserId,
                CreatedAt = DateTime.Now,
                BillDate = DateTime.Now,
                GrantTotal = total,
                Total = total,
                NetTotal = netTotal,
                Discount = itemDiscount,
                Balance = netTotal,
                Paid = false,
                Comments = (Comments ?? "")
            };
            _db.Bills.Add(bill);
            await _db.SaveChangesAsync();
            int serial = 1;
            foreach (var c in cart)
            {
                var gross = Math.Round(c.Qty * c.Rate, 2);
                bill.BillItems.Add(new BillItem
                {
                    Qty = c.Qty, Rate = c.Rate, NetRate = c.Rate,
                    GrossValue = gross, Discount = c.Discount > 0 ? c.Discount : Math.Round(gross - c.NetValue, 2),
                    NetValue = Math.Round(gross - (c.Discount > 0 ? c.Discount : Math.Round(gross - c.NetValue, 2)), 2),
                    Descreption = c.ItemName, DeptId = "01", SearialNo = serial++,
                    ItemId = c.Code, CreatedAt = DateTime.Now, CreaterId = _appSession.WebUserId
                });
            }
            await _db.SaveChangesAsync();

            // ===== Legacy Labsys DBF save (master + detail) using real REPORT / TESTTR schema =====
            var labNo = (!string.IsNullOrWhiteSpace(SelectedRegNo) ? SelectedRegNo.Trim() : (bill.Id.ToString().PadLeft(9, '0')));
            using (var conn = new Microsoft.Data.SqlClient.SqlConnection(_db.Database.GetDbConnection().ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                    "INSERT INTO [HMIS].[dbo].[REPORT] (LABNO,DATE,TIME,CID,HRNO,TYPE,PCODE,CONCODE,SEX,AGE,ATYPE,NAME,BEDNO,PHONE,CONS,CONSBY,CREDITBY,CREDITNO,BALANCE,REFUND,RDATE,REMARKS,TOTAL,NET,CREDIT,POSTED,TRCVD,TPYMT,USERID,UDATE,UTIME) VALUES (@labNo,@date,@time,@cid,@hrNo,@type,@pcode,@concode,@sex,@age,@atype,@name,@bedno,@phone,@cons,@consby,@creditby,@creditno,@balance,@refund,@rdate,@remarks,@total,@net,@credit,@posted,@trcvd,@tpymt,@uid,@udate,@utime); SELECT SCOPE_IDENTITY();", conn))
                {
                    cmd.Parameters.AddWithValue("@labNo", labNo.Length > 9 ? labNo.Substring(0, 9) : labNo);
                    cmd.Parameters.AddWithValue("@date", bill.BillDate != null ? Convert.ToDateTime(bill.BillDate).Date : DateTime.Now.Date);
                    cmd.Parameters.AddWithValue("@time", DateTime.Now.ToString("HH:mm"));
                    cmd.Parameters.AddWithValue("@cid", (patient.PersonId?.ToString() ?? "").Length > 4 ? (patient.PersonId?.ToString() ?? "").Substring(0, 4) : (patient.PersonId?.ToString() ?? ""));
                    cmd.Parameters.AddWithValue("@hrNo", ("01").Length > 7 ? ("01").Substring(0, 7) : "01");
                    cmd.Parameters.AddWithValue("@type", ("O").Length > 3 ? ("O").Substring(0, 3) : "O");
                    cmd.Parameters.AddWithValue("@pcode", (patient.Code ?? "").Length > 10 ? (patient.Code ?? "").Substring(0, 10) : (patient.Code ?? ""));
                    cmd.Parameters.AddWithValue("@concode", ("").Length > 3 ? ("").Substring(0, 3) : "");
                    cmd.Parameters.AddWithValue("@sex", (LegacyPatient?.Sex ?? "").Length > 6 ? (LegacyPatient?.Sex ?? "").Substring(0, 6) : (LegacyPatient?.Sex ?? ""));
                    cmd.Parameters.AddWithValue("@age", LegacyPatient != null ? (int.TryParse(LegacyPatient.Age?.ToString(), out var ageInt) ? (ageInt > 999 ? 999 : ageInt) : 0) : 0);
                    cmd.Parameters.AddWithValue("@atype", "OPD");
                    cmd.Parameters.AddWithValue("@name", (patient.LegacyName ?? patient.Phn ?? "").Length > 25 ? (patient.LegacyName ?? patient.Phn ?? "").Substring(0, 25) : (patient.LegacyName ?? patient.Phn ?? ""));
                    cmd.Parameters.AddWithValue("@bedno", ("").Length > 8 ? ("").Substring(0, 8) : "");
                    cmd.Parameters.AddWithValue("@phone", (patient.PatientPhoneNumber?.ToString() ?? patient.PatientMobileNumber?.ToString() ?? "").Length > 15 ? (patient.PatientPhoneNumber?.ToString() ?? patient.PatientMobileNumber?.ToString() ?? "").Substring(0, 15) : (patient.PatientPhoneNumber?.ToString() ?? patient.PatientMobileNumber?.ToString() ?? ""));
                    cmd.Parameters.AddWithValue("@cons", 0m);
                    cmd.Parameters.AddWithValue("@consby", (patient.PersonId?.ToString() ?? "").Length > 30 ? (patient.PersonId?.ToString() ?? "").Substring(0, 30) : (patient.PersonId?.ToString() ?? ""));
                    cmd.Parameters.AddWithValue("@creditby", (_appSession.LoggedDepartment != null ? _appSession.LoggedDepartment.Id.ToString() : "").Length > 11 ? (_appSession.LoggedDepartment != null ? _appSession.LoggedDepartment.Id.ToString() : "").Substring(0, 11) : (_appSession.LoggedDepartment != null ? _appSession.LoggedDepartment.Id.ToString() : ""));
                    cmd.Parameters.AddWithValue("@creditno", (_appSession.WebUserId?.ToString() ?? "").Length > 10 ? (_appSession.WebUserId?.ToString() ?? "").Substring(0, 10) : (_appSession.WebUserId?.ToString() ?? ""));
                    cmd.Parameters.AddWithValue("@balance", Math.Round(netTotal, 2));
                    cmd.Parameters.AddWithValue("@refund", 0m);
                    cmd.Parameters.AddWithValue("@rdate", bill.BillDate != null ? Convert.ToDateTime(bill.BillDate) : DateTime.Now);
                    cmd.Parameters.AddWithValue("@remarks", ((Comments ?? "").Trim()).Length > 500 ? (Comments ?? "").Trim().Substring(0, 500) : (Comments ?? "").Trim());
                    cmd.Parameters.AddWithValue("@total", total > 999999 ? 999999m : (total < -999999 ? -999999m : Math.Round((decimal)total, 0)));
                    cmd.Parameters.AddWithValue("@net", Math.Round(netTotal, 2) > 999999 ? 999999 : (Math.Round(netTotal, 2) < -999999 ? -999999 : Math.Round((decimal)netTotal, 0)));
                    cmd.Parameters.AddWithValue("@credit", 0m);
                    cmd.Parameters.AddWithValue("@posted", "N");
                    cmd.Parameters.AddWithValue("@trcvd", 0m);
                    cmd.Parameters.AddWithValue("@tpymt", 0m);
                    cmd.Parameters.AddWithValue("@uid", (_appSession.WebUserId?.ToString() ?? "").Length > 10 ? (_appSession.WebUserId?.ToString() ?? "").Substring(0, 10) : (_appSession.WebUserId?.ToString() ?? ""));
                    cmd.Parameters.AddWithValue("@udate", DateTime.Now.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@utime", DateTime.Now.ToString("HH:mm").Length > 5 ? DateTime.Now.ToString("HH:mm").Substring(0, 5) : DateTime.Now.ToString("HH:mm"));
                    await cmd.ExecuteScalarAsync();
                }
                int seq = 1;
                foreach (var c in cart)
                {
                    using (var cmd2 = new Microsoft.Data.SqlClient.SqlCommand(
                        "INSERT INTO [HMIS].[dbo].[TESTTR] (LABNO,TID,TESTNO,CHARGES,TCDATE,REMARKS,URGENT,LID,GID,TNAME,CULTURE,SRNO,REPODT,USERID,UDATE,UTIME) VALUES (@labNo,@tid,@testNo,@charges,@tcdate,@remarks,@urgent,@lid,@gid,@tname,@culture,@srno,@repodt,@uid,@udate,@utime)", conn))
                    {
                        cmd2.Parameters.AddWithValue("@labNo", labNo.Length > 9 ? labNo.Substring(0, 9) : labNo);
                        cmd2.Parameters.AddWithValue("@tid", c.Code ?? "");
                        cmd2.Parameters.AddWithValue("@testNo", seq++.ToString("D3"));
                        cmd2.Parameters.AddWithValue("@charges", Math.Round(c.Qty * c.Rate, 2));
                        cmd2.Parameters.AddWithValue("@tcdate", DateTime.Now.ToString("yyyy-MM-dd"));
                        cmd2.Parameters.AddWithValue("@remarks", c.ItemName ?? "");
                        cmd2.Parameters.AddWithValue("@urgent", " ");
                        cmd2.Parameters.AddWithValue("@lid", "");
                        cmd2.Parameters.AddWithValue("@gid", "");
                        cmd2.Parameters.AddWithValue("@tname", c.ItemName ?? "");
                        cmd2.Parameters.AddWithValue("@culture", 0);
                        cmd2.Parameters.AddWithValue("@srno", seq - 1);
                        cmd2.Parameters.AddWithValue("@repodt", (object)DBNull.Value);
                        cmd2.Parameters.AddWithValue("@uid", _appSession.WebUserId?.ToString() ?? "");
                        cmd2.Parameters.AddWithValue("@udate", DateTime.Now.ToString("yyyy-MM-dd"));
                        cmd2.Parameters.AddWithValue("@utime", DateTime.Now.ToString("HH:mm"));
                        await cmd2.ExecuteNonQueryAsync();
                    }
                }
            }

            HttpContext.Session.Remove(CartKey);
            HttpContext.Session.SetString(LastBillKey, bill.Id.ToString());
            LastBillId = bill.Id;
            Success = "Indent saved successfully. Master saved in REPORT and detail saved in TESTTR.";
            return Page();
        }

        private long? GetSelectedPatientId()
        {
            var val = HttpContext.Session.GetString(PatientKey);
            return long.TryParse(val, out var id) ? id : null;
        }

        private async Task<Patient> GetSelectedPatientAsync()
        {
            var id = GetSelectedPatientId();
            if (id == null) return null;
            return await _db.Patients.Include(p => p.Person).FirstOrDefaultAsync(p => p.Id == id && !p.Retired);
        }

        private List<IndentCartItem> GetCart()
        {
            var raw = HttpContext.Session.GetString(CartKey);
            if (string.IsNullOrWhiteSpace(raw)) return new List<IndentCartItem>();
            try { return JsonSerializer.Deserialize<List<IndentCartItem>>(raw) ?? new List<IndentCartItem>(); }
            catch { return new List<IndentCartItem>(); }
        }

        private async Task SearchItemsAsync()
        {
            var term = ItemSearch?.Trim();
            var q = _db.DeptServices.Where(d => d.Dept == "01" && !d.Retired && d.Billable);
            if (!string.IsNullOrWhiteSpace(term))
                q = q.Where(d => (d.Name != null && d.Name.Contains(term)) || (d.Sname != null && d.Sname.Contains(term)) || (d.Code != null && d.Code.Contains(term)));
            DeptServiceResults = await q.OrderBy(d => d.Name).Take(50).ToListAsync();
        }

        public async Task<IActionResult> OnPostSearchItemsAsync()
        {
            await LoadStateAsync();
            await SearchItemsAsync();
            OpenItemModal = true;
            return Page();
        }

        public async Task<IActionResult> OnGetSearchItemsAsync()
        {
            await LoadStateAsync();
            await SearchItemsAsync();
            OpenItemModal = true;
            return Page();
        }

        public async Task<IActionResult> OnPostStartNewAsync()
        {
            HttpContext.Session.Remove(PatientKey);
            HttpContext.Session.Remove(RegNoKey);
            HttpContext.Session.Remove(CartKey);
            HttpContext.Session.Remove(LastBillKey);
            LastBillId = null;
            return RedirectToPage("/Lab/Indent");
        }

        private void SaveCart(List<IndentCartItem> cart)
        {
            HttpContext.Session.SetString(CartKey, JsonSerializer.Serialize(cart));
        }

        public class IndentCartItem
        {
            public long ItemId { get; set; }
            public string Code { get; set; }
            public string ItemName { get; set; }
            public int Qty { get; set; }
            public double Rate { get; set; }
            public double Discount { get; set; }
            public bool FromDeptService { get; set; }
            public double GrossValue => Math.Round(Qty * Rate, 2);
            public double NetValue => Math.Round(GrossValue - Discount, 2);
        }
    }
}
