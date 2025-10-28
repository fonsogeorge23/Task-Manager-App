using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagementAPI.DTOs.Requests;
using TaskManagementAPI.Services;

namespace TaskManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TasksController : BaseController
    {
        private readonly ITaskService _taskService;
        public TasksController(ITaskService service)
        {
            _taskService = service;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateTask([FromBody] TaskRequest request)
        {
            request.UserId = UserIdFromToken;
            var createdTask = await _taskService.CreateTaskAsync(request);
            return CreatedAtAction(nameof(GetTaskById), new {id = createdTask.Data.Id}, createdTask);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTaskById(int id)
        {
            int userId = UserIdFromToken;
            var task = await _taskService.GetTaskByIdAsync(id, userId);
            if (task == null)
                return NotFound("Task not found or access denied.");
            return Ok(task);
        }

        [HttpGet("user-tasks")]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> GetUserTasks()
        {
            int userId = UserIdFromToken;
            var tasks = await _taskService.GetUserTasksAsync(userId);
            if (tasks == null)
            {
                return NotFound("No tasks found for the user.");
            }
            return Ok(tasks);
        }
    }
}
