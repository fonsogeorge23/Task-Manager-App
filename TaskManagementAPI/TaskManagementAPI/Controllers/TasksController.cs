using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using TaskManagementAPI.DTOs.Requests;
using TaskManagementAPI.DTOs.Responses;
using TaskManagementAPI.Services;
using TaskManagementAPI.Utilities;

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
            var createdTask = await _taskService.CreateTaskAsync(request, UserIdFromToken);
            return HandleResult(createdTask);
        }
        #endregion

        #region RETRIEVE TASKS
        [HttpGet("user-tasks/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetAllTasksByStatus(int userId, [FromQuery] string? status)
        {
            var result = await _taskService.GetTasksForUserAsync(userId, UserIdFromToken, RoleFromToken, status ??= "All");
            return HandleResult<IEnumerable<TaskResponse>>(result);
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
            return HandleResult(updatedTask);
        }

        [Authorize]
        [HttpPatch("activate-task/{id}")]
        public async Task<IActionResult> ActivateTask(int id)
        { 
            var activateTask = await _taskService.ActivateTaskAsync(id, UserIdFromToken, RoleFromToken);
            return HandleResult(activateTask);
        }
        #endregion

        #region DELETE/INACTIVATE TASK
        [Authorize]
        [HttpPatch("inactivate/{id}")]
        public async Task<IActionResult> InactivateTask(int id)
        {
            var result = await _taskService.InactivateTask(id, UserIdFromToken, RoleFromToken);
            if (!result.IsSuccess)
                return Unauthorized(result.Message);
            return Ok(result);
        }

        [Authorize]
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var result = await _taskService.DeleteTaskAsync(id, UserIdFromToken, RoleFromToken);
            if (!result.IsSuccess)
                return Unauthorized(result.Message);
            return Ok(result);
        }
        #endregion
    }
}
