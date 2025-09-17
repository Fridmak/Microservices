using Microsoft.EntityFrameworkCore;
using Shared.Models;
using System.Globalization;
using TaskService.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace TaskService.Services
{
    public class TaskSortHelper : ITaskSorter
    {
        public async Task<List<UserTask>> SortQueryOfTasks(GetTasksQueryParams rules, IQueryable<UserTask> query)
        {
            query = rules.SortBy switch
            {
                SortTasks.ByPriority => query.OrderByDescending(task => task.Priority)
                                            .ThenBy(task => task.DeadLine),
                SortTasks.ByDeadline => query.OrderBy(task => task.DeadLine)
                                            .ThenByDescending(task => task.Priority),
                SortTasks.ByCreationDate => query.OrderByDescending(task => task.CreationTime)
                                                .ThenByDescending(task => task.Priority),
                SortTasks.Default or _ => query.OrderBy(task => task.CreationTime)
            };

            if (rules.maxTasks > 0)
                query = query.Take(rules.maxTasks);

            var tasks = await query.ToListAsync();

            return tasks;
        }
    }

    public interface ITaskSorter
    {
        public Task<List<UserTask>> SortQueryOfTasks(GetTasksQueryParams rules, IQueryable<UserTask> query);
    }
}
