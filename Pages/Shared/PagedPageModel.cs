using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HmisWeb.Pages.Shared
{
    /// <summary>
    /// Base for any page that renders a paged list. Provides the paging properties and the
    /// <see cref="Pager"/> view model consumed by the shared <c>_Pager</c> partial.
    /// </summary>
    public abstract class PagedPageModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int Pg { get; set; } = 1;

        public int PageSize { get; set; } = 25;

        public int TotalCount { get; set; }

        public int TotalPages => PageSize <= 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

        protected int Skip => Pg < 1 ? 0 : (Pg - 1) * PageSize;

        protected int Take => PageSize;

        public PagerModel Pager => new PagerModel
        {
            PageIndex = Pg < 1 ? 1 : Pg,
            TotalPages = TotalPages,
            TotalCount = TotalCount,
            PageSize = PageSize
        };
    }
}