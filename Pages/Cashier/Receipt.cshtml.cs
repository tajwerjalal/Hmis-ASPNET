using HmisWeb.Data;
using HmisWeb.Data.Enums;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Cashier
{
    /// <summary>
    /// Cash Receipts Transaction (migrated from the VFP cashrec form), rebuilt
    /// around the outstanding BILL ledger. The screen first lists the unpaid
    /// transactions (OPD consultation, laboratory / radiology tests, pharmacy
    /// charges, IPD charges, ...) for the working day, with a date-range +
    /// patient / register / invoice search and a cashier-wise history tab.
    ///
    /// Selecting a transaction opens the "Cash Receipts Transaction" popup with
    /// the patient, bill and detail lines already filled in. The cashier picks a
    /// payment mode (Cash, Credit Card, Debit Card, Cheque, Bank Transfer,
    /// Online), enters the amount received and posts. Posting:
    ///   * updates BILL (PaidAmount / Balance / Paid / PaymentMethod),
    ///   * inserts a PAYMENT row per bill,
    ///   * writes the legacy CRCVD + CRCVDDET receipt.
    /// Private OPD bills are collected in full and marked paid; IPD follows the
    /// legacy behaviour of applying the received amount across the encounter's
    /// outstanding bills, oldest first.
    ///
    /// Developer : Tajwer Jalal
    /// Date      : 23-Oct-2026
    /// Time      : 13:20
    /// Change Log: 23-Oct-2026 - rebuilt as unpaid-ledger collection screen with
    ///             receiving popup, multiple payment modes, bill settlement and
    ///             cashier-wise receipt history.
    /// </summary>
    public class ReceiptModel : CashierPageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public ReceiptModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; }
        [BindProperty(SupportsGet = true)] public string Q { get; set; } = "";
        [BindProperty(SupportsGet = true)] public string Cashier { get; set; } = "";
        [BindProperty(SupportsGet = true)] public string Tab { get; set; } = "unpaid";
        [BindProperty(SupportsGet = true)] public long? Id { get; set; }

        public List<UnpaidRow> Unpaid { get; set; } = new();
        public List<CashReceipt> History { get; set; } = new();
        public List<string> Cashiers { get; set; } = new();
        public List<AccountHead> AccountHeads { get; set; } = new();
        public List<SelectListItem> PaymentModes { get; set; } = new();

        public UnpaidRow Selected { get; set; }
        public bool SelectedIsIpd { get; set; }
        public string SelectedPatientName { get; set; } = "";
        public string SelectedPhn { get; set; } = "";
        public string SelectedBhtNo { get; set; } = "";
        public double SelectedDue { get; set; }
        public List<DetailLine> AutoLines { get; set; } = new();

        [BindProperty] public InputModel Input { get; set; } = new();

        public string ErrorMessage { get; set; } = "";
        public string Success { get; set; } = "";

        public class InputModel
        {
            public long BillId { get; set; }
            public DateTime TrDate { get; set; } = DateTime.Today;
            public string TrTime { get; set; } = DateTime.Now.ToString("HH:mm");
            public string Type { get; set; } = "";
            public long? RegNo { get; set; }
            public PaymentMethod Mode { get; set; } = PaymentMethod.Cash;
            public string Reference { get; set; } = "";
            public string CardName { get; set; } = "";
            public string CardNo { get; set; } = "";
            public DateTime? CardValid { get; set; }
            public string CardApprNo { get; set; } = "";
            public decimal CDisc { get; set; }
            public string DiscBy { get; set; } = "";
            public decimal ReceivedAmount { get; set; }
            public decimal Tendered { get; set; }
            public List<DetailLine> Lines { get; set; } = new();
        }

        public class DetailLine
        {
            public string AccNo { get; set; } = "";
            public string Dept { get; set; } = "";
            public string IndentNo { get; set; } = "";
            public string Remarks { get; set; } = "";
            public decimal Amount { get; set; }
        }

        public class UnpaidRow
        {
            public long BillId { get; set; }
            public string InvoiceNumber { get; set; } = "";
            public string LocalNumber { get; set; } = "";
            public string IndentNo { get; set; } = "";
            public DateTime? BillDate { get; set; }
            public string BillTypeName { get; set; } = "";
            public long? RegNo { get; set; }
            public string PatientName { get; set; } = "";
            public string Phn { get; set; } = "";
            public string DepartmentName { get; set; } = "";
            public string DepartmentCode { get; set; } = "";
            public double NetTotal { get; set; }
            public double PaidAmount { get; set; }
            public double Outstanding { get; set; }
            public long? EncounterId { get; set; }
            public string BhtNo { get; set; } = "";
            public bool IsIpd { get; set; }
        }

        public async Task OnGetAsync()
        {
            Success = TempData["ReceiptSuccess"] as string;
            await LoadMasterListsAsync();
            await LoadListsAsync();

            if (Id.HasValue && Id.Value > 0)
                await LoadSelectionAsync(Id.Value, prefillInput: true);
        }

        public async Task<IActionResult> OnPostReceiveAsync()
        {
            ErrorMessage = "";
            Success = "";

            if (Input == null || Input.BillId <= 0)
            {
                ErrorMessage = "Select an unpaid transaction first.";
                await LoadMasterListsAsync();
                await LoadListsAsync();
                return Page();
            }

            var bill = await _db.Bills
                .Include(b => b.Patient).ThenInclude(p => p.Person)
                .Include(b => b.Department)
                .Include(b => b.PatientEncounter)
                .FirstOrDefaultAsync(b => b.Id == Input.BillId && !b.Retired && !b.Cancelled);

            if (bill == null)
            {
                ErrorMessage = "The selected transaction could not be found.";
                await LoadMasterListsAsync();
                await LoadListsAsync();
                return Page();
            }

            var isIpd = IsIpd(bill);
            var scope = await LoadPayableBillsAsync(bill, isIpd);

            double due = Math.Round(scope.Sum(b => Math.Max(0, b.NetTotal - b.PaidAmount)), 2);
            double discount = Math.Round(Math.Max(0, (double)Input.CDisc), 2);
            if (discount > due) discount = due;

            double required = Math.Round(Math.Max(0, due - discount), 2);
            double received = Math.Round(Math.Max(0, (double)Input.ReceivedAmount), 2);

            if (received <= 0.004)
            {
                ErrorMessage = "Enter the amount received.";
                return await ReloadSelectionAsync(bill.Id);
            }

            if (received > required + 0.004)
            {
                ErrorMessage = $"The received amount Rs {received:N2} exceeds the amount due of Rs {required:N2}.";
                return await ReloadSelectionAsync(bill.Id);
            }

            bool cash = Input.Mode == PaymentMethod.Cash;
            double tendered = cash ? Math.Max(received, (double)Input.Tendered) : received;
            double change = cash ? Math.Round(Math.Max(0, tendered - received), 2) : 0;

            var now = DateTime.Now;
            var userId = _session.LoggedUser?.Id;
            var userName = _session.LoggedUser?.UserName ?? "";
            var dept = _session.LoggedDepartment;

            // Apply the received amount across the outstanding bills, oldest first.
            double remaining = received;
            var appliedByBill = new Dictionary<long, double>();
            foreach (var b in scope.OrderBy(x => x.CreatedAt ?? DateTime.MinValue).ThenBy(x => x.Id))
            {
                if (remaining <= 0.004) break;

                double billDue = Math.Round(Math.Max(0, b.NetTotal - b.PaidAmount), 2);
                if (billDue <= 0.004) continue;

                double applied = Math.Round(Math.Min(billDue, remaining), 2);
                remaining = Math.Round(remaining - applied, 2);

                b.PaidAmount = Math.Round(b.PaidAmount + applied, 2);
                b.Balance = Math.Round(Math.Max(0, b.NetTotal - b.PaidAmount), 2);
                b.Paid = b.PaidAmount >= b.NetTotal - 0.004;
                b.PaymentMethod = Input.Mode;
                b.CheckedById = userId;
                b.TenderedAmount = cash ? tendered : 0;
                b.CashPaid = cash ? tendered : 0;
                b.CashBalance = cash ? change : 0;
                if (b.Paid)
                {
                    b.PaidAt = b.PaidAt ?? now;
                    b.BillPaymentCompletelySettled = true;
                }
                else
                {
                    b.BillPaymentCompletelySettled = false;
                }
                if (!string.IsNullOrWhiteSpace(Input.Reference))
                {
                    b.ReferenceNumber = Input.Reference;
                    if (Input.Mode == PaymentMethod.CreditCard || Input.Mode == PaymentMethod.DebitCard || Input.Mode == PaymentMethod.Card)
                        b.CreditCardRefNo = Input.Reference;
                    else if (Input.Mode == PaymentMethod.Cheque)
                        b.ChequeRefNo = Input.Reference;
                }

                _db.Payments.Add(new Payment
                {
                    BillId = b.Id,
                    PaymentMethod = Input.Mode,
                    PaidValue = applied,
                    Realized = true,
                    RealizedAt = now,
                    WrittenAt = now,
                    CreatedAt = now,
                    InstitutionId = b.InstitutionId ?? _session.LoggedSite?.Id,
                    DepartmentId = b.DepartmentId ?? dept?.Id,
                    CreaterId = userId,
                    RealizerId = userId,
                    ReferenceNo = Input.Reference ?? "",
                    Comments = "Cashier receipt",
                    CashbookEntryStated = false,
                    CashbookEntryCompleted = false
                });

                appliedByBill[b.Id] = applied;
            }

            // Write off the discount against the outstanding bills (legacy behaviour).
            if (discount > 0.004)
            {
                double left = discount;
                foreach (var b in scope.OrderBy(x => x.CreatedAt ?? DateTime.MinValue).ThenBy(x => x.Id))
                {
                    if (left <= 0.004) break;
                    double billDue = Math.Round(Math.Max(0, b.NetTotal - b.PaidAmount), 2);
                    if (billDue <= 0.004) continue;

                    double share = Math.Round(Math.Min(billDue, left), 2);
                    left = Math.Round(left - share, 2);
                    b.NetTotal = Math.Round(b.NetTotal - share, 2);
                    b.BillTotal = Math.Round(Math.Max(0, b.BillTotal - share), 2);
                    b.Discount = Math.Round(b.Discount + share, 2);
                    b.PaidAmount = Math.Round(b.PaidAmount + share, 2);
                    b.Balance = Math.Round(Math.Max(0, b.NetTotal - b.PaidAmount), 2);
                    b.Paid = b.NetTotal - b.PaidAmount <= 0.004;
                    if (b.Paid)
                    {
                        b.PaidAt = b.PaidAt ?? now;
                        b.BillPaymentCompletelySettled = true;
                    }
                    b.PaymentMethod = Input.Mode;
                }
            }

            // Allocate the legacy receipt number from CCONTROL (single row).
            var ctrl = await _db.CashControls.FirstOrDefaultAsync();
            if (ctrl == null)
            {
                ctrl = new CashControl { RecNo = 0, PayNo = 0, VouNo = 0 };
                _db.CashControls.Add(ctrl);
            }
            ctrl.RecNo += 1;
            var trNo = ctrl.RecNo.ToString("D9");

            bool settled = scope.All(b => b.Paid || b.NetTotal - b.PaidAmount <= 0.004);

            var receipt = new CashReceipt
            {
                CCode = dept?.Code ?? "",
                LCode = (int)(dept?.Id ?? 0),
                TrNo = trNo,
                TrDate = Input.TrDate == default ? DateTime.Today : Input.TrDate.Date,
                TrTime = string.IsNullOrWhiteSpace(Input.TrTime) ? now.ToString("HH:mm") : Input.TrTime,
                Type = Input.Type ?? "",
                RegNo = (int?)bill.Patient?.PatientId,
                Settle = settled,
                Reference = Input.Reference ?? "",
                Mode = LegacyMode(Input.Mode),
                CardType = (int)Input.Mode,
                CardName = Input.CardName ?? "",
                CardNo = Input.CardNo ?? "",
                CardValid = Input.CardValid,
                CardApprNo = Input.CardApprNo ?? "",
                CDisc = (decimal)discount,
                DiscBy = Input.DiscBy ?? "",
                TotalAmt = (decimal)received,
                UserId = userName,
                UDate = DateTime.Today,
                UTime = now.ToString("HH:mm")
            };

            // Detail lines are rebuilt from the selected bill(s) so the legacy
            // receipt total always agrees with the bills that were settled.
            var lines = await BuildAutoLinesAsync(bill, scope, appliedByBill);

            foreach (var l in lines)
            {
                receipt.Details.Add(new CashReceiptDetail
                {
                    TrNo = trNo,
                    AccNo = string.IsNullOrWhiteSpace(l.AccNo) ? "GENERAL" : l.AccNo,
                    Dept = string.IsNullOrWhiteSpace(l.Dept) ? (bill.Department?.Code ?? "00") : l.Dept,
                    IndentNo = l.IndentNo ?? "",
                    Remarks = l.Remarks ?? "",
                    Amount = l.Amount,
                    UserId = userName,
                    UDate = DateTime.Today,
                    UTime = now.ToString("HH:mm")
                });
            }

            _db.CashReceipts.Add(receipt);
            await _db.SaveChangesAsync();

            var msg = $"Receipt {trNo} posted. Received Rs {received:N2}";
            if (change > 0) msg += $", change Rs {change:N2}";
            if (discount > 0) msg += $", discount Rs {discount:N2}";
            msg += ".";
            TempData["ReceiptSuccess"] = msg;

            // Successful posting opens the 80 mm thermal receipt and prints it.
            return RedirectToPage("/Cashier/ReceiptPrint", new
            {
                trNo = trNo,
                print = 1
            });
        }

        // ------------------------------------------------------------------
        // List loading
        // ------------------------------------------------------------------

        private async Task LoadMasterListsAsync()
        {
            AccountHeads = await _db.AccountHeads.OrderBy(a => a.AccNo).ToListAsync();

            PaymentModes = new List<SelectListItem>
            {
                new SelectListItem("Cash",          ((int)PaymentMethod.Cash).ToString()),
                new SelectListItem("Credit Card",   ((int)PaymentMethod.CreditCard).ToString()),
                new SelectListItem("Debit Card",    ((int)PaymentMethod.DebitCard).ToString()),
                new SelectListItem("Cheque",        ((int)PaymentMethod.Cheque).ToString()),
                new SelectListItem("Bank Transfer", ((int)PaymentMethod.BankTransfer).ToString()),
                new SelectListItem("Online",        ((int)PaymentMethod.Online).ToString())
            };
        }

        private async Task LoadListsAsync()
        {
            var today = DateTime.Today;

            if (FromDate == null)
            {
                // Outstanding bills carry forward. Open the list on the
                // oldest unpaid bill so a receipt is not hidden just because
                // the working date rolled over midnight.
                var earliest = await _db.Bills.AsNoTracking()
                    .Where(b => !b.Retired && !b.Cancelled && !b.Paid
                                && b.PatientId != null
                                && b.NetTotal > 0
                                && (b.NetTotal - b.PaidAmount) > 0.004)
                    .Select(b => (DateTime?)(b.BillDate ?? b.CreatedAt))
                    .MinAsync();
                FromDate = earliest?.Date ?? today;
            }

            if (ToDate == null) ToDate = today > FromDate.Value ? today : FromDate.Value;

            var from = (FromDate ?? today).Date;
            var to = (ToDate ?? from).Date;
            if (to < from) to = from;

            var start = from;
            var end = to.AddDays(1);

            var query = _db.Bills.AsNoTracking()
                .Where(b => !b.Retired && !b.Cancelled && !b.Paid
                            && b.PatientId != null
                            && b.NetTotal > 0
                            && (b.NetTotal - b.PaidAmount) > 0.004);

            query = query.Where(b =>
                (b.BillDate ?? b.CreatedAt) != null &&
                (b.BillDate ?? b.CreatedAt) >= start &&
                (b.BillDate ?? b.CreatedAt) < end);

            if (!string.IsNullOrWhiteSpace(Q))
            {
                var term = Q.Trim();
                if (long.TryParse(term, out var reg))
                {
                    query = query.Where(b =>
                        b.Patient.PatientId == reg ||
                        (b.InvoiceNumber != null && b.InvoiceNumber.Contains(term)) ||
                        (b.LocalNumber != null && b.LocalNumber.Contains(term)));
                }
                else
                {
                    query = query.Where(b =>
                        (b.InvoiceNumber != null && b.InvoiceNumber.Contains(term)) ||
                        (b.LocalNumber != null && b.LocalNumber.Contains(term)) ||
                        (b.Patient.Person.Name != null && b.Patient.Person.Name.Contains(term)) ||
                        (b.Patient.Person.NameWithInitials != null && b.Patient.Person.NameWithInitials.Contains(term)));
                }
            }

            var raw = await query
                .OrderByDescending(b => b.BillDate ?? b.CreatedAt)
                .ThenByDescending(b => b.Id)
                .Take(300)
                .Select(b => new
                {
                    BillId = b.Id,
                    InvoiceNumber = b.InvoiceNumber,
                    LocalNumber = b.LocalNumber,
                    BillDate = b.BillDate ?? b.CreatedAt,
                    BillType = b.BillType,
                    RegNo = b.Patient.PatientId,
                    PatientName = b.Patient.Person.NameWithInitials ?? b.Patient.Person.Name,
                    Phn = b.Patient.Phn,
                    DepartmentName = b.Department.Name,
                    DepartmentCode = b.Department.Code,
                    NetTotal = b.NetTotal,
                    PaidAmount = b.PaidAmount,
                    EncounterId = b.PatientEncounterId,
                    BhtNo = b.PatientEncounter.BhtNo
                })
                .ToListAsync();

            Unpaid = raw.Select(x => new UnpaidRow
            {
                BillId = x.BillId,
                InvoiceNumber = x.InvoiceNumber ?? "",
                LocalNumber = x.LocalNumber ?? "",
                IndentNo = FirstNonEmpty(x.InvoiceNumber, x.LocalNumber, x.BhtNo),
                BillDate = x.BillDate,
                BillTypeName = BillTypeLabel(x.BillType),
                RegNo = x.RegNo,
                PatientName = string.IsNullOrWhiteSpace(x.PatientName) ? "(unknown)" : x.PatientName,
                Phn = x.Phn ?? "",
                DepartmentName = x.DepartmentName ?? "",
                DepartmentCode = x.DepartmentCode ?? "",
                NetTotal = x.NetTotal,
                PaidAmount = x.PaidAmount,
                Outstanding = Math.Round(Math.Max(0, x.NetTotal - x.PaidAmount), 2),
                EncounterId = x.EncounterId,
                BhtNo = x.BhtNo ?? "",
                IsIpd = IsIpdType(x.BillType)
            }).ToList();

            var hq = _db.CashReceipts.AsNoTracking()
                .Where(r => r.TrDate >= start && r.TrDate < end);

            if (!string.IsNullOrWhiteSpace(Cashier))
                hq = hq.Where(r => r.UserId == Cashier);

            if (!string.IsNullOrWhiteSpace(Q) && string.Equals(Tab, "history", StringComparison.OrdinalIgnoreCase))
            {
                var term = Q.Trim();
                hq = hq.Where(r => r.TrNo.Contains(term) ||
                                   (r.Reference != null && r.Reference.Contains(term)) ||
                                   (r.Type != null && r.Type.Contains(term)));
            }

            History = await hq
                .OrderByDescending(r => r.TrDate)
                .ThenByDescending(r => r.TrNo)
                .Take(300)
                .ToListAsync();

            Cashiers = await _db.CashReceipts.AsNoTracking()
                .Where(r => r.UserId != null && r.UserId != "")
                .Select(r => r.UserId)
                .Distinct()
                .OrderBy(u => u)
                .ToListAsync();
        }

        // ------------------------------------------------------------------
        // Selection (modal prefill)
        // ------------------------------------------------------------------

        private async Task LoadSelectionAsync(long billId, bool prefillInput)
        {
            var bill = await _db.Bills.AsNoTracking()
                .Include(b => b.Patient).ThenInclude(p => p.Person)
                .Include(b => b.Department)
                .Include(b => b.PatientEncounter)
                .FirstOrDefaultAsync(b => b.Id == billId && !b.Retired && !b.Cancelled);

            if (bill == null)
            {
                ErrorMessage = "The selected transaction could not be found.";
                return;
            }

            SelectedIsIpd = IsIpd(bill);
            var scope = await LoadPayableBillsAsync(bill, SelectedIsIpd);

            Selected = new UnpaidRow
            {
                BillId = bill.Id,
                InvoiceNumber = bill.InvoiceNumber ?? "",
                LocalNumber = bill.LocalNumber ?? "",
                IndentNo = FirstNonEmpty(bill.InvoiceNumber, bill.LocalNumber, bill.PatientEncounter?.BhtNo),
                BillDate = bill.BillDate ?? bill.CreatedAt,
                BillTypeName = BillTypeLabel(bill.BillType),
                RegNo = bill.Patient?.PatientId,
                PatientName = bill.Patient?.Person?.NameWithInitials ?? bill.Patient?.Person?.Name ?? "(unknown)",
                Phn = bill.Patient?.Phn ?? "",
                DepartmentName = bill.Department?.Name ?? "",
                DepartmentCode = bill.Department?.Code ?? "",
                NetTotal = bill.NetTotal,
                PaidAmount = bill.PaidAmount,
                Outstanding = Math.Round(Math.Max(0, bill.NetTotal - bill.PaidAmount), 2),
                EncounterId = bill.PatientEncounterId,
                BhtNo = bill.PatientEncounter?.BhtNo ?? "",
                IsIpd = SelectedIsIpd
            };

            SelectedPatientName = Selected.PatientName;
            SelectedPhn = Selected.Phn;
            SelectedBhtNo = Selected.BhtNo;
            SelectedDue = Math.Round(scope.Sum(b => Math.Max(0, b.NetTotal - b.PaidAmount)), 2);
            AutoLines = await BuildAutoLinesAsync(bill, scope);

            if (prefillInput)
            {
                Input = new InputModel
                {
                    BillId = bill.Id,
                    TrDate = DateTime.Today,
                    TrTime = DateTime.Now.ToString("HH:mm"),
                    Type = DeriveType(bill),
                    RegNo = bill.Patient?.PatientId,
                    Mode = PaymentMethod.Cash,
                    Reference = "",
                    ReceivedAmount = (decimal)SelectedDue,
                    Tendered = (decimal)SelectedDue,
                    Lines = AutoLines
                };
            }
            else
            {
                Input.BillId = bill.Id;
                if (Input.Lines == null || Input.Lines.Count == 0)
                    Input.Lines = AutoLines;
            }
        }

        private async Task<IActionResult> ReloadSelectionAsync(long billId)
        {
            await LoadMasterListsAsync();
            await LoadListsAsync();
            Id = billId;
            await LoadSelectionAsync(billId, prefillInput: false);
            Tab = "unpaid";
            return Page();
        }

        private async Task<List<Bill>> LoadPayableBillsAsync(Bill bill, bool isIpd)
        {
            if (isIpd && bill.PatientEncounterId != null)
            {
                return await _db.Bills
                    .Include(b => b.Department)
                    .Include(b => b.PatientEncounter)
                    .Where(b => b.PatientEncounterId == bill.PatientEncounterId
                                && !b.Retired && !b.Cancelled && !b.Paid
                                && b.NetTotal > 0
                                && (b.NetTotal - b.PaidAmount) > 0.004)
                    .OrderBy(b => b.CreatedAt ?? DateTime.MinValue)
                    .ThenBy(b => b.Id)
                    .ToListAsync();
            }

            return new List<Bill> { bill };
        }

        private async Task<List<DetailLine>> BuildAutoLinesAsync(Bill bill, List<Bill> scope, IDictionary<long, double> applied = null)
        {
            var lines = new List<DetailLine>();
            var billIds = scope.Select(b => b.Id).ToList();

            var items = await _db.BillItems.AsNoTracking()
                .Where(i => i.BillId != null && billIds.Contains(i.BillId.Value) && !i.Retired)
                .Select(i => new { i.BillId, i.Descreption })
                .ToListAsync();

            foreach (var b in scope)
            {
                double amount = applied != null
                    ? (applied.TryGetValue(b.Id, out var a) ? Math.Round(a, 2) : 0)
                    : Math.Round(Math.Max(0, b.NetTotal - b.PaidAmount), 2);
                if (amount <= 0.004) continue;

                var deptName = b.Department?.Name ?? "";
                var accNo = DeriveAccNo(b, deptName);
                var descriptions = items
                    .Where(i => i.BillId == b.Id)
                    .Select(i => i.Descreption)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct()
                    .Take(6)
                    .ToList();

                lines.Add(new DetailLine
                {
                    AccNo = accNo,
                    Dept = b.Department?.Code ?? (bill.Department?.Code ?? ""),
                    IndentNo = FirstNonEmpty(b.InvoiceNumber, b.LocalNumber, b.PatientEncounter?.BhtNo, b.Id.ToString()),
                    Remarks = descriptions.Count > 0
                        ? string.Join(", ", descriptions)
                        : (BillTypeLabel(b.BillType) is string t && !string.IsNullOrWhiteSpace(t) ? t : "Charge"),
                    Amount = (decimal)amount
                });
            }

            return AggregateLines(lines);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static List<DetailLine> AggregateLines(IEnumerable<DetailLine> lines)
        {
            if (lines == null) return new List<DetailLine>();

            return lines
                .Where(l => l != null)
                .GroupBy(l => new
                {
                    AccNo = string.IsNullOrWhiteSpace(l.AccNo) ? "GENERAL" : l.AccNo.Trim(),
                    Dept = (l.Dept ?? "").Trim()
                })
                .Select(g => new DetailLine
                {
                    AccNo = g.Key.AccNo,
                    Dept = g.Key.Dept,
                    IndentNo = string.Join(", ", g.Select(x => x.IndentNo)
                        .Where(s => !string.IsNullOrWhiteSpace(s)).Distinct()),
                    Remarks = string.Join(", ", g.Select(x => x.Remarks)
                        .Where(s => !string.IsNullOrWhiteSpace(s)).Distinct()),
                    Amount = Math.Round(g.Sum(x => x.Amount), 2)
                })
                .ToList();
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (var v in values)
                if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
            return "";
        }

        private static bool IsIpd(Bill bill)
        {
            if (bill == null) return false;
            if (bill.PatientEncounter?.EncounterType == EncounterType.Inward) return true;
            return IsIpdType(bill.BillType);
        }

        private static bool IsIpdType(BillType? type)
        {
            if (type == null) return false;
            switch (type.Value)
            {
                case BillType.InwardBill:
                case BillType.InwardFinalBill:
                case BillType.InwardPreBill:
                case BillType.InwardProfessional:
                case BillType.InwardTheatre:
                case BillType.InwardServiceBill:
                case BillType.InwardAdjustment:
                    return true;
                default:
                    return false;
            }
        }

        private static string BillTypeLabel(BillType? type)
        {
            if (type == null) return "";
            switch (type.Value)
            {
                case BillType.OpdBill:
                case BillType.OpdPreBill: return "OPD";
                case BillType.OpdChannelPay:
                case BillType.ChannelBill: return "OPD";
                case BillType.OpdReturn: return "OPD Return";
                case BillType.InwardBill:
                case BillType.InwardPreBill: return "IPD";
                case BillType.InwardFinalBill: return "IPD Final";
                case BillType.PharmacyBill:
                case BillType.PharmacySale: return "Pharmacy";
                case BillType.PharmacyReturn: return "Pharmacy Return";
                default: return type.Value.ToString();
            }
        }

        private static string DeriveType(Bill bill)
        {
            if (bill == null) return "";
            if (IsIpd(bill)) return "IPD";

            var dept = (bill.Department?.Name ?? "").ToUpperInvariant();
            if (dept.Contains("PHARM")) return "PHARM";
            if (dept.Contains("LAB")) return "LAB";
            if (dept.Contains("X-RAY") || dept.Contains("XRAY") || dept.Contains("RADIO") || dept.Contains("SCAN") || dept.Contains("CT")) return "RADIO";
            if (dept.Contains("ULTRA")) return "US";
            if (dept.Contains("DENT")) return "DENTAL";

            if (bill.BillType == BillType.OpdBill || bill.BillType == BillType.OpdPreBill ||
                bill.BillType == BillType.OpdChannelPay || bill.BillType == BillType.ChannelBill)
                return "OPD";

            return "OPD";
        }

        private static string DeriveAccNo(Bill bill, string deptName)
        {
            if (bill == null) return "GENERAL";

            if (bill.BillType == BillType.PharmacyBill || bill.BillType == BillType.PharmacySale)
                return "PHARMACY";

            var dept = (deptName ?? "").ToUpperInvariant();
            if (dept.Contains("PHARM")) return "PHARMACY";
            if (dept.Contains("LAB")) return "LABORATORY";
            if (dept.Contains("X-RAY") || dept.Contains("XRAY") || dept.Contains("RADIO") || dept.Contains("SCAN") || dept.Contains("CT"))
                return "RADIOLOGY";
            if (dept.Contains("ULTRA")) return "ULTRASOUND";
            if (dept.Contains("DENT")) return "DENTAL";

            if (IsIpd(bill)) return "IPD";
            if (bill.BillType == BillType.OpdBill || bill.BillType == BillType.OpdPreBill ||
                bill.BillType == BillType.OpdChannelPay || bill.BillType == BillType.ChannelBill)
                return "OPD";

            return "GENERAL";
        }

        private static int LegacyMode(PaymentMethod mode)
        {
            switch (mode)
            {
                case PaymentMethod.Cash: return 1;
                case PaymentMethod.Cheque: return 3;
                case PaymentMethod.BankTransfer:
                case PaymentMethod.Online:
                case PaymentMethod.MobileWallet:
                case PaymentMethod.EasyPaisa:
                case PaymentMethod.JazzCash:
                    return 4;
                default: return 2;
            }
        }

        public static string ModeText(int mode)
        {
            switch (mode)
            {
                case 1: return "Cash";
                case 2: return "Card";
                case 3: return "Cheque";
                case 4: return "Bank / Online";
                default: return "Mode " + mode;
            }
        }
    }
}
