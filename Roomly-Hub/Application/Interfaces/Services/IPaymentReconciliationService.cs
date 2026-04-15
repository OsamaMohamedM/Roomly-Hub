namespace Application.Interfaces.Services
{
    public interface IPaymentReconciliationService
    {
        Task ReconcilePendingPaymentsAsync();
    }
}
