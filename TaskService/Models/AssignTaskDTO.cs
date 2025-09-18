using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskService.Models
{
    public class AssignTaskDTO
    {
        [Required]
        [JsonPropertyName("userId")]
        public string UserId { get; set; }
    }
}
