using DevTaskHubBackend.Data;
using DevTaskHubBackend.Helpers;
using DevTaskHubBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace DevTaskHubBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LogController : ControllerBase
{
    private readonly IActivityLogService _activityLogService;

    public LogController(IActivityLogService activityLogService)
    {
        _activityLogService = activityLogService;
    }

    [RoleAuthorize(UserRole.Manager, UserRole.Developer)]
    [HttpGet("Get-Logs")]
    public async Task<IActionResult> GetLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] Guid? projectId = null, [FromQuery] Guid? taskId = null, CancellationToken cancellationToken = default)
    {
        var result = await _activityLogService.GetLogsAsync(User.GetUserId(), page, pageSize, projectId, taskId, cancellationToken);
        return Ok(new
        {
            Success = true,
            Message = "Activity logs retrieved successfully.",
            Data = result
        });
    }
}
