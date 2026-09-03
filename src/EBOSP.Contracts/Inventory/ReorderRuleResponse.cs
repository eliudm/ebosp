namespace EBOSP.Contracts.Inventory;

public sealed record ReorderRuleResponse(Guid Id, Guid WarehouseId, Guid ProductId, int ReorderLevel);
