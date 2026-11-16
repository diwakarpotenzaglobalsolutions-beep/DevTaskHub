using System.Text.Json.Serialization;

namespace DevTaskHubBackend.Models;

public class GitHubProfileDto
{
    [JsonPropertyName("id")]
    public long GitHubId { get; set; }

    [JsonPropertyName("login")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("html_url")]
    public string? ProfileUrl { get; set; }

    [JsonPropertyName("public_repos")]
    public int PublicRepositories { get; set; }

    [JsonPropertyName("bio")]
    public string? Bio { get; set; }

    [JsonPropertyName("ownershipVerified")]
    public bool OwnershipVerified { get; set; }

    [JsonPropertyName("ownershipVerification")]
    public string OwnershipVerification { get; set; } = "PublicProfileOnly";
}

public class LinkGitHubDto
{
    public string Username { get; set; } = string.Empty;
}

public class LinkedGitHubProfileDto
{
    public long? GitHubId { get; set; }

    public string? Username { get; set; }

    public string? ProfileUrl { get; set; }

    public string? AvatarUrl { get; set; }

    public bool OwnershipVerified { get; set; }

    public string OwnershipVerification { get; set; } = "PublicProfileOnly";
}

public class LinkRepositoryDto
{
    public Guid ProjectId { get; set; }

    public string Owner { get; set; } = string.Empty;

    public string RepositoryName { get; set; } = string.Empty;
}

public class GitHubRepositoryDto
{
    public Guid ProjectId { get; set; }

    public string Owner { get; set; } = string.Empty;

    public string RepositoryName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Url { get; set; }
}

public class LinkPullRequestDto
{
    public string PullRequestUrl { get; set; } = string.Empty;
}

public class GitHubPullRequestDto
{
    public Guid? TaskId { get; set; }

    public int Number { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public bool Draft { get; set; }

    public string? Author { get; set; }

    public string? Url { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? MergedAt { get; set; }

    public DateTime? LastSyncedAt { get; set; }

    public Guid? AssignedToUserId { get; set; }

    public string? AssignedToName { get; set; }

    public bool Linked { get; set; }
}

public class GitHubPullRequestPageDto
{
    public List<GitHubPullRequestDto> Items { get; set; } = [];

    public int Page { get; set; }

    public int PageSize { get; set; }

    public bool HasNextPage { get; set; }
}

public class GitHubPublicRepositoryDto
{
    public string Name { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Owner { get; set; } = string.Empty;

    public string? Url { get; set; }

    public string? Description { get; set; }

    public string Visibility { get; set; } = "public";

    public string? DefaultBranch { get; set; }
}

public class GitHubRepositoryPageDto
{
    public List<GitHubPublicRepositoryDto> Items { get; set; } = [];

    public int Page { get; set; }

    public int PageSize { get; set; }

    public bool HasNextPage { get; set; }
}

public class LivePullRequestDto
{
    public Guid ProjectId { get; set; }

    public int Number { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? Url { get; set; }
}

public class SyncPullRequestResultDto
{
    public int Created { get; set; }

    public int Updated { get; set; }

    public int Skipped { get; set; }

    public List<GitHubPullRequestDto> PullRequests { get; set; } = [];
}