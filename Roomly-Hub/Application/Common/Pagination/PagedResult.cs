namespace Application.Common.Pagination
{
    public class PagedResult<T> where T : class
    {
        public PagedResult(List<T> items, int count, int pageNumber, int pageSize)
        {
            TotalCount = count;
            PageSize = pageSize;
            CurrentPage = pageNumber;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            Items = items;
        }

        public List<T> Items { get; }
        public int CurrentPage { get; }
        public int TotalPages { get; }
        public int PageSize { get; }
        public int TotalCount { get; }
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
    }
}