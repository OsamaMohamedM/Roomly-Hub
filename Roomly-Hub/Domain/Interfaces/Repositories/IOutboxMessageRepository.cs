using Domain.Entities.Outbox;

namespace Domain.Interfaces.Repositories
{
    public interface IOutboxMessageRepository
    {
        Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

        Task<List<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default);

        void Update(OutboxMessage message);
    }
}
