using Shared.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskService.Models
{
    public class UpdateTaskDTO
    {
        [MaxLength(200)]
        [JsonPropertyName("title")]
        public string? Title { get; set; } = null;

        [JsonPropertyName("comment")]
        public UpdateCommentType? Comment { get; set; } = null;

        [MaxLength(2000)]
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
