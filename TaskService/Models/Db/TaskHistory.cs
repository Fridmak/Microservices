using Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace TaskService.Models.Db
{
    public class TaskHistory
    {
        [Required]
        public Guid Id { get; set; }
        [Required]
        public Guid TaskId { get; set; }
        public string? ChangedByUserId { get; set; }
        public DateTime ChangedAt { get; set; }
        public ChangeType Type { get; set; }
        public string? FullTaskState { get; set; } // JSON
    }

    public enum ChangeType
    {
        Created = 0,
        Updated = 1,
        Changed = 2,
        Assigned = 3,
        Deleted = 4
    }
}
