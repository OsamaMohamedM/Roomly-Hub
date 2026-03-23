using Application.DTOs.Booking;
using Domain.Entities.Booking;

namespace Application.Common.Mappers
{
    public interface IBookingMapper
    {
        BookingSummaryDto ToSummaryDto(Booking booking);
        HostBookingRequestDto ToHostRequestDto(Booking booking);
    }

    public class BookingMapper : IBookingMapper
    {
        public BookingSummaryDto ToSummaryDto(Booking booking)
        {
            return new BookingSummaryDto
            {
                Room = booking.Room,
                PaymentMethod = booking.PaymentMethod,
                PaymentStatus = booking.PaymentStatus,
                TotalPrice = booking.TotalPrice,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                CreatedDate = booking.CreatedDate,
                CancelledAt = booking.CancelledAt
            };
        }

        public HostBookingRequestDto ToHostRequestDto(Booking booking)
        {
            return new HostBookingRequestDto
            {
                BookingId = booking.Id,
                RoomId = booking.RoomId,
                GuestId = booking.GuestId,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                CreatedDate = booking.CreatedDate,
                TotalPrice = booking.TotalPrice,
                PaymentMethod = booking.PaymentMethod,
                PaymentStatus = booking.PaymentStatus
            };
        }
    }
}
