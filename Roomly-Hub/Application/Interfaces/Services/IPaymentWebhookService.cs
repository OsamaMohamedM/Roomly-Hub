using Application.Common.Results;
using Application.DTOs.Payment;

namespace Application.Interfaces.Services
{
    public interface IPaymentWebhookService
    {
        Task<Result> HandleSuccessWebhookAsync(WebHookModel webhook, CancellationToken cancellationToken = default);

        Task<Result> HandleFailedWebhookAsync(FaliledWebHook webhook, CancellationToken cancellationToken = default);

        Task<Result> HandleCancelledWebhookAsync(CancelTransactionModel cancelTransaction, CancellationToken cancellationToken = default);
    }
}
