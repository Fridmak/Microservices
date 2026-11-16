namespace Shared.Options
{
    public sealed class ServiceAuthOptions
    {
        public const string SectionName = "Services:Security";

        public string ApiKey { get; set; } = string.Empty;
    }
}
