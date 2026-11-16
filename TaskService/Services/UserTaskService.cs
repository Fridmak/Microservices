using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.Exceptions;
using Shared.Models;
using Shared.Serialization;
using TaskService.Models;
using TaskService.Models.Db;

namespace TaskService.Services
{
    public interface IUserTaskService
    {
        Task<PagedResult<UserTaskResponse>> GetAllTasksAsync(
            GetTasksQueryParams rules,
            Guid currentUserId,
            CancellationToken cancellationToken = default);

        Task<UserTaskResponse> GetTaskByIdAsync(
            Guid id,
            Guid currentUserId,
            CancellationToken cancellationToken = default);

        Task<UserTaskResponse> CreateTaskAsync(
            CreateTaskDTO createTaskDto,
            Guid currentUserId,
            CancellationToken cancellationToken = default);

        Task<UserTaskResponse> UpdateTaskAsync(
            Guid id,
            UpdateTaskDTO updateTaskDto,
            Guid currentUserId,
            CancellationToken cancellationToken = default);

        Task<UserTaskResponse> UpdateStatusAsync(
            Guid id,
            UserTaskStatus status,
            Guid currentUserId,
            CancellationToken cancellationToken = default);

        Task DeleteTaskAsync(
            Guid id,
            bool isHardDelete,
            Guid currentUserId,
            CancellationToken cancellationToken = default);

        Task<UserTaskResponse> AssignTaskAsync(
            Guid taskId,
            Guid userId,
            Guid currentUserId,
            CancellationToken cancellationToken = default);

        Task<PagedResult<TaskHistoryResponse>> GetTaskHistoryAsync(
            Guid id,
            int page,
            int pageSize,
            Guid currentUserId,
            CancellationToken cancellationToken = default);

        Task<UserTaskResponse> RestoreTaskAsync(
            Guid id,
            Guid currentUserId,
            CancellationToken cancellationToken = default);
    }

    public class UserTaskService : IUserTaskService
    {
        private readonly TaskDbContext _context;
        private readonly ILogger<UserTaskService> _logger;
        private readonly ITaskSorter _taskSorter;
        private readonly IUserDirectoryClient _userDirectoryClient;

        public UserTaskService(
            TaskDbContext context,
            ILogger<UserTaskService> logger,
            ITaskSorter taskSorter,
            IUserDirectoryClient userDirectoryClient)
        {
            _context = context;
            _logger = logger;
            _taskSorter = taskSorter;
            _userDirectoryClient = userDirectoryClient;
        }

        public async Task<PagedResult<UserTaskResponse>> GetAllTasksAsync(
            GetTasksQueryParams rules,
            Guid currentUserId,
            CancellationToken cancellationToken = default)
        {
            var page = Math.Max(rules.Page, 1);
            var pageSize = Math.Clamp(rules.PageSize, 1, 100);

            var query = _context.Tasks.AsNoTracking();
            query = rules.Scope == TaskScope.Owned
                ? query.Where(task => task.OwnerId == currentUserId)
                : query.Where(task => task.AssignedToUserId == currentUserId);

            query = _taskSorter.Apply(rules, query);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<UserTaskResponse>
            {
                Items = items.Select(UserTaskResponse.FromEntity).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<UserTaskResponse> GetTaskByIdAsync(
            Guid id,
            Guid currentUserId,
            CancellationToken cancellationToken = default)
        {
            var task = await GetActiveTaskAsync(id, currentUserId, cancellationToken);
            return UserTaskResponse.FromEntity(task);
        }

        public async Task<UserTaskResponse> CreateTaskAsync(
            CreateTaskDTO createTaskDto,
            Guid currentUserId,
            CancellationToken cancellationToken = default)
        {
            var title = NormalizeTitle(createTaskDto.Title);
            var description = NormalizeDescription(createTaskDto.Description);

            if (createTaskDto.DeadLine == default)
            {
                throw new BadRequestException("Необходимо указать срок выполнения задачи.");
            }

            var assigneeId = createTaskDto.AssigneeId.GetValueOrDefault();
            if (assigneeId == Guid.Empty)
            {
                assigneeId = currentUserId;
            }

            if (assigneeId != currentUserId)
            {
                await EnsureUserExistsAsync(assigneeId, cancellationToken);
            }

            var task = new UserTask
            {
                Id = Guid.NewGuid(),
                OwnerId = currentUserId,
                AssignedToUserId = assigneeId,
                CreationTime = DateTime.UtcNow,
                DeadLine = createTaskDto.DeadLine,
                Priority = createTaskDto.Priority,
                Status = UserTaskStatus.Todo,
                Title = title,
                Comment = description is null ? new List<string>() : new List<string> { description }
            };

            _context.Tasks.Add(task);
            AddHistory(task, ChangeType.Created, currentUserId, "Задача создана.");
            EnqueueNotification(
                assigneeId,
                NotificationType.TaskCreated,
                "Новая задача",
                $"Создана задача: {task.Title}",
                task.Id,
                currentUserId);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Task {TaskId} created by {UserId}", task.Id, currentUserId);
            return UserTaskResponse.FromEntity(task);
        }

        public async Task<UserTaskResponse> UpdateTaskAsync(
            Guid id,
            UpdateTaskDTO updateTaskDto,
            Guid currentUserId,
            CancellationToken cancellationToken = default)
        {
            var task = await GetActiveTaskAsync(id, currentUserId, cancellationToken);
            var isOwner = TaskAccess.CanEdit(task, currentUserId);
            var changes = new List<string>();
            var historyType = ChangeType.Updated;

            if (!string.IsNullOrWhiteSpace(updateTaskDto.Title))
            {
                if (!isOwner)
                {
                    throw new ForbiddenException("Менять название задачи может только владелец.");
                }

                var title = NormalizeTitle(updateTaskDto.Title);
                if (!string.Equals(task.Title, title, StringComparison.Ordinal))
                {
                    task.Title = title;
                    changes.Add("название");
                }
            }

            if (updateTaskDto.DeadLine.HasValue && task.DeadLine != updateTaskDto.DeadLine.Value)
            {
                if (!isOwner)
                {
                    throw new ForbiddenException("Менять срок выполнения может только владелец.");
                }

                if (updateTaskDto.DeadLine.Value == default)
                {
                    throw new BadRequestException("Некорректный срок выполнения.");
                }

                task.DeadLine = updateTaskDto.DeadLine.Value;
                changes.Add($"срок выполнения изменён на {task.DeadLine:dd.MM.yyyy}");
            }

            if (updateTaskDto.Priority.HasValue && task.Priority != updateTaskDto.Priority.Value)
            {
                if (!isOwner)
                {
                    throw new ForbiddenException("Менять приоритет может только владелец.");
                }

                task.Priority = updateTaskDto.Priority.Value;
                changes.Add($"приоритет изменён на {task.Priority}");
            }

            if (updateTaskDto.Comment.HasValue)
            {
                if (!TaskAccess.CanComment(task, currentUserId))
                {
                    throw new ForbiddenException("Недостаточно прав для изменения комментариев.");
                }

                switch (updateTaskDto.Comment.Value)
                {
                    case UpdateCommentType.AddComment:
                        var comment = NormalizeDescription(updateTaskDto.Description);
                        if (comment is null)
                        {
                            throw new BadRequestException("Комментарий не может быть пустым.");
                        }

                        task.Comment.Add(comment);
                        _context.Entry(task).Property(t => t.Comment).IsModified = true;
                        changes.Add("добавлен комментарий");
                        historyType = ChangeType.CommentAdded;
                        break;

                    case UpdateCommentType.RemoveLastComment:
                        if (task.Comment.Count == 0)
                        {
                            throw new BadRequestException("У задачи нет комментариев.");
                        }

                        task.Comment.RemoveAt(task.Comment.Count - 1);
                        _context.Entry(task).Property(t => t.Comment).IsModified = true;
                        changes.Add("удалён последний комментарий");
                        historyType = ChangeType.CommentRemoved;
                        break;
                }
            }

            if (changes.Count == 0)
            {
                return UserTaskResponse.FromEntity(task);
            }

            AddHistory(task, historyType, currentUserId, string.Join(", ", changes));
            EnqueueNotification(
                task.AssignedToUserId,
                NotificationType.TaskUpdated,
                "Задача обновлена",
                $"Изменения: {string.Join(", ", changes)}",
                task.Id,
                currentUserId);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Task {TaskId} updated by {UserId}", task.Id, currentUserId);
            return UserTaskResponse.FromEntity(task);
        }

        public async Task<UserTaskResponse> UpdateStatusAsync(
            Guid id,
            UserTaskStatus status,
            Guid currentUserId,
            CancellationToken cancellationToken = default)
        {
            var task = await GetActiveTaskAsync(id, currentUserId, cancellationToken);

            if (task.Status == status)
            {
                return UserTaskResponse.FromEntity(task);
            }

            var oldStatus = task.Status;
            task.Status = status;
            task.CompletedAt = status == UserTaskStatus.Done ? DateTime.UtcNow : null;

            AddHistory(
                task,
                ChangeType.StatusChanged,
                currentUserId,
                $"статус изменён с {oldStatus} на {status}");

            EnqueueNotification(
                task.AssignedToUserId,
                NotificationType.TaskStatusChanged,
                "Статус задачи изменён",
                $"Задача \"{task.Title}\": {oldStatus} → {status}",
                task.Id,
                currentUserId);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Task {TaskId} status changed from {OldStatus} to {NewStatus} by {UserId}",
                task.Id,
                oldStatus,
                status,
                currentUserId);

            return UserTaskResponse.FromEntity(task);
        }

        public async Task DeleteTaskAsync(
            Guid id,
            bool isHardDelete,
            Guid currentUserId,
            CancellationToken cancellationToken = default)
        {
            var task = await GetTaskForAccessAsync(id, currentUserId, cancellationToken);

            if (!TaskAccess.CanDelete(task, currentUserId))
            {
                throw new ForbiddenException("Удалять задачу может только владелец.");
            }

            if (task.IsSoftDeleted && !isHardDelete)
            {
                throw new ConflictException("Задача уже удалена. Используйте hard=true для окончательного удаления.");
            }

            if (isHardDelete)
            {
                AddHistory(task, ChangeType.Deleted, currentUserId, "Задача удалена безвозвратно.");
                _context.Tasks.Remove(task);
            }
            else
            {
                task.IsSoftDeleted = true;
                AddHistory(task, ChangeType.SoftDeleted, currentUserId, "Задача перемещена в удалённые.");
            }

            EnqueueNotification(
                task.AssignedToUserId,
                NotificationType.TaskDeleted,
                "Задача удалена",
                $"Задача \"{task.Title}\" удалена.",
                task.Id,
                currentUserId);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Task {TaskId} deleted by {UserId} (hard: {IsHardDelete})",
                task.Id,
                currentUserId,
                isHardDelete);
        }

        public async Task<UserTaskResponse> RestoreTaskAsync(
            Guid id,
            Guid currentUserId,
            CancellationToken cancellationToken = default)
        {
            var task = await GetTaskForAccessAsync(id, currentUserId, cancellationToken);

            if (!TaskAccess.CanDelete(task, currentUserId))
            {
                throw new ForbiddenException("Восстанавливать задачу может только владелец.");
            }

            if (!task.IsSoftDeleted)
            {
                throw new ConflictException("Задача не находится в удалённых.");
            }

            task.IsSoftDeleted = false;

            AddHistory(task, ChangeType.Restored, currentUserId, "Задача восстановлена.");
            EnqueueNotification(
                task.AssignedToUserId,
                NotificationType.TaskRestored,
                "Задача восстановлена",
                $"Задача \"{task.Title}\" восстановлена.",
                task.Id,
                currentUserId);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Task {TaskId} restored by {UserId}", task.Id, currentUserId);
            return UserTaskResponse.FromEntity(task);
        }

        public async Task<UserTaskResponse> AssignTaskAsync(
            Guid taskId,
            Guid userId,
            Guid currentUserId,
            CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty)
            {
                throw new BadRequestException("Необходимо указать исполнителя.");
            }

            var task = await GetActiveTaskAsync(taskId, currentUserId, cancellationToken);

            if (!TaskAccess.CanAssign(task, currentUserId))
            {
                throw new ForbiddenException("Назначать задачу может только владелец.");
            }

            if (task.AssignedToUserId == userId)
            {
                return UserTaskResponse.FromEntity(task);
            }

            await EnsureUserExistsAsync(userId, cancellationToken);

            var oldAssignee = task.AssignedToUserId;
            task.AssignedToUserId = userId;

            AddHistory(task, ChangeType.Assigned, currentUserId, $"исполнитель изменён на {userId}");

            EnqueueNotification(
                userId,
                NotificationType.TaskAssigned,
                "Задача назначена",
                $"Вам назначена задача: {task.Title}",
                task.Id,
                currentUserId);

            if (oldAssignee != Guid.Empty && oldAssignee != userId)
            {
                EnqueueNotification(
                    oldAssignee,
                    NotificationType.TaskReassigned,
                    "Задача переназначена",
                    $"Задача \"{task.Title}\" переназначена другому пользователю.",
                    task.Id,
                    currentUserId);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Task {TaskId} assigned to {AssigneeId} by {UserId}",
                task.Id,
                userId,
                currentUserId);

            return UserTaskResponse.FromEntity(task);
        }

        public async Task<PagedResult<TaskHistoryResponse>> GetTaskHistoryAsync(
            Guid id,
            int page,
            int pageSize,
            Guid currentUserId,
            CancellationToken cancellationToken = default)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var task = await _context.Tasks
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            if (task is null || !TaskAccess.CanRead(task, currentUserId))
            {
                throw new NotFoundException("Задача не найдена.");
            }

            var query = _context.TaskHistories
                .AsNoTracking()
                .Where(history => history.TaskId == id);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(history => history.ChangedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<TaskHistoryResponse>
            {
                Items = items.Select(TaskHistoryResponse.FromEntity).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        private async Task<UserTask> GetActiveTaskAsync(
            Guid id,
            Guid currentUserId,
            CancellationToken cancellationToken)
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            if (task is null || task.IsSoftDeleted || !TaskAccess.CanRead(task, currentUserId))
            {
                throw new NotFoundException("Задача не найдена.");
            }

            return task;
        }

        private async Task<UserTask> GetTaskForAccessAsync(
            Guid id,
            Guid currentUserId,
            CancellationToken cancellationToken)
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            if (task is null || !TaskAccess.CanRead(task, currentUserId))
            {
                throw new NotFoundException("Задача не найдена.");
            }

            return task;
        }

        private async Task EnsureUserExistsAsync(Guid userId, CancellationToken cancellationToken)
        {
            var exists = await _userDirectoryClient.UserExistsAsync(userId, cancellationToken);
            if (!exists)
            {
                throw new BadRequestException("Исполнитель не найден.");
            }
        }

        private void AddHistory(UserTask task, ChangeType changeType, Guid currentUserId, string details)
        {
            _context.TaskHistories.Add(new TaskHistory
            {
                Id = Guid.NewGuid(),
                TaskId = task.Id,
                Type = changeType,
                Details = details,
                FullTaskState = JsonSerializer.Serialize(TaskSnapshot.From(task), JsonDefaults.Api),
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = currentUserId
            });
        }

        private void EnqueueNotification(
            Guid userId,
            NotificationType type,
            string title,
            string message,
            Guid taskId,
            Guid author)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                Author = author,
                TaskId = taskId,
                CreatedAt = DateTime.UtcNow
            };

            _context.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = "notification",
                Payload = JsonSerializer.Serialize(notification, JsonDefaults.Api),
                CreatedAt = DateTime.UtcNow,
                NextAttemptAt = DateTime.UtcNow
            });
        }

        private static string NormalizeTitle(string? title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new BadRequestException("Название задачи обязательно.");
            }

            var normalized = title.Trim();
            if (normalized.Length > 200)
            {
                throw new BadRequestException("Название задачи не должно превышать 200 символов.");
            }

            return normalized;
        }

        private static string? NormalizeDescription(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return null;
            }

            var normalized = description.Trim();
            if (normalized.Length > 2000)
            {
                throw new BadRequestException("Описание не должно превышать 2000 символов.");
            }

            return normalized;
        }
    }
}
