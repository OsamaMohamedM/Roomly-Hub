using Application.Common.Constants;
using Application.Common.Results;
using Application.DTOs.Notifications;
using Application.Interfaces.Persistence;
using Application.Interfaces.Services;
using Application.Interfaces.Strategies;
using Domain.Entities.Notifications;
using Domain.enums.Notifications;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Application.Services.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly IEnumerable<INotificationChannelStrategy> _strategies;
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<NotificationService> _logger;

        private static readonly IReadOnlyDictionary<NotificationType, NotificationCategory> CategoryMap = new Dictionary<NotificationType, NotificationCategory>
        {
            [NotificationType.BookingRequested] = NotificationCategory.Booking,
            [NotificationType.BookingConfirmed] = NotificationCategory.Booking,
            [NotificationType.BookingRejected] = NotificationCategory.Booking,
            [NotificationType.BookingCancelled] = NotificationCategory.Booking,
            [NotificationType.PaymentConfirmation] = NotificationCategory.Payment,
            [NotificationType.PaymentFailed] = NotificationCategory.Payment,
            [NotificationType.ReviewReceived] = NotificationCategory.Review,
            [NotificationType.KycApproved] = NotificationCategory.Account,
            [NotificationType.KycRejected] = NotificationCategory.Account,
            [NotificationType.AccountLocked] = NotificationCategory.Account,
            [NotificationType.SecurityAlert] = NotificationCategory.Account
        };

        private static readonly Dictionary<NotificationType, List<NotificationChannel>> _supportedChannels = new(){
    { NotificationType.AccountLocked, new List<NotificationChannel> { NotificationChannel.Email } },
    { NotificationType.ReviewReceived, new List<NotificationChannel> { NotificationChannel.InApp , NotificationChannel.Push} },
            {  NotificationType.KycApproved, new List<NotificationChannel> { NotificationChannel.Email, NotificationChannel.InApp, NotificationChannel.Push }  },
            { NotificationType.KycRejected, new List<NotificationChannel> { NotificationChannel.Email, NotificationChannel.InApp, NotificationChannel.Push } },
            {  NotificationType.PaymentConfirmation, new List<NotificationChannel> { NotificationChannel.Email, NotificationChannel.InApp, NotificationChannel.Push  }  },
            { NotificationType.PaymentFailed, new List<NotificationChannel> { NotificationChannel.InApp, NotificationChannel.Push } },
            {NotificationType.BookingRequested, new List<NotificationChannel> { NotificationChannel.InApp, NotificationChannel.Email }},
            {NotificationType.BookingConfirmed, new List<NotificationChannel> { NotificationChannel.InApp, NotificationChannel.Email, NotificationChannel.Push }},
            { NotificationType.BookingRejected, new List<NotificationChannel> { NotificationChannel.InApp, NotificationChannel.Email, NotificationChannel.Push } },
            {  NotificationType.BookingCancelled, new List<NotificationChannel> { NotificationChannel.InApp, NotificationChannel.Email,  NotificationChannel.Push }},
            { NotificationType.SecurityAlert, new List<NotificationChannel> { NotificationChannel.Email } },
        };

        public NotificationService(
            IEnumerable<INotificationChannelStrategy> strategies,
            INotificationRepository notificationRepository,
            IUnitOfWork unitOfWork,
            ILogger<NotificationService> logger)
        {
            _strategies = strategies;
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public static NotificationCategory ResolveCategory(NotificationType type)
        {
            return CategoryMap.TryGetValue(type, out var category)
                ? category
                : NotificationCategory.System;
        }

        public async Task<Result> CreateDefaultPreferencesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var existingPreferences = await _notificationRepository.GetPreferencesByUserIdAsync(userId, cancellationToken);
            var existingMap = existingPreferences
                .Select(p => (p.Category, p.Channel))
                .ToHashSet();

            bool hasNewPreferences = false;

            foreach (var category in Enum.GetValues<NotificationCategory>())
            {
                foreach (var channel in Enum.GetValues<NotificationChannel>())
                {
                    if (existingMap.Contains((category, channel)))
                        continue;

                    var preference = NotificationChannelPreference.Create(userId, category, channel, true);
                    await _notificationRepository.AddPreferenceAsync(preference, cancellationToken);
                    hasNewPreferences = true;
                }
            }
            if (hasNewPreferences)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Result.Success();
        }

        public async Task<Result> SendAsync(Guid userId, NotificationType type, string title, string body, string deepLink, CancellationToken cancellationToken = default)
        {
            var category = ResolveCategory(type);
            var preferences = await _notificationRepository.GetPreferencesByUserIdAsync(userId, cancellationToken);
            foreach (var strategy in _strategies)
            {
                var isEnabled = preferences.Any(p => p.Category == category && p.Channel == strategy.ChannelType && p.IsEnabled);
                var isSupported = _supportedChannels.TryGetValue(type, out var channels) && channels.Contains(strategy.ChannelType);
                if (!isEnabled || !isSupported)
                    continue;
                try
                {
                    var sendResult = await strategy.SendAsync(userId, type, title, body, deepLink, cancellationToken);
                    if (sendResult.IsFailure)
                        _logger.LogWarning("Notification strategy {ChannelType} failed for user {UserId} with code {ErrorCode}", strategy.ChannelType, userId, sendResult.ErrorCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Notification strategy {ChannelType} failed for user {UserId}", strategy.ChannelType, userId);
                }
            }

            return Result.Success();
        }

        public async Task<Result<List<NotificationDto>>> GetUnreadNotificationsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var notifications = await _notificationRepository.GetUnreadByUserIdAsync(userId, cancellationToken);
            return Result<List<NotificationDto>>.Success(notifications.Select(Map).ToList());
        }

        public async Task<Result<int>> GetUnreadNotificationsCountAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var count = await _notificationRepository.GetUnreadCountByUserIdAsync(userId, cancellationToken);
            return Result<int>.Success(count);
        }

        public async Task<Result> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            await _notificationRepository.MarkAllAsReadAsync(userId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result> UpdatePreferencesAsync(Guid userId, UpdatePreferenceRequestDto requestDto, CancellationToken cancellationToken = default)
        {
            if (requestDto == null)
                return Result.Failure(Errors.Codes.Common.ValidationError, Errors.Messages.Common.RequestBodyRequired);

            var preference = await _notificationRepository.GetPreferenceAsync(userId, requestDto.Category, requestDto.Channel, cancellationToken);
            if (preference == null)
            {
                await _notificationRepository.AddPreferenceAsync(NotificationChannelPreference.Create(userId, requestDto.Category, requestDto.Channel, requestDto.IsEnabled), cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }

            preference.SetEnabled(requestDto.IsEnabled);
            await _notificationRepository.UpdatePreferenceAsync(preference, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        private static NotificationDto Map(Notification notification)
        {
            return new NotificationDto
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Title = notification.Title,
                Body = notification.Body,
                Type = notification.Type,
                Category = notification.Category,
                Channel = notification.Channel,
                Status = notification.Status,
                ActionUrl = notification.ActionUrl,
                CreatedAt = notification.CreatedAt,
                ReadAt = notification.ReadAt
            };
        }
    }
}