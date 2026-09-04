using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Audit;
using EBOSP.Infrastructure.Outbox;

namespace EBOSP.IntegrationTests.Audit;

/// <summary>
/// OutboxMessage doesn't implement ITenantOwned (no automatic global query filter applies to it),
/// so AuditEventRepository's explicit tenantId filter is the only thing enforcing isolation here -
/// worth its own test, unlike every other module where the filter is a defense-in-depth backstop.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class AuditEventIsolationTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task ListAsync_ExcludesOtherTenantsEvents()
    {
        var tenantA = Tenant.Create("Audit Tenant A " + Guid.NewGuid());
        var tenantB = Tenant.Create("Audit Tenant B " + Guid.NewGuid());
        var messageA = NewOutboxMessage(tenantA.Id);
        var messageB = NewOutboxMessage(tenantB.Id);

        await using (var seedContext = fixture.CreateContext(new TestCurrentUserContext()))
        {
            seedContext.Tenants.AddRange(tenantA, tenantB);
            seedContext.OutboxMessages.AddRange(messageA, messageB);
            await seedContext.SaveChangesAsync();
        }

        await using var queryContext = fixture.CreateContext(new TestCurrentUserContext());
        var repository = new AuditEventRepository(queryContext);

        var pageA = await repository.ListAsync(tenantA.Id, null, null, null, null, null, null, new PagedRequest(), CancellationToken.None);
        var pageB = await repository.ListAsync(tenantB.Id, null, null, null, null, null, null, new PagedRequest(), CancellationToken.None);

        Assert.Contains(pageA.Items, e => e.Id == messageA.Id);
        Assert.DoesNotContain(pageA.Items, e => e.Id == messageB.Id);
        Assert.Contains(pageB.Items, e => e.Id == messageB.Id);
        Assert.DoesNotContain(pageB.Items, e => e.Id == messageA.Id);
    }

    private static OutboxMessage NewOutboxMessage(Guid tenantId) => new()
    {
        Id = Guid.NewGuid(),
        EventType = "TestEvent",
        TenantId = tenantId,
        CorrelationId = null,
        CausationId = null,
        ActorId = null,
        AggregateType = "Test",
        AggregateId = Guid.NewGuid().ToString(),
        Version = 1,
        Payload = "{}",
        OccurredAt = DateTimeOffset.UtcNow,
    };
}
