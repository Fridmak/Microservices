namespace Shared.Options
{
    public sealed class AuthServiceClientOptions
    {
        public const string SectionName = "Services:AuthService";

        public string BaseUrl { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;

        public int TimeoutSeconds { get; set; } = 3;
    }
}
