using Application.Common.Constants;
using Application.Common.Helpers;
using Application.Common.Results;
using Application.DTOs.Booking;
using Application.Events.Notifications;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services.Wallet;
using Domain.enums.Booking;
using Domain.Interfaces.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Services.Bookings
{
    public class CancelBookingSaga
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<BookingCancelRequestDto> _bookingCancelValidator;
        private readonly IWalletCommandService _walletCommandService;
        private readonly IPublisher _publisher;
        private readonly ILogger<CancelBookingSaga> _logger;

        public CancelBookingSaga(
            IBookingRepository bookingRepository,
            IUnitOfWork unitOfWork,
            IValidator<BookingCancelRequestDto> bookingCancelValidator,
            IWalletCommandService walletCommandService,
            IPublisher publisher,
            ILogger<CancelBookingSaga> logger)
        {
            _bookingRepository = bookingRepository;
            _unitOfWork = unitOfWork;
            _bookingCancelValidator = bookingCancelValidator;
            _walletCommandService = walletCommandService;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task<Result<string>> ExecuteAsync(BookingCancelRequestDto dto, CancellationToken cancellation = default)
        {
            _logger.LogInformation("Starting booking cancellation saga for booking {BookingId}", dto.BookingId);
            var validationResult = await _bookingCancelValidator.ValidateAsync(dto, cancellation);
            if (!validationResult.IsValid)
            {
                return Result<string>.Failure(
                    Errors.Codes.Common.ValidationError,
                    Errors.Messages.Common.RequestValidationFailed,
                    ValidationHelper.ToErrorDictionary(validationResult));
            }

            var booking = await _bookingRepository.GetBookingByIdAsync(dto.BookingId, cancellation);
            if (booking == null)
            {
                return Result<string>.Failure(Errors.Codes.Booking.BookingNotFound, $"{Errors.Messages.Booking.BookingNotFound} With This Id : {dto.BookingId}");
            }

            if (dto.GuestId == null || booking.GuestId != dto.GuestId)
            {
                return Result<string>.Failure(Errors.Codes.Common.UnauthorizedAction, Errors.Messages.Common.UserNotFound);
            }

            if (booking.Status == BookingStatus.Cancelled)
            {
                return Result<string>.Failure(Errors.Codes.Booking.CancellationNotAllowed, Errors.Messages.Booking.CancellationNotAllowed);
            }

            if (booking.PaymentStatus == PaymentStatus.Paid && booking.PaymentMethod == PaymentMethod.Wallet)
            {
                var refundResult = await _walletCommandService.RefundBookingAsync(booking.GuestId, booking.Id, booking.TotalPrice, cancellation);
                if (refundResult.IsFailure)
                {
                    _logger.LogWarning("Cancellation saga stopped because wallet refund failed for booking {BookingId}. ErrorCode: {ErrorCode}", booking.Id, refundResult.ErrorCode);
                    return Result<string>.Failure(refundResult.ErrorCode!, refundResult.ErrorMessage!);
                }
            }

            booking.CancelBooking(dto.GuestId.Value);
            await _bookingRepository.UpdateBookingAsync(booking, cancellation);
            await _unitOfWork.SaveChangesAsync(cancellation);

            if (booking.Room != null)
            {
                try
                {
                    await _publisher.Publish(new BookingCancelledEvent(booking.Id, booking.GuestId, booking.Room.HostId, booking.RoomId), cancellation);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish BookingCancelledEvent for booking {BookingId}", booking.Id);
                }
            }

            return Result<string>.Success("Booking cancelled successfully.");
        }
    }
}
