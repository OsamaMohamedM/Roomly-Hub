namespace Domain.Entities
{
    public class RefreshToken : BaseEntity
    {
        public Guid UserId { get; private set; }
        public string TokenHash { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public DateTime? RevokedAt { get; private set; }

        private RefreshToken()
        { }

        public static RefreshToken Create(Guid userId, string tokenHash, DateTime expiresAt)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));

            if (string.IsNullOrWhiteSpace(tokenHash))
                throw new ArgumentException("Token hash is required.", nameof(tokenHash));

            if (expiresAt <= DateTime.UtcNow)
                throw new ArgumentException("Expiration must be in the future.", nameof(expiresAt));

            return new RefreshToken
            {
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAt = expiresAt
            };
        }

        public void Revoke()
        {
            if (RevokedAt.HasValue)
                throw new InvalidOperationException("Token is already revoked.");

            RevokedAt = DateTime.UtcNow;
            MarkUpdated();
        }

        public bool IsExpired()
        {
            return ExpiresAt < DateTime.UtcNow;
        }

        public bool IsRevoked()
        {
            return RevokedAt.HasValue;
        }

        public bool IsActive()
        {
            return !IsRevoked() && !IsExpired();
        }
    }
}