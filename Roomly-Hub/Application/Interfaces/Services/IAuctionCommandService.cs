using Application.Common.Results;
using Application.DTOs.Auctions;

namespace Application.Interfaces.Services
{
    public interface IAuctionCommandService
    {
        Task<Result<AuctionResponseDto>> CreateAuctionAsync(Guid hostId, CreateAuctionRequestDto dto, CancellationToken ct);
        Task<Result<PlaceBidResultDto>> PlaceBidAsync(Guid bidderId, PlaceBidRequestDto dto, CancellationToken ct);
        Task<Result> CancelAuctionAsync(Guid hostId, Guid auctionId, CancellationToken ct);
        Task<Result> SettleAuctionAsync(Guid auctionId, CancellationToken ct);
        Task<Result> HandlePaymentTimeoutAsync(Guid auctionId, CancellationToken ct);
        Task<Result> ProcessWinnerPaymentAsync(Guid winnerId, AuctionPaymentRequestDto dto, CancellationToken ct);
    }
}
