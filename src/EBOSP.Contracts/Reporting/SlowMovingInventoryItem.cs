namespace EBOSP.Contracts.Reporting;

public sealed record SlowMovingInventoryItem(Guid WarehouseId, Guid ProductId, int QuantityOnHand, DateTimeOffset? LastMovementAt);
