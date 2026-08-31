using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

public enum UserStatus
{
    PendingActivation,
    Active,
    Suspended,
    Disabled,
}

public sealed class User : Entity, ITenantOwned
{
    /// <summary>Spec §16 anomaly rule: "Repeated login failures - 10 failures within configured window".</summary>
    public const int MaxFailedLoginAttempts = 10;

    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private User()
    {
    }

    public static User Register(
        Guid tenantId,
        string email,
        string passwordHash,
        Guid? primaryBranchId,
        UserStatus status,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        return new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            PrimaryBranchId = primaryBranchId,
            Status = status,
            CreatedAt = now,
        };
    }

    public Guid TenantId { get; private init; }

    public string Email { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public Guid? PrimaryBranchId { get; private set; }

    public UserStatus Status { get; private set; }

    public int FailedLoginCount { get; private set; }

    public DateTimeOffset? LockedUntil { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    public bool IsLockedOut(DateTimeOffset now) => LockedUntil is { } until && until > now;

    public bool CanAuthenticate(DateTimeOffset now) => Status == UserStatus.Active && !IsLockedOut(now);

    public void RecordFailedLogin(DateTimeOffset now)
    {
        FailedLoginCount++;
        if (FailedLoginCount >= MaxFailedLoginAttempts)
        {
            LockedUntil = now.Add(LockoutDuration);
        }
    }

    public void RecordSuccessfulLogin()
    {
        FailedLoginCount = 0;
        LockedUntil = null;
    }

    public void ChangePasswordHash(string passwordHash) => PasswordHash = passwordHash;

    public void Activate()
    {
        if (Status == UserStatus.Disabled)
        {
            throw new InvalidOperationException("A disabled user cannot be reactivated.");
        }

        Status = UserStatus.Active;
    }

    public void Suspend()
    {
        if (Status == UserStatus.Disabled)
        {
            throw new InvalidOperationException("A disabled user cannot be suspended.");
        }

        Status = UserStatus.Suspended;
    }

    public void Disable() => Status = UserStatus.Disabled;
}
