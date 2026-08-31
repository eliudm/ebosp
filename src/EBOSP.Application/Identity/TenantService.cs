using EBOSP.Application.Common;
using EBOSP.Contracts.Identity;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Identity;

public sealed class TenantService(
    ITenantRepository tenants,
    IUserRepository users,
    IRoleRepository roles,
    IPasswordHasher passwordHasher,
    IDomainEventRecorder events,
    IUnitOfWork unitOfWork,
    IClock clock) : ITenantService
{
    public async Task<TenantResponse> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.AdminEmail.Trim().ToLowerInvariant();
        if (await users.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new ConflictException("A user with this email already exists.");
        }

        var tenant = Tenant.Create(request.TenantName);
        await tenants.AddAsync(tenant, cancellationToken);

        var passwordHash = passwordHasher.Hash(request.AdminPassword);
        var admin = User.Register(tenant.Id, normalizedEmail, passwordHash, primaryBranchId: null, UserStatus.Active, clock.UtcNow);
        await users.AddAsync(admin, cancellationToken);

        var adminRole = await roles.GetByCodeAsync(RoleCodes.TenantAdmin, cancellationToken)
                         ?? throw new InvalidOperationException("The tenant-admin role is missing from the seeded role catalogue.");
        await roles.AssignRoleAsync(UserRole.Create(tenant.Id, admin.Id, adminRole.Id, branchId: null), cancellationToken);

        events.Record("TenantCreated", tenant.Id, nameof(Tenant), tenant.Id.ToString(), new { tenant.Name }, actorId: admin.Id);
        events.Record("UserCreated", tenant.Id, nameof(User), admin.Id.ToString(), new { admin.Email }, actorId: admin.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new TenantResponse(tenant.Id, tenant.Name, admin.Id, admin.Email);
    }
}
