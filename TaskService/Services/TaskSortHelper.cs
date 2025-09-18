using Microsoft.EntityFrameworkCore;
using Shared.Models;
using TaskService.Models;
using System.Globalization;
using System.Linq.Expressions;

namespace TaskService.Services
{
    public class TaskSortHelper : ITaskSorter
    {
        public async Task<List<UserTask>> SortQueryOfTasks(
            GetTasksQueryParams rules,
            IQueryable<UserTask> query)
        {
            query = query.Where(task => !task.IsSoftDeleted);

            if (rules.PriorityFilter.HasValue)
            {
                query = query.Where(task => task.Priority == rules.PriorityFilter);
            }

            if (!string.IsNullOrWhiteSpace(rules.SearchTerm))
            {
                var normalizedTerm = NormalizeSearchTerm(rules.SearchTerm);
                query = query.Where(BuildSearchExpression(normalizedTerm));
            }

            query = ApplySorting(rules, query);
            query = ApplyPagination(rules, query);

            return await query.ToListAsync();
        }

        private static string NormalizeSearchTerm(string term)
        {
            return term
                .Trim()
                .ToLower(CultureInfo.InvariantCulture)
                .Replace("%", "\\%")
                .Replace("_", "\\_");
        }

        private static Expression<Func<UserTask, bool>> BuildSearchExpression(string term)
        {
            return task => EF.Functions.Like(
                task.Title.ToLower(),
                $"%{term}%",
                "\\");
        }

        private static IQueryable<UserTask> ApplySorting(
            GetTasksQueryParams rules,
            IQueryable<UserTask> query)
        {
            return rules.SortBy switch
            {
                SortTasks.ByPriority => query
                    .OrderByDescending(task => task.Priority)
                    .ThenBy(task => task.DeadLine),

                SortTasks.ByDeadline => query
                    .OrderBy(task => task.DeadLine)
                    .ThenByDescending(task => task.Priority),

                SortTasks.ByCreationDate => query
                    .OrderByDescending(task => task.CreationTime)
                    .ThenByDescending(task => task.Priority),

                _ => query.OrderByDescending(task => task.CreationTime) 
            };
        }

        private static IQueryable<UserTask> ApplyPagination(
           GetTasksQueryParams rules,
           IQueryable<UserTask> query)
        {
            var maxAllowed = 100;
            var take = Math.Min(rules.MaxTasks ?? 10, maxAllowed);

            return take > 0 ? query.Take(take) : query;
        }
    }

    public interface ITaskSorter
    {
        Task<List<UserTask>> SortQueryOfTasks(GetTasksQueryParams rules, IQueryable<UserTask> query);
    }
}