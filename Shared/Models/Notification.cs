using System.ComponentModel.DataAnnotations;

namespace Shared.Models
{
    public enum NotificationType
    {
        TaskCreated = 0,
        TaskAssigned = 1,
        TaskUpdated = 2,
        TaskDeleted = 3,
        TaskReassigned = 4,
        TaskStatusChanged = 5,
        TaskRestored = 6
    }

    public class Notification
    {
        [Required]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        [Required]
        public NotificationType Type { get; set; }

        public Guid? Author { get; set; }

        public Guid? TaskId { get; set; }

        public Guid? RelatedUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; }

        public string? Data { get; set; }
    }
}
