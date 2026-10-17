namespace HmisWeb.Pages.Shared
{
    /// <summary>View model for the shared <c>_Pager</c> partial.</summary>
    public class PagerModel
    {
        public int PageIndex { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 25;
    }
}