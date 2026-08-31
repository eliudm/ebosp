using EBOSP.Application.Common;
using EBOSP.Contracts.Identity;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Identity;

/// <summary>User lifecycle and role assignment (dev guide §11: create/activate/suspend/disable, role management).</summary>
public sealed class UserService(
    IUserRepository users,
    IRoleRepository roles,
    IPasswordHasher passwordHasher,
    IDomainEventRecorder events,
    IUnitOfWork unitOfWork,
    IClock clock) : IUserService
{
    public async Task<UserResponse> CreateUserAsync(Guid tenantId, Guid actingUserId, CreateUserRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await users.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new ConflictException("A user with this email already exists.");
        }

        var passwordHash = passwordHasher.Hash(request.Password);
        var user = User.Register(
            tenantId,
            normalizedEmail,
            passwordHash,
            request.PrimaryBranchId,
            UserStatus.PendingActivation,
            clock.UtcNow);

        await users.AddAsync(user, cancellationToken);
        events.Record("UserCreated", tenantId, nameof(User), user.Id.ToString(), new { user.Email }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(user);
    }

    public async Task ActivateAsync(Guid tenantId, Guid actingUserId, Guid targetUserId, CancellationToken cancellationToken)
    {
        var user = await GetOwnedUserAsync(tenantId, targetUserId, cancellationToken);
        user.Activate();
        events.Record("UserActivated", tenantId, nameof(User), user.Id.ToString(), new { }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SuspendAsync(Guid tenantId, Guid actingUserId, Guid targetUserId, CancellationToken cancellationToken)
    {
        if (actingUserId == targetUserId)
        {
            throw new ForbiddenOperationException("You cannot suspend your own account.");
        }

        var user = await GetOwnedUserAsync(tenantId, targetUserId, cancellationToken);
        user.Suspend();
        events.Record("UserSuspended", tenantId, nameof(User), user.Id.ToString(), new { }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DisableAsync(Guid tenantId, Guid actingUserId, Guid targetUserId, CancellationToken cancellationToken)
    {
        if (actingUserId == targetUserId)
        {
            throw new ForbiddenOperationException("You cannot disable your own account.");
        }

        var user = await GetOwnedUserAsync(tenantId, targetUserId, cancellationToken);
        user.Disable();
        events.Record("UserDisabled", tenantId, nameof(User), user.Id.ToString(), new { }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignRoleAsync(Guid tenantId, Guid actingUserId, Guid targetUserId, AssignRoleRequest request, CancellationToken cancellationToken)
    {
        var user = await GetOwnedUserAsync(tenantId, targetUserId, cancellationToken);
        var role = await roles.GetByCodeAsync(request.RoleCode, cancellationToken)
                   ?? throw new NotFoundException("Role not found.");

        var userRole = UserRole.Create(tenantId, user.Id, role.Id, request.BranchId);
        await roles.AssignRoleAsync(userRole, cancellationToken);

        events.Record("RoleAssigned", tenantId, nameof(User), user.Id.ToString(), new { role.Code }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> GetOwnedUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken) =>
        await users.GetByIdAsync(tenantId, userId, cancellationToken) ?? throw new NotFoundException("User not found.");

    private static UserResponse ToResponse(User user) => new(user.Id, user.Email, user.Status.ToString(), user.PrimaryBranchId);
}
