using EBOSP.Contracts.Identity;

namespace EBOSP.Application.Identity;

public interface IUserService
{
    Task<UserResponse> CreateUserAsync(Guid tenantId, Guid actingUserId, CreateUserRequest request, CancellationToken cancellationToken);

    Task ActivateAsync(Guid tenantId, Guid actingUserId, Guid targetUserId, CancellationToken cancellationToken);

    Task SuspendAsync(Guid tenantId, Guid actingUserId, Guid targetUserId, CancellationToken cancellationToken);

    Task DisableAsync(Guid tenantId, Guid actingUserId, Guid targetUserId, CancellationToken cancellationToken);

    Task AssignRoleAsync(Guid tenantId, Guid actingUserId, Guid targetUserId, AssignRoleRequest request, CancellationToken cancellationToken);
}
