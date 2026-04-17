using Domain.Entities.Notifications;
using Domain.enums.Notifications;
using Domain.Interfaces.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _context;

        public NotificationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            await _context.Notifications.AddAsync(notification, cancellationToken);
        }

        public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Notifications.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        }

        public async Task<List<Notification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Notifications
                .Where(x => x.UserId == userId && x.Status == NotificationStatus.Unread && !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetUnreadCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Notifications.CountAsync(x => x.UserId == userId && x.Status == NotificationStatus.Unread && !x.IsDeleted, cancellationToken);
        }

        public Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task AddPreferenceAsync(NotificationChannelPreference preference, CancellationToken cancellationToken = default)
        {
            await _context.NotificationChannelPreferences.AddAsync(preference, cancellationToken);
        }

        public async Task<NotificationChannelPreference?> GetPreferenceAsync(Guid userId, NotificationCategory category, NotificationChannel channel, CancellationToken cancellationToken = default)
        {
            return await _context.NotificationChannelPreferences
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Category == category && x.Channel == channel && !x.IsDeleted, cancellationToken);
        }

        public Task UpdatePreferenceAsync(NotificationChannelPreference preference, CancellationToken cancellationToken = default)
        {
            _context.NotificationChannelPreferences.Update(preference);
            return Task.CompletedTask;
        }
    }
}