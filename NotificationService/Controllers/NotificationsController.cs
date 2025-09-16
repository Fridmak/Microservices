using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Services;
using Shared.Models;
using System.Security.Claims;

namespace NotificationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            INotificationService notificationService,
            ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpGet("{userId}")]
        [Authorize]
        public async Task<IActionResult> GetNotifications(Guid userId)
        {
            try
            {
                var notifications = await _notificationService.GetNotificationsForUserAsync(userId);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении уведомлений для пользователя {userId}");
                return StatusCode(500, "Ошибка сервера");
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateNotification([FromBody] Notification notification)
        {
            try
            {
                var createdNotification = await _notificationService.CreateNotificationAsync(notification);
                return CreatedAtAction(nameof(GetNotifications), new { userId = createdNotification.UserId }, createdNotification);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Некорректные данные уведомления");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании уведомления");
                return StatusCode(500, "Ошибка сервера");
            }
        }

        [HttpPut("{id}/mark-as-read")]
        [Authorize]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
                {
                    return Forbid("Не удалось определить пользователя");
                }

                var success = await _notificationService.MarkAsReadAsync(id, userId);

                if (!success)
                {
                    return NotFound($"Уведомление с ID {id} не найдено или вы не можете его читать");
                }

                return Ok("Done.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при отметке уведомления {id} как прочитанного");
                return StatusCode(500, "Ошибка сервера");
            }
        }
    }
}