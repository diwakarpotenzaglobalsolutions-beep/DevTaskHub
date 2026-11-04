using DevTaskHubBackend.Data;
using DevTaskHubBackend.Helpers;
using DevTaskHubBackend.Models;
using DevTaskHubBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevTaskHubBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleManageController : ControllerBase
    {
        
    private readonly IRoleManageServices _roleManageServices;

        public RoleManageController(IRoleManageServices roleManageServices)
        {
            _roleManageServices=roleManageServices;
        }
        [RoleAuthorize(UserRole.Admin)]
        [HttpGet("Get-AllUser")]
        public async Task<IActionResult> GetAllUserAsync()
        {
            var result=await _roleManageServices.GetAllUserAsync();

            return Ok(new
            {
                success=true,
                message="Users retrieved successfully..",
                data=result
            });
        }
        [RoleAuthorize(UserRole.Admin)]
        [HttpPost("Role-Assign")]
        public async Task<IActionResult> AssignRoleByUser(RoleAssignDto roleAssignDto)
        {
            var result=await _roleManageServices.AssignRoleAsync(roleAssignDto);

            
            return Ok(new
            {
                success=true,
                message="Role assigned successfully."
            });
        }

        [RoleAuthorize(UserRole.Admin)]
        [HttpGet("Get-AllRole")]
        public async Task<IActionResult> GetAllRoleList()
        {
            var result = await _roleManageServices.GetAllRoleListAsync();

            return Ok(new
            {
                success=true,
                message="All roles retrieved successfully.",
                data=result
            });
        }
    }
}
