using Domain.Enums;

namespace Domain.Entities
{
    public class Otp : BaseEntity
    {
        public Guid UserId { get; private set; }
        public string CodeHash { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public OtpPurpose Purpose { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public DateTime? UsedAt { get; private set; }
        public int FailedAttempts { get; private set; }
        public const int MaxAttempts = 5;

        private Otp()
        { }

        public static Otp Create(
            Guid userId,
            string codeHash,
            OtpPurpose purpose,
            DateTime expiresAt,
            string name = "",
            string description = "")
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));

            if (string.IsNullOrWhiteSpace(codeHash))
                throw new ArgumentException("Code hash is required.", nameof(codeHash));

            if (purpose == OtpPurpose.None)
                throw new ArgumentException("OTP purpose is required.", nameof(purpose));

            if (expiresAt <= DateTime.UtcNow)
                throw new ArgumentException("Expiration must be in the future.", nameof(expiresAt));

            return new Otp
            {
                UserId = userId,
                CodeHash = codeHash,
                Purpose = purpose,
                ExpiresAt = expiresAt,
                Name = name,
                Description = description,
                FailedAttempts = 0
            };
        }

        public bool IsExpired() => DateTime.UtcNow > ExpiresAt;

        public bool IsUsed() => UsedAt.HasValue;

        public bool IsExhausted() => FailedAttempts >= MaxAttempts;

        public bool IsValid() => !IsExpired() && !IsUsed() && !IsExhausted();

        public void IncrementFailedAttempts()
        {
            if (IsUsed() || IsExpired() || IsExhausted())
                return;

            FailedAttempts++;
            MarkUpdated();
        }

        public void MarkAsUsed()
        {
            if (IsUsed())
                throw new InvalidOperationException("OTP has already been used.");

            if (IsExpired())
                throw new InvalidOperationException("OTP has expired.");

            if (IsExhausted())
                throw new InvalidOperationException("OTP has reached maximum attempts.");

            UsedAt = DateTime.UtcNow;
            MarkUpdated();
        }

        public void Invalidate()
        {
            if (IsUsed() || IsExpired())
                return;

            ExpiresAt = DateTime.UtcNow.AddSeconds(-1);
            MarkUpdated();
        }
    }
}