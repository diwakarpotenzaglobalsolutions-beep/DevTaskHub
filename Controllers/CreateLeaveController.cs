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
    public class CreateLeaveController : ControllerBase
    {
        private readonly ILeaveServices _leaveServices;

        public CreateLeaveController(ILeaveServices leaveServices)
        {
            _leaveServices = leaveServices;
        }

        [HttpPost("Create-Leave-Type")]
        [RoleAuthorize(UserRole.Admin, UserRole.Hr)]
        public async Task<IActionResult> CreateLeaveAsync([FromBody] CreateLeaveTypeDto createLeaveTypeDto)
        {
            Guid currentUserId = User.GetUserId();

            var result = await _leaveServices.CreateLeaveTypeAsync(currentUserId, createLeaveTypeDto);

            return Ok(new
            {
                status = result,
                message = result ? "Leave type created successfully." : "Leave type could not be created.",
            });
        }
        [HttpGet("Get-All-Leave-Types")]
        [RoleAuthorize(UserRole.Admin, UserRole.Hr)]
        public async Task<IActionResult> GetAllLeaveTypes(CancellationToken cancellationToken)
        {
            Guid currentUserId = User.GetUserId();
            var result = await _leaveServices.GetAllLeaveTypesAsync(currentUserId);
            return Ok(new
            {
                status = true,
                message = "Leave types fetched successfully.",
                data = result
            });
        }

        [HttpPut("Update-Leave-Type/{id:guid}")]
        [RoleAuthorize(UserRole.Admin, UserRole.Hr)]
        public async Task<IActionResult> UpdateLeaveType(Guid id, [FromBody] CreateLeaveTypeDto dto)
        {
            Guid currentUserId = User.GetUserId();

            if (currentUserId == Guid.Empty)
                return Unauthorized();

            bool result = await _leaveServices.UpdateLeaveTypeAsync(currentUserId, dto, id);

            return Ok(new
            {
                statusCode = StatusCodes.Status200OK,
                message = result
                    ? "Leave type updated successfully."
                    : "No changes were made.",
                data = result
            });
        }


        [RoleAuthorize(UserRole.Admin, UserRole.Hr)]
        [HttpDelete("Delete-Leave-Type/{leaveTypeId:guid}")]
        public async Task<IActionResult> DeleteLeaveType(Guid leaveTypeId)
        {
            Guid currentUserID = User.GetUserId();

            var result = await _leaveServices.DeleteLeaveTypeAsync(currentUserID, leaveTypeId);

            return Ok(new
            {
                success = result,
                message = "Leave type deleted successfully."
            });
        }


    }
}
