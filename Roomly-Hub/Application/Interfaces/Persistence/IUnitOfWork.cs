namespace Application.Interfaces.Persistence
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default,
            System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted);

        Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default,
            System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted);
    }
}