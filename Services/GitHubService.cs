using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using DevTaskHubBackend.Data;
using DevTaskHubBackend.Exceptions;
using DevTaskHubBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace DevTaskHubBackend.Services;

public interface IGitHubService
{
    Task<GitHubProfileDto> GetGitHubProfileAsync(string username,CancellationToken cancellationToken);

    Task<GitHubProfileDto> LinkGitHubAccountAsync(Guid userId,string username,CancellationToken cancellationToken);
}


public class GitHubService : IGitHubService
{
    private readonly HttpClient _httpClient;
    private readonly MySqlDbContext _context;

    public GitHubService(
        HttpClient httpClient,
        MySqlDbContext context)
    {
        _httpClient = httpClient;
        _context = context;
    }

    public async Task<GitHubProfileDto> GetGitHubProfileAsync(
        string username,
        CancellationToken cancellationToken)
    {
        username = username?.Trim() ?? "";

        if (!Regex.IsMatch(
            username,
            @"^(?!-)[a-zA-Z0-9-]{1,39}(?<!-)$"))
        {
            throw new AppException(
                StatusCodes.Status400BadRequest,
                "Invalid GitHub username.");
        }

        using var response = await _httpClient.GetAsync(
            $"users/{username}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new AppException(
                StatusCodes.Status404NotFound,
                "GitHub account not found.");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden ||
            response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new AppException(
                StatusCodes.Status503ServiceUnavailable,
                "GitHub API limit reached. Please try again later.");
        }

        response.EnsureSuccessStatusCode();

        var profile = await response.Content
            .ReadFromJsonAsync<GitHubProfileDto>(
                cancellationToken: cancellationToken);

        return profile ?? throw new AppException(
            StatusCodes.Status502BadGateway,
            "Unable to fetch GitHub account.");
    }

    public async Task<GitHubProfileDto> LinkGitHubAccountAsync(
        Guid userId,
        string username,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(
                u => u.UserID == userId &&
                     u.IsActive &&
                     !u.IsDeleted,
                cancellationToken);

        if (user == null)
        {
            throw new AppException(
                StatusCodes.Status404NotFound,
                "Active user not found.");
        }

        var profile = await GetGitHubProfileAsync(
            username,
            cancellationToken);

        var alreadyLinked = await _context.Users
            .AnyAsync(
                u => u.UserID != userId &&
                     u.GitHubId == profile.GitHubId,
                cancellationToken);

        if (alreadyLinked)
        {
            throw new AppException(
                StatusCodes.Status409Conflict,
                "This GitHub account is already linked to another user.");
        }

        user.GitHubId = profile.GitHubId;
        user.GitHubUserName = profile.Username;
        user.GitHubProfileUrl = profile.ProfileUrl;
        user.GitHubAvatarUrl = profile.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return profile;
    }
}