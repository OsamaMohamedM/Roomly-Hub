using Domain.ValueObjects;

namespace Domain.Entities
{
    public class UserExternalLogin : BaseEntity
    {
        public Guid UserId { get; private set; }
        public string Provider { get; private set; }
        public string ExternalId { get; private set; }
        public Email? Email { get; private set; }

        private UserExternalLogin()
        { }

        public static UserExternalLogin Create(
            Guid userId,
            string provider,
            string externalId,
            Email? email = null)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));

            if (string.IsNullOrWhiteSpace(provider))
                throw new ArgumentException("Provider is required.", nameof(provider));

            if (string.IsNullOrWhiteSpace(externalId))
                throw new ArgumentException("External ID is required.", nameof(externalId));

            return new UserExternalLogin
            {
                UserId = userId,
                Provider = provider.Trim(),
                ExternalId = externalId.Trim(),
                Email = email
            };
        }

        public void UpdateEmail(Email email)
        {
            if (email is null)
                throw new ArgumentNullException(nameof(email));

            Email = email;
            MarkUpdated();
        }
    }
}