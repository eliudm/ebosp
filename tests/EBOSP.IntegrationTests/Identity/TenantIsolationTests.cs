using EBOSP.Application.Identity;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.IntegrationTests.Identity;

/// <summary>Spec §12: "tenant isolation is a security boundary, not merely a UI filter."</summary>
[Collection(DatabaseCollection.Name)]
public class TenantIsolationTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Users_QueryFilter_ExcludesOtherTenants()
    {
        var tenantA = Tenant.Create("Tenant A " + Guid.NewGuid());
        var tenantB = Tenant.Create("Tenant B " + Guid.NewGuid());
        var userA = User.Register(tenantA.Id, $"usera-{Guid.NewGuid():N}@example.com", "hash", null, UserStatus.Active, DateTimeOffset.UtcNow);
        var userB = User.Register(tenantB.Id, $"userb-{Guid.NewGuid():N}@example.com", "hash", null, UserStatus.Active, DateTimeOffset.UtcNow);

        await using (var seedContext = fixture.CreateContext(new TestCurrentUserContext()))
        {
            seedContext.Tenants.AddRange(tenantA, tenantB);
            seedContext.Users.AddRange(userA, userB);
            await seedContext.SaveChangesAsync();
        }

        await using var scopedContext = fixture.CreateContext(new TestCurrentUserContext { TenantId = tenantA.Id });
        var visibleUsers = await scopedContext.Users.Where(u => u.Id == userA.Id || u.Id == userB.Id).ToListAsync();

        Assert.Single(visibleUsers);
        Assert.Equal(userA.Id, visibleUsers[0].Id);
    }

    [Fact]
    public async Task Users_IgnoreQueryFilters_ReturnsAcrossTenants()
    {
        var tenantA = Tenant.Create("Tenant C " + Guid.NewGuid());
        var tenantB = Tenant.Create("Tenant D " + Guid.NewGuid());
        var userA = User.Register(tenantA.Id, $"userc-{Guid.NewGuid():N}@example.com", "hash", null, UserStatus.Active, DateTimeOffset.UtcNow);
        var userB = User.Register(tenantB.Id, $"userd-{Guid.NewGuid():N}@example.com", "hash", null, UserStatus.Active, DateTimeOffset.UtcNow);

        await using var context = fixture.CreateContext(new TestCurrentUserContext());
        context.Tenants.AddRange(tenantA, tenantB);
        context.Users.AddRange(userA, userB);
        await context.SaveChangesAsync();

        var visibleUsers = await context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Id == userA.Id || u.Id == userB.Id)
            .ToListAsync();

        Assert.Equal(2, visibleUsers.Count);
    }

    [Fact]
    public async Task GetPermissionCodesAsync_ForAssignedRole_ReturnsSeededPermissions()
    {
        var tenant = Tenant.Create("Tenant E " + Guid.NewGuid());
        var user = User.Register(tenant.Id, $"usere-{Guid.NewGuid():N}@example.com", "hash", null, UserStatus.Active, DateTimeOffset.UtcNow);

        await using (var seedContext = fixture.CreateContext(new TestCurrentUserContext()))
        {
            seedContext.Tenants.Add(tenant);
            seedContext.Users.Add(user);
            var auditorRole = await seedContext.Roles.SingleAsync(r => r.Code == RoleCodes.Auditor);
            seedContext.UserRoles.Add(UserRole.Create(tenant.Id, user.Id, auditorRole.Id, branchId: null));
            await seedContext.SaveChangesAsync();
        }

        await using var scopedContext = fixture.CreateContext(new TestCurrentUserContext { TenantId = tenant.Id });
        var repository = new RoleRepository(scopedContext);

        var codes = await repository.GetPermissionCodesAsync(tenant.Id, user.Id, CancellationToken.None);

        Assert.Contains(PermissionCodes.AuditRead, codes);
    }
}
