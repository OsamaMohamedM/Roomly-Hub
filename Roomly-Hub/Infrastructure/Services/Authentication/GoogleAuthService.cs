using Application.DTOs;
using Application.Interfaces.Services;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly GoogleSettings _settings;

        public GoogleAuthService(IOptions<GoogleSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task<GoogleUserInfo?> ValidateIdTokenAsync(string idToken, CancellationToken cancellationToken = default)
        {
            try
            {
                var validationSettings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [_settings.ClientId]
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);

                return new GoogleUserInfo
                {
                    GoogleId = payload.Subject,
                    Email = payload.Email,
                    Name = string.IsNullOrWhiteSpace(payload.Name)
                        ? payload.Email.Split('@')[0]
                        : payload.Name,
                    PictureUrl = payload.Picture,
                    EmailVerified = payload.EmailVerified
                };
            }
            catch (InvalidJwtException)
            {
                return null;
            }
        }
    }
}