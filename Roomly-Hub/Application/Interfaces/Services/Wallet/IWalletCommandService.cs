using Application.Common.Results;
using Application.DTOs.Wallet;

namespace Application.Interfaces.Services.Wallet
{
    public interface IWalletCommandService
    {
        Task<Result> TopUpAsync(Guid userId, TopUpRequestDto dto, CancellationToken ct);

        Task<Result> ChargeForBookingAsync(Guid userId, Guid bookingId, decimal amount, CancellationToken ct);

        Task<Result> RefundBookingAsync(Guid userId, Guid bookingId, decimal amount, CancellationToken ct);

        Task<Result> LockAuctionInsuranceAsync(Guid userId, Guid auctionId, decimal amount, CancellationToken ct);

        Task<Result> ReleaseAuctionInsuranceAsync(Guid userId, Guid auctionId, decimal amount, CancellationToken ct);

        Task<Result> ForfeitAuctionInsuranceAsync(Guid userId, Guid auctionId, decimal amount, CancellationToken ct);

        Task<Result> PayoutHostAsync(Guid hostUserId, Guid bookingId, decimal amount, CancellationToken ct);

        Task<Result> WithdrawAsync(Guid userId, WithdrawRequestDto dto, CancellationToken ct);

        Task<Result> CreateWalletAsync(Guid userId, CancellationToken ct);
    }
}