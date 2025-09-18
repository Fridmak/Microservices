using Shared.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskService.Models
{
    public class GetTasksQueryParams
    {
        [JsonPropertyName("maxTasks")]
        [Range(1, 100, ErrorMessage = "Max tasks must be between 1 and 100")]
        public int? MaxTasks { get; set; }

        [JsonPropertyName("sortBy")]
        public SortTasks? SortBy { get; set; }

        [JsonPropertyName("priorityFilter")]
        public Priority? PriorityFilter { get; set; }

        [JsonPropertyName("searchTerm")]
        public string? SearchTerm { get; set; }
    }
}