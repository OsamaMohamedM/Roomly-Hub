using Application.DTOs.Payment.FawaterkRequest;
using Application.Interfaces.Services.Bookings;
using Domain.Entities.Booking;

namespace Application.Services.Bookings
{
    public class BookingPaymentRequestFactory : IBookingPaymentRequestFactory
    {
        public EInvoiceRequestModel Create(Booking booking, int? paymentMethodId, EInvoiceRedirectionUrls? redirectionUrls)
        {
            return new EInvoiceRequestModel
            {
                PaymentMethodId = paymentMethodId,
                CartItems = new List<CartItemModel>
                {
                    new()
                    {
                        Currency = "EGP",
                        Description = "Booking Payment",
                        PricePerNight = booking.Room.PricePerNight,
                        Tax = 0,
                        Quantity = 1,
                        Total = booking.TotalPrice
                    }
                },
                PayLoad = new EInvoicePayload
                {
                    OrderId = booking.Id.ToString(),
                },
                RedirectionUrls = redirectionUrls
            };
        }
    }
}