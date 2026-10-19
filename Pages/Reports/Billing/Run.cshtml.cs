using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HmisWeb.Services;
using HmisWeb.Services.Billing;

#nullable disable

namespace HmisWeb.Pages.Reports.Billing
{
    /// <summary>
    /// GENERIC RUNNER for the migrated legacy IPD list/summary reports.
    ///
    /// Serves every remaining VFP report menu item that is not a per-patient bill
    /// (those have their own pages: RunningBill / FinalBill).  The report itself is chosen
    /// with ?code=, the criteria and the columns come from
    /// <see cref="BillingReportService.Definition"/>, so adding a report is a registry entry
    /// plus a menu link rather than a new page.
    ///
    /// Reports covered (legacy form -> legacy .FRX):
    ///   bs021010 IPD-BLSS/BLSD | bs021020 IPD-CONS/COND | bs021030 IPTSUMC/IPTSUM/IPTSUME
    ///   bs021050 IAUDITS/IAUDITD/IDEPADT | bs021060 IPDCOVLT | ibillstat IBILLSTAT
    ///   billstat BILLSTAT | cdischgrpt CDISCHGRPT
    ///
    /// Developer: Tajwer Jalal | Date: 17-Oct-2026 | Time: 15:35
    /// </summary>
    public class RunModel : PageModel
    {
        private readonly BillingReportService _reports;
        private readonly IAppSession _appSession;

        public RunModel(BillingReportService reports, IAppSession appSession)
        {
            _reports = reports;
            _appSession = appSession;
        }

        public class InputModel
        {
            public bool UseDates { get; set; }
            public DateTime? From { get; set; }
            public DateTime? To { get; set; }
            public string Company { get; set; }
            public string Dept { get; set; }
            public string RegNo { get; set; }
            public string BillNo { get; set; }
            public int Mode { get; set; } = 1;
            public int StartPage { get; set; } = 1;
            public int EndPage { get; set; } = 9999;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        [BindProperty(SupportsGet = true)]
        public string Code { get; set; }

        public BillingReportService.Definition Def { get; private set; }
        public List<(string Id, string Name)> Companies { get; private set; } = new List<(string, string)>();
        public List<(string Id, string Name)> Departments { get; private set; } = new List<(string, string)>();
        /// <summary>Set when the submitted criteria matched nothing - the view shows a dialog.</summary>
        public bool NoData { get; private set; }

        public string Message { get; private set; }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostGenerateAsync()
        {
            await LoadAsync();

            var criteria = new BillingReportService.Criteria
            {
                From = Input.UseDates ? Input.From : (DateTime?)null,
                To = Input.UseDates ? Input.To : (DateTime?)null,
                Company = Input.Company,
                Dept = Input.Dept,
                RegNo = Input.RegNo,
                BillNo = Input.BillNo,
                Mode = Input.Mode < 1 ? 1 : Input.Mode,
                StartPage = Input.StartPage < 1 ? 1 : Input.StartPage,
                EndPage = Input.EndPage < 1 ? 9999 : Input.EndPage,
            };

            var result = await _reports.RenderAsync(Def, criteria, BuildContext());
            if (result.Rows == 0)
            {
                NoData = true;
                Message = "No records matched the selected criteria for " + Def.Title
                        + ". Widen the date range, or clear the company / department / IPD number filter.";
                return Page();
            }
            var bytes = result.Pdf;
            var name = Def.Code + "-" + (Def.Modes.Length > 1 ? "M" + Input.Mode : "S")
                       + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".pdf";
            return File(bytes, "application/pdf", name);
        }

        private BillingReportService.ReportContext BuildContext()
        {
            return new BillingReportService.ReportContext
            {
                HospitalName = _appSession.LoggedSite?.PrintingName
                               ?? _appSession.LoggedSite?.Name
                               ?? "HMIS Hospital",
                HospitalNo = _appSession.LoggedSite?.RegNo
                             ?? _appSession.LoggedSite?.Code
                             ?? "",
                UserId = _appSession.LoggedUser?.WebUserPerson?.NameWithInitials
                         ?? _appSession.LoggedUser?.RealUserName
                         ?? _appSession.LoggedUser?.UserName
                         ?? "",
            };
        }

        private async Task LoadAsync()
        {
            Def = BillingReportService.Find(Code);
            if (Def == null)
            {
                Response.StatusCode = 404;
                Message = "Unknown report code '" + Code + "'.";
                return;
            }
            Companies = await _reports.GetCompaniesAsync();
            Departments = await _reports.GetDepartmentsAsync();
        }
    }
}