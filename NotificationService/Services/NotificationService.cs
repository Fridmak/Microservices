using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shared.Contracts;
using Shared.Exceptions;
using Shared.Models;
using NotificationService.Models;

namespace NotificationService.Services
{
    public interface INotificationService
    {
        Task<PagedResult<Notification>> GetNotificationsForUserAsync(
            Guid userId,
            bool unreadOnly,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<Notification> CreateNotificationAsync(
            Notification notification,
            CancellationToken cancellationToken = default);

        Task<bool> MarkAsReadAsync(
            Guid notificationId,
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(
            Guid notificationId,
            Guid userId,
            CancellationToken cancellationToken = default);
    }

    public class NotificationService : INotificationService
    {
        private readonly NotificationDbContext _context;
        private readonly ILogger<NotificationService> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(
            NotificationDbContext context,
            ILogger<NotificationService> logger,
            IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
        }

        public async Task<PagedResult<Notification>> GetNotificationsForUserAsync(
            Guid userId,
            bool unreadOnly,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.Notifications
                .AsNoTracking()
                .Where(notification => notification.UserId == userId);

            if (unreadOnly)
            {
                query = query.Where(notification => !notification.IsRead);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(notification => notification.CreatedAt)
                .ThenByDescending(notification => notification.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<Notification>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return _context.Notifications
                .AsNoTracking()
                .CountAsync(notification => notification.UserId == userId && !notification.IsRead, cancellationToken);
        }

        public async Task<Notification> CreateNotificationAsync(
            Notification notification,
            CancellationToken cancellationToken = default)
        {
            if (notification is null || notification.UserId == Guid.Empty)
            {
                throw new BadRequestException("Некорректные данные уведомления.");
            }

            if (string.IsNullOrWhiteSpace(notification.Title) || string.IsNullOrWhiteSpace(notification.Message))
            {
                throw new BadRequestException("Заголовок и текст уведомления обязательны.");
            }

            if (notification.Id == Guid.Empty)
            {
                notification.Id = Guid.NewGuid();
            }

            var existing = await _context.Notifications
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == notification.Id, cancellationToken);

            if (existing is not null)
            {
                return existing;
            }

            notification.CreatedAt = DateTime.UtcNow;
            notification.IsRead = false;

            _context.Notifications.Add(notification);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // At-least-once delivery from the outbox: the same notification may be retried.
                return await _context.Notifications
                    .AsNoTracking()
                    .FirstAsync(item => item.Id == notification.Id, cancellationToken);
            }

            _logger.LogInformation(
                "Created notification {NotificationId} of type {Type} for user {UserId}",
                notification.Id,
                notification.Type,
                notification.UserId);

            try
            {
                await _hubContext.Clients
                    .User(notification.UserId.ToString())
                    .SendAsync("ReceiveNotification", notification);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to push notification {NotificationId} via SignalR", notification.Id);
            }

            return notification;
        }

        public async Task<bool> MarkAsReadAsync(
            Guid notificationId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(item => item.Id == notificationId, cancellationToken);

            if (notification is null || notification.UserId != userId)
            {
                return false;
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync(cancellationToken);

                try
                {
                    await _hubContext.Clients
                        .User(notification.UserId.ToString())
                        .SendAsync("NotificationUpdated", notification);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to push notification update {NotificationId} via SignalR", notification.Id);
                }
            }

            return true;
        }

        public async Task<int> MarkAllAsReadAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var affected = await _context.Notifications
                .Where(notification => notification.UserId == userId && !notification.IsRead)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(notification => notification.IsRead, true),
                    cancellationToken);

            _logger.LogInformation("Marked {Count} notifications as read for user {UserId}", affected, userId);
            return affected;
        }

        public async Task<bool> DeleteAsync(
            Guid notificationId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(item => item.Id == notificationId, cancellationToken);

            if (notification is null || notification.UserId != userId)
            {
                return false;
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
