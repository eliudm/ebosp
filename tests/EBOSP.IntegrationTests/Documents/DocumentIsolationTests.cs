using EBOSP.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.IntegrationTests.Documents;

/// <summary>Spec §12: "tenant isolation is a security boundary, not merely a UI filter" - applies just as much to documents as to every other module's tables.</summary>
[Collection(DatabaseCollection.Name)]
public class DocumentIsolationTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Documents_QueryFilter_ExcludesOtherTenants()
    {
        var tenantA = Tenant.Create("Doc Tenant A " + Guid.NewGuid());
        var tenantB = Tenant.Create("Doc Tenant B " + Guid.NewGuid());
        var documentA = Document.Create(tenantA.Id, "PurchaseOrder", Guid.NewGuid().ToString(), "a.pdf", "application/pdf", 10, "key-a", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var documentB = Document.Create(tenantB.Id, "PurchaseOrder", Guid.NewGuid().ToString(), "b.pdf", "application/pdf", 10, "key-b", Guid.NewGuid(), DateTimeOffset.UtcNow);

        await using (var seedContext = fixture.CreateContext(new TestCurrentUserContext()))
        {
            seedContext.Tenants.AddRange(tenantA, tenantB);
            seedContext.Documents.AddRange(documentA, documentB);
            await seedContext.SaveChangesAsync();
        }

        await using var scopedContext = fixture.CreateContext(new TestCurrentUserContext { TenantId = tenantA.Id });
        var visible = await scopedContext.Documents.Where(d => d.Id == documentA.Id || d.Id == documentB.Id).ToListAsync();

        Assert.Single(visible);
        Assert.Equal(documentA.Id, visible[0].Id);
    }
}
