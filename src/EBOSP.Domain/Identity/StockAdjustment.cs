using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

/// <summary>Manual stock correction (dev guide §13.1). Reason is required (spec §15: "inventory.adjust ... Reason required").</summary>
public sealed class StockAdjustment : Entity, ITenantOwned
{
    private StockAdjustment()
    {
    }

    public static StockAdjustment Create(
        Guid tenantId,
        Guid warehouseId,
        Guid productId,
        int quantityDelta,
        string reason,
        Guid actorId,
        DateTimeOffset occurredAt,
        string? idempotencyKey)
    {
        if (quantityDelta == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantityDelta), "Adjustment delta cannot be zero.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A reason is required for a stock adjustment.", nameof(reason));
        }

        return new StockAdjustment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            WarehouseId = warehouseId,
            ProductId = productId,
            QuantityDelta = quantityDelta,
            Reason = reason.Trim(),
            ActorId = actorId,
            OccurredAt = occurredAt,
            IdempotencyKey = idempotencyKey,
        };
    }

    public Guid TenantId { get; private init; }

    public Guid WarehouseId { get; private init; }

    public Guid ProductId { get; private init; }

    public int QuantityDelta { get; private init; }

    public string Reason { get; private init; } = null!;

    public Guid ActorId { get; private init; }

    public DateTimeOffset OccurredAt { get; private init; }

    public string? IdempotencyKey { get; private init; }
}
