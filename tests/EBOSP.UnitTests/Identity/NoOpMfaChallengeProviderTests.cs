using EBOSP.Domain.Identity;
using EBOSP.Application.Identity;

namespace EBOSP.UnitTests.Identity;

public class NoOpMfaChallengeProviderTests
{
    [Fact]
    public async Task IsChallengeRequiredAsync_AlwaysReturnsFalse()
    {
        var provider = new NoOpMfaChallengeProvider();
        var user = User.Register(Guid.NewGuid(), "person@example.com", "hash", null, UserStatus.Active, DateTimeOffset.UtcNow);

        var result = await provider.IsChallengeRequiredAsync(user, CancellationToken.None);

        Assert.False(result);
    }
}
