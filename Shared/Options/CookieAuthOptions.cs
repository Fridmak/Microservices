namespace Shared.Options
{
    public sealed class CookieAuthOptions
    {
        public const string SectionName = "Auth:Cookie";

        public string Name { get; set; } = "auth_token";

        public bool Secure { get; set; } = true;

        public string SameSite { get; set; } = "Strict";

        public int ExpiryHours { get; set; } = 24;
    }
}
