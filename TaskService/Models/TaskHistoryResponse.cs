using TaskService.Models.Db;

namespace TaskService.Models
{
    public sealed class TaskHistoryResponse
    {
        public Guid Id { get; init; }
        public Guid TaskId { get; init; }
        public Guid ChangedByUserId { get; init; }
        public DateTime ChangedAt { get; init; }
        public ChangeType Type { get; init; }
        public string? Details { get; init; }
        public string? FullTaskState { get; init; }

        public static TaskHistoryResponse FromEntity(TaskHistory history)
        {
            return new TaskHistoryResponse
            {
                Id = history.Id,
                TaskId = history.TaskId,
                ChangedByUserId = history.ChangedByUserId,
                ChangedAt = history.ChangedAt,
                Type = history.Type,
                Details = history.Details,
                FullTaskState = history.FullTaskState
            };
        }
    }
}
