using System.ComponentModel.DataAnnotations;

namespace TaskService.Models.Db
{
    public class TaskHistory
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid TaskId { get; set; }

        [Required]
        public Guid ChangedByUserId { get; set; }

        [Required]
        public DateTime ChangedAt { get; set; }

        [Required]
        public ChangeType Type { get; set; }

        [MaxLength(500)]
        public string? Details { get; set; }

        public string? FullTaskState { get; set; }
    }

    public enum ChangeType
    {
        Created = 0,
        Updated = 1,
        Changed = 2,
        Assigned = 3,
        Deleted = 4,
        SoftDeleted = 5,
        StatusChanged = 6,
        Restored = 7,
        CommentAdded = 8,
        CommentRemoved = 9
    }
}
