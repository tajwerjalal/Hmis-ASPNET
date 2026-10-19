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
    /// IPD RUNNING BILL - criteria form + PDF generation.
    ///
    /// Migration of the VFP form FORMS\BS021040.SCX ("Running Bills - IPD Patient"), which
    /// reported the IPDBILL-free, not-yet-billed inpatient slips:
    ///     REGISTER.TYPE='I' AND !REGISTER.BILLGEN AND IBIL_DET.FLAG &lt;&gt; 'P'
    /// Option group in VFP:  1 = Summary -> report IPD-AUDS
    ///                       2 = Detail  -> report IPD-AUDD
    ///
    /// Criteria (at least one is required, exactly as in the VFP form):
    ///     chkdate  -> slip date range on IBIL_MST.SLIPDT
    ///     chkcomp  -> REGISTER.CID
    ///     chkdept  -> IBIL_MST.DEPT
    ///     chkopdreg-> IBIL_MST.HOS_CODE
    ///
    /// Developer: Tajwer Jalal | Date: 17-Oct-2026 | Time: 12:05
    /// </summary>
    public class RunningBillModel : PageModel
    {
        private readonly IpdBillReportService _report;
        private readonly IAppSession _appSession;

        public RunningBillModel(IpdBillReportService report, IAppSession appSession)
        {
            _report = report;
            _appSession = appSession;
        }

        public class InputModel
        {
            /// <summary>VFP chkdate - when set, the From/To slip-date range is applied.</summary>
            public bool UseDates { get; set; }
            public DateTime? From { get; set; }
            public DateTime? To { get; set; }
            /// <summary>VFP cbocomp - REGISTER.CID.</summary>
            public string Company { get; set; }
            /// <summary>VFP cbodept - IBIL_MST.DEPT.</summary>
            public string Dept { get; set; }
            /// <summary>VFP txtopdreg - IBIL_MST.HOS_CODE.</summary>
            public string RegNo { get; set; }
            /// <summary>"summary" -> IPD-AUDS, "detail" -> IPD-AUDD.</summary>
            public string Mode { get; set; } = "summary";
            /// <summary>VFP txtspage / txtepage print range.</summary>
            public int StartPage { get; set; } = 1;
            public int EndPage { get; set; } = 9999;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public List<(string Id, string Name)> Companies { get; private set; } = new List<(string, string)>();
        public List<(string Id, string Name)> Departments { get; private set; } = new List<(string, string)>();
        /// <summary>Set when the submitted criteria matched nothing - the view shows a dialog.</summary>
        public bool NoData { get; private set; }

        public string Message { get; private set; }

        public async Task OnGetAsync()
        {
            await LoadLookupsAsync();
        }

        public async Task<IActionResult> OnPostGenerateAsync()
        {
            await LoadLookupsAsync();

            var criteria = BuildCriteria();
            if (!criteria.HasAnyCriterion)
            {
                Message = "Select at least one criterion - a date range, company, department or IPD number.";
                return Page();
            }

            var result = await _report.RenderRunningBillAsync(criteria, BuildContext());
            if (result.Rows == 0)
            {
                NoData = true;
                Message = string.IsNullOrWhiteSpace(Input.RegNo)
                    ? "No unbilled charges matched the selected criteria. A running bill only covers"
                      + " inpatient slips that have not been billed yet - try a different date range,"
                      + " company or department."
                    : "No unbilled charges found for IPD No. " + Input.RegNo.Trim() + ". A running bill only"
                      + " covers slips that are not yet billed - if this file has already been billed, use"
                      + " Billing > Reports > Patient Bill (Final) instead.";
                return Page();
            }
            var bytes = result.Pdf;
            var name = "IPD-Running-Bill-" +
                       (Input.Mode == "detail" ? "Detail" : "Summary") + "-" +
                       DateTime.Now.ToString("yyyyMMddHHmmss") + ".pdf";
            return File(bytes, "application/pdf", name);
        }

        private IpdBillReportService.ReportCriteria BuildCriteria()
        {
            return new IpdBillReportService.ReportCriteria
            {
                From = Input.UseDates ? Input.From : (DateTime?)null,
                To = Input.UseDates ? Input.To : (DateTime?)null,
                Company = Input.Company,
                Dept = Input.Dept,
                RegNo = Input.RegNo,
                Detail = string.Equals(Input.Mode, "detail", StringComparison.OrdinalIgnoreCase),
                StartPage = Input.StartPage < 1 ? 1 : Input.StartPage,
                EndPage = Input.EndPage < 1 ? 9999 : Input.EndPage,
            };
        }

        private IpdBillReportService.ReportContext BuildContext()
        {
            return new IpdBillReportService.ReportContext
            {
                // VFP globals gccompany / gcloginid
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

        private async Task LoadLookupsAsync()
        {
            Companies = await _report.GetCompaniesAsync();
            Departments = await _report.GetDepartmentsAsync();
        }
    }
}