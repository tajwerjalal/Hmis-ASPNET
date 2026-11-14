using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HmisWeb.Models.Lab;
using HmisWeb.Services;
using HmisWeb.Services.Lab;

namespace HmisWeb.Pages.Lab
{
    /*
     * ============================================================================
     * File........: Pages/Lab/TestResultEntry.cshtml.cs
     * Purpose.....: Thin PageModel for the LB0110 "Test Result Entry" screen.
     *               Exposes the List tab, the Entry tab and the named AJAX
     *               handlers used by the partial views.
     * Inputs......: Query string (labNo/tid/testNo, list filters) and JSON/form
     *               payloads from wwwroot/js/lab-testresult.js.
     * Outputs.....: Razor page + partial HTML (_ListGrid, _EntryPane,
     *               _ResultGrid, _CulturePane) and JSON pick lists / save result.
     * Business
     *   rules.....: 1. A 9 digit scan in the search box opens the indent directly.
     *               2. All data access is delegated to LabResultService and
     *                  CultureService; this class contains no SQL.
     *               3. Every POST is protected by the Razor Pages antiforgery
     *                  token (the page renders @Html.AntiForgeryToken()).
     *               4. Authorization: the Lab module currently relies on the
     *                  application's session/auth middleware (Program.cs). The
     *                  [Authorize] attribute is intentionally not applied here
     *                  because the other Lab pages are not protected either;
     *                  add it to the whole module together, not to one page.
     * Developer...: Tajwer Jalal
     * Date/Time...: 2026-11-02 11:20
     * ============================================================================
     */
    public class TestResultEntryModel : PageModel
    {
        private readonly LabResultService _lab;
        private readonly CultureService _culture;
        private readonly IAppSession _appSession;

        public TestResultEntryModel(LabResultService lab, CultureService culture, IAppSession appSession)
        {
            _lab = lab;
            _culture = culture;
            _appSession = appSession;
        }

        // ---------------- query state ----------------
        [BindProperty(SupportsGet = true)] public string LabNo { get; set; }
        [BindProperty(SupportsGet = true)] public string Tid { get; set; }
        [BindProperty(SupportsGet = true)] public string TestNo { get; set; }

        [BindProperty(SupportsGet = true)] public string Search { get; set; }
        [BindProperty(SupportsGet = true)] public string LabNoFilter { get; set; }
        [BindProperty(SupportsGet = true)] public string Hrno { get; set; }
        [BindProperty(SupportsGet = true)] public string Name { get; set; }
        [BindProperty(SupportsGet = true)] public bool PendingOnly { get; set; }
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 25;

        // ---------------- view data ----------------
        public string ActiveTab { get; set; } = "list";
        public LabIndentPage List { get; set; } = new();
        public LabEntryView Entry { get; set; }
        public List<LabReadingRow> Readings { get; set; } = new();
        public LabCultureView Culture { get; set; } = new();
        public string GroupCode { get; set; } = "";
        public string Success { get; set; }
        public string ErrorMessage { get; set; }

        // =====================================================================
        // PAGE
        // =====================================================================

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Scanner fast path: "230000001" opens the entry screen directly.
                var scan = (Search ?? "").Trim();
                if (scan.Length >= 6 && scan.All(char.IsDigit) && await _lab.IndentExistsAsync(scan))
                    return RedirectToPage("/Lab/TestResultEntry", new { labNo = scan });

                if (!string.IsNullOrWhiteSpace(LabNo))
                {
                    Entry = await _lab.GetEntryAsync(LabNo.Trim(), Tid, TestNo);
                    GroupCode = Entry?.GroupCode ?? "";
                    Readings = Entry?.Readings ?? new List<LabReadingRow>();
                    Culture = Entry?.Culture ?? new LabCultureView();
                    ActiveTab = "entry";
                    if (!string.IsNullOrWhiteSpace(Entry?.Error)) ErrorMessage = Entry.Error;
                    return Page();
                }

                // Single "search" box compatibility: digits -> Labno, text -> Name.
                if (!string.IsNullOrWhiteSpace(scan))
                {
                    if (scan.All(char.IsDigit)) LabNoFilter = scan;
                    else Name = scan;
                }

                List = await _lab.GetIndentPageAsync(LabNoFilter, Hrno, Name, PendingOnly, PageNumber, PageSize);
                ActiveTab = "list";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading the lab work list: " + ex.Message;
                ActiveTab = string.IsNullOrWhiteSpace(LabNo) ? "list" : "entry";
            }
            return Page();
        }

        // =====================================================================
        // AJAX - LIST / ENTRY
        // =====================================================================

        /// <summary>Partial for the List tab (search + paging without a full reload).</summary>
        public async Task<IActionResult> OnGetListAsync()
        {
            List = await _lab.GetIndentPageAsync(LabNoFilter, Hrno, Name, PendingOnly, PageNumber, PageSize);
            return Partial("Partial/_ListGrid", this);
        }

        /// <summary>Partial for the Entry tab (header + tests + sub-tabs).</summary>
        public async Task<IActionResult> OnGetEntryAsync(string labNo, string tid, string testNo)
        {
            Entry = await _lab.GetEntryAsync(labNo, tid, testNo);
            GroupCode = Entry?.GroupCode ?? "";
            Readings = Entry?.Readings ?? new List<LabReadingRow>();
            Culture = Entry?.Culture ?? new LabCultureView();
            if (!string.IsNullOrWhiteSpace(Entry?.Error)) ErrorMessage = Entry.Error;
            return Partial("Partial/_EntryPane", this);
        }

        /// <summary>Partial for the Test Result sub-tab.</summary>
        public async Task<IActionResult> OnGetReadingsAsync(string labNo, string tid, string testNo, string gid)
        {
            Readings = await _lab.GetReadingsAsync(labNo, tid, testNo, includeAllMaster: true);
            GroupCode = gid ?? "";
            return Partial("Partial/_ResultGrid", this);
        }

        /// <summary>Partial for the Update Culture sub-tab (creates the row in edit mode).</summary>
        public async Task<IActionResult> OnGetCultureAsync(string labNo, string tid, string testNo, string gid, bool edit)
        {
            Culture = await _culture.GetAsync(labNo, tid, testNo, ensure: edit);
            GroupCode = gid ?? "";
            return Partial("Partial/_CulturePane", this);
        }

        // =====================================================================
        // AJAX - PICK LISTS
        // =====================================================================

        public async Task<IActionResult> OnGetResultChoicesAsync(string gid, string readId)
            => new JsonResult(await _lab.GetResultChoicesAsync(gid, readId));

        public async Task<IActionResult> OnGetRemarksAsync(string tid)
            => new JsonResult(await _lab.GetRemarksAsync(tid));

        public async Task<IActionResult> OnGetOrganismsAsync()
            => new JsonResult(await _culture.GetOrganismsAsync());

        public async Task<IActionResult> OnGetGrowthRemarksAsync()
            => new JsonResult(await _culture.GetGrowthRemarksAsync());

        public async Task<IActionResult> OnGetMedicinesAsync(string term)
            => new JsonResult(await _culture.GetMedicinesAsync(term, 30));

        /// <summary>All medicines not yet on the test (capped at 500 in CultureService).</summary>
        public async Task<IActionResult> OnGetAllMedicinesAsync(string labNo, string tid, string testNo)
            => new JsonResult(await _culture.GetAllMedicinesAsync(labNo, tid, testNo));

        // =====================================================================
        // AJAX - GRID ACTIONS
        // =====================================================================

        public async Task<IActionResult> OnPostLoadAllGroupAsync(string labNo, string tid, string testNo, string gid)
        {
            Readings = await _lab.LoadAllGroupAsync(labNo, tid, testNo);
            GroupCode = gid ?? "";
            return Partial("Partial/_ResultGrid", this);
        }

        public async Task<IActionResult> OnPostRemoveBlankAsync(string labNo, string tid, string testNo, string gid)
        {
            Readings = await _lab.RemoveBlankAsync(labNo, tid, testNo);
            GroupCode = gid ?? "";
            return Partial("Partial/_ResultGrid", this);
        }

        public async Task<IActionResult> OnPostEnsureCultureAsync(string labNo, string tid, string testNo, string gid)
        {
            Culture = await _culture.GetAsync(labNo, tid, testNo, ensure: true);
            GroupCode = gid ?? "";
            return Partial("Partial/_CulturePane", this);
        }

        // =====================================================================
        // AJAX - SAVE
        // =====================================================================

        /// <summary>
        /// Saves the whole test (REPORT.ATYPE, TESTTR, READTR, CULTURE, CUL_TR)
        /// and returns the URL of the next pending test.
        /// </summary>
        public async Task<IActionResult> OnPostSaveAsync([FromBody] LabSaveRequest request)
        {
            try
            {
                var user = _appSession.LoggedUser?.UserName ?? "WEB";
                var result = await _lab.SaveAsync(request, user);
                return new JsonResult(result);
            }
            catch (LabConcurrencyException ex)
            {
                Response.StatusCode = 409;
                return new JsonResult(new LabSaveResult { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return new JsonResult(new LabSaveResult
                {
                    Success = false,
                    Message = "Error saving results: " + ex.Message
                });
            }
        }
    }
}
