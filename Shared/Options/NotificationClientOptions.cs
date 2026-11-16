namespace Shared.Options
{
    public sealed class NotificationClientOptions
    {
        public const string SectionName = "Services:NotificationService";

        public string BaseUrl { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;

        public int TimeoutSeconds { get; set; } = 5;
    }
}
