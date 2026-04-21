using Domain.Entities.Wallet;
using Domain.Interfaces.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class WalletRepository : IWalletRepository
    {
        private readonly AppDbContext _context;

        public WalletRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            return await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId && !w.IsDeleted, ct);
        }

        public async Task<Wallet?> GetByIdAsync(Guid walletId, CancellationToken ct = default)
        {
            return await _context.Wallets.FirstOrDefaultAsync(w => w.Id == walletId && !w.IsDeleted, ct);
        }

        public async Task AddAsync(Wallet wallet, CancellationToken ct = default)
        {
            await _context.Wallets.AddAsync(wallet, ct);
        }

        public void Update(Wallet wallet)
        {
            _context.Wallets.Update(wallet);
        }

        public async Task AddTransactionAsync(WalletTransaction tx, CancellationToken ct = default)
        {
            await _context.WalletTransactions.AddAsync(tx, ct);
        }

        public async Task<bool> TransactionExistsAsync(string idempotencyKey, CancellationToken ct = default)
        {
            return await _context.WalletTransactions.AnyAsync(t => t.IdempotencyKey == idempotencyKey && !t.IsDeleted, ct);
        }

        public async Task<List<WalletTransaction>> GetTransactionsAsync(Guid walletId, int page, int pageSize, CancellationToken ct = default)
        {
            return await _context.WalletTransactions
                .Where(t => t.WalletId == walletId && !t.IsDeleted)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
        }

        public async Task<int> GetTransactionsCountAsync(Guid walletId, CancellationToken ct = default)
        {
            return await _context.WalletTransactions.CountAsync(t => t.WalletId == walletId && !t.IsDeleted, ct);
        }

        public async Task<decimal> GetBalanceAsync(Guid userId, CancellationToken ct = default)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId && !w.IsDeleted, ct);
            return wallet?.Balance ?? 0m;
        }
    }
}