using DevTaskHubBackend.Data;
using DevTaskHubBackend.Exceptions;
using DevTaskHubBackend.Helpers;
using DevTaskHubBackend.Models;
using DevTaskHubBackend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevTaskHubBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceServices _attendanceServices;

        public AttendanceController(IAttendanceServices attendanceServices)
        {
            _attendanceServices = attendanceServices;
        }

        [RoleAuthorize(Data.UserRole.Manager, UserRole.Developer, UserRole.Hr)]
        [HttpPost("Attendance-Check-In")]
        public async Task<IActionResult> AttendanceCheck()
        {
            Guid currentUserId = User.GetUserId();
            var result = await _attendanceServices.AttendanceAsync(currentUserId);

            if (result is null)
            {
                return Ok(new
                {
                    success = false,
                    message = "No any action please try agen.",
                    data = result
                });
            }
            return Ok(new
            {
                success = true,
                message = "Attendance Check-In successful.",
                data = result
            });
        }
        [RoleAuthorize(Data.UserRole.Manager, UserRole.Developer, UserRole.Hr)]
        [HttpPut("Attendance-Check-Out")]
        public async Task<IActionResult> CheckOut()
        {
            Guid currentUserId = User.GetUserId();
            var result = await _attendanceServices.CheckOutAsync(currentUserId);

            return Ok(new
            {
                status = true,
                message = "Attendance Check-Out successful.",
                data = result
            });
        }
        [RoleAuthorize(Data.UserRole.Manager, UserRole.Developer, UserRole.Hr)]
        [HttpGet("Get-Today-Attendance")]
        public async Task<IActionResult> GetTodayAttendance()
        {
            Guid currentUserId = User.GetUserId();
            var result = await _attendanceServices.TodayAttendanceAsync(currentUserId);

            return Ok(new
            {
                statusCode = StatusCodes.Status200OK,
                message = "Today's attendance fetched successfully.",
                data = result
            });
        }
        [RoleAuthorize(Data.UserRole.Manager, UserRole.Developer, UserRole.Hr)]
        [HttpGet("Get-My-Attendance")]
        public async Task<IActionResult> GetMyAttendance([FromQuery] int? month, [FromQuery] int? year)
        {
            Guid currentUserId = User.GetUserId();
            var result = await _attendanceServices.GetMyAttendanceAsync(currentUserId, month, year);

            return Ok(new
            {
                status = true,
                message = "Attendance history fetched successfully.",
                data = result
            });
        }
        [HttpGet("Get-Team-Attendance")]
        [RoleAuthorize(UserRole.Manager)]
        public async Task<IActionResult> GetTeamAttendance([FromQuery] DateOnly? date)
        {
            Guid currentUserId = User.GetUserId();

            var result = await _attendanceServices.GetTeamAttendanceAsync(currentUserId, date);

            return Ok(new
            {
                statusCode = StatusCodes.Status200OK,
                message = "Team attendance fetched successfully.",
                data = result
            });
        }


        [HttpGet("Get-All-Attendance")]
        [RoleAuthorize(UserRole.Admin, UserRole.Hr)]
        public async Task<IActionResult> GetAllAttendance([FromQuery] DateOnly? date, [FromQuery] Guid? userId)
        {
            Guid currentUserId = User.GetUserId();

            var result = await _attendanceServices.GetAllAttendanceAsync(currentUserId, date, userId ?? Guid.Empty);

            return Ok(new
            {
                statusCode = StatusCodes.Status200OK,
                message = "All attendance fetched successfully.",
                data = result
            });
        }


        [HttpPut("Correct-Attendance/{attendanceId:guid}")]
        [RoleAuthorize(UserRole.Admin, UserRole.Hr)]
        public async Task<IActionResult> CorrectAttendance(Guid attendanceId, [FromBody] CorrectAttendanceDto? dto, CancellationToken cancellationToken)
        {
            if (attendanceId == Guid.Empty)
            {
                throw new AppException(StatusCodes.Status400BadRequest, "A valid attendance id is required.");
            }
            if (dto is null)
            {
                throw new AppException(StatusCodes.Status400BadRequest, "Attendance correction details are required.");
            }

            Guid currentUserId = User.GetUserId();
            bool updated = await _attendanceServices.CorrectAttendanceAsync(currentUserId, attendanceId, dto, cancellationToken);

            return Ok(new
            {
                statusCode = StatusCodes.Status200OK,
                message = updated ? "Attendance corrected successfully." : "No attendance changes were made.",
                data = new { updated }
            });
        }
    }
}
