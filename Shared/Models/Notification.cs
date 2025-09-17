using System.ComponentModel.DataAnnotations;

namespace Shared.Models
{
    public enum NotificationType
    {
        TaskCreated,
        TaskAssigned,
        TaskUpdated,
        TaskDeleted,
        TaskReassigned
    }

    public class Notification
    {
        [Required]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        [Required]
        public NotificationType Type { get; set; }

        public Guid? Author { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;
        public string? Data { get; set; } // JSON
    }
}
