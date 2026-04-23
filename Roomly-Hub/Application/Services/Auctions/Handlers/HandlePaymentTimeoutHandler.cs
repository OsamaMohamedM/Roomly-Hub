using Application.Common.Results;
using Application.Interfaces.BackgroundJobs;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Application.Interfaces.Services.Wallet;
using Domain.Constants;
using Domain.Entities.Auctions;
using Domain.enums.Auction;
using Domain.enums.Notifications;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Application.Services.Auctions.Handlers
{
    public sealed class HandlePaymentTimeoutHandler
    {
        private readonly ILogger<HandlePaymentTimeoutHandler> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuctionRepository _auctionRepository;
        private readonly IWalletCommandService _walletCommandService;
        private readonly IAuctionHubService _auctionHubService;
        private readonly INotificationService _notificationService;
        private readonly IBackgroundJob _backgroundJob;

        public HandlePaymentTimeoutHandler(
            ILogger<HandlePaymentTimeoutHandler> logger,
            IUnitOfWork unitOfWork,
            IAuctionRepository auctionRepository,
            IWalletCommandService walletCommandService,
            IAuctionHubService auctionHubService,
            INotificationService notificationService,
            IBackgroundJob backgroundJob)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _auctionRepository = auctionRepository;
            _walletCommandService = walletCommandService;
            _auctionHubService = auctionHubService;
            _notificationService = notificationService;
            _backgroundJob = backgroundJob;
        }

        public async Task<Result> HandleAsync(Guid auctionId, CancellationToken ct)
        {
            _logger.LogInformation("HandlePaymentTimeout started — AuctionId: {AuctionId}", auctionId);

            var auction = await _auctionRepository.GetByIdWithBidsAsync(auctionId, ct);
            if (auction == null)
                return Result.Success();

            if (auction.Status != AuctionStatus.Pending_Payment)
            {
                _logger.LogWarning("HandlePaymentTimeout — Auction {AuctionId} status is {Status}. Skipping.", auctionId, auction.Status);
                return Result.Success();
            }

            var defaulterId = auction.WinnerId!.Value;
            var defaulterBid = auction.Bids.FirstOrDefault(b => b.BidderId == defaulterId && b.IsWinning);

            if (defaulterBid == null)
            {
                _logger.LogError("HandlePaymentTimeout — Could not find winning bid for defaulter {DefaulterId} on auction {AuctionId}.", defaulterId, auctionId);
                return Result.Success(); // defensive — already handled
            }

            await PenaliseDefaulterAsync(auction, defaulterId, defaulterBid, auctionId, ct);

            var nextEligible = await FindNextEligibleBidderAsync(auctionId, defaulterId, ct);

            if (nextEligible == null)
                return await FullyDefaultAsync(auction, defaulterId, auctionId, ct);

            return await CascadeToNextAsync(auction, nextEligible, auctionId, ct);
        }

        private async Task PenaliseDefaulterAsync(
            Auction auction, Guid defaulterId, AuctionBid defaulterBid, Guid auctionId, CancellationToken ct)
        {
            if (auction.CurrentCascadeDepth == 0)
                await _walletCommandService.ForfeitAuctionInsuranceAsync(
                    defaulterId, auctionId, auction.InsuranceDepositAmount, ct);
            else
                await _walletCommandService.ReleaseAuctionInsuranceAsync(
                    defaulterId, auctionId, auction.InsuranceDepositAmount, ct);

            defaulterBid.MarkForfeited();
        }

        private async Task<AuctionBid?> FindNextEligibleBidderAsync(Guid auctionId, Guid excludeBidderId, CancellationToken ct)
        {
            var candidates = await _auctionRepository.GetTopBiddersAsync(
                auctionId, AuctionConstants.MaxCascadeDepth + 1, ct);

            return candidates
                .Where(b => b.BidderId != excludeBidderId && b.Status != BidStatus.Forfeited)
                .OrderByDescending(b => b.Amount)
                .FirstOrDefault();
        }

        private async Task<Result> FullyDefaultAsync(Auction auction, Guid defaulterId, Guid auctionId, CancellationToken ct)
        {
            _logger.LogInformation("HandlePaymentTimeout — No eligible next bidder. Fully defaulting auction {AuctionId}.", auctionId);
            auction.FullyDefault();
            await _unitOfWork.SaveChangesAsync(ct);

            await _auctionHubService.BroadcastAuctionEndedAsync(auctionId, AuctionStatus.Fully_Defaulted);
            await _notificationService.SendAsync(
                defaulterId,
                NotificationType.AuctionFullyDefaulted,
                "Auction Fully Defaulted",
                "The auction has expired due to non-payment.",
                $"/auctions/{auctionId}", ct);

            return Result.Success();
        }

        private async Task<Result> CascadeToNextAsync(Auction auction, AuctionBid nextBidder, Guid auctionId, CancellationToken ct)
        {
            var newDeadline = DateTime.UtcNow.AddMinutes(AuctionConstants.CascadePaymentWindowMinutes);
            auction.CascadeToNext(nextBidder.BidderId, newDeadline);
            _backgroundJob.ScheduleAuctionPaymentTimeout(auctionId, TimeSpan.FromMinutes(AuctionConstants.CascadePaymentWindowMinutes));
            await _unitOfWork.SaveChangesAsync(ct);

            await _auctionHubService.SendPaymentRequiredAsync(nextBidder.BidderId, auctionId, nextBidder.Amount, newDeadline);
            await _notificationService.SendAsync(
                nextBidder.BidderId,
                NotificationType.AuctionPaymentRequired,
                "You're the New Winner!",
                $"The previous winner defaulted. You now have {AuctionConstants.CascadePaymentWindowMinutes} minutes to pay {nextBidder.Amount:F2}.",
                $"/auctions/{auctionId}/pay", ct);

            _logger.LogInformation("Auction cascaded — AuctionId: {AuctionId}, NewWinnerId: {NewWinnerId}", auctionId, nextBidder.BidderId);
            return Result.Success();
        }
    }
}