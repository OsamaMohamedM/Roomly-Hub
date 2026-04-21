using Domain.Entities.Wallet;

namespace Domain.Interfaces.Repositories
{
    public interface IWalletRepository
    {
        Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

        Task<Wallet?> GetByIdAsync(Guid walletId, CancellationToken ct = default);

        Task AddAsync(Wallet wallet, CancellationToken ct = default);

        void Update(Wallet wallet);

        Task AddTransactionAsync(WalletTransaction tx, CancellationToken ct = default);

        Task<bool> TransactionExistsAsync(string idempotencyKey, CancellationToken ct = default);

        Task<List<WalletTransaction>> GetTransactionsAsync(Guid walletId, int page, int pageSize, CancellationToken ct = default);

        Task<int> GetTransactionsCountAsync(Guid walletId, CancellationToken ct = default);

        Task<decimal> GetBalanceAsync(Guid userId, CancellationToken ct = default);
    }
}