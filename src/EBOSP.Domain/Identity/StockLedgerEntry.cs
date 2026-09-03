using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

public enum StockLedgerEventType
{
    Received,
    Issued,
    Adjusted,
    TransferOut,
    TransferIn,
}

/// <summary>
/// The authoritative, append-only history of stock movements (dev guide §13.2: "Never overwrite
/// historical stock movements"; spec §10.3: "the authoritative history of stock movements").
/// Factory-only - no Update method exists, deliberately.
/// </summary>
public sealed class StockLedgerEntry : Entity, ITenantOwned
{
    private StockLedgerEntry()
    {
    }

    public static StockLedgerEntry Create(
        Guid tenantId,
        Guid warehouseId,
        Guid productId,
        StockLedgerEventType eventType,
        int quantity,
        Guid actorId,
        DateTimeOffset occurredAt,
        string? referenceType,
        Guid? referenceId,
        string? reason,
        string? idempotencyKey) => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            WarehouseId = warehouseId,
            ProductId = productId,
            EventType = eventType,
            Quantity = quantity,
            ActorId = actorId,
            OccurredAt = occurredAt,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Reason = reason,
            IdempotencyKey = idempotencyKey,
        };

    public Guid TenantId { get; private init; }

    public Guid WarehouseId { get; private init; }

    public Guid ProductId { get; private init; }

    public StockLedgerEventType EventType { get; private init; }

    /// <summary>Signed - positive for increases (Received, TransferIn), negative for decreases (Issued, TransferOut); Adjusted carries whatever sign the correction was.</summary>
    public int Quantity { get; private init; }

    public Guid ActorId { get; private init; }

    public DateTimeOffset OccurredAt { get; private init; }

    /// <summary>Nameof the header entity that caused this entry (GoodsReceipt/StockAdjustment/StockTransfer) - null for a plain issue, which has no header entity.</summary>
    public string? ReferenceType { get; private init; }

    public Guid? ReferenceId { get; private init; }

    public string? Reason { get; private init; }

    public string? IdempotencyKey { get; private init; }
}
