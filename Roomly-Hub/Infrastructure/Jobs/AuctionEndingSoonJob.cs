using Application.Interfaces.Services;
using Domain.enums.Auction;
using Domain.enums.Notifications;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs
{
    public class AuctionEndingSoonJob
    {
        private readonly IAuctionRepository _auctionRepository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AuctionEndingSoonJob> _logger;

        public AuctionEndingSoonJob(
            IAuctionRepository auctionRepository,
            INotificationService notificationService,
            ILogger<AuctionEndingSoonJob> logger)
        {
            _auctionRepository = auctionRepository;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task ExecuteAsync(Guid auctionId)
        {
            var auction = await _auctionRepository.GetByIdWithBidsAsync(auctionId, CancellationToken.None);
            if (auction == null || auction.Status != AuctionStatus.Active)
                return;

            var uniqueBidders = auction.Bids.Select(x => x.BidderId).Distinct().ToList();
            var count = 0;
            foreach (var bidderId in uniqueBidders)
            {
                await _notificationService.SendAsync(
                    bidderId,
                    NotificationType.AuctionEndingSoon,
                    "Auction Ending Soon!",
                    "The auction is ending soon. Place your best bid now.",
                    $"/auctions/{auctionId}",
                    CancellationToken.None);
                count++;
            }

            _logger.LogInformation("Auction ending-soon notifications sent. AuctionId: {AuctionId}, Count: {Count}", auctionId, count);
        }
    }
}
