using Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace TaskService.Models
{
    public class GetTasksQueryParams
    {
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;

        public SortTasks? SortBy { get; set; }

        public Priority? PriorityFilter { get; set; }

        public UserTaskStatus? StatusFilter { get; set; }

        [MaxLength(200)]
        public string? SearchTerm { get; set; }

        public TaskScope Scope { get; set; } = TaskScope.Assigned;

        public DateTime? DeadlineFrom { get; set; }

        public DateTime? DeadlineTo { get; set; }
    }

    public enum TaskScope
    {
        Assigned = 0,
        Owned = 1
    }
}
