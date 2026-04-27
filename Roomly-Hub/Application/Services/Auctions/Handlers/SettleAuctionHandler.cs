using Application.Common.Results;
using Application.Interfaces.BackgroundJobs;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Entities.Auctions;
using Domain.enums.Auction;
using Domain.enums.Notifications;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Services.Auctions.Handlers
{
    public sealed class SettleAuctionHandler
    {
        private readonly ILogger<SettleAuctionHandler> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuctionRepository _auctionRepository;
        private readonly IAuctionHubService _auctionHubService;
        private readonly INotificationService _notificationService;
        private readonly IBackgroundJob _backgroundJob;

        public SettleAuctionHandler(
            ILogger<SettleAuctionHandler> logger,
            IUnitOfWork unitOfWork,
            IAuctionRepository auctionRepository,
            IAuctionHubService auctionHubService,
            INotificationService notificationService,
            IBackgroundJob backgroundJob)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _auctionRepository = auctionRepository;
            _auctionHubService = auctionHubService;
            _notificationService = notificationService;
            _backgroundJob = backgroundJob;
        }

        public async Task<Result> HandleAsync(Guid auctionId, CancellationToken ct)
        {
            _logger.LogInformation("SettleAuction started — AuctionId: {AuctionId}", auctionId);

            return await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                var auction = await _auctionRepository.GetByIdWithBidsAsync(auctionId, token);
                if (auction == null)
                {
                    _logger.LogWarning("SettleAuction — Auction {AuctionId} not found. Skipping.", auctionId);
                    return Result.Success();
                }

                if (auction.Status != AuctionStatus.Active)
                {
                    _logger.LogWarning("SettleAuction — Auction {AuctionId} already in status {Status}. Skipping.", auctionId, auction.Status);
                    return Result.Success();
                }

                var winningBid = auction.Bids.FirstOrDefault(b => b.IsWinning);
                if (winningBid == null)
                    return await ExpireWithNoBidsAsync(auction, token);

                return await SetPendingPaymentAsync(auction, winningBid, token);
            }, ct, IsolationLevel.Serializable);
        }

        private async Task<Result> ExpireWithNoBidsAsync(Auction auction, CancellationToken ct)
        {
            _logger.LogInformation("SettleAuction — No bids on {AuctionId}. Expiring.", auction.Id);
            auction.ExpireNoBids();
            await _unitOfWork.SaveChangesAsync(ct);
            await _auctionHubService.BroadcastAuctionEndedAsync(auction.Id, AuctionStatus.Expired_NoBids);
            return Result.Success();
        }

        private async Task<Result> SetPendingPaymentAsync(Auction auction, AuctionBid topBid, CancellationToken ct)
        {
            var topBidder = auction.Bids
                .Where(b => b.Status != BidStatus.Forfeited)
                .OrderByDescending(b => b.Amount)
                .First();

            var deadline = DateTime.UtcNow.AddMinutes(AuctionConstants.WinnerPaymentWindowMinutes);
            auction.SetPendingPayment(topBidder.BidderId, deadline);
            _backgroundJob.ScheduleAuctionPaymentTimeout(auction.Id, TimeSpan.FromMinutes(AuctionConstants.WinnerPaymentWindowMinutes));
            await _unitOfWork.SaveChangesAsync(ct);

            await _auctionHubService.BroadcastAuctionEndedAsync(auction.Id, AuctionStatus.Pending_Payment);
            await _auctionHubService.SendPaymentRequiredAsync(topBidder.BidderId, auction.Id, topBidder.Amount, deadline);
            await _notificationService.SendAsync(
                topBidder.BidderId,
                NotificationType.AuctionWon,
                "You Won the Auction!",
                $"You won the auction with a bid of {topBidder.Amount:F2}. Please complete payment before {deadline:HH:mm UTC}.",
                $"/auctions/{auction.Id}/pay", ct);

            _logger.LogInformation("Auction settled — AuctionId: {AuctionId}, WinnerId: {WinnerId}, Deadline: {Deadline}",
                auction.Id, topBidder.BidderId, deadline);

            return Result.Success();
        }
    }
}