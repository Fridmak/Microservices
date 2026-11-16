using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts;
using Shared.Extensions;
using TaskService.Models;
using TaskService.Services;

namespace TaskService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly IUserTaskService _taskService;

        public TasksController(IUserTaskService taskService)
        {
            _taskService = taskService;
        }

        // GET /api/tasks?page=1&pageSize=20&scope=Assigned&sortBy=ByPriority
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<PagedResult<UserTaskResponse>>> GetAll(
            [FromQuery] GetTasksQueryParams rules,
            CancellationToken cancellationToken)
        {
            var tasks = await _taskService.GetAllTasksAsync(
                rules,
                User.GetRequiredUserId(),
                cancellationToken);

            return Ok(tasks);
        }

        // GET /api/tasks/{id}
        [HttpGet("{id:guid}")]
        [Authorize]
        public async Task<ActionResult<UserTaskResponse>> GetTaskById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var task = await _taskService.GetTaskByIdAsync(
                id,
                User.GetRequiredUserId(),
                cancellationToken);

            return Ok(task);
        }

        // POST /api/tasks
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<UserTaskResponse>> CreateTask(
            [FromBody] CreateTaskDTO task,
            CancellationToken cancellationToken)
        {
            var createdTask = await _taskService.CreateTaskAsync(
                task,
                User.GetRequiredUserId(),
                cancellationToken);

            return CreatedAtAction(nameof(GetTaskById), new { id = createdTask.Id }, createdTask);
        }

        // PUT /api/tasks/{id}
        [HttpPut("{id:guid}")]
        [Authorize]
        public async Task<ActionResult<UserTaskResponse>> UpdateTaskById(
            Guid id,
            [FromBody] UpdateTaskDTO task,
            CancellationToken cancellationToken)
        {
            var updatedTask = await _taskService.UpdateTaskAsync(
                id,
                task,
                User.GetRequiredUserId(),
                cancellationToken);

            return Ok(updatedTask);
        }

        // PATCH /api/tasks/{id}/status
        [HttpPatch("{id:guid}/status")]
        [Authorize]
        public async Task<ActionResult<UserTaskResponse>> UpdateStatus(
            Guid id,
            [FromBody] UpdateTaskStatusDTO dto,
            CancellationToken cancellationToken)
        {
            var updatedTask = await _taskService.UpdateStatusAsync(
                id,
                dto.Status,
                User.GetRequiredUserId(),
                cancellationToken);

            return Ok(updatedTask);
        }

        // DELETE /api/tasks/{id}?hard=false
        [HttpDelete("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> DeleteTaskById(
            Guid id,
            [FromQuery] bool hard = false,
            CancellationToken cancellationToken = default)
        {
            await _taskService.DeleteTaskAsync(
                id,
                hard,
                User.GetRequiredUserId(),
                cancellationToken);

            return NoContent();
        }

        // POST /api/tasks/{id}/restore
        [HttpPost("{id:guid}/restore")]
        [Authorize]
        public async Task<ActionResult<UserTaskResponse>> RestoreTask(
            Guid id,
            CancellationToken cancellationToken)
        {
            var restoredTask = await _taskService.RestoreTaskAsync(
                id,
                User.GetRequiredUserId(),
                cancellationToken);

            return Ok(restoredTask);
        }

        // PUT /api/tasks/{id}/assign
        [HttpPut("{id:guid}/assign")]
        [Authorize]
        public async Task<ActionResult<UserTaskResponse>> AssignTaskById(
            Guid id,
            [FromBody] AssignTaskDTO dto,
            CancellationToken cancellationToken)
        {
            var assignedTask = await _taskService.AssignTaskAsync(
                id,
                dto.UserId,
                User.GetRequiredUserId(),
                cancellationToken);

            return Ok(assignedTask);
        }

        // GET /api/tasks/{id}/history?page=1&pageSize=20
        [HttpGet("{id:guid}/history")]
        [Authorize]
        public async Task<ActionResult<PagedResult<TaskHistoryResponse>>> GetTaskHistory(
            Guid id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var history = await _taskService.GetTaskHistoryAsync(
                id,
                page,
                pageSize,
                User.GetRequiredUserId(),
                cancellationToken);

            return Ok(history);
        }
    }
}
