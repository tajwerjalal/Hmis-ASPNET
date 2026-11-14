// Developer : HMIS Assistant
// Date/Time : 30-Oct-2026 09:00
// Description : Fast sample-collection screen modelled on the legacy Labsys
//               form lb0101 (frmsampcol). One screen: find the patient by
//               registration number / name, add tests from SERVICES (dept 01)
//               and save the indent master into [HMIS].[dbo].[REPORT] and the
//               test lines into [HMIS].[dbo].[TESTTR].
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Models.Legacy;
using HmisWeb.Services;

namespace HmisWeb.Pages.Lab
{
    public class SampleCollectionModel : PageModel
    {
        private const string PatientKey = "LabSamplePatientId";
        private const string RegNoKey = "LabSampleRegNo";
        private const string PatientCodeKey = "LabSamplePatientCode";
        private const string CidKey = "LabSampleCid";
        private const string ConcodeKey = "LabSampleConcode";
        private const string LabNoKey = "LabSampleLabNo";
        private const string CollDateKey = "LabSampleCollDate";
        private const string CollTimeKey = "LabSampleCollTime";
        private const string CartKey = "LabSampleCart";
        private const string LastBillKey = "LabSampleLastBillId";
        private const string EditBillKey = "LabSampleEditBillId";
        private const string EditLabNoKey = "LabSampleEditLabNo";
        private const string EditModeKey = "LabSampleEditMode";

        private readonly ApplicationDbContext _db;
        private readonly ICommonService _common;
        private readonly IAppSession _appSession;

        public SampleCollectionModel(ApplicationDbContext db, ICommonService common, IAppSession appSession)
        {
            _db = db;
            _common = common;
            _appSession = appSession;
        }

        [BindProperty] public string PatientSearch { get; set; }
        [BindProperty(SupportsGet = true)] public long? SelectedPatientId { get; set; }

        /// <summary>
        /// Set by the OPD registration page after a patient is added from the
        /// lab "+" button. The patient is selected and the indent form is filled.
        /// </summary>
        [BindProperty(SupportsGet = true)] public long? SelectPatientId { get; set; }
        [BindProperty(SupportsGet = true)] public long? BillId { get; set; }
        [BindProperty(SupportsGet = true)] public string LabNo { get; set; }
        [BindProperty(SupportsGet = true)] public string Mode { get; set; }
        [BindProperty] public string SelectedRegNo { get; set; }
        [BindProperty] public string SelectedPatientCode { get; set; }
        [BindProperty] public string SelectedCid { get; set; }
        [BindProperty] public string SelectedConcode { get; set; }
        [BindProperty(SupportsGet = true)] public string ItemSearch { get; set; }
        [BindProperty] public string PatientType { get; set; } = "OPD";
        [BindProperty(SupportsGet = true)] public bool OpenItemModal { get; set; }
        [BindProperty(SupportsGet = true)] public bool OpenIndentModal { get; set; }
        [BindProperty(SupportsGet = true)] public string IndentSearch { get; set; }
        [BindProperty] public string LabNoInput { get; set; }
        [BindProperty] public DateTime? CollectionDate { get; set; }
        [BindProperty] public string CollectionTime { get; set; }
        [BindProperty] public long? SelectedItemId { get; set; }
        [BindProperty] public long? ItemToRemove { get; set; }
        [BindProperty] public List<SampleCartItem> Cart { get; set; } = new();
        [BindProperty] public string Comments { get; set; }
        [BindProperty] public string NewName { get; set; }
        [BindProperty] public string NewNameWithInitials { get; set; }
        [BindProperty] public string NewNic { get; set; }
        [BindProperty] public string NewMobile { get; set; }
        [BindProperty] public string NewPhone { get; set; }
        [BindProperty] public Sex? NewSex { get; set; }

        public Patient SelectedPatient { get; set; }
        public PatientLegacyInfo LegacyPatient { get; set; }
        public string PatientCode { get; set; }
        public VisitBillingInfo SelectedVisitBilling { get; set; }
        public List<LegacyPatientRow> SearchResults { get; set; } = new();
        public List<DeptService> DeptServiceResults { get; set; } = new();
        public List<SampleCartItem> DisplayCart { get; set; } = new();
        public List<IndentSearchRow> RecentIndents { get; set; } = new();
        public List<CompanyMaster> Companies { get; set; } = new();
        public List<ConsultantMaster> Consultants { get; set; } = new();
        public List<Resource> Resources { get; set; } = new();
        public string PatientTitle { get; set; }
        public string CompanyCode { get; set; }
        public string CompanyName { get; set; }
        public string ConsultCode { get; set; }
        public string ConsultName { get; set; }
        public string LabNoPreview { get; set; }
        public string CompanyDisplay { get; set; }
        public string ConsultDisplay { get; set; }
        public long? EditingBillId { get; set; }
        public bool IsEditing { get; set; }
        public bool IsViewOnly { get; set; }
        public long? LastBillId { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public string Success { get; set; }
        public double Total { get; set; }
        public double NetTotal { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Opened from the indent history card: hydrate the whole screen
            // (patient, company/consultant, indent no, date/time and tests)
            // from the selected bill before rendering.
            if (BillId.HasValue && BillId.Value > 0)
            {
                await LoadBillIntoSessionAsync(BillId.Value, Mode);
                return RedirectToPage("/Lab/SampleCollection");
            }
            if (!string.IsNullOrWhiteSpace(LabNo))
            {
                await LoadBillFromLabNoAsync(LabNo.Trim(), Mode);
                return RedirectToPage("/Lab/SampleCollection");
            }

            // Returned from OPD registration after adding a new patient: select
            // the patient and clear any previous indent before rendering.
            if (SelectPatientId.HasValue && SelectPatientId.Value > 0)
            {
                await SelectPatientForNewRegistrationAsync(SelectPatientId.Value);
                return RedirectToPage("/Lab/SampleCollection");
            }

            await LoadStateAsync();
            // The test lookup popup is opened with ?OpenItemModal=true; load the
            // SERVICES (dept 01) list so the modal is never empty.
            if (OpenItemModal)
                await SearchItemsAsync();

            if (OpenIndentModal)
                await SearchIndentsAsync();

            return Page();
        }

        private async Task LoadStateAsync()
        {
            if (string.IsNullOrEmpty(Message))
                Message = TempData["LabSampleNotice"] as string;

            SelectedPatient = await GetSelectedPatientAsync();
            DisplayCart = GetCart();
            Cart = DisplayCart;
            LastBillId = long.TryParse(HttpContext.Session.GetString(LastBillKey), out var lb) ? lb : (long?)null;

            EditingBillId = long.TryParse(HttpContext.Session.GetString(EditBillKey), out var eb) ? eb : (long?)null;
            IsEditing = EditingBillId.HasValue;
            IsViewOnly = string.Equals(HttpContext.Session.GetString(EditModeKey), "view", StringComparison.OrdinalIgnoreCase);
            var editLabNo = HttpContext.Session.GetString(EditLabNoKey);

            Companies = await _db.CompanyMasters.AsNoTracking()
                .Where(c => c.Status != "I")
                .OrderBy(c => c.Name)
                .ToListAsync();
            Consultants = await _db.ConsultantMasters.AsNoTracking()
                .Where(c => !c.Retired)
                .OrderBy(c => c.Name)
                .ToListAsync();
            Resources = await _db.Resources.AsNoTracking()
                .Where(r => !r.Retired && r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();

            if (SelectedPatient != null)
            {
                await _common.EnrichLegacyNamesAsync(new[] { SelectedPatient });
                LegacyPatient = await _common.GetPatientLegacyInfoAsync(SelectedPatient);

                // Patients registered by this application keep their demographics
                // in PERSON, not in the legacy PATIENT columns. Fill the gaps so
                // the patient master card is complete for recent registrations.
                if (LegacyPatient == null)
                    LegacyPatient = new PatientLegacyInfo();
                if (string.IsNullOrWhiteSpace(LegacyPatient.Name))
                    LegacyPatient.Name = PatientDisplayName.Of(SelectedPatient);
                if (string.IsNullOrWhiteSpace(LegacyPatient.Sex))
                    LegacyPatient.Sex = SelectedPatient.Person?.Sex?.ToString();
                if (string.IsNullOrWhiteSpace(LegacyPatient.Age))
                {
                    var age = SelectedPatient.Person?.GetAgeString();
                    if (!string.IsNullOrWhiteSpace(age))
                        LegacyPatient.Age = age;
                }
                if (string.IsNullOrWhiteSpace(LegacyPatient.Nic))
                    LegacyPatient.Nic = SelectedPatient.Person?.Nic;
                if (string.IsNullOrWhiteSpace(LegacyPatient.Phone1))
                    LegacyPatient.Phone1 = SelectedPatient.Person?.Phone ?? SelectedPatient.Person?.Mobile;
                if (string.IsNullOrWhiteSpace(LegacyPatient.Address))
                    LegacyPatient.Address = SelectedPatient.Person?.Address;

                // PATIENT.CODE is the permanent patient code; REGISTER.REGNO is
                // issued again on every visit. Keep both values separate so the
                // master card can show the patient code and the visit number.
                PatientCode = HttpContext.Session.GetString(PatientCodeKey);
                if (string.IsNullOrWhiteSpace(PatientCode))
                    PatientCode = !string.IsNullOrWhiteSpace(SelectedPatient.Code) ? SelectedPatient.Code
                                : !string.IsNullOrWhiteSpace(SelectedPatient.Phn) ? SelectedPatient.Phn
                                : SelectedPatient.PatientId?.ToString();

                if (string.IsNullOrWhiteSpace(SelectedRegNo))
                    SelectedRegNo = HttpContext.Session.GetString(RegNoKey);
                if (string.IsNullOrWhiteSpace(SelectedRegNo))
                    SelectedRegNo = LegacyPatient?.RegNo;

                // Complete billing for this visit: REGISTER joined to PATIENT on
                // p.CODE = r.PCODE. Loaded for the patient card's billing popup.
                SelectedVisitBilling = await LoadVisitBillingAsync(SelectedRegNo, SelectedPatient.Code, SelectedPatient.Id);
                if (SelectedVisitBilling != null)
                {
                    if (string.IsNullOrWhiteSpace(SelectedRegNo))
                        SelectedRegNo = SelectedVisitBilling.RegNo;
                    if (string.IsNullOrWhiteSpace(PatientCode))
                        PatientCode = SelectedVisitBilling.PatientCode;
                }
                if (string.IsNullOrWhiteSpace(LegacyPatient.RegNo))
                    LegacyPatient.RegNo = SelectedRegNo;

                PatientTitle = SelectedPatient.Person?.Title?.ToString()?.ToUpperInvariant();

                CompanyCode = HttpContext.Session.GetString(CidKey);
                if (string.IsNullOrWhiteSpace(CompanyCode))
                    CompanyCode = LegacyPatient?.Code; // legacy fallback (rarely a company)
                CompanyName = string.IsNullOrWhiteSpace(CompanyCode)
                    ? null
                    : Companies.FirstOrDefault(c => string.Equals(c.Id?.Trim(), CompanyCode.Trim(), StringComparison.OrdinalIgnoreCase))?.Name;
                CompanyDisplay = string.IsNullOrWhiteSpace(CompanyCode)
                    ? null
                    : (string.IsNullOrWhiteSpace(CompanyName) ? CompanyCode.Trim() : $"{CompanyName} {CompanyCode.Trim()}");

                ConsultCode = HttpContext.Session.GetString(ConcodeKey);
                ConsultName = string.IsNullOrWhiteSpace(ConsultCode)
                    ? null
                    : Consultants.FirstOrDefault(c => string.Equals(c.Code?.Trim(), ConsultCode.Trim(), StringComparison.OrdinalIgnoreCase))?.Name;
                ConsultDisplay = string.IsNullOrWhiteSpace(ConsultCode)
                    ? null
                    : (string.IsNullOrWhiteSpace(ConsultName) ? ConsultCode.Trim() : $"{ConsultName} {ConsultCode.Trim()}");

                if (IsEditing && !string.IsNullOrWhiteSpace(editLabNo))
                    LabNoPreview = editLabNo;
                else if (string.IsNullOrWhiteSpace(LabNoPreview))
                    LabNoPreview = await GenerateLabNoAsync();
            }
            else
            {
                LegacyPatient = null;
                // Auto-generate the indent number even before a patient is selected
                // so the first row is always filled and read-only.
                if (!IsEditing && string.IsNullOrWhiteSpace(LabNoPreview))
                    LabNoPreview = await GenerateLabNoAsync();
            }

            // Restore master fields the user entered before an "Update" post.
            if (string.IsNullOrWhiteSpace(LabNoInput))
                LabNoInput = HttpContext.Session.GetString(LabNoKey);
            if (CollectionDate == null
                && DateTime.TryParse(HttpContext.Session.GetString(CollDateKey), out var storedDate))
                CollectionDate = storedDate;
            if (string.IsNullOrWhiteSpace(CollectionTime))
                CollectionTime = HttpContext.Session.GetString(CollTimeKey);

            if (CollectionDate == null) CollectionDate = DateTime.Now.Date;
            if (string.IsNullOrWhiteSpace(CollectionTime)) CollectionTime = DateTime.Now.ToString("HH:mm");

            // Show the next legacy indent number in the text box; it is still
            // re-generated on save when left untouched, so it can never collide.
            // When editing an existing indent the stored number is shown instead.
            if (IsEditing && !string.IsNullOrWhiteSpace(editLabNo))
                LabNoInput = editLabNo;
            else if (string.IsNullOrWhiteSpace(LabNoInput))
                LabNoInput = LabNoPreview;

            Total = Math.Round(DisplayCart.Sum(c => c.GrossValue), 2);
            NetTotal = Math.Round(DisplayCart.Sum(c => c.NetValue), 2);
        }

        /// <summary>
        /// Hydrates the sample-collection session from an existing indent so the
        /// screen opens with that indent's patient, company, consultant, number,
        /// collection date/time and test lines. Edit is only allowed while the
        /// indent is unpaid and still unposted; otherwise it opens read-only.
        /// </summary>
        private async Task LoadBillIntoSessionAsync(long billId, string mode)
        {
            var bill = await _db.Bills.AsNoTracking()
                .Include(b => b.Patient).ThenInclude(p => p.Person)
                .Include(b => b.BillItems).ThenInclude(bi => bi.Item)
                .FirstOrDefaultAsync(b => b.Id == billId && !b.Retired);

            if (bill?.Patient == null) return;

            var info = await _common.GetPatientLegacyInfoAsync(bill.Patient);
            var legacy = await FindLegacyIndentAsync(bill, info);

            var isPosted = string.Equals(legacy?.Posted, "N", StringComparison.OrdinalIgnoreCase) == false
                           && legacy != null;
            var requestedEdit = string.Equals(mode, "edit", StringComparison.OrdinalIgnoreCase);
            var editable = requestedEdit && !bill.Paid && !isPosted;

            if (requestedEdit && !editable)
                TempData["LabSampleNotice"] = bill.Paid
                    ? "This indent is already paid, so it opened in view-only mode."
                    : "This indent has already been posted, so it opened in view-only mode.";

            var labNo = legacy?.LabNo;
            if (string.IsNullOrWhiteSpace(labNo)) labNo = bill.LocalNumber?.Trim();

            // Resource (LID) per test from the legacy detail rows.
            var lids = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(labNo))
            {
                try
                {
                    using var conn = new Microsoft.Data.SqlClient.SqlConnection(
                        _db.Database.GetDbConnection().ConnectionString);
                    await conn.OpenAsync();
                    using var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                        "SELECT TID, LID FROM [HMIS].[dbo].[TESTTR] WHERE LABNO=@labNo", conn);
                    cmd.Parameters.AddWithValue("@labNo", labNo);
                    using var r = await cmd.ExecuteReaderAsync();
                    while (await r.ReadAsync())
                    {
                        var tid = r.IsDBNull(0) ? null : r.GetString(0).Trim();
                        var lid = r.IsDBNull(1) ? null : r.GetString(1).Trim();
                        if (!string.IsNullOrWhiteSpace(tid)) lids[tid] = lid;
                    }
                }
                catch { /* legacy rows are optional */ }
            }

            var items = bill.BillItems?.Where(i => !i.Retired)
                .OrderBy(i => i.SearialNo).ThenBy(i => i.Id).ToList() ?? new List<BillItem>();
            var codes = items.Select(i => i.ItemId).Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim()).Distinct().ToList();
            var services = codes.Any()
                ? await _db.DeptServices.AsNoTracking()
                    .Where(d => d.Dept == "01" && codes.Contains(d.Code))
                    .ToListAsync()
                : new List<DeptService>();

            var cart = new List<SampleCartItem>();
            foreach (var bi in items)
            {
                var code = bi.ItemId?.Trim();
                var svc = services.FirstOrDefault(s => string.Equals(s.Code?.Trim(), code, StringComparison.OrdinalIgnoreCase));
                cart.Add(new SampleCartItem
                {
                    ItemId = svc?.Id ?? 0,
                    Code = code,
                    ItemName = bi.Descreption ?? bi.Item?.Name ?? svc?.Name ?? code,
                    Qty = (int)(bi.Qty ?? 1),
                    Rate = bi.Rate,
                    RateUrgent = bi.Rate,
                    Urgent = "N",
                    Lid = (code != null && lids.TryGetValue(code, out var lid) && !string.IsNullOrWhiteSpace(lid)) ? lid : "HM",
                    LidName = (code != null && lids.TryGetValue(code, out var l2) && !string.IsNullOrWhiteSpace(l2)) ? l2 : "Hospital",
                    TCDate = (legacy?.Date ?? bill.BillDate ?? DateTime.Now).Date
                });
            }

            var collDate = (legacy?.Date ?? bill.BillDate ?? bill.CreatedAt ?? DateTime.Now).Date;
            var collTime = !string.IsNullOrWhiteSpace(legacy?.Time)
                ? legacy.Time
                : (bill.BillDate ?? bill.CreatedAt ?? DateTime.Now).ToString("HH:mm");

            HttpContext.Session.Remove(LastBillKey);
            HttpContext.Session.SetString(PatientKey, bill.Patient.Id.ToString());

            if (!string.IsNullOrWhiteSpace(info?.RegNo))
                HttpContext.Session.SetString(RegNoKey, info.RegNo.Trim());
            else if (bill.Patient.PatientId != null)
                HttpContext.Session.SetString(RegNoKey, bill.Patient.PatientId.Value.ToString());
            else
                HttpContext.Session.Remove(RegNoKey);

            var patientCode = !string.IsNullOrWhiteSpace(bill.Patient.Code)
                ? bill.Patient.Code.Trim()
                : bill.Patient.PatientId?.ToString();
            if (!string.IsNullOrWhiteSpace(patientCode))
                HttpContext.Session.SetString(PatientCodeKey, patientCode);
            else
                HttpContext.Session.Remove(PatientCodeKey);

            if (!string.IsNullOrWhiteSpace(legacy?.Cid))
                HttpContext.Session.SetString(CidKey, legacy.Cid.Trim());
            else
                HttpContext.Session.Remove(CidKey);

            if (!string.IsNullOrWhiteSpace(legacy?.Concode))
                HttpContext.Session.SetString(ConcodeKey, legacy.Concode.Trim());
            else
                HttpContext.Session.Remove(ConcodeKey);

            if (!string.IsNullOrWhiteSpace(labNo))
                HttpContext.Session.SetString(LabNoKey, labNo.Trim());
            else
                HttpContext.Session.Remove(LabNoKey);

            HttpContext.Session.SetString(CollDateKey, collDate.ToString("yyyy-MM-dd"));
            HttpContext.Session.SetString(CollTimeKey, Trim(collTime, 5));

            HttpContext.Session.SetString(EditBillKey, bill.Id.ToString());
            HttpContext.Session.SetString(EditLabNoKey, labNo?.Trim() ?? "");
            HttpContext.Session.SetString(EditModeKey, editable ? "edit" : "view");

            SaveCart(cart);
        }

        public sealed class IndentSearchRow
        {
            public string LabNo { get; set; }
            public DateTime? Date { get; set; }
            public string Name { get; set; }
            public double Total { get; set; }
            public double Net { get; set; }
            public string Posted { get; set; }
        }

        private sealed class LegacyIndentRow
        {
            public string LabNo { get; set; }
            public DateTime? Date { get; set; }
            public string Time { get; set; }
            public string Cid { get; set; }
            public string Concode { get; set; }
            public string Posted { get; set; }
            public double Total { get; set; }
            public double Net { get; set; }
        }

        /// <summary>
        /// Finds the legacy REPORT master that belongs to a modern bill. New rows
        /// carry the LABNO in BILL.LocalNumber; older rows are matched on the
        /// patient code, collection date and net total.
        /// </summary>
        private async Task<LegacyIndentRow> FindLegacyIndentAsync(Bill bill, PatientLegacyInfo info)
        {
            try
            {
                using var conn = new Microsoft.Data.SqlClient.SqlConnection(
                    _db.Database.GetDbConnection().ConnectionString);
                await conn.OpenAsync();

                Microsoft.Data.SqlClient.SqlCommand cmd;
                var stored = bill.LocalNumber?.Trim();
                if (!string.IsNullOrWhiteSpace(stored))
                {
                    cmd = new Microsoft.Data.SqlClient.SqlCommand(
                        "SELECT TOP 1 LABNO,DATE,TIME,CID,CONCODE,POSTED,TOTAL,NET FROM [HMIS].[dbo].[REPORT] WHERE LABNO=@labNo", conn);
                    cmd.Parameters.AddWithValue("@labNo", stored);
                }
                else
                {
                    var pcode = Trim(info?.Code ?? bill.Patient?.Code, 10);
                    if (string.IsNullOrWhiteSpace(pcode)) return null;

                    var net = Math.Round(bill.NetTotal, 2);
                    var date = (bill.BillDate ?? bill.CreatedAt ?? DateTime.Now).Date;
                    cmd = new Microsoft.Data.SqlClient.SqlCommand(
                        "SELECT TOP 1 LABNO,DATE,TIME,CID,CONCODE,POSTED,TOTAL,NET FROM [HMIS].[dbo].[REPORT] " +
                        "WHERE PCODE=@pcode AND DATE=@date AND ABS(NET-@net)<0.01 ORDER BY UDATE DESC, UTIME DESC", conn);
                    cmd.Parameters.AddWithValue("@pcode", pcode);
                    cmd.Parameters.AddWithValue("@date", date);
                    cmd.Parameters.AddWithValue("@net", net);
                }

                using (cmd)
                using (var r = await cmd.ExecuteReaderAsync())
                {
                    if (!await r.ReadAsync()) return null;
                    return new LegacyIndentRow
                    {
                        LabNo = r.IsDBNull(0) ? null : r.GetString(0).Trim(),
                        Date = r.IsDBNull(1) ? null : r.GetDateTime(1),
                        Time = r.IsDBNull(2) ? null : r.GetString(2).Trim(),
                        Cid = r.IsDBNull(3) ? null : r.GetString(3).Trim(),
                        Concode = r.IsDBNull(4) ? null : r.GetString(4).Trim(),
                        Posted = r.IsDBNull(5) ? null : r.GetString(5).Trim(),
                        Total = r.IsDBNull(6) ? 0 : Convert.ToDouble(r.GetValue(6)),
                        Net = r.IsDBNull(7) ? 0 : Convert.ToDouble(r.GetValue(7))
                    };
                }
            }
            catch
            {
                return null;
            }
        }

        private async Task LoadBillFromLabNoAsync(string labNo, string mode)
        {
            // Try to find modern bill by legacy lab number
            var bill = await _db.Bills.AsNoTracking()
                .FirstOrDefaultAsync(b => b.LocalNumber != null && b.LocalNumber.Trim() == labNo && !b.Retired);
            if (bill != null)
            {
                await LoadBillIntoSessionAsync(bill.Id, mode);
                return;
            }

            // Fallback: load from REPORT / TESTTR by LABNO directly
            try
            {
                using var conn = new Microsoft.Data.SqlClient.SqlConnection(_db.Database.GetDbConnection().ConnectionString);
                await conn.OpenAsync();

                // Master from REPORT
                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                    "SELECT TOP 1 LABNO, DATE, TIME, CID, CONCODE, NAME, PCODE, POSTED, NET FROM [HMIS].[dbo].[REPORT] WHERE LABNO = @labNo", conn);
                cmd.Parameters.AddWithValue("@labNo", labNo);
                using var r = await cmd.ExecuteReaderAsync();
                if (!await r.ReadAsync()) return;

                var reportLabNo = r.IsDBNull(0) ? null : r.GetString(0).Trim();
                var collDate = r.IsDBNull(1) ? DateTime.Now.Date : r.GetDateTime(1);
                var collTime = r.IsDBNull(2) ? DateTime.Now.ToString("HH:mm") : r.GetString(2).Trim();
                var cid = r.IsDBNull(3) ? null : r.GetString(3).Trim();
                var concode = r.IsDBNull(4) ? null : r.GetString(4).Trim();
                var name = r.IsDBNull(5) ? null : r.GetString(5).Trim();
                var pcode = r.IsDBNull(6) ? null : r.GetString(6).Trim();

                var isPosted = string.Equals(r.IsDBNull(7) ? null : r.GetString(7).Trim(), "N", StringComparison.OrdinalIgnoreCase) == false;
                var requestedEdit = string.Equals(mode, "edit", StringComparison.OrdinalIgnoreCase);
                var editable = requestedEdit && !isPosted;

                HttpContext.Session.Remove(EditBillKey);
                HttpContext.Session.Remove(EditLabNoKey);
                HttpContext.Session.Remove(EditModeKey);
                HttpContext.Session.Remove(CartKey);

                // Try to find patient by pcode / legacy info
                Patient patient = null;
                if (!string.IsNullOrWhiteSpace(pcode))
                {
                    patient = await _db.Patients.Include(p => p.Person)
                        .FirstOrDefaultAsync(p => p.Code == pcode && !p.Retired);
                }
                if (patient != null)
                {
                    HttpContext.Session.SetString(PatientKey, patient.Id.ToString());
                    var info = await _common.GetPatientLegacyInfoAsync(patient);
                    if (!string.IsNullOrWhiteSpace(info?.RegNo))
                        HttpContext.Session.SetString(RegNoKey, info.RegNo.Trim());
                    else if (patient.PatientId != null)
                        HttpContext.Session.SetString(RegNoKey, patient.PatientId.Value.ToString());
                }
                else
                {
                    HttpContext.Session.Remove(PatientKey);
                    HttpContext.Session.Remove(RegNoKey);
                }

                if (!string.IsNullOrWhiteSpace(cid)) HttpContext.Session.SetString(CidKey, cid);
                else HttpContext.Session.Remove(CidKey);

                if (!string.IsNullOrWhiteSpace(concode)) HttpContext.Session.SetString(ConcodeKey, concode);
                else HttpContext.Session.Remove(ConcodeKey);

                if (!string.IsNullOrWhiteSpace(reportLabNo)) HttpContext.Session.SetString(LabNoKey, reportLabNo.Trim());
                else HttpContext.Session.Remove(LabNoKey);

                HttpContext.Session.SetString(CollDateKey, collDate.ToString("yyyy-MM-dd"));
                HttpContext.Session.SetString(CollTimeKey, Trim(collTime, 5));
                HttpContext.Session.SetString(EditLabNoKey, reportLabNo?.Trim() ?? "");
                HttpContext.Session.SetString(EditModeKey, editable ? "edit" : "view");

                // Build cart from TESTTR
                var lids = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                using (var cmd2 = new Microsoft.Data.SqlClient.SqlCommand(
                    "SELECT TID, LID FROM [HMIS].[dbo].[TESTTR] WHERE LABNO = @labNo", conn))
                {
                    cmd2.Parameters.AddWithValue("@labNo", reportLabNo);
                    using var tr = await cmd2.ExecuteReaderAsync();
                    while (await tr.ReadAsync())
                    {
                        var tid = tr.IsDBNull(0) ? null : tr.GetString(0).Trim();
                        var lid = tr.IsDBNull(1) ? null : tr.GetString(1).Trim();
                        if (!string.IsNullOrWhiteSpace(tid)) lids[tid] = lid;
                    }
                }

                using (var cmd3 = new Microsoft.Data.SqlClient.SqlCommand(
                    "SELECT TID, TNAME, CHARGES, TCDATE FROM [HMIS].[dbo].[TESTTR] WHERE LABNO = @labNo ORDER BY SRNO", conn))
                {
                    cmd3.Parameters.AddWithValue("@labNo", reportLabNo);
                    using var tr = await cmd3.ExecuteReaderAsync();
                    var cart = new List<SampleCartItem>();
                    while (await tr.ReadAsync())
                    {
                        var tid = tr.IsDBNull(0) ? null : tr.GetString(0).Trim();
                        var tname = tr.IsDBNull(1) ? null : tr.GetString(1).Trim();
                        var charges = tr.IsDBNull(2) ? 0 : Convert.ToDouble(tr.GetValue(2));
                        var tcdate = tr.IsDBNull(3) ? (DateTime?)null : tr.GetDateTime(3);
                        cart.Add(new SampleCartItem
                        {
                            ItemId = 0,
                            Code = tid,
                            ItemName = tname ?? tid,
                            Qty = 1,
                            Rate = charges,
                            RateUrgent = charges,
                            Urgent = "N",
                            Lid = (tid != null && lids.TryGetValue(tid, out var lid) && !string.IsNullOrWhiteSpace(lid)) ? lid : "HM",
                            LidName = (tid != null && lids.TryGetValue(tid, out var l2) && !string.IsNullOrWhiteSpace(l2)) ? l2 : "Hospital",
                            TCDate = (tcdate ?? collDate).Date
                        });
                    }
                    SaveCart(cart);
                }
            }
            catch { /* best-effort */ }
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
            else
                HttpContext.Session.Remove(RegNoKey);

            if (!string.IsNullOrWhiteSpace(SelectedPatientCode))
                HttpContext.Session.SetString(PatientCodeKey, SelectedPatientCode.Trim());
            else
                HttpContext.Session.Remove(PatientCodeKey);

            if (!string.IsNullOrWhiteSpace(SelectedCid))
                HttpContext.Session.SetString(CidKey, SelectedCid.Trim());
            else
                HttpContext.Session.Remove(CidKey);

            if (!string.IsNullOrWhiteSpace(SelectedConcode))
                HttpContext.Session.SetString(ConcodeKey, SelectedConcode.Trim());
            else
                HttpContext.Session.Remove(ConcodeKey);

            // Selecting a different patient always starts a fresh indent.
            HttpContext.Session.Remove(CartKey);
            HttpContext.Session.Remove(EditBillKey);
            HttpContext.Session.Remove(EditLabNoKey);
            HttpContext.Session.Remove(EditModeKey);

            return RedirectToPage("/Lab/SampleCollection");
        }

        /// <summary>
        /// Selects a patient that was just created on the OPD registration
        /// screen and starts a fresh indent for that patient.
        /// </summary>
        private async Task SelectPatientForNewRegistrationAsync(long patientId)
        {
            var patient = await _db.Patients
                .FirstOrDefaultAsync(p => p.Id == patientId && !p.Retired);
            if (patient == null) return;

            HttpContext.Session.SetString(PatientKey, patient.Id.ToString());
            HttpContext.Session.Remove(RegNoKey);
            HttpContext.Session.Remove(PatientCodeKey);
            HttpContext.Session.Remove(CidKey);
            HttpContext.Session.Remove(ConcodeKey);
            HttpContext.Session.Remove(CartKey);
            HttpContext.Session.Remove(EditBillKey);
            HttpContext.Session.Remove(EditLabNoKey);
            HttpContext.Session.Remove(EditModeKey);
        }

        public IActionResult OnPostStartNewAsync()
        {
            HttpContext.Session.Remove(PatientKey);
            HttpContext.Session.Remove(RegNoKey);
            HttpContext.Session.Remove(PatientCodeKey);
            HttpContext.Session.Remove(CidKey);
            HttpContext.Session.Remove(ConcodeKey);
            HttpContext.Session.Remove(LabNoKey);
            HttpContext.Session.Remove(CollDateKey);
            HttpContext.Session.Remove(CollTimeKey);
            HttpContext.Session.Remove(CartKey);
            HttpContext.Session.Remove(LastBillKey);
            HttpContext.Session.Remove(EditBillKey);
            HttpContext.Session.Remove(EditLabNoKey);
            HttpContext.Session.Remove(EditModeKey);
            return RedirectToPage("/Lab/SampleCollection");
        }

        /// <summary>Alias for the "Switch Patient" button in the patient card.</summary>
        public IActionResult OnPostSwitchPatientAsync() => OnPostStartNewAsync();

        /// <summary>
        /// Saves the company / consultant chosen in the patient profile card and
        /// re-prices the batch, exactly like lb0101 does when the panel changes.
        /// </summary>
        public async Task<IActionResult> OnPostUpdateContextAsync()
        {
            var postedCart = Cart;

            var oldCid = HttpContext.Session.GetString(CidKey) ?? "";
            var oldConcode = HttpContext.Session.GetString(ConcodeKey) ?? "";

            if (!string.IsNullOrWhiteSpace(SelectedCid))
                HttpContext.Session.SetString(CidKey, SelectedCid.Trim());
            else
                HttpContext.Session.Remove(CidKey);

            if (!string.IsNullOrWhiteSpace(SelectedConcode))
                HttpContext.Session.SetString(ConcodeKey, SelectedConcode.Trim());
            else
                HttpContext.Session.Remove(ConcodeKey);

            if (!string.IsNullOrWhiteSpace(LabNoInput))
                HttpContext.Session.SetString(LabNoKey, LabNoInput.Trim());
            else
                HttpContext.Session.Remove(LabNoKey);

            if (CollectionDate != null)
                HttpContext.Session.SetString(CollDateKey, CollectionDate.Value.ToString("yyyy-MM-dd"));
            else
                HttpContext.Session.Remove(CollDateKey);

            if (!string.IsNullOrWhiteSpace(CollectionTime))
                HttpContext.Session.SetString(CollTimeKey, CollectionTime.Trim());
            else
                HttpContext.Session.Remove(CollTimeKey);

            var contextChanged =
                !string.Equals(oldCid, SelectedCid?.Trim() ?? "", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(oldConcode, SelectedConcode?.Trim() ?? "", StringComparison.OrdinalIgnoreCase);

            var cart = (postedCart != null && postedCart.Any()) ? postedCart : GetCart();
            if (contextChanged && cart.Any())
            {
                var companyId = await ResolveCompanyIdAsync();

                foreach (var line in cart)
                {
                    var svc = await _db.DeptServices.AsNoTracking()
                        .FirstOrDefaultAsync(d => d.Code == line.Code && d.Dept == "01");
                    if (svc == null) continue;
                    var rate = await _common.GetAppliedRateAsync(svc, companyId);
                    if (rate <= 0) rate = (double)(svc.Rate ?? svc.RateForeigner ?? 0);
                    line.Rate = rate;
                }
            }
            SaveCart(cart);

            return RedirectToPage("/Lab/SampleCollection");
        }

        public async Task<IActionResult> OnPostCreatePatientAsync()
        {
            if (string.IsNullOrWhiteSpace(NewName))
            {
                ErrorMessage = "Patient name is required.";
                await LoadStateAsync();
                return Page();
            }

            var person = new Person
            {
                Name = NewName.Trim(),
                NameWithInitials = string.IsNullOrWhiteSpace(NewNameWithInitials) ? NewName.Trim() : NewNameWithInitials.Trim(),
                Nic = NewNic?.Trim() ?? "",
                Mobile = NewMobile?.Trim() ?? "",
                Phone = NewPhone?.Trim() ?? "",
                Sex = NewSex,
                CreatedAt = DateTime.Now,
                CreaterId = _appSession.WebUserId
            };
            _db.Persons.Add(person);
            await _db.SaveChangesAsync();

            long? maxPatientId = await _db.Patients.MaxAsync(p => (long?)p.PatientId);
            var patient = new Patient
            {
                PersonId = person.Id,
                PatientId = (maxPatientId ?? 0) + 1,
                CreatedAt = DateTime.Now,
                CreaterId = _appSession.WebUserId,
                CreatedInstitutionId = _appSession.LoggedSite?.Id
            };
            _db.Patients.Add(patient);
            await _db.SaveChangesAsync();

            HttpContext.Session.SetString(PatientKey, patient.Id.ToString());
            HttpContext.Session.Remove(RegNoKey);
            HttpContext.Session.Remove(PatientCodeKey);
            HttpContext.Session.Remove(CidKey);
            HttpContext.Session.Remove(ConcodeKey);
            HttpContext.Session.Remove(CartKey);
            HttpContext.Session.Remove(EditBillKey);
            HttpContext.Session.Remove(EditLabNoKey);
            HttpContext.Session.Remove(EditModeKey);
            return RedirectToPage("/Lab/SampleCollection");
        }

        public async Task<IActionResult> OnPostSearchItemsAsync()
        {
            await LoadStateAsync();
            await SearchItemsAsync();
            OpenItemModal = true;
            return Page();
        }

        /// <summary>
        /// "Add Test" button: persists the batch (so edited charges are not lost)
        /// and then opens the SERVICES lookup popup.
        /// </summary>
        public IActionResult OnPostOpenItemsAsync()
        {
            var cart = Cart;
            if (cart != null && cart.Any()) SaveCart(cart);
            return RedirectToPage("/Lab/SampleCollection", new { OpenItemModal = true });
        }

        public async Task<IActionResult> OnGetSearchItemsAsync()
        {
            await LoadStateAsync();
            await SearchItemsAsync();
            OpenItemModal = true;
            return Page();
        }

        private async Task SearchItemsAsync()
        {
            var term = ItemSearch?.Trim();
            // Lookup source is the legacy SERVICES master for department 01,
            // exactly like lb0101 (inactive / retired / non-billable rows and
            // rows without a description are not selectable).
            var q = _db.DeptServices.Where(d => d.Dept == "01" && !d.Retired && d.Billable
                                             && d.Name != null && d.Name.Trim() != "");
            if (!string.IsNullOrWhiteSpace(term))
                q = q.Where(d => (d.Name != null && d.Name.Contains(term))
                              || (d.Sname != null && d.Sname.Contains(term))
                              || (d.Code != null && d.Code.Contains(term)));
            DeptServiceResults = await q.OrderBy(d => d.Name).Take(50).ToListAsync();
        }

        private async Task SearchIndentsAsync()
        {
            var term = IndentSearch?.Trim() ?? "";
            var list = new List<IndentSearchRow>();
            try
            {
                using var conn = new Microsoft.Data.SqlClient.SqlConnection(_db.Database.GetDbConnection().ConnectionString);
                await conn.OpenAsync();
                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
SELECT TOP (60) LABNO, DATE, NAME, TOTAL, NET, POSTED
FROM [HMIS].[dbo].[REPORT]
WHERE (@term = '' OR LABNO LIKE @like OR NAME LIKE @like)
ORDER BY DATE DESC, LABNO DESC", conn);
                cmd.Parameters.AddWithValue("@term", term);
                cmd.Parameters.AddWithValue("@like", "%" + term + "%");
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new IndentSearchRow
                    {
                        LabNo = r.IsDBNull(0) ? null : r.GetString(0).Trim(),
                        Date = r.IsDBNull(1) ? null : r.GetDateTime(1),
                        Name = r.IsDBNull(2) ? null : r.GetString(2).Trim(),
                        Total = r.IsDBNull(3) ? 0 : Convert.ToDouble(r.GetValue(3)),
                        Net = r.IsDBNull(4) ? 0 : Convert.ToDouble(r.GetValue(4)),
                        Posted = r.IsDBNull(5) ? null : r.GetString(5).Trim()
                    });
                }
            }
            catch
            {
                // Best-effort; the modal will simply show no rows.
            }
            RecentIndents = list;
        }

        /// <summary>
        /// Complete billing for one visit. Reads the REGISTER row and joins the
        /// patient master exactly as the legacy screens did:
        /// <c>PATIENT p INNER JOIN REGISTER r ON p.CODE = r.PCODE</c>.
        /// The registration number is preferred; the patient code is the fallback
        /// so a patient card can show the latest visit when no explicit visit was
        /// selected.
        /// </summary>
        private async Task<VisitBillingInfo> LoadVisitBillingAsync(string regNo, string patientCode, long patientAppId = 0)
        {
            var reg = regNo?.Trim() ?? "";
            var code = patientCode?.Trim() ?? "";
            if (reg.Length == 0 && code.Length == 0) return null;

            try
            {
                using var conn = new Microsoft.Data.SqlClient.SqlConnection(
                    _db.Database.GetDbConnection().ConnectionString);
                await conn.OpenAsync();

                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
SELECT TOP (1)
     r.[REGNO]           AS [RegNo]
    ,r.[RDATE]           AS [RDate]
    ,r.[RTIME]           AS [RTime]
    ,r.[PCODE]           AS [PCode]
    ,p.[CODE]            AS [PatientCode]
    ,p.[NAME]            AS [Name]
    ,p.[SEX]             AS [Sex]
    ,p.[AGE]             AS [Age]
    ,r.[TYPE]            AS [Type]
    ,r.[CID]             AS [Cid]
    ,r.[CONCODE]         AS [Concode]
    ,r.[TBILLAMT]        AS [BillAmount]
    ,r.[TRCVD]           AS [Received]
    ,r.[TPYMT]           AS [Paid]
    ,r.[CASHDISC]        AS [Discount]
    ,r.[SETTLE]          AS [Settled]
    ,r.[BILLGEN]         AS [BillGenerated]
    ,r.[BILLSTATUS]      AS [BillStatus]
FROM [HMIS].[dbo].[REGISTER] r
INNER JOIN [HMIS].[dbo].[Patient] p ON p.[CODE] = r.[PCODE]
WHERE (@patientCode <> '' AND r.[PCODE] = @patientCode)
   OR (@patientCode = '' AND @regNo <> '' AND r.[REGNO] = @regNo)
ORDER BY CASE WHEN @regNo <> '' AND r.[REGNO] = @regNo THEN 0 ELSE 1 END,
         CASE WHEN @patientAppId <> 0 AND r.[pid] = @patientAppId THEN 0 ELSE 1 END,
         r.[RDATE] DESC, r.[REGNO] DESC", conn);
                cmd.CommandTimeout = 10;
                cmd.Parameters.AddWithValue("@regNo", reg);
                cmd.Parameters.AddWithValue("@patientCode", code);
                cmd.Parameters.AddWithValue("@patientAppId", patientAppId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) return null;

                string S(int i) => reader.IsDBNull(i) ? null : Convert.ToString(reader.GetValue(i))?.Trim();
                decimal? M(int i) => reader.IsDBNull(i) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(i));
                bool? B(int i) => reader.IsDBNull(i) ? (bool?)null : Convert.ToBoolean(reader.GetValue(i));
                byte? By(int i) => reader.IsDBNull(i) ? (byte?)null : Convert.ToByte(reader.GetValue(i));

                return new VisitBillingInfo
                {
                    RegNo = S(0),
                    RDate = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1),
                    RTime = S(2),
                    PCode = S(3),
                    PatientCode = S(4),
                    Name = S(5),
                    Sex = S(6),
                    Age = S(7),
                    Type = S(8),
                    Cid = S(9),
                    Concode = S(10),
                    BillAmount = M(11),
                    Received = M(12),
                    Paid = M(13),
                    Discount = M(14),
                    Settled = B(15),
                    BillGenerated = B(16),
                    BillStatus = By(17)
                };
            }
            catch
            {
                // Billing is supplementary information; the collection screen
                // must keep working even if the legacy counters cannot be read.
                return null;
            }
        }

        public async Task<IActionResult> OnPostAddItemAsync()
        {
            await LoadStateAsync();
            if (SelectedItemId == null || SelectedItemId <= 0) return RedirectToPage("/Lab/SampleCollection");

            var svc = await _db.DeptServices.FirstOrDefaultAsync(d => d.Id == SelectedItemId && d.Dept == "01" && !d.Retired && d.Billable);
            if (svc == null) return RedirectToPage("/Lab/SampleCollection");

            // Rate resolution follows the legacy lb0101 rule: company/panel rate
            // (COMPANYSERVICERATE mirror) first, then the SERVICES standard rate.
            var companyId = await ResolveCompanyIdAsync();

            var rate = await _common.GetAppliedRateAsync(svc, companyId);
            if (rate <= 0) rate = (double)(svc.Rate ?? svc.RateForeigner ?? 0);

            var cart = GetCart();
            cart.Add(new SampleCartItem
            {
                ItemId = svc.Id,
                Code = svc.Code?.Trim(),
                ItemName = (svc.Name ?? svc.Sname ?? svc.Code)?.Trim(),
                Qty = 1,
                Rate = rate,
                RateUrgent = (double)(svc.RateForeigner ?? svc.Rate ?? 0),
                Urgent = "N",
                Lid = "HM",
                LidName = "Hospital",
                TCDate = DateTime.Now
            });
            SaveCart(cart);
            return RedirectToPage("/Lab/SampleCollection");
        }

        public IActionResult OnPostRemoveItemAsync()
        {
            var cart = (Cart != null && Cart.Any()) ? Cart : GetCart();
            if (ItemToRemove.HasValue && ItemToRemove.Value >= 0 && ItemToRemove.Value < cart.Count)
                cart.RemoveAt((int)ItemToRemove.Value);
            SaveCart(cart);
            return RedirectToPage("/Lab/SampleCollection");
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            // Keep the posted (possibly edited) batch and master fields before
            // LoadStateAsync replaces Cart with the session copy.
            var postedCart = Cart;
            var postedLabNo = LabNoInput;
            var postedDate = CollectionDate;
            var postedTime = CollectionTime;

            await LoadStateAsync();

            if (IsViewOnly)
            {
                Message = "This indent is read-only. Use Edit on the indent history for an unposted, unpaid indent.";
                return Page();
            }

            if (!string.IsNullOrWhiteSpace(postedLabNo)) LabNoInput = postedLabNo;
            if (postedDate != null) CollectionDate = postedDate;
            if (!string.IsNullOrWhiteSpace(postedTime)) CollectionTime = postedTime;

            // The profile dropdowns are posted with the same form, so honour a
            // changed company / consultant even when "Update" was not clicked.
            if (!string.IsNullOrWhiteSpace(SelectedCid))
            {
                CompanyCode = SelectedCid.Trim();
                HttpContext.Session.SetString(CidKey, CompanyCode);
            }
            if (!string.IsNullOrWhiteSpace(SelectedConcode))
            {
                ConsultCode = SelectedConcode.Trim();
                HttpContext.Session.SetString(ConcodeKey, ConsultCode);
            }

            var patientId = GetSelectedPatientId();
            var cart = (postedCart != null && postedCart.Any()) ? postedCart : GetCart();
            if (patientId == null) { Message = "Please select a patient."; return Page(); }
            if (!cart.Any()) { Message = "Add at least one test."; return Page(); }

            var collDate = (CollectionDate ?? DateTime.Now).Date;
            var collTime = string.IsNullOrWhiteSpace(CollectionTime)
                ? DateTime.Now.ToString("HH:mm")
                : CollectionTime.Trim();

            // Editing an existing (unposted, unpaid) indent updates it in place.
            if (IsEditing && EditingBillId.HasValue)
                return await SaveEditedAsync(EditingBillId.Value, cart, collDate, collTime);

            var patient = await _db.Patients.Include(p => p.Person)
                .FirstOrDefaultAsync(p => p.Id == patientId && !p.Retired);
            if (patient == null) { Message = "Patient not found."; return Page(); }

            await _common.EnrichLegacyNamesAsync(new[] { patient });
            var legacy = await _common.GetPatientLegacyInfoAsync(patient);

            double total = Math.Round(cart.Sum(c => c.GrossValue), 2);
            double netTotal = Math.Round(cart.Sum(c => c.NetValue), 2);

            // Generate the legacy number first so the modern bill can carry it in
            // LocalNumber (the history screen uses it to reopen this indent).
            var labNo = await GenerateLabNoAsync();

            var billDate = collDate;
            if (TimeSpan.TryParse(collTime, out var billTimeSpan))
                billDate = collDate.Add(billTimeSpan);

            // ===== Modern bill (used by the printable invoice) =====
            var bill = new Bill
            {
                BillType = BillType.OpdBill,
                PatientId = patient.Id,
                PersonId = patient.PersonId,
                CreaterId = _appSession.WebUserId,
                CreatedAt = DateTime.Now,
                BillDate = billDate,
                LocalNumber = labNo,
                GrantTotal = total,
                Total = total,
                NetTotal = netTotal,
                Discount = 0,
                Balance = netTotal,
                Paid = false,
                Comments = (Comments ?? "")
            };
            _db.Bills.Add(bill);
            await _db.SaveChangesAsync();

            int serial = 1;
            foreach (var c in cart)
            {
                bill.BillItems.Add(new BillItem
                {
                    Qty = c.Qty,
                    Rate = c.EffectiveRate,
                    NetRate = c.EffectiveRate,
                    GrossValue = c.GrossValue,
                    Discount = 0,
                    NetValue = c.NetValue,
                    Descreption = c.ItemName,
                    DeptId = "01",
                    SearialNo = serial++,
                    ItemId = c.Code,
                    CreatedAt = DateTime.Now,
                    CreaterId = _appSession.WebUserId
                });
            }
            await _db.SaveChangesAsync();

            // ===== Legacy Labsys tables: REPORT (master) + TESTTR (detail) =====
            using (var conn = new Microsoft.Data.SqlClient.SqlConnection(_db.Database.GetDbConnection().ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                    "INSERT INTO [HMIS].[dbo].[REPORT] (LABNO,DATE,TIME,CID,HRNO,TYPE,PCODE,CONCODE,SEX,AGE,ATYPE,NAME,BEDNO,PHONE,CONS,CONSBY,CREDITBY,CREDITNO,BALANCE,REFUND,RDATE,REMARKS,TOTAL,NET,CREDIT,POSTED,TRCVD,TPYMT,USERID,UDATE,UTIME) VALUES (@labNo,@date,@time,@cid,@hrNo,@type,@pcode,@concode,@sex,@age,@atype,@name,@bedno,@phone,@cons,@consby,@creditby,@creditno,@balance,@refund,@rdate,@remarks,@total,@net,@credit,@posted,@trcvd,@tpymt,@uid,@udate,@utime)", conn))
                {
                    cmd.Parameters.AddWithValue("@labNo", labNo);
                    cmd.Parameters.AddWithValue("@date", collDate);
                    cmd.Parameters.AddWithValue("@time", Trim(collTime, 5));
                    cmd.Parameters.AddWithValue("@cid", Trim(CompanyCode, 4));
                    cmd.Parameters.AddWithValue("@hrNo", 0m);
                    cmd.Parameters.AddWithValue("@type", Trim(legacy?.RegType ?? "O", 3));
                    cmd.Parameters.AddWithValue("@pcode", Trim(legacy?.Code ?? patient.Code, 10));
                    cmd.Parameters.AddWithValue("@concode", Trim(ConsultCode, 3));
                    cmd.Parameters.AddWithValue("@sex", Trim(legacy?.Sex, 6));
                    cmd.Parameters.AddWithValue("@age", ParseAge(legacy?.Age));
                    cmd.Parameters.AddWithValue("@atype", Trim(legacy?.AType ?? "OPD", 5));
                    cmd.Parameters.AddWithValue("@name", Trim(legacy?.Name ?? patient.LegacyName ?? patient.Phn, 25));
                    cmd.Parameters.AddWithValue("@bedno", Trim(legacy?.BedNo, 8));
                    cmd.Parameters.AddWithValue("@phone", Trim(legacy?.Phone1 ?? patient.PatientPhoneNumber?.ToString() ?? patient.PatientMobileNumber?.ToString(), 15));
                    cmd.Parameters.AddWithValue("@cons", 0m);
                    cmd.Parameters.AddWithValue("@consby", Trim(patient.PersonId?.ToString(), 30));
                    cmd.Parameters.AddWithValue("@creditby", Trim(_appSession.LoggedDepartment != null ? _appSession.LoggedDepartment.Id.ToString() : "CASH", 11));
                    cmd.Parameters.AddWithValue("@creditno", Trim(_appSession.WebUserId?.ToString(), 10));
                    cmd.Parameters.AddWithValue("@balance", Cap(netTotal, 99999999.99m));
                    cmd.Parameters.AddWithValue("@refund", 0m);
                    cmd.Parameters.AddWithValue("@rdate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@remarks", Trim(Comments, 500));
                    cmd.Parameters.AddWithValue("@total", Cap(total, 999999m));
                    cmd.Parameters.AddWithValue("@net", Cap(netTotal, 999999m));
                    cmd.Parameters.AddWithValue("@credit", 0m);
                    cmd.Parameters.AddWithValue("@posted", "N");
                    cmd.Parameters.AddWithValue("@trcvd", 0m);
                    cmd.Parameters.AddWithValue("@tpymt", 0m);
                    cmd.Parameters.AddWithValue("@uid", Trim(_appSession.WebUserId?.ToString(), 10));
                    cmd.Parameters.AddWithValue("@udate", DateTime.Now.Date);
                    cmd.Parameters.AddWithValue("@utime", DateTime.Now.ToString("HH:mm"));
                    await cmd.ExecuteNonQueryAsync();
                }

                int seq = 1;
                foreach (var c in cart)
                {
                    using var cmd2 = new Microsoft.Data.SqlClient.SqlCommand(
                        "INSERT INTO [HMIS].[dbo].[TESTTR] (LABNO,TID,TESTNO,CHARGES,TCDATE,REMARKS,URGENT,LID,GID,TNAME,CULTURE,SRNO,REPODT,USERID,UDATE,UTIME) VALUES (@labNo,@tid,@testNo,@charges,@tcdate,@remarks,@urgent,@lid,@gid,@tname,@culture,@srno,@repodt,@uid,@udate,@utime)", conn);
                    cmd2.Parameters.AddWithValue("@labNo", labNo);
                    cmd2.Parameters.AddWithValue("@tid", Trim(c.Code, 5));
                    cmd2.Parameters.AddWithValue("@testNo", seq.ToString("D3"));
                    cmd2.Parameters.AddWithValue("@charges", Cap(c.GrossValue, 999999m));
                    cmd2.Parameters.AddWithValue("@tcdate", (c.TCDate ?? collDate).Date);
                    cmd2.Parameters.AddWithValue("@remarks", Trim(c.ItemName, 500));
                    cmd2.Parameters.AddWithValue("@urgent", Trim(c.Urgent ?? "N", 1));
                    cmd2.Parameters.AddWithValue("@lid", Trim(c.Lid ?? "HM", 2));
                    cmd2.Parameters.AddWithValue("@gid", "");
                    cmd2.Parameters.AddWithValue("@tname", Trim(c.ItemName, 40));
                    cmd2.Parameters.AddWithValue("@culture", 0);
                    cmd2.Parameters.AddWithValue("@srno", seq);
                    cmd2.Parameters.AddWithValue("@repodt", (object)DBNull.Value);
                    cmd2.Parameters.AddWithValue("@uid", Trim(_appSession.WebUserId?.ToString(), 10));
                    cmd2.Parameters.AddWithValue("@udate", DateTime.Now.Date);
                    cmd2.Parameters.AddWithValue("@utime", DateTime.Now.ToString("HH:mm"));
                    await cmd2.ExecuteNonQueryAsync();
                    seq++;
                }
            }

            HttpContext.Session.Remove(CartKey);
            HttpContext.Session.Remove(LabNoKey);
            HttpContext.Session.Remove(CollDateKey);
            HttpContext.Session.Remove(CollTimeKey);
            HttpContext.Session.Remove(EditBillKey);
            HttpContext.Session.Remove(EditLabNoKey);
            HttpContext.Session.Remove(EditModeKey);
            HttpContext.Session.SetString(LastBillKey, bill.Id.ToString());
            LastBillId = bill.Id;
            return RedirectToPage("/Lab/Invoice", new { billId = bill.Id, format = "thermal", print = 1 });
        }

        /// <summary>
        /// Updates an existing unposted, unpaid indent in place: modern bill and
        /// items are amended, the legacy REPORT master is updated and TESTTR is
        /// rewritten. Removed lines are retired instead of deleted because the
        /// BILLITEM delete plan on this database fails with SQL error 8624.
        /// </summary>
        private async Task<IActionResult> SaveEditedAsync(long billId, List<SampleCartItem> cart, DateTime collDate, string collTime)
        {
            var bill = await _db.Bills.Include(b => b.BillItems)
                .FirstOrDefaultAsync(b => b.Id == billId && !b.Retired);
            if (bill == null)
            {
                Message = "Indent not found.";
                return Page();
            }
            if (bill.Paid)
            {
                Message = "Paid indents cannot be edited.";
                return Page();
            }

            double total = Math.Round(cart.Sum(c => c.GrossValue), 2);
            double netTotal = Math.Round(cart.Sum(c => c.NetValue), 2);

            bill.GrantTotal = total;
            bill.Total = total;
            bill.NetTotal = netTotal;
            bill.Discount = 0;
            bill.Balance = netTotal;
            bill.Comments = Comments ?? bill.Comments;
            bill.EditedAt = DateTime.Now;
            bill.EditorId = _appSession.WebUserId;

            var existing = bill.BillItems
                .Where(i => !i.Retired)
                .OrderBy(i => i.SearialNo).ThenBy(i => i.Id)
                .ToList();

            int serial = 1;
            for (int i = 0; i < Math.Max(cart.Count, existing.Count); i++)
            {
                if (i < cart.Count && i < existing.Count)
                {
                    var c = cart[i];
                    var bi = existing[i];
                    bi.Qty = c.Qty;
                    bi.Rate = c.EffectiveRate;
                    bi.NetRate = c.EffectiveRate;
                    bi.GrossValue = c.GrossValue;
                    bi.NetValue = c.NetValue;
                    bi.Descreption = c.ItemName;
                    bi.ItemId = c.Code;
                    bi.SearialNo = serial++;
                    bi.Retired = false;
                    bi.EditedAt = DateTime.Now;
                    bi.EditorId = _appSession.WebUserId;
                }
                else if (i < cart.Count)
                {
                    var c = cart[i];
                    bill.BillItems.Add(new BillItem
                    {
                        Qty = c.Qty,
                        Rate = c.EffectiveRate,
                        NetRate = c.EffectiveRate,
                        GrossValue = c.GrossValue,
                        Discount = 0,
                        NetValue = c.NetValue,
                        Descreption = c.ItemName,
                        DeptId = "01",
                        SearialNo = serial++,
                        ItemId = c.Code,
                        CreatedAt = DateTime.Now,
                        CreaterId = _appSession.WebUserId
                    });
                }
                else
                {
                    var bi = existing[i];
                    bi.Retired = true;
                    bi.RetiredAt = DateTime.Now;
                    bi.RetirerId = _appSession.WebUserId;
                    bi.Qty = 0;
                    bi.Rate = 0;
                    bi.NetRate = 0;
                    bi.GrossValue = 0;
                    bi.NetValue = 0;
                }
            }

            await _db.SaveChangesAsync();

            // ===== Legacy master / detail =====
            var labNo = HttpContext.Session.GetString(EditLabNoKey)?.Trim();
            if (!string.IsNullOrWhiteSpace(labNo))
            {
                try
                {
                    using var conn = new Microsoft.Data.SqlClient.SqlConnection(
                        _db.Database.GetDbConnection().ConnectionString);
                    await conn.OpenAsync();

                    using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                        "UPDATE [HMIS].[dbo].[REPORT] SET DATE=@date, TIME=@time, CID=@cid, CONCODE=@concode, " +
                        "TOTAL=@total, NET=@net, BALANCE=@balance, REMARKS=@remarks, UDATE=@udate, UTIME=@utime WHERE LABNO=@labNo", conn))
                    {
                        cmd.Parameters.AddWithValue("@labNo", labNo);
                        cmd.Parameters.AddWithValue("@date", collDate);
                        cmd.Parameters.AddWithValue("@time", Trim(collTime, 5));
                        cmd.Parameters.AddWithValue("@cid", Trim(CompanyCode, 4));
                        cmd.Parameters.AddWithValue("@concode", Trim(ConsultCode, 3));
                        cmd.Parameters.AddWithValue("@total", Cap(total, 999999m));
                        cmd.Parameters.AddWithValue("@net", Cap(netTotal, 999999m));
                        cmd.Parameters.AddWithValue("@balance", Cap(netTotal, 99999999.99m));
                        cmd.Parameters.AddWithValue("@remarks", Trim(Comments, 500));
                        cmd.Parameters.AddWithValue("@udate", DateTime.Now.Date);
                        cmd.Parameters.AddWithValue("@utime", DateTime.Now.ToString("HH:mm"));
                        await cmd.ExecuteNonQueryAsync();
                    }

                    using (var del = new Microsoft.Data.SqlClient.SqlCommand(
                        "DELETE FROM [HMIS].[dbo].[TESTTR] WHERE LABNO=@labNo", conn))
                    {
                        del.Parameters.AddWithValue("@labNo", labNo);
                        await del.ExecuteNonQueryAsync();
                    }

                    int seq = 1;
                    foreach (var c in cart)
                    {
                        using var cmd2 = new Microsoft.Data.SqlClient.SqlCommand(
                            "INSERT INTO [HMIS].[dbo].[TESTTR] (LABNO,TID,TESTNO,CHARGES,TCDATE,REMARKS,URGENT,LID,GID,TNAME,CULTURE,SRNO,REPODT,USERID,UDATE,UTIME) VALUES (@labNo,@tid,@testNo,@charges,@tcdate,@remarks,@urgent,@lid,@gid,@tname,@culture,@srno,@repodt,@uid,@udate,@utime)", conn);
                        cmd2.Parameters.AddWithValue("@labNo", labNo);
                        cmd2.Parameters.AddWithValue("@tid", Trim(c.Code, 5));
                        cmd2.Parameters.AddWithValue("@testNo", seq.ToString("D3"));
                        cmd2.Parameters.AddWithValue("@charges", Cap(c.GrossValue, 999999m));
                        cmd2.Parameters.AddWithValue("@tcdate", (c.TCDate ?? collDate).Date);
                        cmd2.Parameters.AddWithValue("@remarks", Trim(c.ItemName, 500));
                        cmd2.Parameters.AddWithValue("@urgent", Trim(c.Urgent ?? "N", 1));
                        cmd2.Parameters.AddWithValue("@lid", Trim(c.Lid ?? "HM", 2));
                        cmd2.Parameters.AddWithValue("@gid", "");
                        cmd2.Parameters.AddWithValue("@tname", Trim(c.ItemName, 40));
                        cmd2.Parameters.AddWithValue("@culture", 0);
                        cmd2.Parameters.AddWithValue("@srno", seq);
                        cmd2.Parameters.AddWithValue("@repodt", (object)DBNull.Value);
                        cmd2.Parameters.AddWithValue("@uid", Trim(_appSession.WebUserId?.ToString(), 10));
                        cmd2.Parameters.AddWithValue("@udate", DateTime.Now.Date);
                        cmd2.Parameters.AddWithValue("@utime", DateTime.Now.ToString("HH:mm"));
                        await cmd2.ExecuteNonQueryAsync();
                        seq++;
                    }
                }
                catch
                {
                    // The modern bill is already saved; legacy mirror failures
                    // must not lose the user's work.
                }
            }

            HttpContext.Session.Remove(CartKey);
            HttpContext.Session.Remove(LabNoKey);
            HttpContext.Session.Remove(CollDateKey);
            HttpContext.Session.Remove(CollTimeKey);
            HttpContext.Session.Remove(EditBillKey);
            HttpContext.Session.Remove(EditLabNoKey);
            HttpContext.Session.Remove(EditModeKey);
            HttpContext.Session.SetString(LastBillKey, bill.Id.ToString());
            LastBillId = bill.Id;
            return RedirectToPage("/Lab/Invoice", new { billId = bill.Id, format = "thermal", print = 1 });
        }

        /// <summary>
        /// Builds the next legacy LABNO: decade digit (year/100 - 18) + day of
        /// month (2 digits) + 6-digit running sequence, exactly like lb0101.
        /// </summary>
        private async Task<string> GenerateLabNoAsync()
        {
            var prefix = (DateTime.Now.Year / 100 - 18).ToString();
            var day = DateTime.Now.Day.ToString("D2");
            var like = prefix + day + "%";

            string maxLabNo = null;
            var conn = _db.Database.GetDbConnection();
            var openedHere = false;
            if (conn.State != System.Data.ConnectionState.Open) { await conn.OpenAsync(); openedHere = true; }
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT TOP (1) [LABNO] FROM [HMIS].[dbo].[REPORT] WHERE [LABNO] LIKE @like ORDER BY [LABNO] DESC";
                var p = cmd.CreateParameter();
                p.ParameterName = "@like";
                p.Value = like;
                cmd.Parameters.Add(p);
                var val = await cmd.ExecuteScalarAsync();
                if (val != null && val != DBNull.Value) maxLabNo = Convert.ToString(val)?.Trim();
            }
            finally
            {
                if (openedHere) await conn.CloseAsync();
            }

            long next = 1;
            if (!string.IsNullOrWhiteSpace(maxLabNo) && maxLabNo.Length >= 6
                && long.TryParse(maxLabNo.Substring(maxLabNo.Length - 6), out var last))
                next = last + 1;

            return prefix + day + next.ToString("D6");
        }

        private static string Trim(string value, int max)
        {
            if (string.IsNullOrEmpty(value)) return "";
            value = value.Trim();
            return value.Length > max ? value.Substring(0, max) : value;
        }

        private static decimal Cap(double value, decimal max)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return 0m;
            var d = Math.Round((decimal)value, 2);
            if (d > max) return max;
            if (d < -max) return -max;
            return d;
        }

        private static int ParseAge(string age)
        {
            if (string.IsNullOrWhiteSpace(age)) return 0;
            if (int.TryParse(age.Trim(), out var n)) return n > 999 ? 999 : n;
            return 0;
        }

        private long? GetSelectedPatientId()
        {
            var val = HttpContext.Session.GetString(PatientKey);
            return long.TryParse(val, out var id) ? id : null;
        }

        /// <summary>
        /// Resolves the company used for the panel rate. The legacy COMPANY code
        /// (e.g. N024) is mapped to the INSTITUTION id that the billing engine
        /// keeps company service rates under; the patient's company employee
        /// link is the fallback, exactly like the legacy compchg lookup.
        /// </summary>
        private async Task<long?> ResolveCompanyIdAsync()
        {
            var code = HttpContext.Session.GetString(CidKey);
            if (string.IsNullOrWhiteSpace(code)) code = SelectedCid;
            if (!string.IsNullOrWhiteSpace(code))
            {
                var trimmed = code.Trim();
                var institutionId = await _db.Institutions.AsNoTracking()
                    .Where(i => i.Code == trimmed)
                    .Select(i => (long?)i.Id)
                    .FirstOrDefaultAsync();
                if (institutionId.HasValue) return institutionId;
            }

            if (SelectedPatient?.PersonId != null)
                return await _common.GetPatientCompanyIdAsync(SelectedPatient.PersonId.Value);

            return null;
        }

        private async Task<Patient> GetSelectedPatientAsync()
        {
            var id = GetSelectedPatientId();
            if (id == null) return null;
            return await _db.Patients.Include(p => p.Person)
                .FirstOrDefaultAsync(p => p.Id == id.Value && !p.Retired);
        }

        private List<SampleCartItem> GetCart()
        {
            var raw = HttpContext.Session.GetString(CartKey);
            if (string.IsNullOrWhiteSpace(raw)) return new List<SampleCartItem>();
            try { return JsonSerializer.Deserialize<List<SampleCartItem>>(raw) ?? new List<SampleCartItem>(); }
            catch { return new List<SampleCartItem>(); }
        }

        private void SaveCart(List<SampleCartItem> cart)
        {
            HttpContext.Session.SetString(CartKey, JsonSerializer.Serialize(cart));
        }

        /// <summary>Billing counters for one REGISTER visit (joined to PATIENT).</summary>
        public class VisitBillingInfo
        {
            public string RegNo { get; set; }
            public DateTime? RDate { get; set; }
            public string RTime { get; set; }
            public string PCode { get; set; }
            public string PatientCode { get; set; }
            public string Name { get; set; }
            public string Sex { get; set; }
            public string Age { get; set; }
            public string Type { get; set; }
            public string Cid { get; set; }
            public string Concode { get; set; }
            public decimal? BillAmount { get; set; }
            public decimal? Received { get; set; }
            public decimal? Paid { get; set; }
            public decimal? Discount { get; set; }
            public bool? Settled { get; set; }
            public bool? BillGenerated { get; set; }
            public byte? BillStatus { get; set; }

            public decimal Balance => (BillAmount ?? 0) - (Received ?? 0) + (Paid ?? 0) - (Discount ?? 0);

            public string BillStatusText => BillStatus switch
            {
                0 => "Open",
                1 => "Discharged",
                2 => "Bill Updating",
                3 => "Bill Generated",
                4 => "Settled",
                _ => "-"
            };

            public string SettledText => Settled.HasValue ? (Settled.Value ? "Yes" : "No") : "-";
            public string BillGeneratedText => BillGenerated.HasValue ? (BillGenerated.Value ? "Yes" : "No") : "-";
        }

        public class SampleCartItem
        {
            public long ItemId { get; set; }
            public string Code { get; set; }
            public string ItemName { get; set; }
            public int Qty { get; set; } = 1;
            public double Rate { get; set; }
            public double RateUrgent { get; set; }
            public string Urgent { get; set; } = "N";
            public string Lid { get; set; } = "HM";
            public string LidName { get; set; }
            public DateTime? TCDate { get; set; }
            public double EffectiveRate => Rate;
            public double GrossValue => Math.Round(Qty * Rate, 2);
            public double NetValue => GrossValue;
        }
    }
}
