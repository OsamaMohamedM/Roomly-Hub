using Application.Common.Constants;
using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Mappers;
using Application.Common.Results;
using Application.DTOs.Booking;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Application.Interfaces.Services.Bookings;
using Domain.Entities.Booking;
using Domain.enums.Booking;
using Domain.Interfaces.Repositories;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Application.Services.Bookings
{
    public class BookingCommandService : IBookingCommandService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRoomRepository _roomRepository;
        private readonly IPaymentService _paymentService;
        private readonly IBookingPaymentRequestFactory _bookingPaymentRequestFactory;
        private readonly IValidator<BookingRequestDto> _bookingRequestValidator;
        private readonly IValidator<BookingCancelRequestDto> _bookingCancelValidator;
        private readonly IBookingMapper _bookingMapper;
        private readonly ILogger<BookingCommandService> _logger;

        public BookingCommandService(
            IBookingRepository bookingRepository,
            IUnitOfWork unitOfWork,
            IRoomRepository roomRepository,
            IPaymentService paymentService,
            IBookingPaymentRequestFactory bookingPaymentRequestFactory,
            IValidator<BookingRequestDto> bookingRequestValidator,
            IValidator<BookingCancelRequestDto> bookingCancelValidator,
            IBookingMapper bookingMapper,
            ILogger<BookingCommandService> logger)
        {
            _bookingRepository = bookingRepository;
            _unitOfWork = unitOfWork;
            _roomRepository = roomRepository;
            _paymentService = paymentService;
            _bookingPaymentRequestFactory = bookingPaymentRequestFactory;
            _bookingRequestValidator = bookingRequestValidator;
            _bookingCancelValidator = bookingCancelValidator;
            _bookingMapper = bookingMapper;
            _logger = logger;
        }

        public async Task<Result<string>> CancelBookingAsync(BookingCancelRequestDto bookingCancelRequestDto, CancellationToken cancellation = default)
        {
            _logger.LogInformation("Starting booking cancellation for booking {BookingId}", bookingCancelRequestDto.BookingId);
            var validationResult = await _bookingCancelValidator.ValidateAsync(bookingCancelRequestDto, cancellation);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Booking cancellation validation failed for booking {BookingId}", bookingCancelRequestDto.BookingId);
                return Result<string>.Failure(
                    Errors.Codes.Common.ValidationError,
                    Errors.Messages.Common.RequestValidationFailed,
                    ValidationHelper.ToErrorDictionary(validationResult));
            }

            var booking = await _bookingRepository.GetBookingByIdAsync(bookingCancelRequestDto.BookingId, cancellation);
            if (booking == null)
            {
                _logger.LogWarning("Booking cancellation failed. Booking {BookingId} not found", bookingCancelRequestDto.BookingId);
                return Result<string>.Failure(Errors.Codes.Booking.BookingNotFound, $"{Errors.Messages.Booking.BookingNotFound} With This Id : {bookingCancelRequestDto.BookingId}");
            }

            if (bookingCancelRequestDto.GuestId == null || booking.GuestId != bookingCancelRequestDto.GuestId)
            {
                _logger.LogWarning("Booking cancellation unauthorized for booking {BookingId}", bookingCancelRequestDto.BookingId);
                return Result<string>.Failure(Errors.Codes.Common.UnauthorizedAction, Errors.Messages.Common.UserNotFound);
            }

            if (booking.Status == BookingStatus.Cancelled)
            {
                _logger.LogWarning("Booking {BookingId} is already cancelled", bookingCancelRequestDto.BookingId);
                return Result<string>.Failure(Errors.Codes.Booking.CancellationNotAllowed, Errors.Messages.Booking.CancellationNotAllowed);
            }

            booking.CancelBooking(bookingCancelRequestDto.GuestId.Value);
            await _bookingRepository.UpdateBookingAsync(booking, cancellation);
            await _unitOfWork.SaveChangesAsync(cancellation);
            _logger.LogInformation("Booking {BookingId} cancelled successfully", bookingCancelRequestDto.BookingId);
            return Result<string>.Success("Booking cancelled successfully.");
        }

        public async Task<Result<CreateBookingResponseDto>> CreateBookingAsync(BookingRequestDto bookingRequestDto, CancellationToken cancellation = default)
        {
            _logger.LogInformation("Starting booking creation for room {RoomId} and guest {GuestId}", bookingRequestDto.RoomId, bookingRequestDto.UserId);
            var validationResult = await _bookingRequestValidator.ValidateAsync(bookingRequestDto, cancellation);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Booking creation validation failed for room {RoomId} and guest {GuestId}", bookingRequestDto.RoomId, bookingRequestDto.UserId);
                return Result<CreateBookingResponseDto>.Failure(
                    Errors.Codes.Common.ValidationError,
                    Errors.Messages.Common.RequestValidationFailed,
                    ValidationHelper.ToErrorDictionary(validationResult));
            }

            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    var room = await _roomRepository.GetByIdAsync(bookingRequestDto.RoomId, token);
                    if (room == null)
                    {
                        return Result<CreateBookingResponseDto>.Failure(Errors.Codes.Room.RoomNotFound, $"{Errors.Messages.Room.RoomNotFound} With This Id : {bookingRequestDto.RoomId}");
                    }

                    if (!room.CanBeBooked())
                    {
                        return Result<CreateBookingResponseDto>.Failure(Errors.Codes.Booking.RoomNotAvailable, Errors.Messages.Booking.RoomNotAvailable);
                    }

                    if (HasBlockedDates(room, bookingRequestDto.StartDate, bookingRequestDto.EndDate))
                    {
                        return Result<CreateBookingResponseDto>.Failure(Errors.Codes.Booking.RoomNotAvailable, Errors.Messages.Booking.RoomNotAvailable);
                    }

                    var canBookRoom = room.BookingMode == BookingMode.RequestAndApprove
                        ? await _bookingRepository.IsRoomAvailableAsync(
                            bookingRequestDto.RoomId,
                            bookingRequestDto.StartDate,
                            bookingRequestDto.EndDate,
                            [BookingStatus.Confirmed, BookingStatus.Completed],
                            null,
                            token)
                        : await _bookingRepository.IsRoomAvailableAsync(
                            bookingRequestDto.RoomId,
                            bookingRequestDto.StartDate,
                            bookingRequestDto.EndDate,
                            token);

                    if (!canBookRoom)
                    {
                        return Result<CreateBookingResponseDto>.Failure(Errors.Codes.Booking.RoomNotAvailable, Errors.Messages.Booking.RoomNotAvailable);
                    }

                    var nights = (bookingRequestDto.EndDate.Date - bookingRequestDto.StartDate.Date).Days;
                    if (nights <= 0)
                    {
                        return Result<CreateBookingResponseDto>.Failure(Errors.Codes.Booking.InvalidBookingDateRange, Errors.Messages.Booking.InvalidBookingDateRange);
                    }

                    var totalPrice = room.PricePerNight * nights;

                    var booking = Booking.CreateBooking(
                        bookingRequestDto.UserId,
                        bookingRequestDto.RoomId,
                        bookingRequestDto.StartDate,
                        bookingRequestDto.EndDate,
                        totalPrice,
                        bookingRequestDto.PaymentMethod,
                        PaymentStatus.Pending,
                        room.BookingMode,
                        room.Source,
                        room.CancellationPolicy);

                    await _bookingRepository.AddBookingAsync(booking, token);
                    await _unitOfWork.SaveChangesAsync(token);

                    string? paymentUrl = null;

                    if (room.BookingMode == BookingMode.InstantBook)
                    {
                        var paymentRequest = _bookingPaymentRequestFactory.Create(booking, bookingRequestDto.PaymentMethodId, bookingRequestDto.RedirectionUrls);
                        var response = await _paymentService.CreateEInvoiceAsync(paymentRequest);
                        if (response == null || string.IsNullOrWhiteSpace(response.Url) || string.IsNullOrWhiteSpace(response.InvoiceId) || string.IsNullOrWhiteSpace(response.InvoiceKey))
                        {
                            return Result<CreateBookingResponseDto>.Failure(Errors.Codes.Booking.PaymentFailed, Errors.Messages.Booking.PaymentFailed);
                        }

                        booking.SetPaymentInvoice(response.InvoiceId, response.InvoiceKey);
                        await _bookingRepository.UpdateBookingAsync(booking, token);
                        await _unitOfWork.SaveChangesAsync(token);
                        paymentUrl = response.Url;
                    }

                    _logger.LogInformation("Booking {BookingId} created successfully with status {Status}", booking.Id, booking.Status);

                    return Result<CreateBookingResponseDto>.Success(new CreateBookingResponseDto
                    {
                        BookingId = booking.Id,
                        Status = booking.Status,
                        PaymentUrl = paymentUrl
                    });
                }, cancellation);
            }
            catch (ConcurrencyException)
            {
                _logger.LogWarning("Booking creation concurrency conflict for room {RoomId} and guest {GuestId}", bookingRequestDto.RoomId, bookingRequestDto.UserId);
                return Result<CreateBookingResponseDto>.Failure(Errors.Codes.Booking.ConcurrencyConflict, Errors.Messages.Booking.ConcurrencyConflict);
            }
        }

        public async Task<Result<BookingSummaryDto>> UpdateBookingAsync(BookingRequestDto bookingRequestDto, CancellationToken cancellation = default)
        {
            _logger.LogInformation("Starting booking update for booking {BookingId}", bookingRequestDto.BookingId);
            var validationResult = await _bookingRequestValidator.ValidateAsync(bookingRequestDto, cancellation);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Booking update validation failed for booking {BookingId}", bookingRequestDto.BookingId);
                return Result<BookingSummaryDto>.Failure(
                    Errors.Codes.Common.ValidationError,
                    Errors.Messages.Common.RequestValidationFailed,
                    ValidationHelper.ToErrorDictionary(validationResult));
            }

            if (!bookingRequestDto.BookingId.HasValue || bookingRequestDto.BookingId.Value == Guid.Empty)
            {
                return Result<BookingSummaryDto>.Failure(Errors.Codes.Common.ValidationError, "BookingId is required for update.");
            }

            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    var booking = await _bookingRepository.GetBookingByIdAsync(bookingRequestDto.BookingId.Value, token);
                    if (booking == null)
                    {
                        return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.BookingNotFound, $"{Errors.Messages.Booking.BookingNotFound} With This Id : {bookingRequestDto.BookingId.Value}");
                    }

                    if (booking.GuestId != bookingRequestDto.UserId)
                    {
                        return Result<BookingSummaryDto>.Failure(Errors.Codes.Common.UnauthorizedAction, Errors.Messages.Common.UserNotFound);
                    }

                    var room = await _roomRepository.GetByIdAsync(booking.RoomId, token);
                    if (room == null)
                    {
                        return Result<BookingSummaryDto>.Failure(Errors.Codes.Room.RoomNotFound, $"{Errors.Messages.Room.RoomNotFound} With This Id : {booking.RoomId}");
                    }

                    if (!room.CanBeBooked())
                    {
                        return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.RoomNotAvailable, Errors.Messages.Booking.RoomNotAvailable);
                    }

                    if (HasBlockedDates(room, bookingRequestDto.StartDate, bookingRequestDto.EndDate))
                    {
                        return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.RoomNotAvailable, Errors.Messages.Booking.RoomNotAvailable);
                    }

                    var canBookRoom = room.BookingMode == BookingMode.RequestAndApprove
                        ? await _bookingRepository.IsRoomAvailableAsync(
                            booking.RoomId,
                            bookingRequestDto.StartDate,
                            bookingRequestDto.EndDate,
                            new[] { BookingStatus.Confirmed, BookingStatus.Completed },
                            booking.Id,
                            token)
                        : await _bookingRepository.IsRoomAvailableAsync(
                            booking.RoomId,
                            bookingRequestDto.StartDate,
                            bookingRequestDto.EndDate,
                            booking.Id,
                            token);

                    if (!canBookRoom)
                    {
                        return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.RoomNotAvailable, Errors.Messages.Booking.RoomNotAvailable);
                    }

                    booking.UpdateBooking(bookingRequestDto.StartDate, bookingRequestDto.EndDate);

                    await _bookingRepository.UpdateBookingAsync(booking, token);
                    await _unitOfWork.SaveChangesAsync(token);

                    return Result<BookingSummaryDto>.Success(_bookingMapper.ToSummaryDto(booking));
                }, cancellation);
            }
            catch (ConcurrencyException)
            {
                _logger.LogWarning("Booking update concurrency conflict for booking {BookingId}", bookingRequestDto.BookingId);
                return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.ConcurrencyConflict, Errors.Messages.Booking.ConcurrencyConflict);
            }
        }

        public async Task<Result> ApproveBookingRequestAsync(Guid hostId, Guid bookingId, CancellationToken cancellation = default)
        {
            _logger.LogInformation("Host {HostId} is approving booking {BookingId}", hostId, bookingId);
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    var booking = await _bookingRepository.GetBookingByIdAsync(bookingId, token);
                    if (booking == null)
                    {
                        return Result.Failure(Errors.Codes.Booking.BookingNotFound, Errors.Messages.Booking.BookingNotFound);
                    }

                    if (booking.Room == null || booking.Room.HostId != hostId)
                    {
                        return Result.Failure(Errors.Codes.Common.UnauthorizedAction, Errors.Messages.Room.UnauthorizedAction);
                    }

                    if (booking.Status != BookingStatus.Pending)
                    {
                        return Result.Failure(Errors.Codes.Booking.InvalidBookingState, Errors.Messages.Booking.InvalidBookingState);
                    }

                    if (booking.IsApprovedByHost)
                    {
                        return Result.Failure(Errors.Codes.Booking.InvalidBookingState, Errors.Messages.Booking.InvalidBookingState);
                    }

                    var canConfirmBooking = await _bookingRepository.IsRoomAvailableAsync(
                        booking.RoomId,
                        booking.CheckInDate,
                        booking.CheckOutDate,
                        new[] { BookingStatus.Confirmed, BookingStatus.Completed },
                        booking.Id,
                        token);

                    if (!canConfirmBooking)
                    {
                        return Result.Failure(Errors.Codes.Booking.RoomNotAvailable, Errors.Messages.Booking.RoomNotAvailable);
                    }

                    booking.ApproveByHost();

                    var competingRequests = await _bookingRepository.GetOverlappingPendingRequestsAsync(
                        booking.RoomId,
                        booking.CheckInDate,
                        booking.CheckOutDate,
                        booking.Id,
                        token);

                    foreach (var competing in competingRequests)
                    {
                        competing.CancelBooking(hostId);
                        await _bookingRepository.UpdateBookingAsync(competing, token);
                    }

                    await _bookingRepository.UpdateBookingAsync(booking, token);
                    await _unitOfWork.SaveChangesAsync(token);
                    _logger.LogInformation("Booking {BookingId} approved by host {HostId}. Rejected {Count} competing requests", bookingId, hostId, competingRequests.Count());
                    return Result.Success();
                }, cancellation);
            }
            catch (ConcurrencyException)
            {
                _logger.LogWarning("Booking approval concurrency conflict for booking {BookingId} and host {HostId}", bookingId, hostId);
                return Result.Failure(Errors.Codes.Booking.ConcurrencyConflict, Errors.Messages.Booking.ConcurrencyConflict);
            }
        }

        public async Task<Result> RejectBookingRequestAsync(Guid hostId, Guid bookingId, CancellationToken cancellation = default)
        {
            _logger.LogInformation("Host {HostId} is rejecting booking {BookingId}", hostId, bookingId);
            var booking = await _bookingRepository.GetBookingByIdAsync(bookingId, cancellation);
            if (booking == null)
            {
                return Result.Failure(Errors.Codes.Booking.BookingNotFound, Errors.Messages.Booking.BookingNotFound);
            }

            if (booking.Room == null || booking.Room.HostId != hostId)
            {
                return Result.Failure(Errors.Codes.Common.UnauthorizedAction, Errors.Messages.Room.UnauthorizedAction);
            }

            if (booking.Status != BookingStatus.Pending)
            {
                return Result.Failure(Errors.Codes.Booking.InvalidBookingState, Errors.Messages.Booking.InvalidBookingState);
            }

            booking.CancelBooking(hostId);
            await _bookingRepository.UpdateBookingAsync(booking, cancellation);
            await _unitOfWork.SaveChangesAsync(cancellation);
            _logger.LogInformation("Booking {BookingId} rejected by host {HostId}", bookingId, hostId);
            return Result.Success();
        }

        private static bool HasBlockedDates(Domain.Entities.Rooms.Room room, DateTime checkIn, DateTime checkOut)
        {
            var checkInDate = DateOnly.FromDateTime(checkIn.Date);
            var checkOutDate = DateOnly.FromDateTime(checkOut.Date);

            return room.Availabilities.Any(a =>
                !a.IsDeleted &&
                a.BlockedDate >= checkInDate &&
                a.BlockedDate < checkOutDate);
        }
    }
}