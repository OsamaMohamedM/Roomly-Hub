using Application.Common.Constants;
using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Mappers;
using Application.Common.Results;
using Application.DTOs.Booking;
using Application.DTOs.Payment;
using Application.DTOs.Payment.FawaterkRequest;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Domain.Entities.Booking;
using Domain.enums.Booking;
using Domain.Interfaces.Repositories;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Application.Services.Bookings
{
    public class BookingServices : IBookingServices
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRoomRepository _roomRepository;
        private readonly IPaymentService _paymentService;
        private readonly IValidator<CreateBookingPaymentRequestDto> _createBookingPaymentValidator;
        private readonly IValidator<BookingRequestDto> _bookingRequestValidator;
        private readonly IValidator<BookingCancelRequestDto> _bookingCancelValidator;
        private readonly IBookingMapper _bookingMapper;
        private readonly ILogger<BookingServices> _logger;

        public BookingServices(
            IBookingRepository bookingRepository,
            IUnitOfWork unitOfWork,
            IRoomRepository roomRepository,
            IPaymentService paymentService,
            IValidator<CreateBookingPaymentRequestDto> createBookingPaymentValidator,
            IValidator<BookingRequestDto> bookingRequestValidator,
            IValidator<BookingCancelRequestDto> bookingCancelValidator,
            IBookingMapper bookingMapper,
            ILogger<BookingServices> logger)
        {
            _bookingRepository = bookingRepository;
            _unitOfWork = unitOfWork;
            _roomRepository = roomRepository;
            _paymentService = paymentService;
            _createBookingPaymentValidator = createBookingPaymentValidator;
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

        public async Task<Result<string>> CreateBookingAsync(BookingRequestDto bookingRequestDto, CancellationToken cancellation = default)
        {
            _logger.LogInformation("Starting booking creation for room {RoomId} and guest {GuestId}", bookingRequestDto.RoomId, bookingRequestDto.UserId);
            var validationResult = await _bookingRequestValidator.ValidateAsync(bookingRequestDto, cancellation);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Booking creation validation failed for room {RoomId} and guest {GuestId}", bookingRequestDto.RoomId, bookingRequestDto.UserId);
                return Result<string>.Failure(
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
                        return Result<string>.Failure(Errors.Codes.Room.RoomNotFound, $"{Errors.Messages.Room.RoomNotFound} With This Id : {bookingRequestDto.RoomId}");
                    }

                    if (!room.CanBeBooked())
                    {
                        return Result<string>.Failure(Errors.Codes.Booking.RoomNotAvailable, Errors.Messages.Booking.RoomNotAvailable);
                    }

                    if (HasBlockedDates(room, bookingRequestDto.StartDate, bookingRequestDto.EndDate))
                    {
                        return Result<string>.Failure(Errors.Codes.Booking.RoomNotAvailable, Errors.Messages.Booking.RoomNotAvailable);
                    }

                    var canBookRoom = room.BookingMode == BookingMode.RequestAndApprove
                        ? await _bookingRepository.IsRoomAvailableAsync(
                            bookingRequestDto.RoomId,
                            bookingRequestDto.StartDate,
                            bookingRequestDto.EndDate,
                            new[] { BookingStatus.Confirmed, BookingStatus.Completed },
                            null,
                            token)
                        : await _bookingRepository.IsRoomAvailableAsync(
                            bookingRequestDto.RoomId,
                            bookingRequestDto.StartDate,
                            bookingRequestDto.EndDate,
                            token);

                    if (!canBookRoom)
                    {
                        return Result<string>.Failure(Errors.Codes.Booking.RoomNotAvailable, Errors.Messages.Booking.RoomNotAvailable);
                    }

                    var totalPrice = room.PricePerNight * (decimal)(bookingRequestDto.EndDate - bookingRequestDto.StartDate).TotalDays;

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

                    var resultMessage = booking.Status == BookingStatus.Pending
                        ? "Booking request submitted and awaiting host approval."
                        : $"Booking is confirmed and the total price is {totalPrice}";

                    _logger.LogInformation("Booking {BookingId} created successfully with status {Status}", booking.Id, booking.Status);

                    return Result<string>.Success(resultMessage);
                }, cancellation);
            }
            catch (ConcurrencyException)
            {
                _logger.LogWarning("Booking creation concurrency conflict for room {RoomId} and guest {GuestId}", bookingRequestDto.RoomId, bookingRequestDto.UserId);
                return Result<string>.Failure(Errors.Codes.Booking.ConcurrencyConflict, Errors.Messages.Booking.ConcurrencyConflict);
            }
        }

        public async Task<Result<EInvoiceResponseData>> CreateBookingPaymentInvoiceAsync(Guid guestId, CreateBookingPaymentRequestDto requestDto, CancellationToken cancellation = default)
        {
            _logger.LogInformation("Starting payment invoice creation for booking {BookingId} and guest {GuestId}", requestDto.BookingId, guestId);

            var validationResult = await _createBookingPaymentValidator.ValidateAsync(requestDto, cancellation);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Payment invoice validation failed for booking {BookingId}", requestDto.BookingId);
                return Result<EInvoiceResponseData>.Failure(
                    Errors.Codes.Common.ValidationError,
                    Errors.Messages.Common.RequestValidationFailed,
                    ValidationHelper.ToErrorDictionary(validationResult));
            }

            var booking = await _bookingRepository.GetBookingByIdAsync(requestDto.BookingId, cancellation);
            if (booking == null)
            {
                _logger.LogWarning("Payment invoice creation failed. Booking {BookingId} not found", requestDto.BookingId);
                return Result<EInvoiceResponseData>.Failure(Errors.Codes.Booking.BookingNotFound, Errors.Messages.Booking.BookingNotFound);
            }

            if (booking.GuestId != guestId)
            {
                _logger.LogWarning("Payment invoice unauthorized for booking {BookingId} and guest {GuestId}", requestDto.BookingId, guestId);
                return Result<EInvoiceResponseData>.Failure(Errors.Codes.Common.UnauthorizedAction, Errors.Messages.Common.UserNotFound);
            }

            if (booking.Status != BookingStatus.Confirmed)
            {
                _logger.LogWarning("Payment invoice blocked for booking {BookingId}. Status is {Status}", requestDto.BookingId, booking.Status);
                return Result<EInvoiceResponseData>.Failure(Errors.Codes.Booking.InvalidBookingState, Errors.Messages.Booking.InvalidBookingState);
            }

            if (booking.PaymentStatus == PaymentStatus.Paid)
            {
                _logger.LogWarning("Payment invoice skipped for booking {BookingId}. Already paid", requestDto.BookingId);
                return Result<EInvoiceResponseData>.Failure(Errors.Codes.Booking.InvalidBookingState, "Booking is already paid.");
            }

            var paymentRequest = new EInvoiceRequestModel
            {
                PaymentMethodId = requestDto.PaymentMethodId,
                CartItems = new List<CartItemModel>
                {
                    new()
                    {
                        Price = booking.TotalPrice,
                        Quantity = 1
                    }
                },
                PayLoad = new EInvoicePayload
                {
                    OrderId = booking.Id.ToString(),
                },
                RedirectionUrls = requestDto.RedirectionUrls
            };

            var response = await _paymentService.CreateEInvoiceAsync(paymentRequest);
            if (response == null)
            {
                _logger.LogError("Payment invoice creation failed from provider for booking {BookingId}", requestDto.BookingId);
                return Result<EInvoiceResponseData>.Failure(Errors.Codes.Booking.PaymentFailed, Errors.Messages.Booking.PaymentFailed);
            }

            _logger.LogInformation("Payment invoice created for booking {BookingId} with invoice key {InvoiceKey}", requestDto.BookingId, response.InvoiceKey);
            return Result<EInvoiceResponseData>.Success(response);
        }

        public async Task<Result<IEnumerable<BookingSummaryDto>>> GetBookingsByGuestAsync(Guid guestId, CancellationToken cancellation = default)
        {
            _logger.LogInformation("Loading bookings for guest {GuestId}", guestId);
            var bookings = await _bookingRepository.GetBookingsByGuestIdAsync(guestId, cancellation);
            var bookingSummaries = bookings.Select(_bookingMapper.ToSummaryDto).ToList();

            return Result<IEnumerable<BookingSummaryDto>>.Success(bookingSummaries);
        }

        public async Task<Result<BookingSummaryDto>> GetBookingSummaryAsync(Guid bookingId, CancellationToken cancellation = default)
        {
            _logger.LogInformation("Loading booking summary for booking {BookingId}", bookingId);
            var booking = await _bookingRepository.GetBookingByIdAsync(bookingId, cancellation);
            if (booking == null)
            {
                return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.BookingNotFound, $"{Errors.Messages.Booking.BookingNotFound} With This Id : {bookingId}");
            }

            return Result<BookingSummaryDto>.Success(_bookingMapper.ToSummaryDto(booking));
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

        public async Task<Result<IEnumerable<HostBookingRequestDto>>> GetPendingRequestsForHostAsync(Guid hostId, CancellationToken cancellation = default)
        {
            _logger.LogInformation("Loading pending booking requests for host {HostId}", hostId);
            var requests = await _bookingRepository.GetPendingRequestsByHostIdAsync(hostId, cancellation);
            var response = requests.Select(_bookingMapper.ToHostRequestDto).ToList();

            return Result<IEnumerable<HostBookingRequestDto>>.Success(response);
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

                    booking.MarkAsConfirmed();

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

        public async Task<Result<BookingSummaryDto>> MarkBookingAsPaidAsync(Guid bookingId, CancellationToken cancellation = default)
        {
            _logger.LogInformation("Marking booking {BookingId} as paid", bookingId);

            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    var booking = await _bookingRepository.GetBookingByIdAsync(bookingId, token);
                    if (booking == null)
                    {
                        return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.BookingNotFound, $"{Errors.Messages.Booking.BookingNotFound} With This Id : {bookingId}");
                    }

                    if (booking.Status == BookingStatus.Cancelled)
                    {
                        return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.InvalidBookingState, Errors.Messages.Booking.InvalidBookingState);
                    }

                    if (booking.PaymentStatus != PaymentStatus.Paid)
                    {
                        booking.MarkAsPaid();
                        await _bookingRepository.UpdateBookingAsync(booking, token);
                        await _unitOfWork.SaveChangesAsync(token);
                        _logger.LogInformation("Booking {BookingId} marked as paid", bookingId);
                    }

                    return Result<BookingSummaryDto>.Success(_bookingMapper.ToSummaryDto(booking));
                }, cancellation);
            }
            catch (ConcurrencyException)
            {
                _logger.LogWarning("Booking payment concurrency conflict for booking {BookingId}", bookingId);
                return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.ConcurrencyConflict, Errors.Messages.Booking.ConcurrencyConflict);
            }
        }

        public async Task<Result<BookingSummaryDto>> MarkBookingAsRefundedAsync(Guid bookingId, CancellationToken cancellation = default)
        {
            _logger.LogInformation("Marking booking {BookingId} as refunded", bookingId);

            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    var booking = await _bookingRepository.GetBookingByIdAsync(bookingId, token);
                    if (booking == null)
                    {
                        return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.BookingNotFound, $"{Errors.Messages.Booking.BookingNotFound} With This Id : {bookingId}");
                    }

                    if (booking.PaymentStatus != PaymentStatus.Paid)
                    {
                        return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.InvalidBookingState, Errors.Messages.Booking.InvalidBookingState);
                    }

                    booking.MarkAsRefunded();
                    await _bookingRepository.UpdateBookingAsync(booking, token);
                    await _unitOfWork.SaveChangesAsync(token);

                    return Result<BookingSummaryDto>.Success(_bookingMapper.ToSummaryDto(booking));
                }, cancellation);
            }
            catch (ConcurrencyException)
            {
                _logger.LogWarning("Booking refund concurrency conflict for booking {BookingId}", bookingId);
                return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.ConcurrencyConflict, Errors.Messages.Booking.ConcurrencyConflict);
            }
        }

        public async Task<Result<IEnumerable<HostBookingRequestDto>>> GetHostRoomBookingsAsync(Guid hostId, Guid roomId, DateTime from, DateTime to, CancellationToken cancellation = default)
        {
            if (roomId == Guid.Empty || from >= to)
            {
                return Result<IEnumerable<HostBookingRequestDto>>.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestValidationFailed);
            }

            var bookings = await _bookingRepository.GetRoomBookingsForHostAsync(hostId, roomId, from, to, cancellation);
            var result = bookings.Select(_bookingMapper.ToHostRequestDto).ToList();
            return Result<IEnumerable<HostBookingRequestDto>>.Success(result);
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