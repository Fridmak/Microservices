using System.Security.Claims;

namespace Shared.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid? GetUserId(this ClaimsPrincipal principal)
        {
            var value = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }

        public static Guid GetRequiredUserId(this ClaimsPrincipal principal)
        {
            return principal.GetUserId()
                ?? throw new UnauthorizedAccessException("User id claim is missing.");
        }
    }
}
