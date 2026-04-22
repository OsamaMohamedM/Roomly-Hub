using Domain.Entities.Auctions;
using Domain.enums.Auction;
using Domain.Interfaces.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class AuctionRepository : IAuctionRepository
    {
        private readonly AppDbContext _context;

        public AuctionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Auction?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _context.Set<Auction>()
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, ct);
        }

        public async Task<Auction?> GetByIdWithBidsAsync(Guid id, CancellationToken ct)
        {
            return await _context.Set<Auction>()
                .Include(a => a.Bids.OrderByDescending(b => b.Amount))
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, ct);
        }

        public async Task<Auction?> GetActiveByRoomIdAsync(Guid roomId, CancellationToken ct)
        {
            return await _context.Set<Auction>()
                .FirstOrDefaultAsync(a => a.RoomId == roomId && a.Status == AuctionStatus.Active && !a.IsDeleted, ct);
        }

        public async Task<int> CountActiveByHostIdAsync(Guid hostId, CancellationToken ct)
        {
            return await _context.Set<Auction>()
                .CountAsync(a => a.HostId == hostId && a.Status == AuctionStatus.Active && !a.IsDeleted, ct);
        }

        public async Task<List<Auction>> GetActiveAsync(CancellationToken ct)
        {
            return await _context.Set<Auction>()
                .Where(a => a.Status == AuctionStatus.Active && a.EndTime > DateTime.UtcNow && !a.IsDeleted)
                .OrderBy(a => a.EndTime)
                .ToListAsync(ct);
        }

        public async Task<List<Auction>> GetByHostIdAsync(Guid hostId, CancellationToken ct)
        {
            return await _context.Set<Auction>()
                .Where(a => a.HostId == hostId && !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<List<AuctionBid>> GetTopBiddersAsync(Guid auctionId, int count, CancellationToken ct)
        {
            return await _context.Set<AuctionBid>()
                .Where(b => b.AuctionId == auctionId && b.Status != BidStatus.Forfeited && !b.IsDeleted)
                .OrderByDescending(b => b.Amount)
                .Take(count)
                .ToListAsync(ct);
        }

        public async Task<List<AuctionBid>> GetBidsByUserAsync(Guid userId, CancellationToken ct)
        {
            return await _context.Set<AuctionBid>()
                .Where(b => b.BidderId == userId && !b.IsDeleted)
                .OrderByDescending(b => b.PlacedAt)
                .ToListAsync(ct);
        }

        public async Task AddAsync(Auction auction, CancellationToken ct)
        {
            await _context.Set<Auction>().AddAsync(auction, ct);
        }

        public void Update(Auction auction)
        {
            _context.Set<Auction>().Update(auction);
        }
    }
}