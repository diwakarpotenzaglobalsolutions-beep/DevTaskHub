using DevTaskHubBackend.Data;
using DevTaskHubBackend.Helpers;
using DevTaskHubBackend.Models;
using DevTaskHubBackend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DevTaskHubBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectServices _projectServices;


        public ProjectsController(IProjectServices projectServices)
        {
            _projectServices = projectServices;
        }

        [RoleAuthorize(UserRole.Manager)]
        [HttpPost("Create-Project")]

        public async Task<IActionResult> CreteProject([FromBody] CreateProjectDto createProjectDto)
        {
            Guid currentUserId = User.GetUserId();
            var result = await _projectServices.CreateProjectAsync(currentUserId, createProjectDto);
            return StatusCode(StatusCodes.Status201Created, new
            {
                Success = true,
                Message = "Project created successfully.",
                Data = result
            });
        }
        [HttpGet("Get-All-Projects")]
        [RoleAuthorize(UserRole.Manager, UserRole.Developer)]
        public async Task<IActionResult> GetAllProjects([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            Guid currentUserId = User.GetUserId();
            var projects = await _projectServices.GetProjectAsync(currentUserId, page, pageSize);
            return Ok(new
            {
                Success = true,
                Message = "Projects retrieved successfully.",
                Data = projects
            });
        }
        [RoleAuthorize(UserRole.Admin, UserRole.Manager)]
        [HttpPut("Edit-Project/{projectId:guid}")]
        public async Task<IActionResult> EditProject([FromRoute] Guid projectId, [FromBody] EditProjectDto editProjectDto)
        {
            Guid currentUserId = User.GetUserId();
            var result = await _projectServices.EditProjectAsync(projectId, editProjectDto, currentUserId);
            return Ok(new
            {
                status = true,
                message = "Project edit successfully.",
                data = result
            });
        }
        [RoleAuthorize(UserRole.Manager)]
        [HttpDelete("Delete-Project/{projectId:guid}")]
        public async Task<IActionResult> DeleteProject(Guid projectId, CancellationToken cancellationToken)
        {
            Guid currentUserId = User.GetUserId();
            await _projectServices.DeleteProjectAsync(projectId, currentUserId, cancellationToken);
            return Ok(new
            {
                Success = true,
                Message = "Project deleted successfully."
            });
        }

        [RoleAuthorize(UserRole.Admin)]
        [HttpPut("Reassign-Manager-Project")]
        public async Task<IActionResult> ReassignProject([FromBody] ReAsignProjectDto request)
        {
            await _projectServices.ReAsignToProjectAsync(request);

            return Ok(new
            {
                Success = true,
                Message = "Project reassigned successfully."
            });
        }
        [RoleAuthorize(UserRole.Manager)]
        [HttpPost("Assign-Developer-To-Project")]

        public async Task<IActionResult> AssignDeveloperToProject([FromBody] AssignDevelopersDto request, CancellationToken cancellationToken)
        {
            Guid currentUserID = User.GetUserId();
            await _projectServices.AssignDeveloperToProject(request, currentUserID, cancellationToken);

            return Ok(new
            {
                success=true,
                message="Developer assigned to project successfully."
            });
        }

        [RoleAuthorize(UserRole.Manager)]
        [HttpPut("Update-Project-Developers")]
        public async Task<IActionResult> UpdateProjectDevelopers([FromBody] AssignDevelopersDto request, CancellationToken cancellationToken)
        {
            Guid currentUserID = User.GetUserId();
            await _projectServices.AssignDeveloperToProject(request, currentUserID, cancellationToken);

            return Ok(new
            {
                success = true,
                message = "Project developers updated successfully."
            });
        }

        [RoleAuthorize(UserRole.Manager, UserRole.Developer)]
        [HttpGet("Get-Project-Developers/{projectId:guid}")]
        public async Task<IActionResult> GetProjectDevelopers(Guid projectId, CancellationToken cancellationToken)
        {
            Guid currentUserID = User.GetUserId();
            var result = await _projectServices.GetProjectDevelopersAsync(projectId, currentUserID, cancellationToken);

            return Ok(new
            {
                success = true,
                message = "Project developers retrieved successfully.",
                data = result,
            });
        }
    }











}
