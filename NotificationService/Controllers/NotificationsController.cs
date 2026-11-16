using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Services;
using Shared.Contracts;
using Shared.Exceptions;
using Shared.Extensions;
using Shared.Models;

namespace NotificationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<PagedResult<Notification>>> GetNotifications(
            [FromQuery] bool unreadOnly = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var userId = User.GetRequiredUserId();
            var notifications = await _notificationService.GetNotificationsForUserAsync(
                userId,
                unreadOnly,
                page,
                pageSize,
                cancellationToken);

            return Ok(notifications);
        }

        [HttpGet("unread-count")]
        [Authorize]
        public async Task<ActionResult<object>> GetUnreadCount(CancellationToken cancellationToken)
        {
            var userId = User.GetRequiredUserId();
            var count = await _notificationService.GetUnreadCountAsync(userId, cancellationToken);

            return Ok(new { count });
        }

        [HttpPut("{id:guid}/read")]
        [Authorize]
        public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
        {
            var success = await _notificationService.MarkAsReadAsync(
                id,
                User.GetRequiredUserId(),
                cancellationToken);

            if (!success)
            {
                throw new NotFoundException("Уведомление не найдено.");
            }

            return NoContent();
        }

        [HttpPut("read-all")]
        [Authorize]
        public async Task<ActionResult<object>> MarkAllAsRead(CancellationToken cancellationToken)
        {
            var count = await _notificationService.MarkAllAsReadAsync(
                User.GetRequiredUserId(),
                cancellationToken);

            return Ok(new { count });
        }

        [HttpDelete("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var success = await _notificationService.DeleteAsync(
                id,
                User.GetRequiredUserId(),
                cancellationToken);

            if (!success)
            {
                throw new NotFoundException("Уведомление не найдено.");
            }

            return NoContent();
        }
    }
}
