namespace EBOSP.Domain.Common;

/// <summary>
/// Marker for every tenant-owned entity (spec §13: "Every tenant-owned table must carry TenantId").
/// <see cref="EBOSP.Infrastructure.Persistence.AppDbContext"/> applies a single global query filter
/// to every entity implementing this interface, so tenant isolation is enforced in the data layer
/// rather than left to each query author (spec §12: "not merely a UI filter").
/// </summary>
public interface ITenantOwned
{
    Guid TenantId { get; }
}
