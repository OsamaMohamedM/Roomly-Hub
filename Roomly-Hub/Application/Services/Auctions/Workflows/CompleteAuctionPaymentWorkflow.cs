using Application.Common.Constants;
using Application.Common.Results;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Application.Interfaces.Services.Wallet;
using Domain.Entities.Auctions;
using Domain.Entities.Booking;
using Domain.enums.Booking;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Application.Services.Auctions.Workflows
{
    public sealed class CompleteAuctionPaymentWorkflow
    {
        private readonly ILogger<CompleteAuctionPaymentWorkflow> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWalletCommandService _walletCommandService;
        private readonly IBookingRepository _bookingRepository;

        public CompleteAuctionPaymentWorkflow(
            ILogger<CompleteAuctionPaymentWorkflow> logger,
            IUnitOfWork unitOfWork,
            IPaymentService paymentService,
            IWalletCommandService walletCommandService,
            IBookingRepository bookingRepository)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _walletCommandService = walletCommandService;
            _bookingRepository = bookingRepository;
        }

        public async Task<Result> ExecuteAsync(Guid winnerId, Auction auction, PaymentMethod paymentMethod, CancellationToken ct)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                var checkInDateTime = auction.CheckInDate.ToDateTime(System.TimeOnly.MinValue);
                var checkOutDateTime = auction.CheckOutDate.ToDateTime(System.TimeOnly.MinValue);

                var existingBooking = await _bookingRepository.GetBookingsByGuestIdAsync(winnerId, token)
                    .ContinueWith(t => t.Result.FirstOrDefault(b =>
                        b.RoomId == auction.RoomId &&
                        b.CheckInDate == checkInDateTime &&
                        b.CheckOutDate == checkOutDateTime), token);

                if (existingBooking != null)
                {
                    _logger.LogWarning("Booking already exists for Winner {WinnerId}, Auction {AuctionId}", winnerId, auction.Id);
                    return Result.Success();
                }

                var winnerBid = auction.Bids.FirstOrDefault(b => b.BidderId == winnerId && b.IsWinning);
                if (winnerBid == null)
                {
                    _logger.LogError("Winner bid not found for auction {AuctionId}, WinnerId {WinnerId}", auction.Id, winnerId);
                    return Result.Failure(Errors.Codes.Auction.NotWinner, "Winner bid not found");
                }

                if (paymentMethod == PaymentMethod.Wallet)
                {
                    return await ExecuteWalletPaymentPathAsync(winnerId, auction, winnerBid, checkInDateTime, checkOutDateTime, token);
                }
                else
                {
                    return await ExecuteExternalPaymentPathAsync(winnerId, auction, winnerBid, paymentMethod, checkInDateTime, checkOutDateTime, token);
                }
            }, ct);
        }

        private async Task<Result> ExecuteWalletPaymentPathAsync(
            Guid winnerId,
            Auction auction,
            AuctionBid winnerBid,
            DateTime checkInDateTime,
            DateTime checkOutDateTime,
            CancellationToken token)
        {
            _logger.LogInformation("Processing wallet payment for auction {AuctionId}, WinnerId {WinnerId}, Amount {Amount}",
                auction.Id, winnerId, winnerBid.Amount);

            var chargeResult = await _walletCommandService.ChargeForAuctionAsync(winnerId, auction.Id, winnerBid.Amount, token);
            if (chargeResult.IsFailure)
            {
                _logger.LogError("Failed to charge wallet for auction {AuctionId}, WinnerId {WinnerId}: {ErrorCode} - {ErrorMessage}",
                    auction.Id, winnerId, chargeResult.ErrorCode, chargeResult.ErrorMessage);
                return chargeResult;
            }

            var releaseResult = await _walletCommandService.ReleaseAuctionInsuranceAsync(winnerId, auction.Id, auction.InsuranceDepositAmount, token);
            if (releaseResult.IsFailure)
            {
                _logger.LogError("Failed to release insurance for auction {AuctionId}, WinnerId {WinnerId}: {ErrorCode} - {ErrorMessage}",
                    auction.Id, winnerId, releaseResult.ErrorCode, releaseResult.ErrorMessage);
                return releaseResult;
            }

            winnerBid.MarkPaid();
            auction.Complete();

            var booking = Booking.CreateBooking(
                guestId: winnerId,
                roomId: auction.RoomId,
                checkInDate: checkInDateTime,
                checkOutDate: checkOutDateTime,
                totalPrice: winnerBid.Amount,
                paymentMethod: PaymentMethod.Wallet,
                paymentStatus: PaymentStatus.Paid,
                bookingMode: BookingMode.InstantBook,
                sourceStatus: SourceStatus.Auction,
                cancellationPolicy: CancellationPolicy.FreeCancellation);

            booking.MarkAsPaid();
            booking.MarkAsConfirmed();

            await _bookingRepository.AddBookingAsync(booking, token);
            await _unitOfWork.SaveChangesAsync(token);

            _logger.LogInformation("Wallet payment completed - AuctionId: {AuctionId}, WinnerId: {WinnerId}, BookingId: {BookingId}, Amount: {Amount}",
                auction.Id, winnerId, booking.Id, winnerBid.Amount);

            return Result.Success();
        }

        private async Task<Result> ExecuteExternalPaymentPathAsync(
            Guid winnerId,
            Auction auction,
            AuctionBid winnerBid,
            PaymentMethod paymentMethod,
            DateTime checkInDateTime,
            DateTime checkOutDateTime,
            CancellationToken token)
        {
            _logger.LogInformation("Processing external payment for auction {AuctionId}, WinnerId {WinnerId}, Amount {Amount}, Method {PaymentMethod}",
                auction.Id, winnerId, winnerBid.Amount, paymentMethod);

            winnerBid.MarkPaid();
            auction.Complete();

            var booking = Booking.CreateBooking(
                guestId: winnerId,
                roomId: auction.RoomId,
                checkInDate: checkInDateTime,
                checkOutDate: checkOutDateTime,
                totalPrice: winnerBid.Amount,
                paymentMethod: paymentMethod,
                paymentStatus: PaymentStatus.Pending,
                bookingMode: BookingMode.InstantBook,
                sourceStatus: SourceStatus.Auction,
                cancellationPolicy: CancellationPolicy.FreeCancellation);

            await _bookingRepository.AddBookingAsync(booking, token);
            await _unitOfWork.SaveChangesAsync(token);

            _logger.LogInformation("Booking created for external payment - AuctionId: {AuctionId}, BookingId: {BookingId}, WinnerId: {WinnerId}",
                auction.Id, booking.Id, winnerId);

            return Result.Success();
        }
    }
}