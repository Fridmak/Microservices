using Microsoft.EntityFrameworkCore;
using Shared.Models;
using System.Linq.Expressions;
using TaskService.Models;

namespace TaskService.Services
{
    public class TaskSortHelper : ITaskSorter
    {
        public IQueryable<UserTask> Apply(GetTasksQueryParams rules, IQueryable<UserTask> query)
        {
            query = query.Where(task => !task.IsSoftDeleted);

            if (rules.PriorityFilter.HasValue)
            {
                query = query.Where(task => task.Priority == rules.PriorityFilter.Value);
            }

            if (rules.StatusFilter.HasValue)
            {
                query = query.Where(task => task.Status == rules.StatusFilter.Value);
            }

            if (rules.DeadlineFrom.HasValue)
            {
                query = query.Where(task => task.DeadLine >= rules.DeadlineFrom.Value);
            }

            if (rules.DeadlineTo.HasValue)
            {
                query = query.Where(task => task.DeadLine <= rules.DeadlineTo.Value);
            }

            if (!string.IsNullOrWhiteSpace(rules.SearchTerm))
            {
                var pattern = $"%{NormalizeSearchTerm(rules.SearchTerm)}%";
                query = query.Where(task => EF.Functions.ILike(task.Title, pattern, "\\"));
            }

            return ApplySorting(rules, query);
        }

        private static string NormalizeSearchTerm(string term)
        {
            return term
                .Trim()
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");
        }

        private static IQueryable<UserTask> ApplySorting(
            GetTasksQueryParams rules,
            IQueryable<UserTask> query)
        {
            return rules.SortBy switch
            {
                SortTasks.ByPriority => query
                    .OrderByDescending(PriorityRankDescending)
                    .ThenBy(task => task.DeadLine)
                    .ThenBy(task => task.Id),

                SortTasks.ByDeadline => query
                    .OrderBy(task => task.DeadLine)
                    .ThenByDescending(PriorityRankDescending)
                    .ThenBy(task => task.Id),

                SortTasks.ByCreationDate => query
                    .OrderByDescending(task => task.CreationTime)
                    .ThenByDescending(PriorityRankDescending)
                    .ThenBy(task => task.Id),

                _ => query
                    .OrderByDescending(task => task.CreationTime)
                    .ThenBy(task => task.Id)
            };
        }

        // Business rank: Low < Normal < High < Necessary.
        // Stored numeric enum values are kept for backward compatibility with existing data.
        private static readonly Expression<Func<UserTask, int>> PriorityRankDescending = task =>
            task.Priority == Priority.Necessary
                ? 3
                : task.Priority == Priority.High
                    ? 2
                    : task.Priority == Priority.Normal
                        ? 1
                        : 0;
    }

    public interface ITaskSorter
    {
        IQueryable<UserTask> Apply(GetTasksQueryParams rules, IQueryable<UserTask> query);
    }
}
