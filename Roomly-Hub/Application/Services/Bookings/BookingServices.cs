using Application.Common.Constants;
using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Mappers;
using Application.Common.Results;
using Application.DTOs.Booking;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Domain.Entities.Booking;
using Domain.enums.Booking;
using Domain.Interfaces.Repositories;
using FluentValidation;

namespace Application.Services.Bookings
{
    public class BookingServices : IBookingServices
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRoomRepository _roomRepository;
        private readonly IValidator<BookingRequestDto> _bookingRequestValidator;
        private readonly IValidator<BookingCancelRequestDto> _bookingCancelValidator;
        private readonly IBookingMapper _bookingMapper;

        public BookingServices(
            IBookingRepository bookingRepository,
            IUnitOfWork unitOfWork,
            IRoomRepository roomRepository,
            IValidator<BookingRequestDto> bookingRequestValidator,
            IValidator<BookingCancelRequestDto> bookingCancelValidator,
            IBookingMapper bookingMapper)
        {
            _bookingRepository = bookingRepository;
            _unitOfWork = unitOfWork;
            _roomRepository = roomRepository;
            _bookingRequestValidator = bookingRequestValidator;
            _bookingCancelValidator = bookingCancelValidator;
            _bookingMapper = bookingMapper;
        }

        public async Task<Result<string>> CancelBookingAsync(BookingCancelRequestDto bookingCancelRequestDto, CancellationToken cancellation = default)
        {
            var validationResult = await _bookingCancelValidator.ValidateAsync(bookingCancelRequestDto, cancellation);
            if (!validationResult.IsValid)
            {
                return Result<string>.Failure(
                    Errors.Codes.Common.ValidationError,
                    Errors.Messages.Common.RequestValidationFailed,
                    ValidationHelper.ToErrorDictionary(validationResult));
            }

            var booking = await _bookingRepository.GetBookingByIdAsync(bookingCancelRequestDto.BookingId, cancellation);
            if (booking == null)
            {
                return Result<string>.Failure(Errors.Codes.Booking.BookingNotFound, $"{Errors.Messages.Booking.BookingNotFound} With This Id : {bookingCancelRequestDto.BookingId}");
            }

            if (bookingCancelRequestDto.GuestId == null || booking.GuestId != bookingCancelRequestDto.GuestId)
            {
                return Result<string>.Failure(Errors.Codes.Common.UnauthorizedAction, Errors.Messages.Common.UserNotFound);
            }

            if (booking.Status == BookingStatus.Cancelled)
            {
                return Result<string>.Failure(Errors.Codes.Booking.CancellationNotAllowed, Errors.Messages.Booking.CancellationNotAllowed);
            }

            booking.CancelBooking(bookingCancelRequestDto.GuestId.Value);
            await _bookingRepository.UpdateBookingAsync(booking, cancellation);
            await _unitOfWork.SaveChangesAsync(cancellation);
            return Result<string>.Success("Booking cancelled successfully.");
        }

        public async Task<Result<string>> CreateBookingAsync(BookingRequestDto bookingRequestDto, CancellationToken cancellation = default)
        {
            var validationResult = await _bookingRequestValidator.ValidateAsync(bookingRequestDto, cancellation);
            if (!validationResult.IsValid)
            {
                return Result<string>.Failure(
                    Errors.Codes.Common.ValidationError,
                    Errors.Messages.Common.RequestValidationFailed,
                    ValidationHelper.ToErrorDictionary(validationResult));
            }

            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    var canBookRoom = await _bookingRepository.IsRoomAvailableAsync(
                        bookingRequestDto.RoomId,
                        bookingRequestDto.StartDate,
                        bookingRequestDto.EndDate,
                        token);

                    if (!canBookRoom)
                    {
                        return Result<string>.Failure(Errors.Codes.Booking.RoomNotAvailable, Errors.Messages.Booking.RoomNotAvailable);
                    }

                    var room = await _roomRepository.GetByIdAsync(bookingRequestDto.RoomId, token);
                    if (room == null)
                    {
                        return Result<string>.Failure(Errors.Codes.Room.RoomNotFound, $"{Errors.Messages.Room.RoomNotFound} With This Id : {bookingRequestDto.RoomId}");
                    }

                    if (!room.CanBeBooked())
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
                        bookingRequestDto.PaymentStatus,
                        room.BookingMode,
                        room.Source,
                        room.CancellationPolicy);

                    await _bookingRepository.AddBookingAsync(booking, token);
                    await _unitOfWork.SaveChangesAsync(token);

                    var resultMessage = booking.Status == BookingStatus.Pending
                        ? "Booking request submitted and awaiting host approval."
                        : $"Booking is confirmed and the total price is {totalPrice}";

                    return Result<string>.Success(resultMessage);
                }, cancellation);
            }
            catch (ConcurrencyException)
            {
                return Result<string>.Failure(Errors.Codes.Booking.ConcurrencyConflict, Errors.Messages.Booking.ConcurrencyConflict);
            }
        }

        public async Task<Result<IEnumerable<BookingSummaryDto>>> GetBookingsByGuestAsync(Guid guestId, CancellationToken cancellation = default)
        {
            var bookings = await _bookingRepository.GetBookingsByGuestIdAsync(guestId, cancellation);
            var bookingSummaries = bookings.Select(_bookingMapper.ToSummaryDto).ToList();

            return Result<IEnumerable<BookingSummaryDto>>.Success(bookingSummaries);
        }

        public async Task<Result<BookingSummaryDto>> GetBookingSummaryAsync(Guid bookingId, CancellationToken cancellation = default)
        {
            var booking = await _bookingRepository.GetBookingByIdAsync(bookingId, cancellation);
            if (booking == null)
            {
                return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.BookingNotFound, $"{Errors.Messages.Booking.BookingNotFound} With This Id : {bookingId}");
            }

            return Result<BookingSummaryDto>.Success(_bookingMapper.ToSummaryDto(booking));
        }

        public async Task<Result<BookingSummaryDto>> UpdateBookingAsync(BookingRequestDto bookingRequestDto, CancellationToken cancellation = default)
        {
            var validationResult = await _bookingRequestValidator.ValidateAsync(bookingRequestDto, cancellation);
            if (!validationResult.IsValid)
            {
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

                    var canBookRoom = await _bookingRepository.IsRoomAvailableAsync(
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
                return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.ConcurrencyConflict, Errors.Messages.Booking.ConcurrencyConflict);
            }
        }

        public async Task<Result<IEnumerable<HostBookingRequestDto>>> GetPendingRequestsForHostAsync(Guid hostId, CancellationToken cancellation = default)
        {
            var requests = await _bookingRepository.GetPendingRequestsByHostIdAsync(hostId, cancellation);
            var response = requests.Select(_bookingMapper.ToHostRequestDto).ToList();

            return Result<IEnumerable<HostBookingRequestDto>>.Success(response);
        }

        public async Task<Result> ApproveBookingRequestAsync(Guid hostId, Guid bookingId, CancellationToken cancellation = default)
        {
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

                    booking.MarkAsConfirmed();
                    await _bookingRepository.UpdateBookingAsync(booking, token);
                    await _unitOfWork.SaveChangesAsync(token);
                    return Result.Success();
                }, cancellation);
            }
            catch (ConcurrencyException)
            {
                return Result.Failure(Errors.Codes.Booking.ConcurrencyConflict, Errors.Messages.Booking.ConcurrencyConflict);
            }
        }

        public async Task<Result> RejectBookingRequestAsync(Guid hostId, Guid bookingId, CancellationToken cancellation = default)
        {
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
            return Result.Success();
        }
    }
}