using Domain.Entities.Outbox;
using Domain.Interfaces.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class OutboxMessageRepository : IOutboxMessageRepository
    {
        private readonly AppDbContext _context;

        public OutboxMessageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            await _context.OutboxMessages.AddAsync(message, cancellationToken);
        }

        public async Task<List<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default)
        {
            return await _context.OutboxMessages
                .Where(x => x.ProcessedAt == null && !x.IsDeleted)
                .OrderBy(x => x.OccurredAt)
                .Take(batchSize)
                .ToListAsync(cancellationToken);
        }

        public void Update(OutboxMessage message)
        {
            _context.OutboxMessages.Update(message);
        }
    }
}
