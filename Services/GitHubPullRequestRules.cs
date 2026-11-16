using System.Text.RegularExpressions;
using DevTaskHubBackend.Data;
using DevTaskHubBackend.Exceptions;

namespace DevTaskHubBackend.Services;

public static class GitHubPullRequestRules
{
    public const string UsernamePattern = @"^(?!-)[a-zA-Z0-9-]{1,39}(?<!-)$";
    public const string RepositoryPattern = @"^[a-zA-Z0-9._-]{1,100}$";

    public static bool TryParsePullRequestUrl(string? value, out string owner, out string repository, out int number)
    {
        owner = string.Empty;
        repository = string.Empty;
        number = 0;

        if (string.IsNullOrWhiteSpace(value) || !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(uri.UserInfo))
        {
            return false;
        }

        var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4
            || !parts[2].Equals("pull", StringComparison.OrdinalIgnoreCase)
            || !Regex.IsMatch(parts[0], UsernamePattern)
            || !Regex.IsMatch(parts[1], RepositoryPattern)
            || !int.TryParse(parts[3], out number)
            || number <= 0)
        {
            return false;
        }

        owner = parts[0];
        repository = parts[1];
        return true;
    }

    public static string MapStatus(string? state, bool draft, DateTime? mergedAt)
    {
        if (mergedAt != null)
        {
            return nameof(PullRequestStatus.Merged);
        }

        if (string.Equals(state, "closed", StringComparison.OrdinalIgnoreCase))
        {
            return nameof(PullRequestStatus.Closed);
        }

        if (string.Equals(state, "open", StringComparison.OrdinalIgnoreCase))
        {
            return draft ? nameof(PullRequestStatus.Draft) : nameof(PullRequestStatus.Open);
        }

        throw new AppException(StatusCodes.Status502BadGateway, "GitHub returned an unknown pull request state.");
    }
}
