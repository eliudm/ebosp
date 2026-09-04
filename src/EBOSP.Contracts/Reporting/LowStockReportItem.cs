namespace EBOSP.Contracts.Reporting;

public sealed record LowStockReportItem(Guid WarehouseId, Guid ProductId, int QuantityOnHand, int ReorderLevel);
