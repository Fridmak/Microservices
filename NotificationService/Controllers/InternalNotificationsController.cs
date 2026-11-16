using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NotificationService.Services;
using Shared.Models;
using Shared.Options;
using Shared.Security;

namespace NotificationService.Controllers
{
    [ApiController]
    [Route("internal/notifications")]
    public class InternalNotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ServiceAuthOptions _serviceAuthOptions;

        public InternalNotificationsController(
            INotificationService notificationService,
            IOptions<ServiceAuthOptions> serviceAuthOptions)
        {
            _notificationService = notificationService;
            _serviceAuthOptions = serviceAuthOptions.Value;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] Notification notification,
            CancellationToken cancellationToken)
        {
            if (!ApiKeyValidator.IsValid(Request, _serviceAuthOptions.ApiKey))
            {
                return Unauthorized();
            }

            var created = await _notificationService.CreateNotificationAsync(notification, cancellationToken);
            return Ok(created);
        }
    }
}
