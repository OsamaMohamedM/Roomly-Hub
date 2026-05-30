namespace Application.Interfaces.Services
{
    public interface IOutboxDispatcher
    {
        Task DispatchPendingAsync(CancellationToken cancellationToken = default);
    }
}
