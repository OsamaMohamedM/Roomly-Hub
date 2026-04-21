using Application.Common.Constants;
using Application.Common.Pagination;
using Application.Common.Results;
using Application.DTOs.Wallet;
using Application.Interfaces.Services.Wallet;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Application.Services.Wallet
{
    public class WalletQueryService : IWalletQueryService
    {
        private readonly IWalletRepository _walletRepository;
        private readonly ILogger<WalletQueryService> _logger;

        public WalletQueryService(IWalletRepository walletRepository, ILogger<WalletQueryService> logger)
        {
            _walletRepository = walletRepository;
            _logger = logger;
        }

        public async Task<Result<WalletResponseDto>> GetWalletAsync(Guid userId, CancellationToken ct)
        {
            _logger.LogInformation("Loading wallet for user {UserId}", userId);
            var wallet = await _walletRepository.GetByUserIdAsync(userId, ct);
            if (wallet == null)
            {
                _logger.LogWarning("Wallet not found for user {UserId}", userId);
                return Result<WalletResponseDto>.Failure(Errors.Codes.Wallet.NotFound, Errors.Messages.Wallet.NotFound);
            }

            return Result<WalletResponseDto>.Success(new WalletResponseDto
            {
                Id = wallet.Id,
                Balance = wallet.Balance,
                InsuranceHeldBalance = wallet.InsuranceHeldBalance
            });
        }

        public async Task<Result<WalletHistoryResponseDto>> GetHistoryAsync(Guid userId, int page, int pageSize, CancellationToken ct)
        {
            _logger.LogInformation("Loading wallet history for user {UserId}. Page: {Page}, PageSize: {PageSize}", userId, page, pageSize);
            if (page <= 0 || pageSize <= 0)
            {
                _logger.LogWarning("Invalid wallet history paging for user {UserId}. Page: {Page}, PageSize: {PageSize}", userId, page, pageSize);
                return Result<WalletHistoryResponseDto>.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestValidationFailed);
            }

            var wallet = await _walletRepository.GetByUserIdAsync(userId, ct);
            if (wallet == null)
            {
                _logger.LogWarning("Wallet history requested but wallet not found for user {UserId}", userId);
                return Result<WalletHistoryResponseDto>.Failure(Errors.Codes.Wallet.NotFound, Errors.Messages.Wallet.NotFound);
            }

            var transactions = await _walletRepository.GetTransactionsAsync(wallet.Id, page, pageSize, ct);
            var count = await _walletRepository.GetTransactionsCountAsync(wallet.Id, ct);

            var items = transactions.Select(t => new TransactionDto
            {
                Id = t.Id,
                Amount = t.Amount,
                Type = t.Type.ToString(),
                Description = t.Description,
                ReferenceType = t.ReferenceType?.ToString(),
                ReferenceId = t.ReferenceId,
                CreatedAt = t.CreatedAt
            }).ToList();

            var response = new WalletHistoryResponseDto
            {
                Wallet = new WalletResponseDto
                {
                    Id = wallet.Id,
                    Balance = wallet.Balance,
                    InsuranceHeldBalance = wallet.InsuranceHeldBalance
                },
                Transactions = new PagedResult<TransactionDto>(items, count, page, pageSize)
            };

            _logger.LogInformation("Wallet history loaded for user {UserId}. Returned {ReturnedCount} of {TotalCount}", userId, items.Count, count);
            return Result<WalletHistoryResponseDto>.Success(response);
        }

        public async Task<Result<bool>> HasSufficientBalanceForAuctionAsync(Guid userId, decimal requiredAmount, CancellationToken ct)
        {
            _logger.LogInformation("Checking wallet balance for auction. UserId: {UserId}, RequiredAmount: {RequiredAmount}", userId, requiredAmount);
            if (requiredAmount <= 0)
            {
                _logger.LogWarning("Invalid required auction amount for user {UserId}: {RequiredAmount}", userId, requiredAmount);
                return Result<bool>.Failure(Errors.Codes.Wallet.InvalidAmount, Errors.Messages.Wallet.InvalidAmount);
            }

            var wallet = await _walletRepository.GetByUserIdAsync(userId, ct);
            if (wallet == null)
            {
                _logger.LogWarning("Auction balance check failed. Wallet not found for user {UserId}", userId);
                return Result<bool>.Failure(Errors.Codes.Wallet.NotFound, Errors.Messages.Wallet.NotFound);
            }

            var hasBalance = wallet.Balance >= requiredAmount;
            _logger.LogInformation("Auction balance check completed. UserId: {UserId}, HasSufficientBalance: {HasBalance}", userId, hasBalance);
            return Result<bool>.Success(hasBalance);
        }
    }
}