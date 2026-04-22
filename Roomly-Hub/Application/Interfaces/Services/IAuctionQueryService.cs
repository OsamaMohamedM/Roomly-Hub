using Application.Common.Pagination;
using Application.Common.Results;
using Application.DTOs.Auctions;

namespace Application.Interfaces.Services
{
    public interface IAuctionQueryService
    {
        Task<Result<PagedResult<AuctionSummaryDto>>> GetActiveAuctionsAsync(int page, int pageSize, CancellationToken ct);
        Task<Result<AuctionResponseDto>> GetAuctionByIdAsync(Guid auctionId, CancellationToken ct);
        Task<Result<List<BidResponseDto>>> GetMyBidsAsync(Guid userId, CancellationToken ct);
        Task<Result<List<AuctionSummaryDto>>> GetMyAuctionsAsync(Guid hostId, CancellationToken ct);
    }
}
