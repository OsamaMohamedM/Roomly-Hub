using Application.Common.Constants;
using Application.Common.Results;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Application.Interfaces.Services.Wallet;
using Domain.Interfaces.Repositories;
using Domain.enums.Notifications;
using Microsoft.Extensions.Logging;

namespace Application.Services.Auctions.Handlers
{
    public sealed class CancelAuctionHandler
    {
        private readonly ILogger<CancelAuctionHandler> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuctionRepository _auctionRepository;
        private readonly IWalletCommandService _walletCommandService;
        private readonly IAuctionHubService _auctionHubService;
        private readonly INotificationService _notificationService;

        public CancelAuctionHandler(
            ILogger<CancelAuctionHandler> logger,
            IUnitOfWork unitOfWork,
            IAuctionRepository auctionRepository,
            IWalletCommandService walletCommandService,
            IAuctionHubService auctionHubService,
            INotificationService notificationService)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _auctionRepository = auctionRepository;
            _walletCommandService = walletCommandService;
            _auctionHubService = auctionHubService;
            _notificationService = notificationService;
        }

        public async Task<Result> HandleAsync(Guid hostId, Guid auctionId, CancellationToken ct)
        {
            _logger.LogInformation("CancelAuction started — HostId: {HostId}, AuctionId: {AuctionId}", hostId, auctionId);

            var auction = await _auctionRepository.GetByIdWithBidsAsync(auctionId, ct);
            if (auction == null)
                return Result.Failure(Errors.Codes.Auction.NotFound, "Auction not found.");

            if (auction.HostId != hostId)
                return Result.Failure(Errors.Codes.Common.UnauthorizedAction, "You are not the host of this auction.");

            if (!auction.CanCancel())
                return Result.Failure(Errors.Codes.Auction.CannotCancel, "This auction cannot be cancelled.");

            await ReleaseAllLockedInsurancesAsync(auction, ct);
            _logger.LogDebug("Hangfire cancellation pending IBackgroundJob.DeleteJob — SettlementJobId: {SId}", auction.SettlementJobId);

            auction.CancelByHost();
            await _unitOfWork.SaveChangesAsync(ct);

            await _auctionHubService.BroadcastCancelledAsync(auctionId);
            await NotifyAllBiddersAsync(auction, auctionId, ct);

            _logger.LogInformation("Auction cancelled — AuctionId: {AuctionId}", auctionId);
            return Result.Success();
        }

        private async Task ReleaseAllLockedInsurancesAsync(Domain.Entities.Auctions.Auction auction, CancellationToken ct)
        {
            var bidderIds = auction.Bids
                .Where(b => b.InsuranceLocked)
                .Select(b => b.BidderId)
                .Distinct();

            foreach (var bidderId in bidderIds)
                await _walletCommandService.ReleaseAuctionInsuranceAsync(
                    bidderId, auction.Id, auction.InsuranceDepositAmount, ct);
        }

        private async Task NotifyAllBiddersAsync(Domain.Entities.Auctions.Auction auction, Guid auctionId, CancellationToken ct)
        {
            var uniqueBidderIds = auction.Bids.Select(b => b.BidderId).Distinct();
            foreach (var bidderId in uniqueBidderIds)
                await _notificationService.SendAsync(
                    bidderId,
                    NotificationType.AuctionCancelled,
                    "Auction Cancelled",
                    "The auction you participated in has been cancelled by the host.",
                    $"/auctions/{auctionId}", ct);
        }
    }
}