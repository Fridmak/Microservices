using Shared.Models;

namespace TaskService.Models
{
    public class GetTasksQueryParams
    {
        public int maxTasks { get; set; } = 10;
        public SortTasks SortBy { get; set; } = SortTasks.Default;
        public Priority Search { get; set; }
        public string? SearchWord { get; set; } // ToDo
    }
}
