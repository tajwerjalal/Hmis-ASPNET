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
    /// PATIENT BILL (FINAL) - IPD - criteria form + PDF generation.
    ///
    /// Migration of the VFP form FORMS\BS021001.SCX ("Patient Bills (Final) - IPD Patient").
    /// The VFP query selects the generated bills first -
    ///     cursor rp : IPDBILL where REGISTER.TYPE='I' AND empty(IPDBILL.STAT)
    /// and then the lines -
    ///     cursor audit : rp -> IBIL_MST (IBIL_MST.BILL = rp.BILLNO) -> IBIL_DET
    /// Option group in VFP:  1 = Summary -> report IPD-B-SMN
    ///                       2 = Detail  -> report IPD-B-DT
    ///
    /// Criteria (at least one is required, exactly as in the VFP form):
    ///     chkdate  -> bill date range on IPDBILL.BDATE
    ///     chkcomp  -> REGISTER.CID
    ///     chkdept  -> IBIL_MST.DEPT
    ///     chkopdreg-> IPDBILL.HOS_CODE
    ///
    /// Developer: Tajwer Jalal | Date: 17-Oct-2026 | Time: 12:10
    /// </summary>
    public class FinalBillModel : PageModel
    {
        private readonly IpdBillReportService _report;
        private readonly IAppSession _appSession;

        public FinalBillModel(IpdBillReportService report, IAppSession appSession)
        {
            _report = report;
            _appSession = appSession;
        }

        public class InputModel
        {
            /// <summary>VFP chkdate - when set, the From/To bill-date range is applied.</summary>
            public bool UseDates { get; set; }
            public DateTime? From { get; set; }
            public DateTime? To { get; set; }
            /// <summary>VFP cbocomp - REGISTER.CID.</summary>
            public string Company { get; set; }
            /// <summary>VFP cbodept - IBIL_MST.DEPT.</summary>
            public string Dept { get; set; }
            /// <summary>VFP txtopdreg - IPDBILL.HOS_CODE.</summary>
            public string RegNo { get; set; }
            /// <summary>"summary" -> IPD-B-SMN, "detail" -> IPD-B-DT.</summary>
            public string Mode { get; set; } = "summary";
            /// <summary>VFP txtspage / txtepage print range.</summary>
            public int StartPage { get; set; } = 1;
            public int EndPage { get; set; } = 9999;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public List<(string Id, string Name)> Companies { get; private set; } = new List<(string, string)>();
        public List<(string Id, string Name)> Departments { get; private set; } = new List<(string, string)>();
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

            var bytes = await _report.RenderFinalBillAsync(criteria, BuildContext());
            var name = "IPD-Bill-Final-" +
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