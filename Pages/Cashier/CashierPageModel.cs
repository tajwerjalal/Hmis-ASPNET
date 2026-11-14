using HmisWeb.Data;
using HmisWeb.Models;
using HmisWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Cashier
{
    /// <summary>
    /// Base page for every Cashier data-entry screen (receipts, payments, day
    /// end posting, refunds, misc fees, inpatient receipts and indents).
    ///
    /// The legacy VFP cashier opened a cash counter (CASHCNT row) before any
    /// transaction could be captured. This base class restores that rule: the
    /// signed-in user must have an open counter for the selected department,
    /// otherwise the data-entry page is not rendered and the user is sent to
    /// the "counter required" page.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 23-Oct-2026
    /// Time      : 12:35
    /// Change Log: 23-Oct-2026 - created; blocks cashier data entry until a
    ///             counter is open for the current user.
    /// </summary>
    public abstract class CashierPageModel : PageModel
    {
        /// <summary>Session key holding "CCode|LCode" of the counter the user opened/claimed.</summary>
        public const string CounterSessionKey = "CashierCounterKey";

        public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            var http = context.HttpContext;
            var db = http.RequestServices.GetRequiredService<ApplicationDbContext>();
            var appSession = http.RequestServices.GetRequiredService<IAppSession>();

            var user = appSession.LoggedUser;
            var dept = appSession.LoggedDepartment;

            if (user == null)
            {
                context.Result = new RedirectToPageResult("/Account/Login");
                return;
            }

            if (dept == null || await FindOpenCounterAsync(db, http, user.UserName, dept.Id) == null)
            {
                context.Result = new RedirectToPageResult("/Cashier/CounterRequired");
                return;
            }

            await next();
        }

        /// <summary>
        /// Resolves the counter that is currently open for the user.
        ///
        /// The session key set by Create Counter / Set Default Counter is tried
        /// first (it survives a counter code that is not the department code);
        /// if the session was lost (browser restart) the open CASHCNT row for
        /// the user + department is used instead.
        /// </summary>
        public static async Task<CashCounterSession> FindOpenCounterAsync(
            ApplicationDbContext db, HttpContext http, string userName, long departmentId)
        {
            var deptId = departmentId > int.MaxValue ? int.MaxValue : (int)departmentId;

            var key = http?.Session?.GetString(CounterSessionKey);
            if (!string.IsNullOrEmpty(key))
            {
                var separator = key.LastIndexOf('|');
                if (separator > 0 && int.TryParse(key.Substring(separator + 1), out var lineCode))
                {
                    var counterCode = key.Substring(0, separator);
                    var byKey = await db.CashCounterSessions
                        .FirstOrDefaultAsync(s => s.CCode == counterCode && s.LCode == lineCode && s.Status == "A");
                    if (byKey != null && byKey.LCode == deptId)
                        return byKey;
                }
            }

            if (string.IsNullOrWhiteSpace(userName))
                return null;

            var name = userName.Trim();
            return await db.CashCounterSessions
                .Where(s => s.Status == "A" && s.LCode == deptId && s.ActiveBy == name)
                .OrderByDescending(s => s.ActiveDt)
                .ThenByDescending(s => s.ActiveTm)
                .FirstOrDefaultAsync();
        }
    }
}
