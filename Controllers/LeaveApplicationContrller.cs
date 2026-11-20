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
    public class LeaveApplicationContrller : ControllerBase
    {

        private readonly ILeaveApplicationServices _leaveApplicationServices;


        public LeaveApplicationContrller(ILeaveApplicationServices leaveApplicationServices)
        {
            _leaveApplicationServices = leaveApplicationServices;
        }
        [RoleAuthorize(UserRole.Manager, UserRole.Hr, UserRole.Developer)]
        [HttpGet("Get-Available-Leave-Types")]
        public async Task<IActionResult> GetAvailableLeaveTypes(CancellationToken cancellationToken)
        {
            var result = await _leaveApplicationServices.GetAvailableLeaveTypesAsync(User.GetUserId(), cancellationToken);
            return Ok(new { statusCode = StatusCodes.Status200OK, data = result });
        }

        [RoleAuthorize(UserRole.Manager, UserRole.Hr, UserRole.Developer)]
        [HttpPost("Apply-Leave")]
        public async Task<IActionResult> ApplyLeave([FromBody] ApplyLeaveDto dto)
        {
            Guid currentUserId = User.GetUserId();
            bool result = await _leaveApplicationServices.ApplyLeaveAsync(currentUserId, dto);
            return Ok(new
            {
                statusCode = StatusCodes.Status200OK,
                message = "Leave applied successfully.",
                data = result
            });
        }
        [RoleAuthorize(UserRole.Manager, UserRole.Hr, UserRole.Developer)]
        [HttpGet("Get-My-Leaves")]
        public async Task<IActionResult> GetMyLeaves()
        {
            Guid currentUserId = User.GetUserId();

            var result = await _leaveApplicationServices.GetMyLeavesAsync(currentUserId);

            return Ok(new
            {
                statusCode = StatusCodes.Status200OK,
                message = "My leaves fetched successfully.",
                data = result
            });
        }

    }
}
