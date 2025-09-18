using System.Text.Json.Serialization;

namespace TaskService.Models
{
    public class DeleteTaskDto
    {
        [JsonPropertyName("isHardDelete")]
        public bool IsHardDelete { get; set; } = false;
    }
}
