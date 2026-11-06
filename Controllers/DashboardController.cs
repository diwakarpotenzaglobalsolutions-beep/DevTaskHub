using DevTaskHubBackend.Data;
using DevTaskHubBackend.Helpers;
using DevTaskHubBackend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevTaskHubBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardServices _dashboardServices;

        public DashboardController(IDashboardServices dashboardServices)
        {
            _dashboardServices = dashboardServices;
        }
        [RoleAuthorize(UserRole.Manager, UserRole.Developer)]
        [HttpGet("Get-Dashboard")]
        public async Task<IActionResult> GetDashboard(
        CancellationToken cancellationToken)
        {
            Guid currentUserID = User.GetUserId();
            var result = await _dashboardServices.GetDashboardAsync(currentUserID,cancellationToken);

            return Ok(new
            {
                Success = true,
                Message = "Dashboard data retrieved successfully.",
                Data = result
            });
        }
    }
}
