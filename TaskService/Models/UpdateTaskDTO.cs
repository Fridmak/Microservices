using Shared.Models;

namespace TaskService.Models
{
    public class UpdateTaskDTO
    {
        public UpdateCommentType Comment { get; set; } = UpdateCommentType.None;
        public string? Description { get; set; }
        public DateTime? DeadLine { get; set; }
        public Priority? Priority { get; set; }
    }

    public enum UpdateCommentType
    {
        None,
        AddComment,
        RemoveLastComment
    }
}
