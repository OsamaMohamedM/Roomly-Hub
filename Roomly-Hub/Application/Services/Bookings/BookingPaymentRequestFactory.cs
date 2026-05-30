using Application.DTOs.Payment.FawaterkRequest;
using Application.Interfaces.Services.Bookings;
using Domain.Entities.Booking;
using System.Globalization;

namespace Application.Services.Bookings
{
    public class BookingPaymentRequestFactory : IBookingPaymentRequestFactory
    {
        public EInvoiceRequestModel Create(Booking booking, int? paymentMethodId, EInvoiceRedirectionUrls? redirectionUrls)
        {
            return new EInvoiceRequestModel
            {
                PaymentMethodId = paymentMethodId,
                CartTotal = booking.TotalPrice.ToString("F2", CultureInfo.InvariantCulture),
                Currency = "EGP",
                Customer = new CustomerModel
                {
                    FirstName = booking.User?.Name ?? "Guest",
                    LastName = "Guest",
                    Email = booking.User?.Email ?? "guest@roomly.com",
                },
                CartItems = new List<CartItemModel>
                {
                    new()
                    {
                        Name = $"Booking Room: {booking.Room?.Title ?? booking.RoomId.ToString()}",
                        Price = booking.TotalPrice.ToString("F2",CultureInfo.InvariantCulture),
                        Quantity = "1"
                    }
                },
                RedirectionUrls = redirectionUrls
            };
        }
    }
}
