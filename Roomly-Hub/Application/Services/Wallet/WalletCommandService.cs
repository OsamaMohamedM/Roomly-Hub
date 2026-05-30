using Application.Common.Constants;
using Application.Common.Results;
using Application.DTOs.Wallet;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services.Wallet;
using Domain.Entities.Wallet;
using Domain.enums.Booking;
using Domain.enums.Wallet;
using Domain.Interfaces.Repositories;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Application.Services.Wallet
{
    public class WalletCommandService : IWalletCommandService
    {
        private const decimal MinimumWithdrawalAmount = 100m;
        private const decimal PlatformCommissionRate = 0.10m;

        private readonly IWalletRepository _walletRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<TopUpRequestDto> _topUpValidator;
        private readonly IValidator<WithdrawRequestDto> _withdrawValidator;
        private readonly ILogger<WalletCommandService> _logger;

        public WalletCommandService(
            IWalletRepository walletRepository,
            IUnitOfWork unitOfWork,
            IValidator<TopUpRequestDto> topUpValidator,
            IValidator<WithdrawRequestDto> withdrawValidator,
            ILogger<WalletCommandService> logger)
        {
            _walletRepository = walletRepository;
            _unitOfWork = unitOfWork;
            _topUpValidator = topUpValidator;
            _withdrawValidator = withdrawValidator;
            _logger = logger;
        }

        public async Task<Result> CreateWalletAsync(Guid userId, CancellationToken ct)
        {
            _logger.LogInformation("Creating wallet for user {UserId}", userId);
            var existing = await _walletRepository.GetByUserIdAsync(userId, ct);
            if (existing != null)
            {
                _logger.LogWarning("Wallet already exists for user {UserId}", userId);
                return Result.Failure(Errors.Codes.Wallet.AlreadyExists, Errors.Messages.Wallet.AlreadyExists);
            }

            var wallet = Domain.Entities.Wallet.Wallet.Create(userId);
            await _walletRepository.AddAsync(wallet, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Wallet created for user {UserId}", userId);
            return Result.Success();
        }

        public async Task<Result> TopUpAsync(Guid userId, TopUpRequestDto dto, CancellationToken ct)
        {
            _logger.LogInformation("Starting wallet top-up for user {UserId}. Amount: {Amount}, PaymentMethod: {PaymentMethod}", userId, dto?.Amount, dto?.PaymentMethod);
            var validation = await _topUpValidator.ValidateAsync(dto, ct);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Wallet top-up validation failed for user {UserId}", userId);
                return Result.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestValidationFailed);
            }

            if (!Enum.TryParse<PaymentMethod>(dto.PaymentMethod, true, out var paymentMethod) || paymentMethod == PaymentMethod.Wallet)
            {
                _logger.LogWarning("Wallet top-up invalid payment method for user {UserId}. PaymentMethod: {PaymentMethod}", userId, dto.PaymentMethod);
                return Result.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestValidationFailed);
            }

            var idempotencyKey = $"topup-pending-{dto.ExternalRef}";
            if (await _walletRepository.TransactionExistsAsync(idempotencyKey, ct))
            {
                _logger.LogInformation("Wallet top-up idempotent hit for user {UserId}. Key: {IdempotencyKey}", userId, idempotencyKey);
                return Result.Success();
            }

            var wallet = await _walletRepository.GetByUserIdAsync(userId, ct);
            if (wallet == null)
            {
                _logger.LogWarning("Wallet top-up failed. Wallet not found for user {UserId}", userId);
                return Result.Failure(Errors.Codes.Wallet.NotFound, Errors.Messages.Wallet.NotFound);
            }

            if (dto.Amount <= 0)
            {
                _logger.LogWarning("Wallet top-up failed due to invalid amount for user {UserId}. Amount: {Amount}", userId, dto.Amount);
                return Result.Failure(Errors.Codes.Wallet.InvalidAmount, Errors.Messages.Wallet.InvalidAmount);
            }

            await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                var tx = WalletTransaction.Create(
                    wallet.Id,
                    dto.Amount,
                    TransactionType.PendingTopUp,
                    "Pending wallet top-up awaiting verified settlement",
                    idempotencyKey,
                    paymentMethod: paymentMethod,
                    externalRef: dto.ExternalRef);

                await _walletRepository.AddTransactionAsync(tx, token);
                await _unitOfWork.SaveChangesAsync(token);
            }, ct);

            _logger.LogInformation("Wallet top-up completed for user {UserId}. Amount: {Amount}, PaymentMethod: {PaymentMethod}", userId, dto.Amount, dto.PaymentMethod);
            return Result.Success();
        }

        public async Task<Result> ChargeForBookingAsync(Guid userId, Guid bookingId, decimal amount, CancellationToken ct)
        {
            _logger.LogInformation("Charging wallet for booking. UserId: {UserId}, BookingId: {BookingId}, Amount: {Amount}", userId, bookingId, amount);
            if (amount <= 0)
            {
                _logger.LogWarning("Charge for booking failed due to invalid amount. UserId: {UserId}, BookingId: {BookingId}, Amount: {Amount}", userId, bookingId, amount);
                return Result.Failure(Errors.Codes.Wallet.InvalidAmount, Errors.Messages.Wallet.InvalidAmount);
            }

            var idempotencyKey = $"booking-charge-{bookingId}";
            if (await _walletRepository.TransactionExistsAsync(idempotencyKey, ct))
            {
                _logger.LogInformation("Charge for booking idempotent hit. BookingId: {BookingId}", bookingId);
                return Result.Success();
            }

            var wallet = await _walletRepository.GetByUserIdAsync(userId, ct);
            if (wallet == null)
            {
                _logger.LogWarning("Charge for booking failed. Wallet not found for user {UserId}", userId);
                return Result.Failure(Errors.Codes.Wallet.NotFound, Errors.Messages.Wallet.NotFound);
            }

            if (!wallet.CanWithdraw(amount))
            {
                _logger.LogWarning("Charge for booking failed due to insufficient funds. UserId: {UserId}, BookingId: {BookingId}, Amount: {Amount}", userId, bookingId, amount);
                return Result.Failure(Errors.Codes.Wallet.InsufficientFunds, Errors.Messages.Wallet.InsufficientFunds);
            }

            await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                wallet.Debit(amount);
                _walletRepository.Update(wallet);

                var tx = WalletTransaction.Create(
                    wallet.Id,
                    -amount,
                    TransactionType.BookingPayment,
                    "Booking payment from wallet",
                    idempotencyKey,
                    ReferenceType.Booking,
                    bookingId,
                    PaymentMethod.Wallet);

                await _walletRepository.AddTransactionAsync(tx, token);
                await _unitOfWork.SaveChangesAsync(token);
            }, ct);

            _logger.LogInformation("Charge for booking completed. UserId: {UserId}, BookingId: {BookingId}, Amount: {Amount}", userId, bookingId, amount);
            return Result.Success();
        }

        public async Task<Result> ChargeForAuctionAsync(Guid userId, Guid auctionId, decimal amount, CancellationToken ct)
        {
            _logger.LogInformation("Charging wallet for auction. UserId: {UserId}, AuctionId: {AuctionId}, Amount: {Amount}", userId, auctionId, amount);
            if (amount <= 0)
                return Result.Failure(Errors.Codes.Wallet.InvalidAmount, Errors.Messages.Wallet.InvalidAmount);

            var idempotencyKey = $"auction-charge-{auctionId}-{userId}";
            if (await _walletRepository.TransactionExistsAsync(idempotencyKey, ct))
            {
                _logger.LogInformation("Charge for auction idempotent hit. AuctionId: {AuctionId}", auctionId);
                return Result.Success();
            }

            var wallet = await _walletRepository.GetByUserIdAsync(userId, ct);
            if (wallet == null)
                return Result.Failure(Errors.Codes.Wallet.NotFound, Errors.Messages.Wallet.NotFound);

            if (!wallet.CanWithdraw(amount))
                return Result.Failure(Errors.Codes.Wallet.InsufficientFunds, Errors.Messages.Wallet.InsufficientFunds);

            await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                wallet.Debit(amount);
                _walletRepository.Update(wallet);

                var tx = WalletTransaction.Create(
                    wallet.Id,
                    -amount,
                    TransactionType.BookingPayment,   // reuse BookingPayment type; a dedicated AuctionPayment type can be added later
                    "Auction winner payment",
                    idempotencyKey,
                    ReferenceType.Auction,
                    auctionId,
                    PaymentMethod.Wallet);

                await _walletRepository.AddTransactionAsync(tx, token);
                await _unitOfWork.SaveChangesAsync(token);
            }, ct);

            _logger.LogInformation("Charge for auction completed. UserId: {UserId}, AuctionId: {AuctionId}, Amount: {Amount}", userId, auctionId, amount);
            return Result.Success();
        }

        public async Task<Result> RefundBookingAsync(Guid userId, Guid bookingId, decimal amount, CancellationToken ct)
        {
            _logger.LogInformation("Refunding wallet for booking. UserId: {UserId}, BookingId: {BookingId}, Amount: {Amount}", userId, bookingId, amount);
            if (amount <= 0)
            {
                _logger.LogWarning("Refund booking failed due to invalid amount. UserId: {UserId}, BookingId: {BookingId}, Amount: {Amount}", userId, bookingId, amount);
                return Result.Failure(Errors.Codes.Wallet.InvalidAmount, Errors.Messages.Wallet.InvalidAmount);
            }

            var idempotencyKey = $"booking-refund-{bookingId}";
            if (await _walletRepository.TransactionExistsAsync(idempotencyKey, ct))
            {
                _logger.LogInformation("Refund booking idempotent hit. BookingId: {BookingId}", bookingId);
                return Result.Success();
            }

            var wallet = await _walletRepository.GetByUserIdAsync(userId, ct);
            if (wallet == null)
            {
                _logger.LogWarning("Refund booking failed. Wallet not found for user {UserId}", userId);
                return Result.Failure(Errors.Codes.Wallet.NotFound, Errors.Messages.Wallet.NotFound);
            }

            await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                wallet.Credit(amount);
                _walletRepository.Update(wallet);

                var tx = WalletTransaction.Create(
                    wallet.Id,
                    amount,
                    TransactionType.Refund,
                    "Booking refund",
                    idempotencyKey,
                    ReferenceType.Booking,
                    bookingId,
                    PaymentMethod.Wallet);

                await _walletRepository.AddTransactionAsync(tx, token);
                await _unitOfWork.SaveChangesAsync(token);
            }, ct);

            _logger.LogInformation("Refund booking completed. UserId: {UserId}, BookingId: {BookingId}, Amount: {Amount}", userId, bookingId, amount);
            return Result.Success();
        }

        public async Task<Result> LockAuctionInsuranceAsync(Guid userId, Guid auctionId, decimal amount, CancellationToken ct)
        {
            _logger.LogInformation("Locking auction insurance. UserId: {UserId}, AuctionId: {AuctionId}, Amount: {Amount}", userId, auctionId, amount);
            if (amount <= 0)
            {
                _logger.LogWarning("Lock auction insurance failed due to invalid amount. UserId: {UserId}, AuctionId: {AuctionId}, Amount: {Amount}", userId, auctionId, amount);
                return Result.Failure(Errors.Codes.Wallet.InvalidAmount, Errors.Messages.Wallet.InvalidAmount);
            }

            var idempotencyKey = $"auction-lock-{auctionId}-{userId}";
            if (await _walletRepository.TransactionExistsAsync(idempotencyKey, ct))
            {
                _logger.LogInformation("Lock auction insurance idempotent hit. UserId: {UserId}, AuctionId: {AuctionId}", userId, auctionId);
                return Result.Success();
            }

            var wallet = await _walletRepository.GetByUserIdAsync(userId, ct);
            if (wallet == null)
            {
                _logger.LogWarning("Lock auction insurance failed. Wallet not found for user {UserId}", userId);
                return Result.Failure(Errors.Codes.Wallet.NotFound, Errors.Messages.Wallet.NotFound);
            }

            if (!wallet.CanWithdraw(amount))
            {
                _logger.LogWarning("Lock auction insurance failed due to insufficient funds. UserId: {UserId}, AuctionId: {AuctionId}, Amount: {Amount}", userId, auctionId, amount);
                return Result.Failure(Errors.Codes.Wallet.InsufficientFunds, Errors.Messages.Wallet.InsufficientFunds);
            }

            await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                wallet.LockInsurance(amount);
                _walletRepository.Update(wallet);

                var tx = WalletTransaction.Create(
                    wallet.Id,
                    -amount,
                    TransactionType.AuctionInsuranceLock,
                    "Auction insurance lock",
                    idempotencyKey,
                    ReferenceType.Auction,
                    auctionId,
                    PaymentMethod.Wallet);

                await _walletRepository.AddTransactionAsync(tx, token);
                await _unitOfWork.SaveChangesAsync(token);
            }, ct);

            _logger.LogInformation("Lock auction insurance completed. UserId: {UserId}, AuctionId: {AuctionId}, Amount: {Amount}", userId, auctionId, amount);
            return Result.Success();
        }

        public async Task<Result> ReleaseAuctionInsuranceAsync(Guid userId, Guid auctionId, decimal amount, CancellationToken ct)
        {
            _logger.LogInformation("Releasing auction insurance. UserId: {UserId}, AuctionId: {AuctionId}, Amount: {Amount}", userId, auctionId, amount);
            if (amount <= 0)
            {
                _logger.LogWarning("Release auction insurance failed due to invalid amount. UserId: {UserId}, AuctionId: {AuctionId}, Amount: {Amount}", userId, auctionId, amount);
                return Result.Failure(Errors.Codes.Wallet.InvalidAmount, Errors.Messages.Wallet.InvalidAmount);
            }

            var idempotencyKey = $"auction-release-{auctionId}-{userId}";
            if (await _walletRepository.TransactionExistsAsync(idempotencyKey, ct))
            {
                _logger.LogInformation("Release auction insurance idempotent hit. UserId: {UserId}, AuctionId: {AuctionId}", userId, auctionId);
                return Result.Success();
            }

            var wallet = await _walletRepository.GetByUserIdAsync(userId, ct);
            if (wallet == null)
            {
                _logger.LogWarning("Release auction insurance failed. Wallet not found for user {UserId}", userId);
                return Result.Failure(Errors.Codes.Wallet.NotFound, Errors.Messages.Wallet.NotFound);
            }

            if (wallet.InsuranceHeldBalance < amount)
            {
                _logger.LogWarning("Release auction insurance failed due to insufficient held insurance. UserId: {UserId}, AuctionId: {AuctionId}, Amount: {Amount}", userId, auctionId, amount);
                return Result.Failure(Errors.Codes.Wallet.InsufficientFunds, Errors.Messages.Wallet.InsufficientFunds);
            }

            await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                wallet.ReleaseInsurance(amount);
                _walletRepository.Update(wallet);

                var tx = WalletTransaction.Create(
                    wallet.Id,
                    amount,
                    TransactionType.AuctionInsuranceRelease,
                    "Auction insurance release",
                    idempotencyKey,
                    ReferenceType.Auction,
                    auctionId,
                    PaymentMethod.Wallet);

                await _walletRepository.AddTransactionAsync(tx, token);
                await _unitOfWork.SaveChangesAsync(token);
            }, ct);

            _logger.LogInformation("Release auction insurance completed. UserId: {UserId}, AuctionId: {AuctionId}, Amount: {Amount}", userId, auctionId, amount);
            return Result.Success();
        }

        public async Task<Result> ForfeitAuctionInsuranceAsync(Guid userId, Guid auctionId, decimal amount, CancellationToken ct)
        {
            _logger.LogInformation("Forfeiting auction insurance. UserId: {UserId}, AuctionId: {AuctionId}, Amount: {Amount}", userId, auctionId, amount);
            if (amount <= 0)
            {
                _logger.LogWarning("Forfeit auction insurance failed due to invalid amount. UserId: {UserId}, AuctionId: {AuctionId}, Amount: {Amount}", userId, auctionId, amount);
                return Result.Failure(Errors.Codes.Wallet.InvalidAmount, Errors.Messages.Wallet.InvalidAmount);
            }

            var idempotencyKey = $"auction-forfeit-{auctionId}-{userId}";
            if (await _walletRepository.TransactionExistsAsync(idempotencyKey, ct))
            {
                _logger.LogInformation("Forfeit auction insurance idempotent hit. UserId: {UserId}, AuctionId: {AuctionId}", userId, auctionId);
                return Result.Success();
            }

            var wallet = await _walletRepository.GetByUserIdAsync(userId, ct);
            if (wallet == null)
            {
                _logger.LogWarning("Forfeit auction insurance failed. Wallet not found for user {UserId}", userId);
                return Result.Failure(Errors.Codes.Wallet.NotFound, Errors.Messages.Wallet.NotFound);
            }

            if (wallet.InsuranceHeldBalance < amount)
            {
                _logger.LogWarning("Forfeit auction insurance failed due to insufficient held insurance. UserId: {UserId}, AuctionId: {AuctionId}, Amount: {Amount}", userId, auctionId, amount);
                return Result.Failure(Errors.Codes.Wallet.InsufficientFunds, Errors.Messages.Wallet.InsufficientFunds);
            }

            await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                wallet.ForfeitInsurance(amount);
                _walletRepository.Update(wallet);

                var tx = WalletTransaction.Create(
                    wallet.Id,
                    -amount,
                    TransactionType.AuctionInsuranceForfeiture,
                    "Auction insurance forfeiture",
                    idempotencyKey,
                    ReferenceType.Auction,
                    auctionId,
                    PaymentMethod.Wallet);

                await _walletRepository.AddTransactionAsync(tx, token);
                await _unitOfWork.SaveChangesAsync(token);
            }, ct);

            _logger.LogInformation("Forfeit auction insurance completed. UserId: {UserId}, AuctionId: {AuctionId}, Amount: {Amount}", userId, auctionId, amount);
            return Result.Success();
        }

        public async Task<Result> PayoutHostAsync(Guid hostUserId, Guid bookingId, decimal amount, CancellationToken ct)
        {
            _logger.LogInformation("Starting host payout. HostUserId: {HostUserId}, BookingId: {BookingId}, Amount: {Amount}", hostUserId, bookingId, amount);
            if (amount <= 0)
            {
                _logger.LogWarning("Host payout failed due to invalid amount. HostUserId: {HostUserId}, BookingId: {BookingId}, Amount: {Amount}", hostUserId, bookingId, amount);
                return Result.Failure(Errors.Codes.Wallet.InvalidAmount, Errors.Messages.Wallet.InvalidAmount);
            }

            var idempotencyKey = $"payout-{bookingId}";
            if (await _walletRepository.TransactionExistsAsync(idempotencyKey, ct))
            {
                _logger.LogInformation("Host payout idempotent hit. BookingId: {BookingId}", bookingId);
                return Result.Success();
            }

            var wallet = await _walletRepository.GetByUserIdAsync(hostUserId, ct);
            if (wallet == null)
            {
                _logger.LogWarning("Host payout failed. Wallet not found for host {HostUserId}", hostUserId);
                return Result.Failure(Errors.Codes.Wallet.NotFound, Errors.Messages.Wallet.NotFound);
            }

            var netAmount = decimal.Round(amount * (1 - PlatformCommissionRate), 4, MidpointRounding.AwayFromZero);

            await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                wallet.Credit(netAmount);
                _walletRepository.Update(wallet);

                var tx = WalletTransaction.Create(
                    wallet.Id,
                    netAmount,
                    TransactionType.HostPayout,
                    "Host payout",
                    idempotencyKey,
                    ReferenceType.Booking,
                    bookingId,
                    PaymentMethod.Wallet);

                await _walletRepository.AddTransactionAsync(tx, token);
                await _unitOfWork.SaveChangesAsync(token);
            }, ct);

            _logger.LogInformation("Host payout completed. HostUserId: {HostUserId}, BookingId: {BookingId}, NetAmount: {NetAmount}", hostUserId, bookingId, netAmount);
            return Result.Success();
        }

        public async Task<Result> WithdrawAsync(Guid userId, WithdrawRequestDto dto, CancellationToken ct)
        {
            _logger.LogInformation("Starting wallet withdrawal for user {UserId}. Amount: {Amount}", userId, dto?.Amount);
            var validation = await _withdrawValidator.ValidateAsync(dto, ct);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Wallet withdrawal validation failed for user {UserId}", userId);
                return Result.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestValidationFailed);
            }

            if (dto.Amount < MinimumWithdrawalAmount)
            {
                _logger.LogWarning("Wallet withdrawal below minimum for user {UserId}. Amount: {Amount}", userId, dto.Amount);
                return Result.Failure(Errors.Codes.Wallet.WithdrawMinimum, Errors.Messages.Wallet.WithdrawMinimum);
            }

            var idempotencyKey = $"withdraw-{userId}-{dto.Amount}-{DateTime.UtcNow:yyyyMMddHHmmss}";
            if (await _walletRepository.TransactionExistsAsync(idempotencyKey, ct))
            {
                _logger.LogInformation("Wallet withdrawal idempotent hit for user {UserId}. Key: {IdempotencyKey}", userId, idempotencyKey);
                return Result.Success();
            }

            var wallet = await _walletRepository.GetByUserIdAsync(userId, ct);
            if (wallet == null)
            {
                _logger.LogWarning("Wallet withdrawal failed. Wallet not found for user {UserId}", userId);
                return Result.Failure(Errors.Codes.Wallet.NotFound, Errors.Messages.Wallet.NotFound);
            }

            if (!wallet.CanWithdraw(dto.Amount))
            {
                _logger.LogWarning("Wallet withdrawal failed due to insufficient funds for user {UserId}. Amount: {Amount}", userId, dto.Amount);
                return Result.Failure(Errors.Codes.Wallet.InsufficientFunds, Errors.Messages.Wallet.InsufficientFunds);
            }

            await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                wallet.Debit(dto.Amount);
                _walletRepository.Update(wallet);

                var tx = WalletTransaction.Create(
                    wallet.Id,
                    -dto.Amount,
                    TransactionType.Withdrawal,
                    "Wallet withdrawal",
                    idempotencyKey,
                    ReferenceType.Withdrawal,
                    null,
                    PaymentMethod.Wallet);

                await _walletRepository.AddTransactionAsync(tx, token);
                await _unitOfWork.SaveChangesAsync(token);
            }, ct);

            _logger.LogInformation("Wallet withdrawal completed. UserId: {UserId}, Amount: {Amount}", userId, dto.Amount);
            return Result.Success();
        }
    }
}
