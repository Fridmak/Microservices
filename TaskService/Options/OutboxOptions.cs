namespace TaskService.Options
{
    public sealed class OutboxOptions
    {
        public const string SectionName = "Outbox";

        public int BatchSize { get; set; } = 20;

        public int MaxAttempts { get; set; } = 10;

        public int PollIntervalSeconds { get; set; } = 2;
    }
}
