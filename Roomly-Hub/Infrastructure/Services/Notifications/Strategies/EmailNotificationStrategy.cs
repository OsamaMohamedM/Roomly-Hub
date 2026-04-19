using Application.Common.Constants;
using Application.Common.Results;
using Application.Interfaces.Services;
using Application.Interfaces.Strategies;
using Domain.enums.Notifications;
using Domain.Interfaces.Repositories;

namespace Application.Services.Notifications.Strategies
{
    public class EmailNotificationStrategy : INotificationChannelStrategy
    {
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;

        public EmailNotificationStrategy(IEmailService emailService, IUserRepository userRepository)
        {
            _emailService = emailService;
            _userRepository = userRepository;
        }

        public NotificationChannel ChannelType => NotificationChannel.Email;

        public async Task<Result> SendAsync(Guid userId, NotificationType type, string title, string body, string deepLink, CancellationToken ct)
        {
            var user = await _userRepository.GetByIdAsync(userId, ct);
            if (user == null)
                return Result.Failure(Errors.Codes.Common.UserNotFound, Errors.Messages.Common.UserNotFound);

            var htmlBody = string.IsNullOrWhiteSpace(deepLink)
                ? body
                : $"{body}<br/><a href=\"{deepLink}\">Open</a>";

            await _emailService.SendEmailAsync(user.Email.Value, title, htmlBody, ct);
            return Result.Success();
        }
    }
}
