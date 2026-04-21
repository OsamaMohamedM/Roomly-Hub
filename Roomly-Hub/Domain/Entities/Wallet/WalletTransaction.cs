using Domain.Entities;
using Domain.enums.Booking;
using Domain.enums.Wallet;

namespace Domain.Entities.Wallet
{
    public class WalletTransaction : BaseEntity
    {
        public Guid WalletId { get; private set; }
        public decimal Amount { get; private set; }
        public TransactionType Type { get; private set; }
        public ReferenceType? ReferenceType { get; private set; }
        public Guid? ReferenceId { get; private set; }
        public PaymentMethod? PaymentMethod { get; private set; }
        public string? ExternalRef { get; private set; }
        public string Description { get; private set; }
        public string IdempotencyKey { get; private set; }

        private WalletTransaction()
        {
            Description = string.Empty;
            IdempotencyKey = string.Empty;
        }

        public static WalletTransaction Create(
            Guid walletId,
            decimal amount,
            TransactionType type,
            string description,
            string idempotencyKey,
            ReferenceType? referenceType = null,
            Guid? referenceId = null,
            PaymentMethod? paymentMethod = null,
            string? externalRef = null)
        {
            return new WalletTransaction
            {
                WalletId = walletId,
                Amount = amount,
                Type = type,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                PaymentMethod = paymentMethod,
                ExternalRef = externalRef,
                Description = description,
                IdempotencyKey = idempotencyKey
            };
        }
    }
}