using Shared.Models;

namespace TaskService.Models
{
    public sealed class UserTaskResponse
    {
        public Guid Id { get; init; }
        public Guid OwnerId { get; init; }
        public Guid AssignedToUserId { get; init; }
        public DateTime CreationTime { get; init; }
        public string Title { get; init; } = string.Empty;
        public DateTime DeadLine { get; init; }
        public Priority Priority { get; init; }
        public UserTaskStatus Status { get; init; }
        public DateTime? CompletedAt { get; init; }
        public IReadOnlyList<string> Comment { get; init; } = Array.Empty<string>();

        public static UserTaskResponse FromEntity(UserTask task)
        {
            return new UserTaskResponse
            {
                Id = task.Id,
                OwnerId = task.OwnerId,
                AssignedToUserId = task.AssignedToUserId,
                CreationTime = task.CreationTime,
                Title = task.Title,
                DeadLine = task.DeadLine,
                Priority = task.Priority,
                Status = task.Status,
                CompletedAt = task.CompletedAt,
                Comment = task.Comment
            };
        }
    }
}
