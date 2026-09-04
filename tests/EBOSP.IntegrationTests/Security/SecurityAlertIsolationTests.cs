using EBOSP.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.IntegrationTests.Security;

/// <summary>Spec §12: "tenant isolation is a security boundary, not merely a UI filter" - applies just as much to security alerts as to every other module's tables.</summary>
[Collection(DatabaseCollection.Name)]
public class SecurityAlertIsolationTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task SecurityAlerts_QueryFilter_ExcludesOtherTenants()
    {
        var tenantA = Tenant.Create("Security Tenant A " + Guid.NewGuid());
        var tenantB = Tenant.Create("Security Tenant B " + Guid.NewGuid());
        var alertA = SecurityAlert.Raise(tenantA.Id, "RepeatedLoginFailures", SecurityAlertSeverity.High, "desc", DateTimeOffset.UtcNow);
        var alertB = SecurityAlert.Raise(tenantB.Id, "RepeatedLoginFailures", SecurityAlertSeverity.High, "desc", DateTimeOffset.UtcNow);

        await using (var seedContext = fixture.CreateContext(new TestCurrentUserContext()))
        {
            seedContext.Tenants.AddRange(tenantA, tenantB);
            seedContext.SecurityAlerts.AddRange(alertA, alertB);
            await seedContext.SaveChangesAsync();
        }

        await using var scopedContext = fixture.CreateContext(new TestCurrentUserContext { TenantId = tenantA.Id });
        var visible = await scopedContext.SecurityAlerts.Where(a => a.Id == alertA.Id || a.Id == alertB.Id).ToListAsync();

        Assert.Single(visible);
        Assert.Equal(alertA.Id, visible[0].Id);
    }
}
