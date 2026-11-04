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