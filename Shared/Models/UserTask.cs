using System.ComponentModel.DataAnnotations;

namespace Shared.Models
{
    public class UserTask
    {
        public Guid Id { get; set; }

        [Required]
        public Guid OwnerId { get; set; }

        [Required]
        public Guid AssignedToUserId { get; set; }

        [Required]
        public DateTime CreationTime { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public DateTime DeadLine { get; set; }

        [Required]
        public Priority Priority { get; set; }

        public UserTaskStatus Status { get; set; } = UserTaskStatus.Todo;

        public DateTime? CompletedAt { get; set; }

        public bool IsSoftDeleted { get; set; }

        public List<string> Comment { get; set; } = new();
    }

    public enum Priority
    {
        Normal = 0,
        High = 1,
        Low = 2,
        Necessary = 3
    }

    public enum UserTaskStatus
    {
        Todo = 0,
        InProgress = 1,
        Done = 2,
        Cancelled = 3
    }
}
