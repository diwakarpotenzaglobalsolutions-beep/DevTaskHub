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

public class GitHubPullRequestDto
{
    public Guid TaskId { get; set; }

    public int Number { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? Url { get; set; }

    public DateTime? LastSyncedAt { get; set; }

    public Guid? AssignedToUserId { get; set; }

    public string? AssignedToName { get; set; }
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

    public List<GitHubPullRequestDto> PullRequests { get; set; } = [];
}