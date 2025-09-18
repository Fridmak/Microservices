using Shared.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskService.Models
{
    public class CreateTaskDTO
    {
        [Required]
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [Required]
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [Required]
        [JsonPropertyName("deadLine")]
        public DateTime DeadLine { get; set; }

        [Required]
        [JsonPropertyName("priority")]
        public Priority Priority { get; set; }
    }
}
