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

    Task<GitHubPullRequestDto> LinkPullRequestAsync(Guid userId, Guid taskId, string pullRequestUrl, CancellationToken cancellationToken);

    Task<GitHubPullRequestPageDto> GetPullRequestsAsync(Guid userId, Guid projectId, int page, int pageSize, string? status, CancellationToken cancellationToken = default);

    Task<GitHubRepositoryPageDto> GetPublicRepositoriesAsync(string username, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<List<GitHubPullRequestDto>> GetPullRequestsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<List<GitHubPullRequestDto>> GetLivePullRequestsAsync(Guid userId, CancellationToken cancellationToken = default);
}

public class GitHubService : IGitHubService
{
    private readonly HttpClient _httpClient;
    private readonly MySqlDbContext _context;
    private readonly IActivityLogService _activityLogService;
    private readonly IGitHubOwnershipVerifier _ownershipVerifier;

    public GitHubService(
        HttpClient httpClient,
        MySqlDbContext context,
        IActivityLogService activityLogService,
        IGitHubOwnershipVerifier ownershipVerifier)
    {
        _httpClient = httpClient;
        _activityLogService = activityLogService;
        _context = context;
        _ownershipVerifier = ownershipVerifier;
    }

    public async Task<GitHubProfileDto> GetGitHubProfileAsync(string username, CancellationToken cancellationToken)
    {
        username = username?.Trim() ?? "";
        if (!Regex.IsMatch(username, GitHubPullRequestRules.UsernamePattern))
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Invalid GitHub username.");
        }

        var profile = await ReadGitHubAsync<GitHubProfileDto>(
            $"users/{Uri.EscapeDataString(username)}",
            "GitHub account not found.",
            cancellationToken);
        profile.OwnershipVerified = false;
        profile.OwnershipVerification = "PublicProfileOnly";
        return profile;
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

        var linked = ToLinkedProfile(user);
        linked.OwnershipVerified = await _ownershipVerifier.IsOwnershipVerifiedAsync(user, cancellationToken);
        linked.OwnershipVerification = linked.OwnershipVerified ? "Verified" : "PublicProfileOnly";
        return linked;
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
        if (!Regex.IsMatch(owner, GitHubPullRequestRules.UsernamePattern) || !Regex.IsMatch(repositoryName, GitHubPullRequestRules.RepositoryPattern))
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Invalid GitHub repository.");
        }

        var user = await GetActiveUserAsync(userId, cancellationToken);
        var project = await GetProjectAsync(user, request.ProjectId, membersCanAccess: false, cancellationToken);
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
        _activityLogService.Add(project.ProjectId, project.ProjectName, user.UserID, user.FullName, "RepositoryLinked", $"{user.FullName} linked GitHub repository {canonicalOwner}/{canonicalName} to project \"{project.ProjectName}\".");
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
        var user = await GetActiveUserAsync(userId, cancellationToken);
        var project = await GetProjectAsync(user, projectId, membersCanAccess: false, cancellationToken);
        project.GitHubOwner = null;
        project.GitHubRepoName = null;
        project.UpdatedAt = DateTime.UtcNow;
        _activityLogService.Add(project.ProjectId, project.ProjectName, user.UserID, user.FullName, "RepositoryUnlinked", $"{user.FullName} unlinked the GitHub repository from project \"{project.ProjectName}\".");
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

        var linkedTasks = await _context.Tasks
            .Where(task => task.ProjectId == project.ProjectId && task.GitHubPRNumber != null)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var updated = 0;
        var skipped = 0;

        foreach (var task in linkedTasks)
        {
            GitHubPullRequestApi? pull;
            try
            {
                pull = await ReadPullRequestAsync(project.GitHubOwner, project.GitHubRepoName, task.GitHubPRNumber!.Value, cancellationToken);
            }
            catch (AppException exception) when (exception.StatusCode == StatusCodes.Status404NotFound)
            {
                skipped++;
                continue;
            }

            if (!BelongsToRepository(pull, project.GitHubOwner, project.GitHubRepoName))
            {
                skipped++;
                continue;
            }

            var status = GitHubPullRequestRules.MapStatus(pull.State, pull.Draft, pull.MergedAt);
            var url = string.IsNullOrWhiteSpace(pull.HtmlUrl) ? task.GitHubPRUrl : pull.HtmlUrl;
            if (!string.Equals(task.GitHubPRStatus, status, StringComparison.Ordinal) || !string.Equals(task.GitHubPRUrl, url, StringComparison.Ordinal))
            {
                task.GitHubPRStatus = status;
                task.GitHubPRUrl = url;
                task.LastSyncedAt = now;
                updated++;
            }
        }

        _activityLogService.Add(project.ProjectId, project.ProjectName, user.UserID, user.FullName, "PullRequestsSynced", $"{user.FullName} synced linked pull requests on project \"{project.ProjectName}\" ({updated} updated, {skipped} skipped).");
        await _context.SaveChangesAsync(cancellationToken);
        return new SyncPullRequestResultDto
        {
            Created = 0,
            Updated = updated,
            Skipped = skipped,
            PullRequests = await ListPullRequestsAsync(project, cancellationToken)
        };
    }

    public async Task<GitHubPullRequestDto> LinkPullRequestAsync(Guid userId, Guid taskId, string pullRequestUrl, CancellationToken cancellationToken)
    {
        var user = await GetActiveUserAsync(userId, cancellationToken);
        var task = await _context.Tasks.FirstOrDefaultAsync(item => item.TaskId == taskId && !item.IsDeleted, cancellationToken);
        if (task == null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "Task not found.");
        }

        var project = await _context.Projects.FirstOrDefaultAsync(item => item.ProjectId == task.ProjectId && !item.IsDeleted, cancellationToken);
        if (project == null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "Project not found.");
        }

        await EnsureCanLinkPullRequestAsync(user, project, task, cancellationToken);
        if (string.IsNullOrWhiteSpace(project.GitHubOwner) || string.IsNullOrWhiteSpace(project.GitHubRepoName))
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Link a GitHub repository to this project before linking a pull request.");
        }

        if (!GitHubPullRequestRules.TryParsePullRequestUrl(pullRequestUrl, out var owner, out var repository, out var number))
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Invalid GitHub pull request URL.");
        }

        if (!owner.Equals(project.GitHubOwner, StringComparison.OrdinalIgnoreCase)
            || !repository.Equals(project.GitHubRepoName, StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException(StatusCodes.Status400BadRequest, "This pull request does not belong to the project's GitHub repository.");
        }

        var pull = await ReadPullRequestAsync(project.GitHubOwner, project.GitHubRepoName, number, cancellationToken);
        if (!BelongsToRepository(pull, project.GitHubOwner, project.GitHubRepoName))
        {
            throw new AppException(StatusCodes.Status400BadRequest, "This pull request does not belong to the project's GitHub repository.");
        }

        var existing = await _context.Tasks
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                item => item.ProjectId == project.ProjectId && item.GitHubPRNumber == number && item.TaskId != task.TaskId,
                cancellationToken);
        if (existing != null)
        {
            throw new AppException(StatusCodes.Status409Conflict, "This pull request is already linked to another task.");
        }

        if (task.GitHubPRNumber is not null && task.GitHubPRNumber != number)
        {
            throw new AppException(StatusCodes.Status409Conflict, "This task already has a linked pull request.");
        }

        task.GitHubPRNumber = number;
        task.GitHubPRStatus = GitHubPullRequestRules.MapStatus(pull.State, pull.Draft, pull.MergedAt);
        task.GitHubPRUrl = string.IsNullOrWhiteSpace(pull.HtmlUrl)
            ? $"https://github.com/{project.GitHubOwner}/{project.GitHubRepoName}/pull/{number}"
            : pull.HtmlUrl;
        task.LastSyncedAt = DateTime.UtcNow;
        _activityLogService.Add(project.ProjectId, project.ProjectName, user.UserID, user.FullName, "PullRequestLinked", $"{user.FullName} linked pull request #{number} to task \"{task.Title}\".", task.TaskId, task.Title);
        await _context.SaveChangesAsync(cancellationToken);

        return new GitHubPullRequestDto
        {
            TaskId = task.TaskId,
            Number = number,
            Title = task.Title,
            Status = task.GitHubPRStatus ?? string.Empty,
            Draft = task.GitHubPRStatus == nameof(PullRequestStatus.Draft),
            Author = pull.User?.Login,
            Url = task.GitHubPRUrl,
            CreatedAt = pull.CreatedAt,
            UpdatedAt = pull.UpdatedAt,
            MergedAt = pull.MergedAt,
            LastSyncedAt = task.LastSyncedAt,
            AssignedToUserId = task.AssignedToUserId,
            Linked = true
        };
    }

    public async Task<GitHubPullRequestPageDto> GetPullRequestsAsync(Guid userId, Guid projectId, int page, int pageSize, string? status, CancellationToken cancellationToken = default)
    {
        var user = await GetActiveUserAsync(userId, cancellationToken);
        var project = await GetProjectAsync(user, projectId, membersCanAccess: true, cancellationToken);
        if (string.IsNullOrWhiteSpace(project.GitHubOwner) || string.IsNullOrWhiteSpace(project.GitHubRepoName))
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Link a GitHub repository before listing pull requests.");
        }

        var (normalizedPage, normalizedPageSize) = NormalizePage(page, pageSize);
        var gitHubState = ToGitHubState(status, out var statusFilter);
        var pulls = await ReadGitHubAsync<List<GitHubPullRequestApi>>(
            $"repos/{Uri.EscapeDataString(project.GitHubOwner)}/{Uri.EscapeDataString(project.GitHubRepoName)}/pulls?state={gitHubState}&per_page={normalizedPageSize}&page={normalizedPage}",
            "GitHub repository not found or it is private.",
            cancellationToken) ?? [];

        var numbers = pulls.Select(pull => pull.Number).ToArray();
        var linkedTasks = await _context.Tasks.AsNoTracking()
            .Where(task => task.ProjectId == project.ProjectId && task.GitHubPRNumber != null && numbers.Contains(task.GitHubPRNumber.Value))
            .Select(task => new
            {
                task.TaskId,
                task.GitHubPRNumber,
                task.AssignedToUserId,
                task.LastSyncedAt,
                AssignedToName = task.AssignedToUserId == null
                    ? null
                    : _context.Users.Where(assignee => assignee.UserID == task.AssignedToUserId).Select(assignee => assignee.FullName).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
        var linkedByNumber = linkedTasks.ToDictionary(task => task.GitHubPRNumber!.Value);

        var items = new List<GitHubPullRequestDto>();
        foreach (var pull in pulls)
        {
            if (!BelongsToRepository(pull, project.GitHubOwner, project.GitHubRepoName))
            {
                continue;
            }

            var mapped = GitHubPullRequestRules.MapStatus(pull.State, pull.Draft, pull.MergedAt);
            if (statusFilter != null && !string.Equals(mapped, statusFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            linkedByNumber.TryGetValue(pull.Number, out var linked);
            items.Add(new GitHubPullRequestDto
            {
                TaskId = linked?.TaskId,
                Number = pull.Number,
                Title = string.IsNullOrWhiteSpace(pull.Title) ? $"PR #{pull.Number}" : pull.Title,
                Status = mapped,
                Draft = pull.Draft || mapped == nameof(PullRequestStatus.Draft),
                Author = pull.User?.Login,
                Url = pull.HtmlUrl,
                CreatedAt = pull.CreatedAt,
                UpdatedAt = pull.UpdatedAt,
                MergedAt = pull.MergedAt,
                LastSyncedAt = linked?.LastSyncedAt,
                AssignedToUserId = linked?.AssignedToUserId,
                AssignedToName = linked?.AssignedToName,
                Linked = linked != null
            });
        }

        return new GitHubPullRequestPageDto
        {
            Items = items,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            HasNextPage = pulls.Count == normalizedPageSize
        };
    }

    public async Task<GitHubRepositoryPageDto> GetPublicRepositoriesAsync(string username, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        username = username?.Trim() ?? "";
        if (!Regex.IsMatch(username, GitHubPullRequestRules.UsernamePattern))
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Invalid GitHub username.");
        }

        var (normalizedPage, normalizedPageSize) = NormalizePage(page, pageSize);
        var repositories = await ReadGitHubAsync<List<GitHubRepositoryApi>>(
            $"users/{Uri.EscapeDataString(username)}/repos?type=owner&per_page={normalizedPageSize}&page={normalizedPage}",
            "GitHub account not found.",
            cancellationToken) ?? [];

        var items = repositories
            .Where(repo => repo.Private != true && !string.Equals(repo.Visibility, "private", StringComparison.OrdinalIgnoreCase))
            .Select(repo => new GitHubPublicRepositoryDto
            {
                Name = repo.Name,
                FullName = string.IsNullOrWhiteSpace(repo.FullName) ? $"{username}/{repo.Name}" : repo.FullName,
                Owner = repo.Owner?.Login ?? username,
                Url = repo.HtmlUrl,
                Description = repo.Description,
                Visibility = string.IsNullOrWhiteSpace(repo.Visibility) ? "public" : repo.Visibility,
                DefaultBranch = repo.DefaultBranch
            })
            .ToList();

        return new GitHubRepositoryPageDto
        {
            Items = items,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            HasNextPage = repositories.Count == normalizedPageSize
        };
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
                Draft = task.GitHubPRStatus == nameof(PullRequestStatus.Draft),
                Url = task.GitHubPRUrl,
                LastSyncedAt = task.LastSyncedAt,
                Linked = true,
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
                Draft = task.GitHubPRStatus == nameof(PullRequestStatus.Draft),
                Url = task.GitHubPRUrl,
                LastSyncedAt = task.LastSyncedAt,
                Linked = true,
                AssignedToUserId = task.AssignedToUserId,
                AssignedToName = task.AssignedToUserId == null
                    ? null
                    : _context.Users.Where(user => user.UserID == task.AssignedToUserId).Select(user => user.FullName).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<GitHubPullRequestApi> ReadPullRequestAsync(string owner, string repository, int number, CancellationToken cancellationToken)
    {
        return await ReadGitHubAsync<GitHubPullRequestApi>(
            $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repository)}/pulls/{number}",
            "GitHub pull request not found.",
            cancellationToken);
    }

    private static bool BelongsToRepository(GitHubPullRequestApi pull, string owner, string repository)
    {
        var repo = pull.Base?.Repo;
        if (repo?.Owner == null || string.IsNullOrWhiteSpace(repo.Name))
        {
            return true;
        }

        return repo.Owner.Login.Equals(owner, StringComparison.OrdinalIgnoreCase)
            && repo.Name.Equals(repository, StringComparison.OrdinalIgnoreCase);
    }

    private static Task EnsureCanLinkPullRequestAsync(User user, Projects project, Tasks task, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (user.RoleId == UserRole.Admin.ToId())
        {
            return Task.CompletedTask;
        }

        if (user.RoleId == UserRole.Manager.ToId())
        {
            if (project.AssignedToUserId != user.UserID)
            {
                throw new AppException(StatusCodes.Status403Forbidden, "You do not have permission for this project.");
            }

            return Task.CompletedTask;
        }

        if (user.RoleId == UserRole.Developer.ToId())
        {
            if (task.AssignedToUserId != user.UserID)
            {
                throw new AppException(StatusCodes.Status403Forbidden, "You can link a pull request only to a task assigned to you.");
            }

            return Task.CompletedTask;
        }

        throw new AppException(StatusCodes.Status403Forbidden, "You do not have permission to link a pull request.");
    }

    private static (int Page, int PageSize) NormalizePage(int page, int pageSize)
    {
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 30;
        }

        if (pageSize > 100)
        {
            pageSize = 100;
        }

        return (page, pageSize);
    }

    private static string ToGitHubState(string? status, out string? statusFilter)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            statusFilter = null;
            return "all";
        }

        if (!Enum.TryParse<PullRequestStatus>(status.Trim(), ignoreCase: true, out var parsed))
        {
            throw new AppException(StatusCodes.Status400BadRequest, "Invalid pull request status.");
        }

        statusFilter = parsed.ToString();
        return parsed is PullRequestStatus.Open or PullRequestStatus.Draft ? "open" : "closed";
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

        [JsonPropertyName("full_name")]
        public string? FullName { get; set; }

        [JsonPropertyName("private")]
        public bool? Private { get; set; }

        [JsonPropertyName("visibility")]
        public string? Visibility { get; set; }

        [JsonPropertyName("default_branch")]
        public string? DefaultBranch { get; set; }

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

        [JsonPropertyName("draft")]
        public bool Draft { get; set; }

        [JsonPropertyName("merged_at")]
        public DateTime? MergedAt { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("user")]
        public GitHubOwnerApi? User { get; set; }

        [JsonPropertyName("base")]
        public GitHubPullRequestBaseApi? Base { get; set; }
    }

    private sealed class GitHubPullRequestBaseApi
    {
        [JsonPropertyName("repo")]
        public GitHubRepositoryApi? Repo { get; set; }
    }
}
