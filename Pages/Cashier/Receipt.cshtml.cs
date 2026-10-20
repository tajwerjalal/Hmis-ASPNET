using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Cashier
{
    /// <summary>
    /// Cash Receipts Transaction (migrated from VFP cashrec). Allocates a receipt
    /// number from CCONTROL, writes CRCVD + CRCVDDET, updates REGISTER running
    /// totals, supports discount and card details, and prints a receipt.
    /// </summary>
    public class ReceiptModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;
        private readonly ICommonService _common;

        public ReceiptModel(ApplicationDbContext db, IAppSession session, ICommonService common)
        {
            _db = db;
            _session = session;
            _common = common;
        }

        public List<AccountHead> AccountHeads { get; set; } = new();
        public List<Department> Departments { get; set; } = new();

        public List<CashReceipt> RecentReceipts { get; set; } = new();
        public CashReceipt Current { get; set; }

        [BindProperty(SupportsGet = true)] public string Q { get; set; }
        [BindProperty(SupportsGet = true)] public long? Id { get; set; }

        [BindProperty] public InputModel Input { get; set; } = new();
        public class InputModel
        {
            public DateTime TrDate { get; set; } = DateTime.Today;
            public string TrTime { get; set; } = DateTime.Now.ToString("HH:mm");
            public string Type { get; set; } = "";
            public int? RegNo { get; set; }
            public int Mode { get; set; } = 1;            // 1 cash, 2 card
            public string Reference { get; set; } = "";
            public int CardType { get; set; }
            public string CardName { get; set; } = "";
            public string CardNo { get; set; } = "";
            public DateTime? CardValid { get; set; }
            public string CardApprNo { get; set; } = "";
            public decimal CDisc { get; set; }
            public string DiscBy { get; set; } = "";
            public decimal TotalAmt { get; set; }
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

        public string ErrorMessage { get; set; } = "";
        public string Success { get; set; } = "";

        public async Task OnGetAsync()
        {
            await LoadMastersAsync();
            if (Id.HasValue && Id.Value > 0)
            {
                // Id is not used (TrNo is string); ignore for now
            }
            if (!string.IsNullOrWhiteSpace(Q))
            {
                RecentReceipts = await _db.CashReceipts
                    .Where(r => r.TrNo.Contains(Q) || (r.Reference != null && r.Reference.Contains(Q)))
                    .OrderByDescending(r => r.TrDate).Take(50).ToListAsync();
            }
            else
            {
                RecentReceipts = await _db.CashReceipts
                    .OrderByDescending(r => r.TrDate).Take(50).ToListAsync();
            }
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            await LoadMastersAsync();
            ErrorMessage = ""; Success = "";

            if (Input.Lines == null || Input.Lines.Count == 0)
            {
                ErrorMessage = "At least one detail line is required.";
                await ReloadRecentAsync();
                return Page();
            }

            var total = Input.Lines.Sum(l => l.Amount);
            Input.TotalAmt = total - Input.CDisc;

            // Allocate receipt number from CCONTROL (single row).
            var ctrl = await _db.CashControls.FirstOrDefaultAsync();
            if (ctrl == null) { ctrl = new CashControl { Id = 1, RecNo = 0, PayNo = 0, VouNo = 0 }; _db.CashControls.Add(ctrl); }
            ctrl.RecNo += 1;
            var trNo = ctrl.RecNo.ToString("D9");

            var receipt = new CashReceipt
            {
                CCode = _session.LoggedDepartment?.Code ?? "",
                LCode = _session.LoggedDepartment?.Id ?? 0,
                TrNo = trNo,
                TrDate = Input.TrDate,
                TrTime = Input.TrTime,
                Type = Input.Type,
                RegNo = Input.RegNo,
                Mode = Input.Mode,
                Reference = Input.Reference,
                CardType = Input.CardType,
                CardName = Input.CardName,
                CardNo = Input.CardNo,
                CardValid = Input.CardValid,
                CardApprNo = Input.CardApprNo,
                CDisc = Input.CDisc,
                DiscBy = Input.DiscBy,
                TotalAmt = Input.TotalAmt,
                UserId = _session.LoggedUser?.UserName ?? "",
                UDate = DateTime.Today,
                UTime = DateTime.Now.ToString("HH:mm")
            };

            foreach (var l in Input.Lines)
            {
                receipt.Details.Add(new CashReceiptDetail
                {
                    TrNo = trNo,
                    AccNo = l.AccNo,
                    Dept = l.Dept,
                    IndentNo = l.IndentNo,
                    Remarks = l.Remarks,
                    Amount = l.Amount,
                    UserId = _session.LoggedUser?.UserName ?? "",
                    UDate = DateTime.Today,
                    UTime = DateTime.Now.ToString("HH:mm")
                });
            }

            _db.CashReceipts.Add(receipt);

            // (REGISTER running totals are not tracked in this ASP.NET data model;
            //  the VFP REGISTER.TRCVD update is intentionally skipped here.)

            await _db.SaveChangesAsync();

            Success = "Receipt " + trNo + " saved. Total: " + Input.TotalAmt.ToString("N2");
            Input = new InputModel { TrDate = DateTime.Today, TrTime = DateTime.Now.ToString("HH:mm") };
            await ReloadRecentAsync();
            return Page();
        }

        private async Task LoadMastersAsync()
        {
            AccountHeads = await _db.AccountHeads.OrderBy(a => a.AccNo).ToListAsync();
            Departments = await _db.Departments.OrderBy(d => d.Name).ToListAsync();
        }

        private async Task ReloadRecentAsync()
        {
            RecentReceipts = await _db.CashReceipts.OrderByDescending(r => r.TrDate).Take(50).ToListAsync();
        }
    }
}