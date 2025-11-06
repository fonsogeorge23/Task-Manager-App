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
            var result = await _taskService.GetTasksForUserAsync(userId, UserIdFromToken, status ??= "All");
            return HandleResult<IEnumerable<TaskResponse>>(result);
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTaskById(int id)
        {
            var task = await _taskService.GetActiveTaskByIdAsync(id, UserIdFromToken);
            return HandleResult(task);
        }
        #endregion

        #region UPDATE TASK
        [Authorize]
        [HttpPatch("update/{taskId}")]
        public async Task<IActionResult> UpdateTask(int taskId, [FromBody] TaskRequest request)
        {
            var updatedTask = await _taskService.UpdateTaskAsync(taskId, request, UserIdFromToken);
            return HandleResult(updatedTask);
        }

        [Authorize]
        [HttpPatch("activate-task/{taskId}")]
        public async Task<IActionResult> ActivateTask(int taskId)
        {
            var activateTask = await _taskService.ActivateTaskAsync(taskId, UserIdFromToken);

            return HandleResult(activateTask);
        }
        #endregion

        #region DELETE/INACTIVATE TASK
        [Authorize]
        [HttpPatch("inactivate/{taskId}")]
        public async Task<IActionResult> InactivateTask(int taskId)
        {
            var result = await _taskService.InactivateTask(taskId, UserIdFromToken);
            return HandleResult(result);
        }

        [Authorize]
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var result = await _taskService.DeleteTaskAsync(id, UserIdFromToken);
            return HandleResult(result);
        }
        #endregion
    }
}