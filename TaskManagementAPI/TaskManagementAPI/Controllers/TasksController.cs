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
            var createdTask = await _taskService.CreateTaskService(request, UserIdFromToken);
            return HandleResult(createdTask);
        }
        #endregion

        #region RETRIEVE TASKS
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTaskById(int id)
        {
            var task = await _taskService.GetTaskByIdService(id, UserIdFromToken);
            return HandleResult(task);
        }

        [Authorize]
        [HttpGet("user-tasks/{userId}")]
        public async Task<IActionResult> GetAllTasksByStatus(int userId, [FromQuery] string? status)
        {
            var result = await _taskService.GetTaskForUserService(userId, UserIdFromToken, status ??= "All");
            return HandleResult<IEnumerable<TaskResponse>>(result);
        }

        [Authorize]
        [HttpGet("user-tasks/{userId}/search")]
        public async Task<IActionResult> SearchTasks(int userId, [FromQuery] string query)
        {
            var result = await _taskService.SearchTasksService(userId, UserIdFromToken, query);
            return HandleResult<IEnumerable<TaskResponse>>(result);
        }

        [Authorize]
        [HttpGet("user-tasks/{userId}/summary")]
        public async Task<IActionResult> GetTaskSummary(int userId)
        {
            var result = await _taskService.GetTaskSummaryService(userId, UserIdFromToken);
            return HandleResult(result);
        }
        #endregion

        #region UPDATE TASK
        [Authorize]
        [HttpPatch("update/{taskId}")]
        public async Task<IActionResult> UpdateTask(int taskId, [FromBody] TaskRequest request)
        {
            var updatedTask = await _taskService.UpdateTaskService(taskId, request, UserIdFromToken);
            return HandleResult(updatedTask);
        }

        [Authorize]
        [HttpPatch("update-status/{taskId}")]
        public async Task<IActionResult> UpdateTaskStatus(int taskId, [FromQuery] string status)
        {
            var result = await _taskService.UpdateTaskStatusService(taskId, status, UserIdFromToken);
            return HandleResult(result);
        }

        [Authorize]
        [HttpPatch("update-priority/{taskId}")]
        public async Task<IActionResult> UpdateTaskPriority(int taskId, [FromQuery] string priority)
        {
            var result = await _taskService.UpdateTaskPriorityService(taskId, priority, UserIdFromToken);
            return HandleResult(result);
        }

        [Authorize]
        [HttpPatch("activate-task/{taskId}")]
        public async Task<IActionResult> ActivateTask(int taskId)
        {
            var activateTask = await _taskService.ActivateTaskService(taskId, UserIdFromToken);

            return HandleResult(activateTask);
        }
        #endregion

        #region DELETE/INACTIVATE TASK
        [Authorize]
        [HttpPatch("inactivate/{taskId}")]
        public async Task<IActionResult> InactivateTask(int taskId)
        {
            var result = await _taskService.InactivateTaskService(taskId, UserIdFromToken);
            return HandleResult(result);
        }

        [Authorize]
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var result = await _taskService.DeleteTaskService(id, UserIdFromToken);
            return HandleResult(result);
        }
        #endregion
    }
}