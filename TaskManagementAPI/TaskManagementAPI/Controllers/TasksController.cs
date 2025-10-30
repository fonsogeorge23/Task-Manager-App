using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagementAPI.DTOs.Requests;
using TaskManagementAPI.Services;

namespace TaskManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class TasksController : BaseController
    {
        private readonly ITaskService _taskService;
        public TasksController(ITaskService service)
        {
            _taskService = service;
        }

        #region CREATE TASK FOR USER
        [HttpPost("create")]
        public async Task<IActionResult> CreateTask([FromBody] TaskRequest request)
        {
            request.UserId = UserIdFromToken;
            var createdTask = await _taskService.CreateTaskAsync(request);
            return CreatedAtAction(nameof(CreateTask), new {id = createdTask.Data.Id}, createdTask);
        }

        #endregion

        #region RETRIEVE TASKS

        [HttpGet("user-tasks/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetUserTasks(int userId)
        {
            // Ensure that users can only access their own tasks unless they are an Admin
            if (UserIdFromToken != userId && RoleFromToken != "Admin")
            {
                return Unauthorized("You are not authorized to access these tasks.");
            }

            // Retrieve tasks for the specified user
            var tasks = await _taskService.GetUserTasksAsync(userId);
            if (tasks == null || !tasks.Any())
            {
                return NotFound("No tasks found for the user.");
            }
            return Ok(tasks);
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTaskById(int id)
        {
            var task = await _taskService.GetTaskByIdAsync(id, UserIdFromToken, RoleFromToken);

            if (task == null)
                return NotFound("Task not found or access denied.");

            return Ok(task);
        }
        #endregion

        #region UPDATE TASK
        [Authorize]
        [HttpPut("update/{taskId}")]
        public async Task<IActionResult> UpdateTask(int taskId, [FromBody] TaskRequest request)
        {
            var updatedTask = await _taskService.UpdateTaskAsync(taskId, request, UserIdFromToken, RoleFromToken);
            if (!updatedTask.IsSuccess)
                return NotFound(updatedTask.ErrorMessage);
            return Ok(updatedTask);
        }

        [Authorize]
        [HttpPatch("activate-task/{id}")]
        public async Task<IActionResult> ActivateTask(int id)
        { var result = await _taskService.ActivateTaskAsync(id, UserIdFromToken, RoleFromToken);
            if (!result.IsSuccess)
                return Unauthorized(result.ErrorMessage);
            return Ok(result);
        }
        #endregion

        #region DELETE/INACTIVATE TASK
        [Authorize]
        [HttpPatch("inactivate/{id}")]
        public async Task<IActionResult> InactivateTask(int id)
        {
            var result = await _taskService.InactivateTask(id, UserIdFromToken, RoleFromToken);
            if (!result.IsSuccess)
                return Unauthorized(result.ErrorMessage);
            return Ok(result);
        }

        [Authorize]
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var result = await _taskService.DeleteTaskAsync(id, UserIdFromToken, RoleFromToken);
            if (!result.IsSuccess)
                return Unauthorized(result.ErrorMessage);
            return Ok(result);
        }
        #endregion
    }
}
