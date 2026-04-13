using Application.DTOs.Payment.FawaterkRequest;
using Domain.Entities.Booking;

namespace Application.Interfaces.Services.Bookings
{
    public interface IBookingPaymentRequestFactory
    {
        EInvoiceRequestModel Create(Booking booking, int? paymentMethodId, EInvoiceRedirectionUrls? redirectionUrls);
    }
}
