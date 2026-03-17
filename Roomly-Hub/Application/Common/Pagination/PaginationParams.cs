namespace Application.Common.Pagination
{


    public class PaginationParams
    {
        public PaginationParams()
        { }

        public int Page { get; set; } = 1;
        public int Total { get; set; }
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int PageCount { get; set; }
    }
}