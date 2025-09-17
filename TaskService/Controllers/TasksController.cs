using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskService.Models;
using TaskService.Services;

namespace TaskService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private IUserTaskService _taskService;

        public TasksController(IUserTaskService taskService)
        {
            _taskService = taskService;
        }

        // GET /api/tasks
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll(GetTasksQueryParams rules)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var tasks = await _taskService.GetAllTasksAsync(userId, rules);

            return Ok(tasks);
        }

        // GET /api/tasks/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetTaskById(Guid id)
        {
            var task = await _taskService.GetTaskByIdAsync(id);

            if (task == null)
                return NotFound();

            return Ok(task);
        }

        // POST /api/tasks
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskDTO task)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var createdTask = await _taskService.CreateTaskAsync(task, userId);

            if (createdTask != null)
                return Ok();

            return BadRequest();
        }

        // PUT /api/tasks/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateTaskById(Guid id, [FromBody] UpdateTaskDTO task)
        {
            var updatedTask = await _taskService.UpdateTaskByIdAsync(id, task);

            if (updatedTask != null)
                return Ok();

            return BadRequest();
        }

        // DELETE /api/tasks/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteTaskById(Guid id, [FromBody] DeleteTaskDto dto)
        {
            if (id == Guid.Empty)
                return BadRequest("Invalid ID");

            var deletedTask = await _taskService.DeleteTaskByIdAsync(id, dto.IsHardDelete);

            if (!deletedTask)
                return NotFound();

            return NoContent();
        }

        // PUT /api/tasks/{id}/assign
        [HttpPut("{id}/assign")]
        [Authorize]
        public async Task<IActionResult> AssignTaskById([FromRoute] string id, [FromBody] AssignTaskDTO userId)
        {
            var assignedTask = await _taskService.AssignTaskByIdAsync(Guid.Parse(id), Guid.Parse(userId.UserId));

            if (assignedTask)
                return Ok();

            return BadRequest();
        }

        [HttpGet("{id}/history")]
        [Authorize]
        public async Task<IActionResult> GetTaskHistory(Guid id)
        {
            var history = await _taskService.GetTaskHistory(id);

            return Ok(history);
        }
    }
}