using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities
{
    public class User : BaseEntity
    {
        private const int MaxKycAttempts = 3;
        private const int MaxLoginFailAttempts = 5;

        public string Name { get; private set; }
        public Email Email { get; private set; }
        public string PasswordHash { get; private set; }
        public PhoneNumber? PhoneNumber { get; private set; }
        public string? ProfilePhotoUrl { get; private set; }
        public string? Bio { get; private set; }
        public bool EmailVerified { get; private set; }
        public SubmissionStatus KycStatus { get; private set; }
        public int KycAttemptCount { get; private set; }
        public UserRole Role { get; private set; } = UserRole.None;
        public AdminRole AdminRole { get; private set; } = AdminRole.None;
        public bool IsActive { get; private set; }
        public int LoginFailCount { get; private set; }
        public bool IsLocked { get; private set; }
        public string? LockoutToken { get; private set; }
        public DateTime? LockoutTokenExpiresAt { get; private set; }

        private readonly List<RefreshToken> _refreshTokens = new();
        public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

        private readonly List<UserExternalLogin> _externalLogins = new();
        public IReadOnlyCollection<UserExternalLogin> ExternalLogins => _externalLogins.AsReadOnly();

        private readonly List<KycSubmission> _kycSubmissions = new();
        public IReadOnlyCollection<KycSubmission> KycSubmissions => _kycSubmissions.AsReadOnly();
        private readonly List<Otp> _Otps = new();
        public IReadOnlyCollection<Otp> Otp => _Otps.AsReadOnly();

        private User()
        { }

        public static User Create(string name, Email email, string passwordHash, UserRole role)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required.", nameof(name));

            if (email is null)
                throw new ArgumentNullException(nameof(email));

            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Password hash is required.", nameof(passwordHash));

            return new User
            {
                Name = name.Trim(),
                Email = email,
                PasswordHash = passwordHash,
                Role = role,
                IsActive = true,
                EmailVerified = false,
                KycStatus = SubmissionStatus.NotSubmitted,
                KycAttemptCount = 0,
                LoginFailCount = 0,
                IsLocked = false
            };
        }

        public void SetPasswordHash(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
                throw new ArgumentException("Password hash cannot be empty.", nameof(hash));

            PasswordHash = hash;
            MarkUpdated();
        }

        public void IncrementLoginFailCount()
        {
            LoginFailCount++;

            if (LoginFailCount >= MaxLoginFailAttempts)
            {
                IsLocked = true;
            }

            MarkUpdated();
        }

        public void ResetLoginFailCount()
        {
            LoginFailCount = 0;
            MarkUpdated();
        }

        public void Lock(string token, DateTime expiresAt)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Lockout token is required.", nameof(token));

            if (expiresAt <= DateTime.UtcNow)
                throw new ArgumentException("Expiration must be in the future.", nameof(expiresAt));

            IsLocked = true;
            LockoutToken = token;
            LockoutTokenExpiresAt = expiresAt;
            MarkUpdated();
        }

        public void Unlock()
        {
            if (!IsLocked)
                throw new InvalidOperationException("User is not locked.");

            IsLocked = false;
            LockoutToken = null;
            LockoutTokenExpiresAt = null;
            LoginFailCount = 0;
            MarkUpdated();
        }

        public void VerifyEmail()
        {
            if (EmailVerified)
                throw new InvalidOperationException("Email is already verified.");

            EmailVerified = true;
            MarkUpdated();
        }

        public void SetPhoneNumber(PhoneNumber phone)
        {
            if (phone is null)
                throw new ArgumentNullException(nameof(phone));

            PhoneNumber = phone;

            MarkUpdated();
        }

        public void IncrementKycAttempts()
        {
            if (KycAttemptCount >= MaxKycAttempts)
                throw new InvalidOperationException($"Maximum KYC attempts ({MaxKycAttempts}) reached.");

            KycAttemptCount++;

            if (KycAttemptCount >= MaxKycAttempts)
            {
                Deactivate();
            }

            MarkUpdated();
        }

        public void ApproveKyc()
        {
            if (KycStatus == SubmissionStatus.Approved)
                throw new InvalidOperationException("KYC is already approved.");

            KycStatus = SubmissionStatus.Approved;
            MarkUpdated();
        }

        public void RejectKyc()
        {
            if (KycStatus == SubmissionStatus.Rejected)
                throw new InvalidOperationException("KYC is already rejected.");

            KycStatus = SubmissionStatus.Rejected;
            MarkUpdated();
        }

        public void SetKycPending()
        {
            if (KycStatus == SubmissionStatus.Pending)
                throw new InvalidOperationException("KYC is already pending.");

            KycStatus = SubmissionStatus.Pending;
            MarkUpdated();
        }

        public void UpdateProfile(string name, string bio)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty.", nameof(name));

            Name = name.Trim();
            Bio = bio?.Trim();
            MarkUpdated();
        }

        public void SetProfilePhoto(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("Profile photo URL cannot be empty.", nameof(url));

            if (url.Length > 500)
                throw new ArgumentException("Profile photo URL cannot exceed 500 characters.", nameof(url));

            ProfilePhotoUrl = url;
            MarkUpdated();
        }

        public void Activate()
        {
            if (IsActive)
                throw new InvalidOperationException("User is already active.");

            IsActive = true;
            MarkUpdated();
        }

        public void Deactivate()
        {
            if (!IsActive)
                throw new InvalidOperationException("User is already inactive.");

            IsActive = false;
            MarkUpdated();
        }

        public void ChangeRole(UserRole newRole)
        {
            if (Role == newRole)
                throw new InvalidOperationException($"User already has role {newRole}.");

            Role = newRole;
            MarkUpdated();
        }

        public void AssignAdminRole(AdminRole adminRole)
        {
            AdminRole = adminRole;
            MarkUpdated();
        }

        public void RemoveAdminRole()
        {
            if (AdminRole == AdminRole.None)
                throw new InvalidOperationException("User does not have an admin role.");

            AdminRole = AdminRole.None;
            MarkUpdated();
        }

        public void AddRefreshToken(RefreshToken token)
        {
            if (token is null)
                throw new ArgumentNullException(nameof(token));

            if (token.UserId != Id)
                throw new InvalidOperationException("Refresh token does not belong to this user.");

            _refreshTokens.Add(token);
        }

        public void RevokeRefreshToken(string tokenHash)
        {
            if (string.IsNullOrWhiteSpace(tokenHash))
                throw new ArgumentException("Token hash is required.", nameof(tokenHash));

            var token = _refreshTokens.FirstOrDefault(x => x.TokenHash == tokenHash);

            if (token is null)
                throw new InvalidOperationException("Refresh token not found.");

            token.Revoke();
        }

        public void RevokeAllRefreshTokens()
        {
            foreach (var token in _refreshTokens.Where(t => t.IsActive()))
            {
                token.Revoke();
            }
        }

        public void AddKycSubmission(KycSubmission submission)
        {
            if (submission is null)
                throw new ArgumentNullException(nameof(submission));

            if (submission.UserId != Id)
                throw new InvalidOperationException("KYC submission does not belong to this user.");

            _kycSubmissions.Add(submission);
        }

        public void AddExternalLogin(UserExternalLogin login)
        {
            if (login is null)
                throw new ArgumentNullException(nameof(login));

            if (login.UserId != Id)
                throw new InvalidOperationException("External login does not belong to this user.");

            var exists = _externalLogins.Any(x =>
                x.Provider == login.Provider &&
                x.ExternalId == login.ExternalId);

            if (exists)
                throw new InvalidOperationException("External login already exists.");

            _externalLogins.Add(login);
        }

        public bool CanPlaceBid()
        {
            return IsEligibleForMarketplaceActions();
        }

        public bool CanCreateListing()
        {
            return IsEligibleForMarketplaceActions();
        }

        private bool IsEligibleForMarketplaceActions()
        {
            return IsActive &&
                   EmailVerified &&
                   KycStatus == SubmissionStatus.Approved &&
                   !IsLocked;
        }

        public bool HasAdminRole(AdminRole role)
        {
            return AdminRole == role;
        }

        public bool IsInRole(UserRole role)
        {
            return Role == role || Role == UserRole.Both;
        }

        public void AddOtp(Otp otp)
        {
            if (otp is null)
                throw new ArgumentNullException(nameof(otp));
            if (otp.UserId != Id)
                throw new InvalidOperationException("OTP does not belong to this user.");
            _Otps.Add(otp);
        }
    }
}