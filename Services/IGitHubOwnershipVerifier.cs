using DevTaskHubBackend.Entities;

namespace DevTaskHubBackend.Services;

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
