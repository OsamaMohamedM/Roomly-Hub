using Application.Common.Constants;
using Application.Common.Results;
using Application.DTOs.Auctions;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Application.Interfaces.Services.Wallet;
using Domain.Constants;
using Domain.Entities.Auctions;
using Domain.enums.Auction;
using Domain.enums.Notifications;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Application.Services.Auctions.Handlers
{
    public sealed class PlaceBidHandler
    {
        private readonly ILogger<PlaceBidHandler> _logger;
        private readonly IValidator<PlaceBidRequestDto> _validator;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuctionRepository _auctionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IWalletRepository _walletRepository;
        private readonly IWalletCommandService _walletCommandService;
        private readonly IAuctionHubService _auctionHubService;
        private readonly INotificationService _notificationService;

        public PlaceBidHandler(
            ILogger<PlaceBidHandler> logger,
            IValidator<PlaceBidRequestDto> validator,
            IUnitOfWork unitOfWork,
            IAuctionRepository auctionRepository,
            IUserRepository userRepository,
            IWalletRepository walletRepository,
            IWalletCommandService walletCommandService,
            IAuctionHubService auctionHubService,
            INotificationService notificationService)
        {
            _logger = logger;
            _validator = validator;
            _unitOfWork = unitOfWork;
            _auctionRepository = auctionRepository;
            _userRepository = userRepository;
            _walletRepository = walletRepository;
            _walletCommandService = walletCommandService;
            _auctionHubService = auctionHubService;
            _notificationService = notificationService;
        }

        public async Task<Result<PlaceBidResultDto>> HandleAsync(Guid bidderId, PlaceBidRequestDto dto, CancellationToken ct)
        {
            _logger.LogInformation("PlaceBid started — BidderId: {BidderId}, AuctionId: {AuctionId}, Amount: {Amount}",
                bidderId, dto.AuctionId, dto.Amount);

            var validationResult = await _validator.ValidateAsync(dto, ct);
            if (!validationResult.IsValid)
            {
                return Result<PlaceBidResultDto>.Failure(
                    Errors.Codes.Common.ValidationError,
                    Errors.Messages.Common.RequestValidationFailed);
            }

            return await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                var auction = await _auctionRepository.GetByIdWithBidsAsync(dto.AuctionId, token);
                if (auction == null)
                {
                    return Result<PlaceBidResultDto>.Failure(Errors.Codes.Auction.NotFound, "Auction not found.");
                }

                if (!auction.CanBid())
                {
                    return Result<PlaceBidResultDto>.Failure(Errors.Codes.Auction.NotActive, "Auction is not accepting bids.");
                }

                if (bidderId == auction.HostId)
                {
                    return Result<PlaceBidResultDto>.Failure(Errors.Codes.Auction.CannotBidOwnAuction, "Host cannot bid on their own auction.");
                }

                var bidder = await _userRepository.GetByIdAsync(bidderId);
                if (bidder == null)
                {
                    return Result<PlaceBidResultDto>.Failure(Errors.Codes.Common.UserNotFound, Errors.Messages.Common.UserNotFound);
                }

                if (bidder.KycStatus != SubmissionStatus.Approved)
                {
                    return Result<PlaceBidResultDto>.Failure(Errors.Codes.Common.UnauthorizedAction, "Bidder has not completed KYC verification.");
                }

                var minNextBid = auction.GetMinNextBid();
                if (dto.Amount < minNextBid)
                {
                    return Result<PlaceBidResultDto>.Failure(Errors.Codes.Auction.BidTooLow, $"Bid must be at least {minNextBid:F2}.");
                }

                var walletBalance = await _walletRepository.GetBalanceAsync(bidderId, token);
                if (walletBalance < auction.InsuranceDepositAmount)
                {
                    return Result<PlaceBidResultDto>.Success(new PlaceBidResultDto
                    {
                        InsufficientFunds = true,
                        RequiredAmount = auction.InsuranceDepositAmount
                    });
                }

                var prevWinningBid = auction.Bids.FirstOrDefault(b => b.IsWinning);
                if (prevWinningBid != null && prevWinningBid.BidderId == bidderId)
                {
                    return Result<PlaceBidResultDto>.Failure(Errors.Codes.Auction.BidTooLow, "You are already the highest bidder.");
                }

                var newBid = AuctionBid.Create(dto.AuctionId, bidderId, dto.Amount);
                newBid.MarkInsuranceLocked();

                var sniped = (auction.EndTime - DateTime.UtcNow).TotalSeconds <= AuctionConstants.AntiSnipingTriggerSeconds;

                var lockResult = await _walletCommandService.LockAuctionInsuranceAsync(bidderId, dto.AuctionId, auction.InsuranceDepositAmount, token);
                if (lockResult.IsFailure)
                {
                    return Result<PlaceBidResultDto>.Failure(lockResult.ErrorCode!, lockResult.ErrorMessage!);
                }

                if (prevWinningBid != null)
                {
                    var releaseResult = await _walletCommandService.ReleaseAuctionInsuranceAsync(
                        prevWinningBid.BidderId, dto.AuctionId, auction.InsuranceDepositAmount, token);
                    if (releaseResult.IsFailure)
                    {
                        return Result<PlaceBidResultDto>.Failure(releaseResult.ErrorCode!, releaseResult.ErrorMessage!);
                    }
                }

                if (prevWinningBid != null)
                {
                    prevWinningBid.MarkOutbid();
                }

                auction.UpdateHighestBid(dto.Amount, bidderId);

                if (sniped)
                {
                    auction.ExtendEndTime(AuctionConstants.AntiSnipingExtensionSeconds);
                }

                auction.Bids.Add(newBid);
                await _unitOfWork.SaveChangesAsync(token);

                await BroadcastAsync(auction, dto, newBid, prevWinningBid, sniped, token);

                _logger.LogInformation("Bid placed — AuctionId: {AuctionId}, BidderId: {BidderId}, Amount: {Amount}",
                    dto.AuctionId, bidderId, dto.Amount);

                return Result<PlaceBidResultDto>.Success(new PlaceBidResultDto
                {
                    Bid = newBid.ToBidDto(),
                    InsufficientFunds = false
                });
            }, ct);
        }

        private async Task BroadcastAsync(
            Auction auction, PlaceBidRequestDto dto, AuctionBid newBid,
            AuctionBid? prevWinningBid, bool sniped, CancellationToken ct)
        {
            int bidCount = auction.Bids.Count;
            decimal newMinNextBid = auction.GetMinNextBid();

            await _auctionHubService.BroadcastNewBidAsync(dto.AuctionId, dto.Amount, bidCount, newMinNextBid);

            if (sniped)
            {
                await _auctionHubService.BroadcastExtendedAsync(dto.AuctionId, auction.EndTime);
            }

            if (prevWinningBid != null)
            {
                await _auctionHubService.SendOutbidNotificationAsync(prevWinningBid.BidderId, dto.AuctionId, dto.Amount);
                await _notificationService.SendAsync(
                    prevWinningBid.BidderId,
                    NotificationType.AuctionOutbid,
                    "You've been outbid!",
                    $"Someone placed a higher bid of {dto.Amount:F2} on the auction.",
                    $"/auctions/{dto.AuctionId}", ct);
            }
        }
    }
}
