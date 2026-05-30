using Application.Common.Constants;
using Application.Common.Results;
using Application.Common.Mappers;
using Application.DTOs.Booking;
using Application.Interfaces.Services.Bookings;
using Domain.Interfaces.Repositories;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Services.Bookings
{
    public class BookingQueryService : IBookingQueryService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IUserRepository _userRepository;
        private readonly IBookingMapper _bookingMapper;
        private readonly ILogger<BookingQueryService> _logger;

        public BookingQueryService(
            IBookingRepository bookingRepository,
            IUserRepository userRepository,
            IBookingMapper bookingMapper,
            ILogger<BookingQueryService> logger)
        {
            _bookingRepository = bookingRepository;
            _userRepository = userRepository;
            _bookingMapper = bookingMapper;
            _logger = logger;
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

        public async Task<Result<BookingSummaryDto>> GetAuthorizedBookingSummaryAsync(Guid requesterId, Guid bookingId, CancellationToken cancellation = default)
        {
            var booking = await _bookingRepository.GetBookingByIdAsync(bookingId, cancellation);
            if (booking == null)
            {
                return Result<BookingSummaryDto>.Failure(Errors.Codes.Booking.BookingNotFound, $"{Errors.Messages.Booking.BookingNotFound} With This Id : {bookingId}");
            }

            var requester = await _userRepository.GetByIdAsync(requesterId, cancellation);
            var isAdmin = requester?.AdminRole is AdminRole.Moderator or AdminRole.SuperAdmin;
            var isGuest = booking.GuestId == requesterId;
            var isHost = booking.Room?.HostId == requesterId;

            if (!isGuest && !isHost && !isAdmin)
            {
                return Result<BookingSummaryDto>.Failure(Errors.Codes.Common.UnauthorizedAction, Errors.Messages.Room.UnauthorizedAction);
            }

            return Result<BookingSummaryDto>.Success(_bookingMapper.ToSummaryDto(booking));
        }

        public async Task<Result<IEnumerable<HostBookingRequestDto>>> GetPendingRequestsForHostAsync(Guid hostId, CancellationToken cancellation = default)
        {
            _logger.LogInformation("Loading pending booking requests for host {HostId}", hostId);
            var requests = await _bookingRepository.GetPendingRequestsByHostIdAsync(hostId, cancellation);
            var response = requests.Select(_bookingMapper.ToHostRequestDto).ToList();
            return Result<IEnumerable<HostBookingRequestDto>>.Success(response);
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
    }
}
