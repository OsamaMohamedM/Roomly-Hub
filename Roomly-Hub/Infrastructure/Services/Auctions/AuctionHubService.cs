using Application.Interfaces.Services;
using Domain.enums.Auction;
using Infrastructure.Services.Auctions.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Services.Auctions
{
    public class AuctionHubService : IAuctionHubService
    {
        private readonly IHubContext<AuctionHub> _context;

        public AuctionHubService(IHubContext<AuctionHub> context)
        {
            _context = context;
        }

        public Task BroadcastNewBidAsync(Guid auctionId, decimal currentHighest, int bidCount, decimal minNextBid)
        {
            return _context.Clients.Group($"auction-{auctionId}")
                .SendAsync("auction:new-bid", new
                {
                    AuctionId = auctionId,
                    CurrentHighest = currentHighest,
                    BidCount = bidCount,
                    MinNextBid = minNextBid
                });
        }

        public Task BroadcastExtendedAsync(Guid auctionId, DateTime newEndTime)
        {
            return _context.Clients.Group($"auction-{auctionId}")
                .SendAsync("auction:extended", new
                {
                    AuctionId = auctionId,
                    NewEndTime = newEndTime
                });
        }

        public Task BroadcastAuctionEndedAsync(Guid auctionId, AuctionStatus status)
        {
            return _context.Clients.Group($"auction-{auctionId}")
                .SendAsync("auction:ended", new
                {
                    AuctionId = auctionId,
                    Status = status.ToString()
                });
        }

        public Task BroadcastCancelledAsync(Guid auctionId)
        {
            return _context.Clients.Group($"auction-{auctionId}")
                .SendAsync("auction:cancelled", new
                {
                    AuctionId = auctionId
                });
        }

        public Task SendPaymentRequiredAsync(Guid winnerId, Guid auctionId, decimal amount, DateTime deadline)
        {
            return _context.Clients.User(winnerId.ToString())
                .SendAsync("auction:payment-required", new
                {
                    AuctionId = auctionId,
                    Amount = amount,
                    Deadline = deadline
                });
        }

        public Task SendOutbidNotificationAsync(Guid outbidUserId, Guid auctionId, decimal newHighest)
        {
            return _context.Clients.User(outbidUserId.ToString())
                .SendAsync("auction:outbid", new
                {
                    AuctionId = auctionId,
                    NewHighest = newHighest
                });
        }
    }
}