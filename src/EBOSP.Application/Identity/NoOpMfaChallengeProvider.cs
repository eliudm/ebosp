using EBOSP.Domain.Identity;

namespace EBOSP.Application.Identity;

public sealed class NoOpMfaChallengeProvider : IMfaChallengeProvider
{
    public Task<bool> IsChallengeRequiredAsync(User user, CancellationToken cancellationToken) => Task.FromResult(false);
}
