using Domain.Entities.Auctions;

namespace Domain.Interfaces.Repositories
{
    public interface IAuctionRepository
    {
        Task<Auction?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<Auction?> GetByIdWithBidsAsync(Guid id, CancellationToken ct);
        Task<Auction?> GetActiveByRoomIdAsync(Guid roomId, CancellationToken ct);
        Task<int> CountActiveByHostIdAsync(Guid hostId, CancellationToken ct);
        Task<int> CountActiveAsync(CancellationToken ct);
        Task<List<Auction>> GetActiveAsync(CancellationToken ct);
        Task<List<Auction>> GetActivePagedAsync(int page, int pageSize, CancellationToken ct);
        Task<List<Auction>> GetByHostIdAsync(Guid hostId, CancellationToken ct);
        Task<List<AuctionBid>> GetTopBiddersAsync(Guid auctionId, int count, CancellationToken ct);
        Task<List<AuctionBid>> GetBidsByUserAsync(Guid userId, CancellationToken ct);
        Task AddAsync(Auction auction, CancellationToken ct);
        void Update(Auction auction);
    }
}
