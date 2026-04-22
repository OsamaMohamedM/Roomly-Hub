using Domain.enums.Auction;

namespace Application.Interfaces.Services
{
    public interface IAuctionHubService
    {
        Task BroadcastNewBidAsync(Guid auctionId, decimal currentHighest, int bidCount, decimal minNextBid);

        Task BroadcastExtendedAsync(Guid auctionId, DateTime newEndTime);

        Task BroadcastAuctionEndedAsync(Guid auctionId, AuctionStatus status);

        Task BroadcastCancelledAsync(Guid auctionId);

        Task SendPaymentRequiredAsync(Guid winnerId, Guid auctionId, decimal amount, DateTime
        deadline);

        Task SendOutbidNotificationAsync(Guid outbidUserId, Guid auctionId, decimal
        newHighest);
    }
}