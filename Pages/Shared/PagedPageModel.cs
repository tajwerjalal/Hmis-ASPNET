using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HmisWeb.Pages.Shared
{
    /// <summary>
    /// Paging contract shared by every service list screen (setup grids and
    /// the service-list reports) so the same pager partial can render on all
    /// of them.
    /// </summary>
    public interface IPagedGrid
    {
        int Page { get; }
        int PageSize { get; }
        int TotalCount { get; }
        int TotalPages { get; }
        int FirstRow { get; }
        int LastRow { get; }
        bool HasPrevious { get; }
        bool HasNext { get; }
    }

    /// <summary>
    /// Pager state handed to the shared _Pager partial.
    ///
    /// It builds on the original <see cref="PagerModel"/> (the view model the
    /// billing screens already use) and adds the extra members the shared
    /// partial needs (the first/last row of the window and the prev/next
    /// flags).
    /// </summary>
    public class PagerState : PagerModel, IPagedGrid
    {
        /// <summary>1-based page number.</summary>
        public int Page
        {
            get => PageIndex;
            set => PageIndex = value;
        }

        public int FirstRow => TotalCount == 0 ? 0 : ((PageIndex - 1) * PageSize) + 1;
        public int LastRow => System.Math.Min(PageIndex * PageSize, TotalCount);
        public bool HasPrevious => PageIndex > 1;
        public bool HasNext => PageIndex < TotalPages;
    }

    /// <summary>
    /// Base page model with server-side paging.
    ///
    /// Two query-string spellings are accepted so the older billing/lab
    /// screens (which use "Pg") and the newer service lists (which use
    /// "page") can share one implementation and one pager partial:
    ///   PageNumber / Pg  - the requested page number
    ///   PageSize         - rows per page (25 / 50 / 100 / 200)
    ///
    /// Developer : [TAJWER JALAL]
    /// Created   : 22-Oct-2026 20:40 (restored + extended: the original
    ///             base of the billing/lab paging was overwritten while
    ///             adding pagination to the service lists; the legacy API
    ///             Pg / Skip / Take / Pager is kept intact here).
    /// Change Log: 22-Oct-2026 20:55 - legacy API restored, pager state and
    ///             ApplyPaging/ClampPaging helpers added.
    /// </summary>
    public abstract class PagedPageModel : PageModel, IPagedGrid
    {
        /// <summary>The page shown (interface member of IPagedGrid).</summary>
        int IPagedGrid.Page => PageNumber;

        /// <summary>
        /// 1-based page number (query key "PageNumber"). The key cannot be
        /// called "page": Razor Pages uses the route value "page" for the page
        /// path itself, so a query key of that name is shadowed by the route
        /// value and an int can never bind from it.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Legacy spelling of the page number ("Pg" query key) used by the
        /// billing screens. It is a plain property on purpose - a computed
        /// setter interfered with the binding of "page" - and the two keys are
        /// reconciled in <see cref="ClampPaging"/>.
        /// </summary>
        [BindProperty(Name = "Pg", SupportsGet = true)]
        public int Pg { get; set; }

        /// <summary>Rows per page.</summary>
        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 50;

        /// <summary>Total number of rows matching the filter (all pages).</summary>
        public int TotalCount { get; set; }

        public int TotalPages => PageSize <= 0 ? 1 : (int)System.Math.Ceiling(TotalCount / (double)PageSize);
        public int FirstRow => TotalCount == 0 ? 0 : ((EffectivePage - 1) * PageSize) + 1;
        public int LastRow => System.Math.Min(EffectivePage * PageSize, TotalCount);
        public bool HasPrevious => EffectivePage > 1;
        public bool HasNext => EffectivePage < TotalPages;

        /// <summary>
        /// The page actually shown, clamped into range.
        ///
        /// The legacy "Pg" key is reconciled here (not only in
        /// <see cref="ClampPaging"/>) because the older billing screens read
        /// <see cref="Skip"/> straight after setting TotalCount and never call
        /// ClampPaging - without this they ignored "Pg" and always showed page
        /// one.
        /// </summary>
        public int EffectivePage
        {
            get
            {
                SyncLegacyPageKey();
                if (PageNumber < 1) PageNumber = 1;
                if (TotalPages > 0 && PageNumber > TotalPages) PageNumber = TotalPages;
                return PageNumber;
            }
        }

        /// <summary>
        /// Copies the legacy "Pg" key over to PageNumber when the canonical
        /// key was not given (PageNumber is 1 or less and Pg carries a page).
        /// </summary>
        private void SyncLegacyPageKey()
        {
            if (Pg > 0 && PageNumber <= 1)
                PageNumber = Pg;
        }

        /// <summary>Rows to skip for the current page (legacy helper).</summary>
        public int Skip => (EffectivePage - 1) * (PageSize <= 0 ? 50 : PageSize);

        /// <summary>Rows to take for the current page (legacy helper).</summary>
        public int Take => PageSize <= 0 ? 50 : PageSize;

        /// <summary>Pager state for the shared _Pager partial.</summary>
        public PagerState Pager => new PagerState
        {
            Page = EffectivePage,
            PageSize = PageSize <= 0 ? 50 : PageSize,
            TotalCount = TotalCount,
            // the legacy PagerModel stores TotalPages instead of computing it,
            // so it has to be handed over explicitly - otherwise the pager
            // prints "page X of 1" and hides the Next link
            TotalPages = TotalPages
        };

        /// <summary>
        /// Fixes an out-of-range request: an unknown page size falls back to
        /// 50 rows and a page beyond the last one is clamped, so a stale link
        /// never shows an empty grid while rows do exist.
        /// </summary>
        protected void ClampPaging()
        {
            // "PageNumber" and the legacy "Pg" key are both accepted: the
            // canonical key wins when it was given, otherwise the legacy key
            // is used
            SyncLegacyPageKey();

            if (PageSize <= 0) PageSize = 50;
            if (PageNumber < 1) PageNumber = 1;
            if (TotalPages > 0 && PageNumber > TotalPages) PageNumber = TotalPages;
        }

        /// <summary>Applies the current page window to a query.</summary>
        protected IQueryable<T> ApplyPaging<T>(IQueryable<T> query)
            => query.Skip((EffectivePage - 1) * (PageSize <= 0 ? 50 : PageSize)).Take(PageSize <= 0 ? 50 : PageSize);

        /// <summary>Applies the current page window to an in-memory list.</summary>
        protected List<T> ApplyPaging<T>(IEnumerable<T> rows)
            => rows.Skip((EffectivePage - 1) * (PageSize <= 0 ? 50 : PageSize)).Take(PageSize <= 0 ? 50 : PageSize).ToList();
    }
}
