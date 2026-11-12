using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using DevTaskHubBackend.Data;
using DevTaskHubBackend.Entities;
using DevTaskHubBackend.Exceptions;
using DevTaskHubBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace DevTaskHubBackend.Services;

public interface IGitHubService
{
    Task<GitHubProfileDto> GetGitHubProfileAsync(string username, CancellationToken cancellationToken);

    Task<GitHubProfileDto> LinkGitHubAccountAsync(Guid userId, string username, CancellationToken cancellationToken);

    Task<LinkedGitHubProfileDto> GetLinkedProfileAsync(Guid userId, CancellationToken cancellationToken);

    Task<GitHubProfileDto> VerifyLinkedProfileAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> UnlinkGitHubAccountAsync(Guid userId, CancellationToken cancellationToken);

    Task<GitHubRepositoryDto> LinkRepositoryAsync(Guid userId, LinkRepositoryDto request, CancellationToken cancellationToken);

    Task<GitHubRepositoryDto> GetRepositoryAsync(Guid userId, Guid projectId, CancellationToken cancellationToken);

    Task<bool> UnlinkRepositoryAsync(Guid userId, Guid projectId, CancellationToken cancellationToken);

    Task<SyncPullRequestResultDto> SyncPullRequestsAsync(Guid userId, Guid projectId, CancellationToken cancellationToken);

    Task<List<GitHubPullRequestDto>> GetPullRequestsAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default);

    Task<List<GitHubPullRequestDto>> GetPullRequestsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<List<GitHubPullRequestDto>> GetLivePullRequestsAsync(Guid userId, CancellationToken cancellationToken = default);
}

public class GitHubService : IGitHubService
{
    private const string UsernamePattern = @"^(?!-)[a-zA-Z0-9-]{1,39}(?<!-)$";
    private const string RepositoryPattern = @"^[a-zA-Z0-9._-]{1,100}$";

    private readonly HttpClient _httpClient;
    private readonly MySqlDbContext _context;

    public GitHubService(HttpClient httpClient, MySqlDbContext context)
    {
        _httpClient = httpClient;
        _context = context;
    }

    public async Task<GitHubProfileDto> GetGitHubProfileAsync(string username, CancellationToken cancellationToken)
    {
        username = username?.Trim() ?? "";
        if (!Regex.IsMatch(username, UsernamePattern))
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Invalid GitHub username.");
        }

        return await ReadGitHubAsync<GitHubProfileDto>(
            $"users/{Uri.EscapeDataString(username)}",
            "GitHub account not found.",
            cancellationToken);
    }

    public async Task<GitHubProfileDto> LinkGitHubAccountAsync(Guid userId, string username, CancellationToken cancellationToken)
    {
        var user = await GetActiveUserAsync(userId, cancellationToken);
        var profile = await GetGitHubProfileAsync(username, cancellationToken);
        await EnsureGitHubIdIsFreeAsync(userId, profile.GitHubId, cancellationToken);
        SaveProfile(user, profile);
        await _context.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public async Task<LinkedGitHubProfileDto> GetLinkedProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await GetActiveUserAsync(userId, cancellationToken);
        if (user.GitHubId is null || string.IsNullOrWhiteSpace(user.GitHubUserName))
        {
            throw new AppException(StatusCodes.Status404NotFound, "GitHub account is not linked.");
        }

        return ToLinkedProfile(user);
    }

    public async Task<GitHubProfileDto> VerifyLinkedProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await GetActiveUserAsync(userId, cancellationToken);
        if (string.IsNullOrWhiteSpace(user.GitHubUserName))
        {
            throw new AppException(StatusCodes.Status400BadRequest, "GitHub account is not linked.");
        }

        var profile = await GetGitHubProfileAsync(user.GitHubUserName, cancellationToken);
        await EnsureGitHubIdIsFreeAsync(userId, profile.GitHubId, cancellationToken);
        SaveProfile(user, profile);
        await _context.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public async Task<bool> UnlinkGitHubAccountAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await GetActiveUserAsync(userId, cancellationToken);
        user.GitHubId = null;
        user.GitHubUserName = null;
        user.GitHubProfileUrl = null;
        user.GitHubAvatarUrl = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<GitHubRepositoryDto> LinkRepositoryAsync(Guid userId, LinkRepositoryDto request, CancellationToken cancellationToken)
    {
        var owner = request.Owner?.Trim() ?? "";
        var repositoryName = request.RepositoryName?.Trim() ?? "";
        if (!Regex.IsMatch(owner, UsernamePattern) || !Regex.IsMatch(repositoryName, RepositoryPattern))
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Invalid GitHub repository.");
        }

        var project = await GetProjectAsync(userId, request.ProjectId, membersCanAccess: false, cancellationToken);
        var repo = await ReadGitHubAsync<GitHubRepositoryApi>(
            $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repositoryName)}",
            "GitHub repository not found or it is private.",
            cancellationToken);

        var canonicalOwner = repo.Owner?.Login ?? owner;
        var canonicalName = string.IsNullOrWhiteSpace(repo.Name) ? repositoryName : repo.Name;
        var alreadyLinked = await _context.Projects
            .IgnoreQueryFilters()
            .AnyAsync(
                p => p.ProjectId != project.ProjectId &&
                     p.GitHubOwner == canonicalOwner &&
                     p.GitHubRepoName == canonicalName,
                cancellationToken);
        if (alreadyLinked)
        {
            throw new AppException(StatusCodes.Status409Conflict, "This GitHub repository is already linked to another project.");
        }

        project.GitHubOwner = canonicalOwner;
        project.GitHubRepoName = canonicalName;
        project.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return ToRepositoryDto(project, repo.Description, repo.HtmlUrl);
    }

    public async Task<GitHubRepositoryDto> GetRepositoryAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
    {
        var project = await GetProjectAsync(userId, projectId, membersCanAccess: true, cancellationToken);
        if (string.IsNullOrWhiteSpace(project.GitHubOwner) || string.IsNullOrWhiteSpace(project.GitHubRepoName))
        {
            throw new AppException(StatusCodes.Status404NotFound, "GitHub repository is not linked to this project.");
        }

        var repo = await ReadGitHubAsync<GitHubRepositoryApi>(
            $"repos/{Uri.EscapeDataString(project.GitHubOwner)}/{Uri.EscapeDataString(project.GitHubRepoName)}",
            "GitHub repository not found or it is private.",
            cancellationToken);
        return ToRepositoryDto(project, repo.Description, repo.HtmlUrl);
    }

    public async Task<bool> UnlinkRepositoryAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
    {
        var project = await GetProjectAsync(userId, projectId, membersCanAccess: false, cancellationToken);
        project.GitHubOwner = null;
        project.GitHubRepoName = null;
        project.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<SyncPullRequestResultDto> SyncPullRequestsAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
    {
        var user = await GetActiveUserAsync(userId, cancellationToken);
        var project = await GetProjectAsync(user, projectId, membersCanAccess: true, cancellationToken);
        if (string.IsNullOrWhiteSpace(project.GitHubOwner) || string.IsNullOrWhiteSpace(project.GitHubRepoName))
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Link a GitHub repository before syncing pull requests.");
        }

        var pulls = await ReadGitHubAsync<List<GitHubPullRequestApi>>(
            $"repos/{Uri.EscapeDataString(project.GitHubOwner)}/{Uri.EscapeDataString(project.GitHubRepoName)}/pulls?state=all&per_page=50",
            "GitHub repository not found or it is private.",
            cancellationToken);

        var existing = await _context.Tasks
            .IgnoreQueryFilters()
            .Where(task => task.ProjectId == project.ProjectId && task.GitHubPRNumber != null)
            .ToListAsync(cancellationToken);
        var byNumber = existing.ToDictionary(task => task.GitHubPRNumber!.Value);
        var now = DateTime.UtcNow;
        var created = 0;
        var updated = 0;

        foreach (var pull in pulls)
        {
            var status = pull.MergedAt != null
                ? PullRequestStatus.Merged
                : string.Equals(pull.State, "closed", StringComparison.OrdinalIgnoreCase)
                    ? PullRequestStatus.Closed
                    : PullRequestStatus.Open;
            var title = string.IsNullOrWhiteSpace(pull.Title) ? $"PR #{pull.Number}" : pull.Title.Trim();
            if (title.Length > 200)
            {
                title = title[..200];
            }

            if (!byNumber.TryGetValue(pull.Number, out var task))
            {
                task = new Tasks
                {
                    TaskId = Guid.CreateVersion7(),
                    ProjectId = project.ProjectId,
                    CreatedByUserId = user.UserID,
                    Title = title,
                    Priority = TaskPriority.Medium.ToString(),
                    GitHubPRNumber = pull.Number
                };
                _context.Tasks.Add(task);
                byNumber[pull.Number] = task;
                created++;
            }
            else
            {
                task.Title = title;
                updated++;
            }

            task.GitHubPRStatus = status.ToString();
            task.GitHubPRUrl = pull.HtmlUrl;
            task.LastSyncedAt = now;
            task.Status = status == PullRequestStatus.Open ? nameof(Data.TaskStatus.InReview) : nameof(Data.TaskStatus.Done);
            task.IsDeleted = false;
            task.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return new SyncPullRequestResultDto
        {
            Created = created,
            Updated = updated,
            PullRequests = await ListPullRequestsAsync(project, cancellationToken)
        };
    }

    public async Task<List<GitHubPullRequestDto>> GetPullRequestsAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default)
    {
        var user = await GetActiveUserAsync(userId, cancellationToken);
        var project = await GetProjectAsync(user, projectId, membersCanAccess: true, cancellationToken);
        return await ListPullRequestsAsync(project, cancellationToken);
    }

    public Task<List<GitHubPullRequestDto>> GetPullRequestsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return GetLivePullRequestsAsync(userId, cancellationToken);
    }

    public async Task<List<GitHubPullRequestDto>> GetLivePullRequestsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await GetActiveUserAsync(userId, cancellationToken);
        var query = _context.Tasks.AsNoTracking().Where(task => task.GitHubPRNumber != null);

        if (user.RoleId == UserRole.Manager.ToId())
        {
            query = query.Where(task => _context.Projects.Any(project =>
                project.ProjectId == task.ProjectId && project.AssignedToUserId == user.UserID));
        }
        else if (user.RoleId == UserRole.Developer.ToId())
        {
            query = query.Where(task => _context.ProjectMembers.Any(member =>
                member.ProjectId == task.ProjectId && member.UserId == user.UserID));
        }
        else if (user.RoleId != UserRole.Admin.ToId())
        {
            throw new AppException(StatusCodes.Status403Forbidden, "You do not have permission to view pull requests.");
        }

        return await query
            .OrderByDescending(task => task.LastSyncedAt)
            .Select(task => new GitHubPullRequestDto
            {
                TaskId = task.TaskId,
                Number = task.GitHubPRNumber!.Value,
                Title = task.Title,
                Status = task.GitHubPRStatus ?? string.Empty,
                Url = task.GitHubPRUrl,
                LastSyncedAt = task.LastSyncedAt,
                AssignedToUserId = task.AssignedToUserId,
                AssignedToName = task.AssignedToUserId == null
                    ? null
                    : _context.Users.Where(user => user.UserID == task.AssignedToUserId).Select(user => user.FullName).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<List<GitHubPullRequestDto>> ListPullRequestsAsync(Projects project, CancellationToken cancellationToken)
    {
        return await _context.Tasks.AsNoTracking()
            .Where(task => task.ProjectId == project.ProjectId && task.GitHubPRNumber != null)
            .OrderByDescending(task => task.GitHubPRNumber)
            .Select(task => new GitHubPullRequestDto
            {
                TaskId = task.TaskId,
                Number = task.GitHubPRNumber!.Value,
                Title = task.Title,
                Status = task.GitHubPRStatus ?? string.Empty,
                Url = task.GitHubPRUrl,
                LastSyncedAt = task.LastSyncedAt,
                AssignedToUserId = task.AssignedToUserId,
                AssignedToName = task.AssignedToUserId == null
                    ? null
                    : _context.Users.Where(user => user.UserID == task.AssignedToUserId).Select(user => user.FullName).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<T> ReadGitHubAsync<T>(string relativeUrl, string notFoundMessage, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(relativeUrl, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new AppException(StatusCodes.Status404NotFound, notFoundMessage);
        }

        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests)
        {
            throw new AppException(StatusCodes.Status503ServiceUnavailable, "GitHub API limit reached. Please try again later.");
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new AppException(StatusCodes.Status502BadGateway, "GitHub rejected the request.");
        }

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        return body ?? throw new AppException(StatusCodes.Status502BadGateway, "Unable to read the GitHub response.");
    }

    private async Task<User> GetActiveUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId && u.IsActive && !u.IsDeleted,cancellationToken);
        if (user == null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "Active user not found.");
        }

        return user;
    }

    private async Task EnsureGitHubIdIsFreeAsync(Guid userId, long gitHubId, CancellationToken cancellationToken)
    {
        var alreadyLinked = await _context.Users.AnyAsync(
            u => u.UserID != userId && u.GitHubId == gitHubId,
            cancellationToken);
        if (alreadyLinked)
        {
            throw new AppException(StatusCodes.Status409Conflict, "This GitHub account is already linked to another user.");
        }
    }

    private static void SaveProfile(User user, GitHubProfileDto profile)
    {
        user.GitHubId = profile.GitHubId;
        user.GitHubUserName = profile.Username;
        user.GitHubProfileUrl = profile.ProfileUrl;
        user.GitHubAvatarUrl = profile.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;
    }

    private static LinkedGitHubProfileDto ToLinkedProfile(User user)
    {
        return new LinkedGitHubProfileDto
        {
            GitHubId = user.GitHubId,
            Username = user.GitHubUserName,
            ProfileUrl = user.GitHubProfileUrl,
            AvatarUrl = user.GitHubAvatarUrl
        };
    }

    private async Task<Projects> GetProjectAsync(Guid userId, Guid projectId, bool membersCanAccess, CancellationToken cancellationToken)
    {
        var user = await GetActiveUserAsync(userId, cancellationToken);
        return await GetProjectAsync(user, projectId, membersCanAccess, cancellationToken);
    }

    private async Task<Projects> GetProjectAsync(User user, Guid projectId, bool membersCanAccess, CancellationToken cancellationToken)
    {
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == projectId && !p.IsDeleted, cancellationToken);
        if (project == null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "Project not found.");
        }

        var isAdmin = user.RoleId == UserRole.Admin.ToId();
        var isAssignedManager = user.RoleId == UserRole.Manager.ToId() && project.AssignedToUserId == user.UserID;
        if (isAdmin || isAssignedManager)
        {
            return project;
        }

        if (membersCanAccess && user.RoleId == UserRole.Developer.ToId())
        {
            var isMember = await _context.ProjectMembers.AnyAsync(
                member => member.ProjectId == project.ProjectId && member.UserId == user.UserID,
                cancellationToken);
            if (isMember)
            {
                return project;
            }
        }

        throw new AppException(StatusCodes.Status403Forbidden, "You do not have permission for this project.");
    }

    private static GitHubRepositoryDto ToRepositoryDto(Projects project, string? description, string? url)
    {
        return new GitHubRepositoryDto
        {
            ProjectId = project.ProjectId,
            Owner = project.GitHubOwner ?? string.Empty,
            RepositoryName = project.GitHubRepoName ?? string.Empty,
            Description = description,
            Url = url
        };
    }

    private sealed class GitHubRepositoryApi
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("owner")]
        public GitHubOwnerApi? Owner { get; set; }
    }

    private sealed class GitHubOwnerApi
    {
        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;
    }

    private sealed class GitHubPullRequestApi
    {
        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;

        [JsonPropertyName("merged_at")]
        public DateTime? MergedAt { get; set; }
    }
}
