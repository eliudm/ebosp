using EBOSP.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.IntegrationTests.Notifications;

/// <summary>Spec §12: "tenant isolation is a security boundary, not merely a UI filter" - applies just as much to notifications as to every other module's tables.</summary>
[Collection(DatabaseCollection.Name)]
public class NotificationIsolationTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Notifications_QueryFilter_ExcludesOtherTenants()
    {
        var tenantA = Tenant.Create("Notif Tenant A " + Guid.NewGuid());
        var tenantB = Tenant.Create("Notif Tenant B " + Guid.NewGuid());
        var notificationA = Notification.Create(tenantA.Id, Guid.NewGuid(), "Title A", null, null, null, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var notificationB = Notification.Create(tenantB.Id, Guid.NewGuid(), "Title B", null, null, null, Guid.NewGuid(), DateTimeOffset.UtcNow);

        await using (var seedContext = fixture.CreateContext(new TestCurrentUserContext()))
        {
            seedContext.Tenants.AddRange(tenantA, tenantB);
            seedContext.Notifications.AddRange(notificationA, notificationB);
            await seedContext.SaveChangesAsync();
        }

        await using var scopedContext = fixture.CreateContext(new TestCurrentUserContext { TenantId = tenantA.Id });
        var visible = await scopedContext.Notifications.Where(n => n.Id == notificationA.Id || n.Id == notificationB.Id).ToListAsync();

        Assert.Single(visible);
        Assert.Equal(notificationA.Id, visible[0].Id);
    }
}
