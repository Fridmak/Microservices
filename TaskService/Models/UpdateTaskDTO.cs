using Shared.Models;
using System.Text.Json.Serialization;

namespace TaskService.Models
{
    public class UpdateTaskDTO
    {
        [JsonPropertyName("comment")]
        public UpdateCommentType? Comment { get; set; } = null;
        [JsonPropertyName("description")]
        public string? Description { get; set; } = null;
        [JsonPropertyName("deadline")]
        public DateTime? DeadLine { get; set; } = null;
        [JsonPropertyName("priority")]
        public Priority? Priority { get; set; } = null;
    }

    public enum UpdateCommentType
    {
        AddComment,
        RemoveLastComment
    }
}
