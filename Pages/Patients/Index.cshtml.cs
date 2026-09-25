using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HmisWeb.Models;
using HmisWeb.Services;

namespace HmisWeb.Pages.Patients
{
    public class IndexModel : PageModel
    {
        private readonly ICommonService _common;

        public IndexModel(ICommonService common)
        {
            _common = common;
        }

        [BindProperty(SupportsGet = true)]
        public string Q { get; set; }

        public List<Patient> Patients { get; set; } = new();

        public async Task OnGetAsync()
        {
            if (!string.IsNullOrWhiteSpace(Q))
                Patients = await _common.SearchPatientsAsync(Q);
        }
    }
}