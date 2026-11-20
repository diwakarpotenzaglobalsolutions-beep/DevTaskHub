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
    public class LeaveAllocationController : ControllerBase
    {
        private readonly ILeaveAllocationServices _leaveManagenmentServices;

        public LeaveAllocationController(ILeaveAllocationServices leaveManagenmentServices)
        {
            _leaveManagenmentServices = leaveManagenmentServices;
        }

        [HttpPost("Allocate-Leave-Balance")]
        [RoleAuthorize(UserRole.Admin, UserRole.Hr)]
        public async Task<IActionResult> AllocateLeaveBalance([FromBody] AllocateLeaveBalanceDto dto)
        {
            Guid currentUserId = User.GetUserId();

            if (currentUserId == Guid.Empty)
                return Unauthorized();

            bool result = await _leaveManagenmentServices.AllocateLeaveBalanceAsync(currentUserId, dto);

            return Ok(new
            {
                statusCode = StatusCodes.Status200OK,
                message = "Leave balance allocated successfully.",
                data = result
            });
        }
        [HttpGet("Get-Employee-Balance/{userId:guid}")]
        [RoleAuthorize(UserRole.Admin, UserRole.Hr)]
        public async Task<IActionResult> GetEmployeeBalance(Guid userId, [FromQuery] int? year)
        {
            Guid currentUserId = User.GetUserId();

            if (currentUserId == Guid.Empty)
                return Unauthorized();

            var result = await _leaveManagenmentServices.GetEmployeeBalanceAsync(currentUserId, userId, year);

            return Ok(new
            {
                status = true,
                message = "Employee leave balance fetched successfully.",
                data = result
            });
        }
        [RoleAuthorize(UserRole.Hr,UserRole.Manager,UserRole.Developer)]
        [HttpGet("Get-My-Leave-Balance")]
        public async Task<IActionResult> GetMyLeaveBalance([FromQuery] int? year)
        {
            Guid currentUserId = User.GetUserId();

            var result = await _leaveManagenmentServices.GetMyLeaveBalanceAsync(currentUserId, year);

            return Ok(new
            {
                statusCode = StatusCodes.Status200OK,
                message = "My leave balance fetched successfully.",
                data = result
            });
        }

    }
}
