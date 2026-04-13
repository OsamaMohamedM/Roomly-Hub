using Application.Common.Results;
using Application.DTOs.Booking;

namespace Application.Interfaces.Services.Bookings
{
    public interface IBookingCommandService
    {
        Task<Result<CreateBookingResponseDto>> CreateBookingAsync(Guid guestId, CreateBookingDto createBookingDto, CancellationToken cancellation = default);

        Task<Result<BookingSummaryDto>> UpdateBookingAsync(BookingRequestDto bookingRequestDto, CancellationToken cancellation = default);

        Task<Result<string>> CancelBookingAsync(BookingCancelRequestDto bookingCancelRequestDto, CancellationToken cancellation = default);

        Task<Result> ApproveBookingRequestAsync(Guid hostId, Guid bookingId, CancellationToken cancellation = default);

        Task<Result> RejectBookingRequestAsync(Guid hostId, Guid bookingId, CancellationToken cancellation = default);
    }
}