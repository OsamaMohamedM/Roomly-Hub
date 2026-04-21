using Application.Common.Results;
using Application.DTOs.Wallet;

namespace Application.Interfaces.Services.Wallet
{
    public interface IWalletQueryService
    {
        Task<Result<WalletResponseDto>> GetWalletAsync(Guid userId, CancellationToken ct);

        Task<Result<WalletHistoryResponseDto>> GetHistoryAsync(Guid userId, int page, int pageSize, CancellationToken ct);

        Task<Result<bool>> HasSufficientBalanceForAuctionAsync(Guid userId, decimal requiredAmount, CancellationToken ct);
    }
}