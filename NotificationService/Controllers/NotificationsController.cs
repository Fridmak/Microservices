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
        private readonly string _apiKey;

        public NotificationsController(
            INotificationService notificationService,
            ILogger<NotificationsController> logger,
            IConfiguration configuration)
        {
            _notificationService = notificationService;

            _apiKey = configuration["Services:Security:ApiKey"]
                ?? throw new InvalidOperationException("ApiKey not configured.");
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
                return StatusCode(500, "Ошибка сервера");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateNotification([FromBody] Notification notification)
        {
            if (!Request.Headers.TryGetValue("Service-Api-Key", out var apiKey))
            {
                return Unauthorized("Missing ApiKey");
            }
            if (apiKey != _apiKey)
            {
                return Unauthorized("Invalid API Key");
            }

            try
            {
                var createdNotification = await _notificationService.CreateNotificationAsync(notification);
                return CreatedAtAction(nameof(GetNotifications), new { userId = createdNotification.UserId }, createdNotification);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
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
                return StatusCode(500, "Ошибка сервера");
            }
        }
    }
}