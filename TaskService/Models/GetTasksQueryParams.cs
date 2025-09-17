using Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace TaskService.Models
{
    public class GetTasksQueryParams
    {
        [Range(1, 100, ErrorMessage = "Max tasks must be between 1 and 100")]
        public int MaxTasks { get; set; } = 10;
        public SortTasks SortBy { get; set; } = SortTasks.Default;
        public Priority? PriorityFilter { get; set; }

        [StringLength(100, ErrorMessage = "Search term too long")]
        public string? SearchTerm { get; set; }
    }
}
