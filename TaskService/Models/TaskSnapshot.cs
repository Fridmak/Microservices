using Shared.Models;

namespace TaskService.Models
{
    public sealed record TaskSnapshot(
        Guid Id,
        Guid OwnerId,
        Guid AssignedToUserId,
        string Title,
        DateTime CreationTime,
        DateTime DeadLine,
        Priority Priority,
        UserTaskStatus Status,
        DateTime? CompletedAt,
        bool IsSoftDeleted,
        IReadOnlyList<string> Comments)
    {
        public static TaskSnapshot From(UserTask task)
        {
            return new TaskSnapshot(
                task.Id,
                task.OwnerId,
                task.AssignedToUserId,
                task.Title,
                task.CreationTime,
                task.DeadLine,
                task.Priority,
                task.Status,
                task.CompletedAt,
                task.IsSoftDeleted,
                task.Comment);
        }
    }
}
