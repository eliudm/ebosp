namespace EBOSP.Contracts.Inventory;

public sealed record StockTransferResponse(Guid Id, Guid FromWarehouseId, Guid ToWarehouseId, Guid ProductId, int Quantity, Guid ActorId, DateTimeOffset OccurredAt);
