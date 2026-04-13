using Application.Common.Results;
using Application.DTOs.Booking;

namespace Application.Interfaces.Services.Bookings
{
    public interface IBookingQueryService
    {
        Task<Result<BookingSummaryDto>> GetBookingSummaryAsync(Guid bookingId, CancellationToken cancellation = default);

        Task<Result<IEnumerable<BookingSummaryDto>>> GetBookingsByGuestAsync(Guid guestId, CancellationToken cancellation = default);

        Task<Result<IEnumerable<HostBookingRequestDto>>> GetPendingRequestsForHostAsync(Guid hostId, CancellationToken cancellation = default);

        Task<Result<IEnumerable<HostBookingRequestDto>>> GetHostRoomBookingsAsync(Guid hostId, Guid roomId, DateTime from, DateTime to, CancellationToken cancellation = default);
    }
}
