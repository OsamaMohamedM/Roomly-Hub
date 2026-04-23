using Application.Common.Constants;
using Application.Common.Results;
using Application.DTOs.Auctions;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Application.Interfaces.Services.Wallet;
using Domain.enums.Auction;
using Domain.enums.Notifications;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Application.Services.Auctions.Handlers
{
   
    public sealed class ProcessWinnerPaymentHandler
    {
        private readonly ILogger<ProcessWinnerPaymentHandler> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuctionRepository _auctionRepository;
        private readonly IWalletRepository _walletRepository;
        private readonly IWalletCommandService _walletCommandService;
        private readonly IBookingServices _bookingServices;
        private readonly INotificationService _notificationService;

        public ProcessWinnerPaymentHandler(
            ILogger<ProcessWinnerPaymentHandler> logger,
            IUnitOfWork unitOfWork,
            IAuctionRepository auctionRepository,
            IWalletRepository walletRepository,
            IWalletCommandService walletCommandService,
            IBookingServices bookingServices,
            INotificationService notificationService)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _auctionRepository = auctionRepository;
            _walletRepository = walletRepository;
            _walletCommandService = walletCommandService;
            _bookingServices = bookingServices;
            _notificationService = notificationService;
        }

        public async Task<Result> HandleAsync(Guid winnerId, AuctionPaymentRequestDto dto, CancellationToken ct)
        {
            _logger.LogInformation("ProcessWinnerPayment started — WinnerId: {WinnerId}, AuctionId: {AuctionId}", winnerId, dto.AuctionId);

            var auction = await _auctionRepository.GetByIdWithBidsAsync(dto.AuctionId, ct);
            if (auction == null)
                return Result.Failure(Errors.Codes.Auction.NotFound, "Auction not found.");

            if (auction.Status != AuctionStatus.Pending_Payment)
                return Result.Failure(Errors.Codes.Auction.NotPendingPayment, "Auction is not awaiting payment.");

            if (auction.WinnerId != winnerId)
                return Result.Failure(Errors.Codes.Auction.NotWinner, "You are not the winner of this auction.");

            if (auction.PaymentDeadline.HasValue && auction.PaymentDeadline.Value < DateTime.UtcNow)
                return Result.Failure(Errors.Codes.Auction.PaymentExpired, "Payment deadline has passed.");

            var winnerBid = auction.Bids.FirstOrDefault(b => b.BidderId == winnerId && b.IsWinning);
            if (winnerBid == null)
                return Result.Failure(Errors.Codes.Auction.NotWinner, "Winner bid not found.");

            var walletBalance = await _walletRepository.GetBalanceAsync(winnerId, ct);
            if (walletBalance < winnerBid.Amount)
                return Result.Failure(Errors.Codes.Wallet.InsufficientFunds, Errors.Messages.Wallet.InsufficientFunds);
            await _walletCommandService.ChargeForAuctionAsync(winnerId, dto.AuctionId, winnerBid.Amount, ct);
            await _walletCommandService.ReleaseAuctionInsuranceAsync(winnerId, dto.AuctionId, auction.InsuranceDepositAmount, ct);
            winnerBid.MarkPaid();
            auction.Complete();
            await _bookingServices.CreateFromAuctionAsync(auction, winnerId, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            await SendConfirmationNotificationsAsync(winnerId, winnerBid.Amount, ct);

            _logger.LogInformation("Auction payment processed — AuctionId: {AuctionId}, WinnerId: {WinnerId}, Amount: {Amount}",
                dto.AuctionId, winnerId, winnerBid.Amount);

            return Result.Success();
        }

        private async Task SendConfirmationNotificationsAsync(Guid winnerId, decimal amount, CancellationToken ct)
        {
            await _notificationService.SendAsync(
                winnerId,
                NotificationType.AuctionPaymentConfirmation,
                "Payment Confirmed",
                $"Your payment of {amount:F2} has been confirmed. Your booking is ready!",
                "/bookings", ct);

            await _notificationService.SendAsync(
                winnerId,
                NotificationType.BookingConfirmed,
                "Booking Confirmed",
                "Your booking from the auction has been created successfully.",
                "/bookings", ct);
        }
    }
}
