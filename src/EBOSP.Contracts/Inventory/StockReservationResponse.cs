namespace EBOSP.Contracts.Inventory;

public sealed record StockReservationResponse(Guid Id, Guid WarehouseId, Guid ProductId, int Quantity, string Status, DateTimeOffset CreatedAt, DateTimeOffset? ReleasedAt);
