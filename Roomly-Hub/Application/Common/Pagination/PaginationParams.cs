namespace Application.Common.Pagination
{


    public class PaginationParams
    {
        private int _pageSize = 10;
        private const int MaxPageSize = 100;

        public PaginationParams()
        { }

        public int Page { get; set; } = 1;
        public int Total { get; set; }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value < 1 ? 10 : value;
        }

        public int TotalPages { get; set; }
        public int PageCount { get; set; }
    }
}