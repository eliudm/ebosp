using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

/// <summary>Header linking the outflow and inflow of one atomic transfer (dev guide §13.2: "auditable outflow and inflow linked by one transfer transaction").</summary>
public sealed class StockTransfer : Entity, ITenantOwned
{
    private StockTransfer()
    {
    }

    public static StockTransfer Create(
        Guid tenantId,
        Guid fromWarehouseId,
        Guid toWarehouseId,
        Guid productId,
        int quantity,
        string? reason,
        Guid actorId,
        DateTimeOffset occurredAt,
        string? idempotencyKey)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        if (fromWarehouseId == toWarehouseId)
        {
            throw new ArgumentException("A transfer must be between two different warehouses.", nameof(toWarehouseId));
        }

        return new StockTransfer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FromWarehouseId = fromWarehouseId,
            ToWarehouseId = toWarehouseId,
            ProductId = productId,
            Quantity = quantity,
            Reason = reason,
            ActorId = actorId,
            OccurredAt = occurredAt,
            IdempotencyKey = idempotencyKey,
        };
    }

    public Guid TenantId { get; private init; }

    public Guid FromWarehouseId { get; private init; }

    public Guid ToWarehouseId { get; private init; }

    public Guid ProductId { get; private init; }

    public int Quantity { get; private init; }

    public string? Reason { get; private init; }

    public Guid ActorId { get; private init; }

    public DateTimeOffset OccurredAt { get; private init; }

    public string? IdempotencyKey { get; private init; }
}
