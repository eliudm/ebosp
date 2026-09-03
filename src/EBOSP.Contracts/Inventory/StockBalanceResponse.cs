namespace EBOSP.Contracts.Inventory;

public sealed record StockBalanceResponse(Guid WarehouseId, Guid ProductId, int QuantityOnHand, int QuantityReserved, int QuantityAvailable);
