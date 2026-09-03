namespace EBOSP.Contracts.Inventory;

public sealed record StockAdjustmentResponse(Guid Id, Guid WarehouseId, Guid ProductId, int QuantityDelta, string Reason, Guid ActorId, DateTimeOffset OccurredAt);
