using Application.Common.Results;
using Application.DTOs.Auctions;
using Application.Interfaces.Services;
using Application.Services.Auctions.Handlers;

namespace Application.Services.Auctions
{
    public class AuctionCommandService : IAuctionCommandService
    {
        private readonly CreateAuctionHandler _createHandler;
        private readonly PlaceBidHandler _placeBidHandler;
        private readonly CancelAuctionHandler _cancelHandler;
        private readonly SettleAuctionHandler _settleHandler;
        private readonly HandlePaymentTimeoutHandler _paymentTimeoutHandler;
        private readonly ProcessWinnerPaymentHandler _processPaymentHandler;

        public AuctionCommandService(
            CreateAuctionHandler createHandler,
            PlaceBidHandler placeBidHandler,
            CancelAuctionHandler cancelHandler,
            SettleAuctionHandler settleHandler,
            HandlePaymentTimeoutHandler paymentTimeoutHandler,
            ProcessWinnerPaymentHandler processPaymentHandler)
        {
            _createHandler = createHandler;
            _placeBidHandler = placeBidHandler;
            _cancelHandler = cancelHandler;
            _settleHandler = settleHandler;
            _paymentTimeoutHandler = paymentTimeoutHandler;
            _processPaymentHandler = processPaymentHandler;
        }

        public Task<Result<AuctionResponseDto>> CreateAuctionAsync(Guid hostId, CreateAuctionRequestDto dto, CancellationToken ct)
            => _createHandler.HandleAsync(hostId, dto, ct);

        public Task<Result<PlaceBidResultDto>> PlaceBidAsync(Guid bidderId, PlaceBidRequestDto dto, CancellationToken ct)
            => _placeBidHandler.HandleAsync(bidderId, dto, ct);

        public Task<Result> CancelAuctionAsync(Guid hostId, Guid auctionId, CancellationToken ct)
            => _cancelHandler.HandleAsync(hostId, auctionId, ct);

        public Task<Result> SettleAuctionAsync(Guid auctionId, CancellationToken ct)
            => _settleHandler.HandleAsync(auctionId, ct);

        public Task<Result> HandlePaymentTimeoutAsync(Guid auctionId, CancellationToken ct)
            => _paymentTimeoutHandler.HandleAsync(auctionId, ct);

        public Task<Result> ProcessWinnerPaymentAsync(Guid winnerId, AuctionPaymentRequestDto dto, CancellationToken ct)
            => _processPaymentHandler.HandleAsync(winnerId, dto, ct);
    }
}