using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Shared.Models;

namespace TaskService.Models
{
    public class UpdateTaskStatusDTO
    {
        [Required]
        [JsonPropertyName("status")]
        public UserTaskStatus Status { get; set; }
    }
}
