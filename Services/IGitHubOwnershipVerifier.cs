using DevTaskHubBackend.Entities;

namespace DevTaskHubBackend.Services;

/// <summary>
/// Extension point for real GitHub account ownership.
/// A public username lookup is not proof that the signed-in user owns that GitHub account.
/// Replace this with GitHub OAuth or a GitHub App before treating a profile as verified.
/// </summary>
public interface IGitHubOwnershipVerifier
{
    Task<bool> IsOwnershipVerifiedAsync(User user, CancellationToken cancellationToken);
}

public sealed class PublicProfileGitHubOwnershipVerifier : IGitHubOwnershipVerifier
{
    public Task<bool> IsOwnershipVerifiedAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }
}
