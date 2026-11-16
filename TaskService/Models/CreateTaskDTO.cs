using Shared.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskService.Models
{
    public class CreateTaskDTO
    {
        [Required]
        [MaxLength(200)]
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [Required]
        [JsonPropertyName("deadLine")]
        public DateTime DeadLine { get; set; }

        [Required]
        [JsonPropertyName("priority")]
        public Priority Priority { get; set; } = Priority.Normal;

        [JsonPropertyName("assigneeId")]
        public Guid? AssigneeId { get; set; }
    }
}
