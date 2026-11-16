using DevTaskHubBackend.Data;
using DevTaskHubBackend.Helpers;
using DevTaskHubBackend.Models;
using DevTaskHubBackend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevTaskHubBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly ITaskServices _taskServices;

        public TaskController(ITaskServices taskServices)
        {
            _taskServices = taskServices;
        }

        [RoleAuthorize(Data.UserRole.Admin, UserRole.Manager)]
        [HttpPost("Create-Task")]
        public async Task<IActionResult> CreateTask(CreateTaskDto createTaskDto, CancellationToken cancellationToken)
        {
            Guid currentUserID = User.GetUserId();

            var result = await _taskServices.CreateTaskAsync(createTaskDto, currentUserID, cancellationToken);

            return Ok(new
            {
                success = result,
                message = "Create task successfully."
            });
        }
        [RoleAuthorize(UserRole.Manager, UserRole.Developer)]
        [HttpPost("Get-All-Tasks")]
        public async Task<IActionResult> GetAllTask([FromQuery] string? search, [FromQuery] string? status, [FromQuery] string? priority, [FromQuery] Guid? projectId, [FromQuery] string? sortBy, [FromQuery] string? sortOrder, CancellationToken cancellationToken)
        {
            Guid currentUserID = User.GetUserId();
            var result = await _taskServices.GetTaskAsync(currentUserID, search, status, priority, projectId, sortBy, sortOrder, cancellationToken);
            return Ok(new
            {
                success = true,
                message = result.Count > 0 ? "Tasks retrieved successfully." : "Task not found.",
                data = result
            });
        }

        [RoleAuthorize(UserRole.Admin, UserRole.Manager)]
        [HttpPut("Edit-Task/{taskID:guid}")]
        public async Task<IActionResult> EditTask([FromRoute] Guid taskID, [FromBody] EditTaskDto request, CancellationToken cancellationToken)
        {
            Guid currentUserID = User.GetUserId();

            await _taskServices.EditTaskAsync(taskID, request, currentUserID, cancellationToken);

            return Ok(new
            {
                Success = true,
                Message = "Task updated successfully."
            });
        }
        [RoleAuthorize(UserRole.Manager)]
        [HttpPost("Assign-Task")]
        public async Task<IActionResult> AssignTask([FromBody] AssignTaskDto request, CancellationToken cancellationToken)
        {
            Guid currentUserID = User.GetUserId();

            await _taskServices.AssignTaskAsync(request, currentUserID);

            return Ok(new
            {
                Success = true,
                Message = "Task assigned successfully."
            });
        }

        [RoleAuthorize(UserRole.Manager, UserRole.Developer)]
        [HttpPut("Update-Task-Status/{taskID:guid}")]
        public async Task<IActionResult> UpdateTaskStatus([FromRoute] Guid taskID, [FromBody] UpdateTaskStatusDto request, CancellationToken cancellationToken)
        {
            Guid currentUserID = User.GetUserId();
            await _taskServices.UpdateTaskStatusAsync(taskID, request, currentUserID, cancellationToken);
            return Ok(new
            {
                Success = true,
                Message = "Task status updated successfully."
            });
        }

        [RoleAuthorize(UserRole.Manager, UserRole.Developer)]
        [HttpGet("Get-Task-Details/{taskID:guid}")]
        public async Task<IActionResult> GetTaskDetails(Guid taskID, CancellationToken cancellationToken)
        {
            var result = await _taskServices.GetTaskDetailsAsync(User.GetUserId(), taskID, cancellationToken);
            return Ok(new
            {
                Success = true,
                Message = "Task details retrieved successfully.",
                Data = result
            });
        }

        [RoleAuthorize(UserRole.Manager, UserRole.Developer)]
        [HttpPost("Add-Comment/{taskID:guid}")]
        public async Task<IActionResult> AddComment(Guid taskID, [FromBody] UpdateTaskCommentDto request, CancellationToken cancellationToken)
        {
            await _taskServices.AddTaskCommentAsync(User.GetUserId(), taskID, request, cancellationToken);
            return Ok(new
            {
                Success = true,
                Message = "Comment added successfully."
            });
        }

        [RoleAuthorize(UserRole.Manager, UserRole.Developer)]
        [HttpPut("Edit-Comment/{commentId:guid}")]
        public async Task<IActionResult> EditComment(Guid commentId, [FromBody] UpdateTaskCommentDto request, CancellationToken cancellationToken)
        {
            await _taskServices.EditTaskCommentAsync(User.GetUserId(), commentId, request, cancellationToken);
            return Ok(new
            {
                Success = true,
                Message = "Comment updated successfully."
            });
        }

        [RoleAuthorize(UserRole.Manager, UserRole.Developer)]
        [HttpDelete("Delete-Comment/{commentId:guid}")]
        public async Task<IActionResult> DeleteComment(Guid commentId, CancellationToken cancellationToken)
        {
            await _taskServices.DeleteTaskCommentAsync(User.GetUserId(), commentId, cancellationToken);
            return Ok(new
            {
                Success = true,
                Message = "Comment deleted successfully."
            });
        }

        [RoleAuthorize(UserRole.Manager, UserRole.Developer)]
        [HttpPut("Update-Task-Comment/{taskID:guid}")]
        public async Task<IActionResult> UpdateTaskComment([FromRoute] Guid taskID, [FromBody] UpdateTaskCommentDto request, CancellationToken cancellationToken)
        {
            Guid currentUserID = User.GetUserId();

            await _taskServices.AddTaskCommentAsync(currentUserID, taskID, request, cancellationToken);

            return Ok(new
            {
                Success = true,
                Message = "Comment added successfully."
            });
        }
    }
}
