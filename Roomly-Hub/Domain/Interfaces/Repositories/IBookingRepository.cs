using Domain.Entities.Booking;
using Domain.enums.Booking;

namespace Domain.Interfaces.Repositories
{
    public interface IBookingRepository
    {
        public Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellation = default);

        public Task<IEnumerable<Booking>> GetBookingsByGuestIdAsync(Guid guestId, CancellationToken cancellation = default);

        public Task<IEnumerable<Booking>> GetPendingRequestsByHostIdAsync(Guid hostId, CancellationToken cancellation = default);

        public Task AddBookingAsync(Booking booking, CancellationToken cancellation = default);

        public Task UpdateBookingAsync(Booking booking, CancellationToken cancellation = default);

        public Task DeleteBookingAsync(Guid bookingId, CancellationToken cancellation = default);

        public Task<bool> IsRoomAvailableAsync(Guid roomId, DateTime checkIn, DateTime checkOut, CancellationToken cancellation = default);

        public Task<bool> IsRoomAvailableAsync(Guid roomId, DateTime checkIn, DateTime checkOut, Guid? excludeBookingId = null, CancellationToken cancellation = default);

        public Task<bool> IsRoomAvailableAsync(
            Guid roomId,
            DateTime checkIn,
            DateTime checkOut,
            IEnumerable<BookingStatus> blockingStatuses,
            Guid? excludeBookingId = null,
            CancellationToken cancellation = default);

        public Task<IEnumerable<Booking>> GetOverlappingPendingRequestsAsync(
            Guid roomId,
            DateTime checkIn,
            DateTime checkOut,
            Guid excludedBookingId,
            CancellationToken cancellation = default);
    }
}