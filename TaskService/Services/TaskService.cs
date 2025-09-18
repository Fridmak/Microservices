using Microsoft.EntityFrameworkCore;
using Shared.Models;
using System.Text.Json;
using TaskService.Models;
using TaskService.Models.Db;

namespace TaskService.Services
{
    public interface IUserTaskService
    {
        // GET /api/tasks
        Task<List<UserTask>> GetAllTasksAsync(GetTasksQueryParams? rules, Guid currentUserId);

        // GET /api/tasks/{id}
        Task<UserTask> GetTaskByIdAsync(Guid id);

        // POST /api/tasks
        Task<UserTask> CreateTaskAsync(CreateTaskDTO createTaskDto, Guid currentUserId);

        // PUT /api/tasks/{id}
        Task<UserTask> UpdateTaskByIdAsync(Guid id, UpdateTaskDTO updateTaskDto, Guid currentUserId);

        // DELETE /api/tasks/{id}
        Task<bool> DeleteTaskByIdAsync(Guid id, bool isHardDelete, Guid currentUserId);

        // PUT /api/tasks/{id}/assign
        Task<bool> AssignTaskByIdAsync(Guid tskId, Guid userId, Guid currentUserId);

        // GET /api/id/history
        Task<List<TaskHistory>> GetTaskHistory(Guid id);
    }

    public class UserTaskService : IUserTaskService
    {
        private readonly TaskDbContext _context;
        private readonly ILogger<UserTaskService> _logger;
        private readonly INotificationConnection _notificationConnection;
        private readonly ITaskSorter _taskSorter;

        public UserTaskService(
            ILogger<UserTaskService> logger,
            TaskDbContext context,
            INotificationConnection notificationConnection,
            ITaskSorter taskSorter)
        {
            _logger = logger;
            _context = context;
            _notificationConnection = notificationConnection;
            _taskSorter = taskSorter;
        }

        public async Task<List<UserTask>> GetAllTasksAsync(GetTasksQueryParams? rules, Guid currentUserId)
        {
            try
            {
                var query = _context.Tasks
                    .Where(task => task.AssignedToUserId == currentUserId);

                var tasks = await _taskSorter.SortQueryOfTasks(rules ?? new GetTasksQueryParams(), query);

                _logger.LogInformation($"Получено {tasks.Count} задач для пользователя {currentUserId}");
                return tasks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении всех задач для пользователя {currentUserId}");
                throw;
            }
        }

        public async Task<UserTask> GetTaskByIdAsync(Guid id)
        {
            try
            {
                var task = await _context.Tasks
                    .FirstOrDefaultAsync(t => t.Id == id && !t.IsSoftDeleted);

                if (task == null)
                    _logger.LogWarning($"Задача с ID {id} не найдена");

                return task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении задачи с ID {id}");
                throw;
            }
        }

        public async Task<UserTask> CreateTaskAsync(CreateTaskDTO createTaskDto, Guid currentUserId)
        {
            try
            {
                var task = new UserTask
                {
                    Id = Guid.NewGuid(),
                    AssignedToUserId = currentUserId,
                    CreationTime = DateTime.UtcNow,
                    DeadLine = createTaskDto.DeadLine,
                    Priority = createTaskDto.Priority,
                    Title = createTaskDto.Title,
                    Comment = new List<string> { createTaskDto.Description }
                };

                _context.Tasks.Add(task);
                await _context.SaveChangesAsync();

                await SaveHistoryAsync(task, ChangeType.Created, currentUserId);

                try
                {
                    await _notificationConnection.SendTaskNotificationAsync(
                        userId: task.AssignedToUserId,
                        type: NotificationType.TaskCreated,
                        title: "Новая задача",
                        message: $"Создана новая задача: {createTaskDto.Title ?? "Без названия"}",
                        taskId: task.Id,
                        author: currentUserId
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Не удалось отправить уведомление о создании задачи {task.Id}");
                }

                _logger.LogInformation($"Создана новая задача с ID {task.Id}");
                return task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании задачи");
                throw;
            }
        }

        public async Task<UserTask> UpdateTaskByIdAsync(Guid id, UpdateTaskDTO updateTaskDto, Guid currentUserId)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(id);

                if (task == null || task.IsSoftDeleted) return null;

                var changes = new List<string>();

                if (updateTaskDto.DeadLine != null && task.DeadLine != updateTaskDto.DeadLine.Value)
                {
                    changes.Add($"срок выполнения изменен на {updateTaskDto.DeadLine.Value:dd.MM.yyyy}");
                    task.DeadLine = updateTaskDto.DeadLine.Value;
                }

                if (updateTaskDto.Priority != null && task.Priority != updateTaskDto.Priority != null)
                {
                    changes.Add($"приоритет изменен на {updateTaskDto.Priority.Value}");
                    task.Priority = updateTaskDto.Priority.Value;
                }

                if (updateTaskDto.Comment == UpdateCommentType.AddComment && updateTaskDto.Description != null)
                {
                    changes.Add($"добавлен комментарий");
                    task.Comment.Add(updateTaskDto.Description);
                }

                if (updateTaskDto.Comment == UpdateCommentType.RemoveLastComment && task.Comment.Any())
                {
                    changes.Add($"удален последний комментарий");
                    task.Comment.RemoveAt(task.Comment.Count - 1);
                }

                _context.Tasks.Update(task);
                await _context.SaveChangesAsync();

                await SaveHistoryAsync(task, ChangeType.Updated, currentUserId);

                if (changes.Any())
                {
                    try
                    {
                        await _notificationConnection.SendTaskNotificationAsync(
                            userId: task.AssignedToUserId,
                            type: NotificationType.TaskUpdated,
                            title: "Задача обновлена",
                            message: $"Изменения: {string.Join(", ", changes)}",
                            taskId: task.Id,
                            author: currentUserId
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Не удалось отправить уведомление об обновлении задачи {TaskId}", task.Id);
                    }
                }

                _logger.LogInformation($"Задача с ID {id} обновлена");
                return task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при обновлении задачи с ID {id}");
                throw;
            }
        }

        public async Task<bool> DeleteTaskByIdAsync(Guid id, bool IsHardDelete, Guid currentUserId)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(id);
                if (task == null)
                    return false;

                await SaveHistoryAsync(task, IsHardDelete? ChangeType.Deleted : ChangeType.SoftDeleted, currentUserId);

                if (IsHardDelete)
                    _context.Tasks.Remove(task);
                else
                    task.IsSoftDeleted = true;

                await _context.SaveChangesAsync();

                try
                {
                    await _notificationConnection.SendTaskNotificationAsync(
                        userId: task.AssignedToUserId,
                        type: NotificationType.TaskDeleted,
                        title: "Задача удалена",
                        message: $"Задача была удалена с флагом hard: {IsHardDelete}",
                        taskId: task.Id,
                        author: currentUserId
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось отправить уведомление об удалении задачи {TaskId}", task.Id);
                }

                _logger.LogInformation($"Задача с ID {id} удалена с флагом hard: {IsHardDelete}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при удалении задачи с ID {id}");
                return false;
            }
        }

        public async Task<bool> AssignTaskByIdAsync(Guid taskId, Guid userId, Guid currentUserId)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(taskId);
                if (task == null || task.IsSoftDeleted)
                    return false;

                var oldAssignee = task.AssignedToUserId;
                task.AssignedToUserId = userId;

                await _context.SaveChangesAsync();
                await SaveHistoryAsync(task, ChangeType.Assigned, currentUserId);

                try
                {
                    await _notificationConnection.SendTaskNotificationAsync(
                        userId: userId,
                        type: NotificationType.TaskAssigned,
                        title: "Задача назначена",
                        message: $"Вам назначена задача",
                        taskId: taskId
                    );

                    if (oldAssignee != Guid.Empty && oldAssignee != userId)
                    {
                        await _notificationConnection.SendTaskNotificationAsync(
                            userId: oldAssignee,
                            type: NotificationType.TaskReassigned,
                            title: "Задача переназначена",
                            message: $"Задача переназначена другому пользователю",
                            taskId: taskId,
                            author: currentUserId
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось отправить уведомление о назначении задачи {TaskId}", taskId);
                }

                _logger.LogInformation($"Задача с ID {taskId} назначена пользователю {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при назначении задачи {taskId} пользователю {userId}");
                return false;
            }
        }

        private async Task SaveHistoryAsync(UserTask task, ChangeType changeType, Guid currentUserId)
        {
            var history = new TaskHistory
            {
                Id = Guid.NewGuid(),
                TaskId = task.Id,
                Type = changeType,
                FullTaskState = JsonSerializer.Serialize(task),
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = currentUserId
            };

            _context.TaskHistories.Add(history);
            await _context.SaveChangesAsync();
        }

        public async Task<List<TaskHistory>> GetTaskHistory(Guid id)
        {
            var history = await _context.TaskHistories
                .Where(h => h.TaskId == id)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();

            return history;
        }
    }
}