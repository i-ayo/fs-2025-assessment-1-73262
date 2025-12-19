namespace dublinBikeBlazor.DTO
{
    // Generic class for paged results
    public class PagedResult<T>
    {
        // List of items in the current page
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
