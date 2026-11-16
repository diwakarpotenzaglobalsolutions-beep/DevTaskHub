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

    [HttpGet("Get-Profile/{username}")]
    [RoleAuthorize(UserRole.Manager, UserRole.Developer)]
    public async Task<IActionResult> GetGitHubProfile(string username, CancellationToken cancellationToken)
    {
        var profile = await _githubService.GetGitHubProfileAsync(username, cancellationToken);
        return Ok(new
        {
            Success = true,
            Message = "GitHub account retrieved successfully.",
            Data = profile
        });
    }

    [HttpPost("Link-Profile")]
    [RoleAuthorize(UserRole.Manager, UserRole.Developer)]
    public async Task<IActionResult> LinkGitHub([FromBody] LinkGitHubDto request, CancellationToken cancellationToken)
    {
        var profile = await _githubService.LinkGitHubAccountAsync(User.GetUserId(), request.Username, cancellationToken);
        return Ok(new
        {
            Success = true,
            Message = "GitHub profile linked successfully.",
            Data = profile
        });
    }

    [HttpGet("My-Profile")]
    [RoleAuthorize(UserRole.Manager, UserRole.Developer)]
    public async Task<IActionResult> MyProfile(CancellationToken cancellationToken)
    {
        var profile = await _githubService.GetLinkedProfileAsync(User.GetUserId(), cancellationToken);
        return Ok(new
        {
            Success = true,
            Message = "Linked GitHub profile retrieved successfully.",
            Data = profile
        });
    }

    [HttpPost("Verify-Profile")]
    [RoleAuthorize(UserRole.Manager, UserRole.Developer)]
    public async Task<IActionResult> VerifyProfile(CancellationToken cancellationToken)
    {
        var profile = await _githubService.VerifyLinkedProfileAsync(User.GetUserId(), cancellationToken);
        return Ok(new
        {
            Success = true,
            Message = "GitHub public profile refreshed. Ownership is not verified.",
            Data = profile
        });
    }

    [HttpDelete("Unlink-Profile")]
    [RoleAuthorize(UserRole.Manager, UserRole.Developer)]
    public async Task<IActionResult> UnlinkProfile(CancellationToken cancellationToken)
    {
        await _githubService.UnlinkGitHubAccountAsync(User.GetUserId(), cancellationToken);
        return Ok(new
        {
            Success = true,
            Message = "GitHub profile unlinked successfully."
        });
    }
    [HttpPost("Link-Repository")]
    [RoleAuthorize(UserRole.Manager)]
    public async Task<IActionResult> LinkRepository([FromBody] LinkRepositoryDto request, CancellationToken cancellationToken)
    {
        var repository = await _githubService.LinkRepositoryAsync(User.GetUserId(), request, cancellationToken);
        return Ok(new
        {
            Success = true,
            Message = "GitHub repository linked successfully.",
            Data = repository
        });
    }

    [HttpGet("Repository/{projectId:guid}")]
    [RoleAuthorize(UserRole.Manager, UserRole.Developer)]
    public async Task<IActionResult> GetRepository(Guid projectId, CancellationToken cancellationToken)
    {
        var repository = await _githubService.GetRepositoryAsync(User.GetUserId(), projectId, cancellationToken);
        return Ok(new
        {
            Success = true,
            Message = "GitHub repository retrieved successfully.",
            Data = repository
        });
    }

    [HttpDelete("Repository/{projectId:guid}")]
    [RoleAuthorize(UserRole.Manager)]
    public async Task<IActionResult> UnlinkRepository(Guid projectId, CancellationToken cancellationToken)
    {
        await _githubService.UnlinkRepositoryAsync(User.GetUserId(), projectId, cancellationToken);
        return Ok(new
        {
            Success = true,
            Message = "GitHub repository unlinked successfully."
        });
    }

    [HttpPost("Sync-PullRequests/{projectId:guid}")]
    [RoleAuthorize(UserRole.Manager, UserRole.Developer)]
    public async Task<IActionResult> SyncPullRequests(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await _githubService.SyncPullRequestsAsync(User.GetUserId(), projectId, cancellationToken);
        return Ok(new
        {
            Success = true,
            Message = "Linked GitHub pull requests synced. New tasks were not created.",
            Data = result
        });
    }

    [HttpPut("Link-PullRequest/{taskId:guid}")]
    [RoleAuthorize(UserRole.Manager, UserRole.Developer)]
    public async Task<IActionResult> LinkPullRequest(Guid taskId, [FromBody] LinkPullRequestDto request, CancellationToken cancellationToken)
    {
        var pullRequest = await _githubService.LinkPullRequestAsync(User.GetUserId(), taskId, request?.PullRequestUrl ?? string.Empty, cancellationToken);
        return Ok(new
        {
            Success = true,
            Message = "GitHub pull request linked successfully.",
            Data = pullRequest
        });
    }

    [HttpGet("Get-Repositories/{username}")]
    [RoleAuthorize(UserRole.Manager, UserRole.Developer)]
    public async Task<IActionResult> GetRepositories(string username, [FromQuery] int page = 1, [FromQuery] int pageSize = 30, CancellationToken cancellationToken = default)
    {
        var repositories = await _githubService.GetPublicRepositoriesAsync(username, page, pageSize, cancellationToken);
        return Ok(new
        {
            Success = true,
            Message = "Public GitHub repositories retrieved successfully.",
            Data = repositories
        });
    }

    [HttpGet("PullRequests/{projectId:guid}")]
    [RoleAuthorize(UserRole.Manager, UserRole.Developer)]
    public async Task<IActionResult> GetPullRequests(Guid projectId, [FromQuery] int page = 1, [FromQuery] int pageSize = 30, [FromQuery] string? status = null, CancellationToken cancellationToken = default)
    {
        var pullRequests = await _githubService.GetPullRequestsAsync(User.GetUserId(), projectId, page, pageSize, status, cancellationToken);
        return Ok(new
        {
            Success = true,
            Message = "GitHub pull requests retrieved successfully.",
            Data = pullRequests
        });
    }

    [HttpGet("Live-PullRequests")]
    [RoleAuthorize(UserRole.Manager, UserRole.Developer)]
    public async Task<IActionResult> LivePullRequests(CancellationToken cancellationToken)
    {
        var pullRequests = await _githubService.GetLivePullRequestsAsync(User.GetUserId(), cancellationToken);
        return Ok(new
        {
            Success = true,
            Message = "GitHub pull requests retrieved successfully.",
            Data = pullRequests
        });
    }
}
