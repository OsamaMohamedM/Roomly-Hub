using Application.Common.Results;
using Application.DTOs.Booking;

namespace Application.Interfaces.Services
{
    public interface IBookingServices
    {
        public Task<Result<string>> CreateBookingAsync(BookingRequestDto bookingRequestDto, CancellationToken cancellation = default);

        public Task<Result<BookingSummaryDto>> UpdateBookingAsync(BookingRequestDto bookingRequestDto, CancellationToken cancellation = default);

        public Task<Result<string>> CancelBookingAsync(BookingCancelRequestDto bookingCancelRequestDto, CancellationToken cancellation = default);

        public Task<Result<BookingSummaryDto>> GetBookingSummaryAsync(Guid bookingId, CancellationToken cancellation = default);

        public Task<Result<IEnumerable<BookingSummaryDto>>> GetBookingsByGuestAsync(Guid guestId, CancellationToken cancellation = default);

        public Task<Result<IEnumerable<HostBookingRequestDto>>> GetPendingRequestsForHostAsync(Guid hostId, CancellationToken cancellation = default);

        public Task<Result> ApproveBookingRequestAsync(Guid hostId, Guid bookingId, CancellationToken cancellation = default);

        public Task<Result> RejectBookingRequestAsync(Guid hostId, Guid bookingId, CancellationToken cancellation = default);
    }
}