using DevTaskHubBackend.Data;
using DevTaskHubBackend.Helpers;
using DevTaskHubBackend.Models;
using DevTaskHubBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace DevTaskHubBackend.Controllers;

[ApiController]
[Route("api/github")]
public class GitHubController : ControllerBase
{
    private readonly IGitHubService _githubService;

    public GitHubController(IGitHubService githubService)
    {
        _githubService = githubService;
    }

    // Preview GitHub account
    [HttpGet("Get-Profile/{username}")]
    [RoleAuthorize(UserRole.Manager, UserRole.Developer,UserRole.Developer)]
    public async Task<IActionResult> GetGitHubProfile(
        string username,
        CancellationToken cancellationToken)
    {
        var profile = await _githubService.GetGitHubProfileAsync(
            username,
            cancellationToken);

        return Ok(new
        {
            Success = true,
            Message = "GitHub account retrieved successfully.",
            Data = profile
        });
    }

    // Save GitHub account association
    [HttpPost("Link-Profile")]
    [RoleAuthorize(UserRole.Manager, UserRole.Developer,UserRole.Developer)]
    public async Task<IActionResult> LinkGitHub([FromBody] LinkGitHubDto request,CancellationToken cancellationToken)
    {
        Guid userId = User.GetUserId();

        if (userId == Guid.Empty)
            return Unauthorized();

        var profile = await _githubService.LinkGitHubAccountAsync(
            userId,
            request.Username,
            cancellationToken);

        return Ok(new
        {
            Success = true,
            Message = "GitHub profile associated successfully. Ownership not verified.",
            Data = profile
        });
    }
}

