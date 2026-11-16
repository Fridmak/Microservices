using AuthService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shared.Options;
using Shared.Security;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("internal/users")]
    public class InternalUsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ServiceAuthOptions _serviceAuthOptions;

        public InternalUsersController(
            IUserService userService,
            IOptions<ServiceAuthOptions> serviceAuthOptions)
        {
            _userService = userService;
            _serviceAuthOptions = serviceAuthOptions.Value;
        }

        [HttpGet("{id:guid}/exists")]
        public async Task<IActionResult> Exists(Guid id, CancellationToken cancellationToken)
        {
            if (!ApiKeyValidator.IsValid(Request, _serviceAuthOptions.ApiKey))
            {
                return Unauthorized();
            }

            var user = await _userService.GetByIdAsync(id, cancellationToken);
            return Ok(new { exists = user is not null });
        }
    }
}
