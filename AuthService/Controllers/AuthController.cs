using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.Models;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Exceptions;
using Shared.Extensions;
using Shared.Models;
using Shared.Options;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly LoginAttemptLimiter _loginAttemptLimiter;
        private readonly JwtOptions _jwtOptions;
        private readonly CookieAuthOptions _cookieOptions;

        public AuthController(
            IUserService userService,
            LoginAttemptLimiter loginAttemptLimiter,
            IOptions<JwtOptions> jwtOptions,
            IOptions<CookieAuthOptions> cookieOptions)
        {
            _userService = userService;
            _loginAttemptLimiter = loginAttemptLimiter;
            _jwtOptions = jwtOptions.Value;
            _cookieOptions = cookieOptions.Value;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterUserDTO model,
            CancellationToken cancellationToken)
        {
            var user = await _userService.RegisterAsync(
                model.Username,
                model.Email,
                model.Password,
                cancellationToken);

            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginUserDTO model,
            CancellationToken cancellationToken)
        {
            var email = UserService.NormalizeEmail(model.Email);

            if (_loginAttemptLimiter.IsBlocked(email, out var retryAfter))
            {
                Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString();
                return StatusCode(StatusCodes.Status429TooManyRequests, new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many login attempts",
                    Detail = "Повторите попытку позже.",
                    Instance = HttpContext.Request.Path
                });
            }

            var user = await _userService.AuthenticateAsync(email, model.Password, cancellationToken);

            if (user is null)
            {
                _loginAttemptLimiter.RegisterFailure(email);
                return Unauthorized(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = "Неверный email или пароль.",
                    Instance = HttpContext.Request.Path
                });
            }

            _loginAttemptLimiter.Reset(email);

            var token = GenerateJwtToken(user);
            var expiresAt = DateTimeOffset.UtcNow.AddHours(_cookieOptions.ExpiryHours);
            Response.Cookies.Append(_cookieOptions.Name, token, CreateCookieOptions(expiresAt));

            return Ok(new
            {
                user,
                expiresAt
            });
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        public IActionResult Logout()
        {
            Response.Cookies.Delete(_cookieOptions.Name, CreateCookieOptions());
            return NoContent();
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me(CancellationToken cancellationToken)
        {
            var userId = User.GetRequiredUserId();
            var user = await _userService.GetByIdAsync(userId, cancellationToken);

            if (user is null)
            {
                throw new NotFoundException("Пользователь не найден.");
            }

            return Ok(user);
        }

        [HttpGet("users")]
        [Authorize]
        public async Task<IActionResult> GetUsers(
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var users = await _userService.GetUsersAsync(search, page, pageSize, cancellationToken);
            return Ok(users);
        }

        [HttpGet("users/{id:guid}")]
        [Authorize]
        public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
        {
            var user = await _userService.GetByIdAsync(id, cancellationToken);
            if (user is null)
            {
                throw new NotFoundException("Пользователь не найден.");
            }

            return Ok(new UserSummary(user.Id, user.Name));
        }

        private string GenerateJwtToken(PublicUser user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = _jwtOptions.Issuer,
                Audience = _jwtOptions.Audience,
                Expires = DateTime.UtcNow.AddHours(_jwtOptions.ExpiryHours),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        private CookieOptions CreateCookieOptions(DateTimeOffset? expires = null)
        {
            var sameSite = Enum.TryParse<SameSiteMode>(_cookieOptions.SameSite, true, out var parsed)
                ? parsed
                : SameSiteMode.Strict;

            return new CookieOptions
            {
                HttpOnly = true,
                Secure = _cookieOptions.Secure,
                SameSite = sameSite,
                Expires = expires,
                Path = "/",
                IsEssential = true
            };
        }
    }
}
