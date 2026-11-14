using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Cashier
{
    public class PaymentModel : CashierPageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppSession _session;

        public PaymentModel(ApplicationDbContext db, IAppSession session)
        {
            _db = db;
            _session = session;
        }

        public List<AccountHead> AccountHeads { get; set; } = new();
        public List<Department> Departments { get; set; } = new();
        public List<CashPayment> Recent { get; set; } = new();

        [BindProperty] public InputModel Input { get; set; } = new();
        public class InputModel
        {
            public DateTime TrDate { get; set; } = DateTime.Today;
            public string TrTime { get; set; } = DateTime.Now.ToString("HH:mm");
            public string Type { get; set; } = "";
            public int? RegNo { get; set; }
            public string ECode { get; set; } = "";
            public string Reference { get; set; } = "";
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
            Recent = await _db.CashPayments.OrderByDescending(p => p.TrDate).Take(50).ToListAsync();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            await LoadMastersAsync();
            ErrorMessage = ""; Success = "";

            if (Input.Lines == null || Input.Lines.Count == 0)
            {
                ErrorMessage = "At least one detail line is required.";
                Recent = await _db.CashPayments.OrderByDescending(p => p.TrDate).Take(50).ToListAsync();
                return Page();
            }

            Input.TotalAmt = Input.Lines.Sum(l => l.Amount) - Input.CDisc;

            var ctrl = await _db.CashControls.FirstOrDefaultAsync();
            if (ctrl == null) { ctrl = new CashControl { Id = 1 }; _db.CashControls.Add(ctrl); }
            ctrl.PayNo += 1;
            var trNo = ctrl.PayNo.ToString("D9");

            var pay = new CashPayment
            {
                CCode = _session.LoggedDepartment?.Code ?? "",
                LCode = (int)(_session.LoggedDepartment?.Id ?? 0),
                TrNo = trNo,
                TrDate = Input.TrDate,
                TrTime = Input.TrTime,
                Type = Input.Type,
                RegNo = Input.RegNo,
                ECode = Input.ECode,
                Reference = Input.Reference,
                CDisc = Input.CDisc,
                DiscBy = Input.DiscBy,
                TotalAmt = Input.TotalAmt,
                UserId = _session.LoggedUser?.UserName ?? "",
                UDate = DateTime.Today,
                UTime = DateTime.Now.ToString("HH:mm")
            };
            foreach (var l in Input.Lines)
            {
                pay.Details.Add(new CashPaymentDetail
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
            _db.CashPayments.Add(pay);

            // (REGISTER running totals are not tracked in this ASP.NET data model.)

            await _db.SaveChangesAsync();
            Success = "Payment " + trNo + " saved. Total: " + Input.TotalAmt.ToString("N2");
            Input = new InputModel { TrDate = DateTime.Today, TrTime = DateTime.Now.ToString("HH:mm") };
            Recent = await _db.CashPayments.OrderByDescending(p => p.TrDate).Take(50).ToListAsync();
            return Page();
        }

        private async Task LoadMastersAsync()
        {
            AccountHeads = await _db.AccountHeads.OrderBy(a => a.AccNo).ToListAsync();
            Departments = await _db.Departments.OrderBy(d => d.Name).ToListAsync();
        }
    }
}