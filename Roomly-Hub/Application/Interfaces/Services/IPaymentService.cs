using Application.DTOs.Payment;
using Application.DTOs.Payment.FawaterkRequest;
using Domain.Entities.Payment;
using Domain.enums.Booking;

namespace Application.Interfaces.Services
{
    public interface IPaymentService
    {
        Task<EInvoiceResponseData?> CreateEInvoiceAsync(EInvoiceRequestModel eInvoice);

        Task<IList<PaymentMethoodModel>?> GetPaymentMethods();

        Task<BasePaymentResponse?> GeneralPay(EInvoiceRequestModel invoice);

        bool VerifyWebhook(WebHookModel webHook);

        bool VerifyCancelTransaction(CancelTransactionModel cancelTransaction);

        bool VerifyApiKeyTransaction(string apiKey);

        Task<PaymentStatus> CheckInvoiceStatusAsync(string invoiceReference);
    }
}