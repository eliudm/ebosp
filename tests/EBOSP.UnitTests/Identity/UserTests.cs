using EBOSP.Domain.Identity;

namespace EBOSP.UnitTests.Identity;

public class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static User NewUser() =>
        User.Register(Guid.NewGuid(), "person@example.com", "hash", primaryBranchId: null, UserStatus.Active, Now);

    [Fact]
    public void RecordFailedLogin_BelowThreshold_DoesNotLockOut()
    {
        var user = NewUser();

        for (var i = 0; i < User.MaxFailedLoginAttempts - 1; i++)
        {
            user.RecordFailedLogin(Now);
        }

        Assert.False(user.IsLockedOut(Now));
    }

    [Fact]
    public void RecordFailedLogin_AtThreshold_LocksOut()
    {
        // Spec §16: "Repeated login failures - 10 failures within configured window".
        var user = NewUser();

        for (var i = 0; i < User.MaxFailedLoginAttempts; i++)
        {
            user.RecordFailedLogin(Now);
        }

        Assert.True(user.IsLockedOut(Now));
        Assert.False(user.CanAuthenticate(Now));
    }

    [Fact]
    public void RecordSuccessfulLogin_ResetsFailedLoginState()
    {
        var user = NewUser();
        for (var i = 0; i < User.MaxFailedLoginAttempts; i++)
        {
            user.RecordFailedLogin(Now);
        }

        user.RecordSuccessfulLogin();

        Assert.False(user.IsLockedOut(Now));
        Assert.Equal(0, user.FailedLoginCount);
    }

    [Fact]
    public void CanAuthenticate_SuspendedUser_ReturnsFalse()
    {
        var user = NewUser();
        user.Suspend();

        Assert.False(user.CanAuthenticate(Now));
    }

    [Fact]
    public void Suspend_DisabledUser_Throws()
    {
        var user = NewUser();
        user.Disable();

        Assert.Throws<InvalidOperationException>(() => user.Suspend());
    }

    [Fact]
    public void Activate_DisabledUser_Throws()
    {
        var user = NewUser();
        user.Disable();

        Assert.Throws<InvalidOperationException>(() => user.Activate());
    }
}
