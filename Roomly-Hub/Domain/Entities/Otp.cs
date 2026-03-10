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
                Description = description
            };
        }

        public bool IsExpired()
        {
            return DateTime.UtcNow > ExpiresAt;
        }

        public bool IsUsed()
        {
            return UsedAt.HasValue;
        }

        public bool IsValid()
        {
            return !IsExpired() && !IsUsed();
        }

        public void MarkAsUsed()
        {
            if (IsUsed())
                throw new InvalidOperationException("OTP has already been used.");

            if (IsExpired())
                throw new InvalidOperationException("OTP has expired.");

            UsedAt = DateTime.UtcNow;
            MarkUpdated();
        }
    }
}
