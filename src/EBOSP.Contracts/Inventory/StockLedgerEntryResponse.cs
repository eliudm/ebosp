namespace EBOSP.Contracts.Inventory;

public sealed record StockLedgerEntryResponse(
    Guid Id,
    Guid WarehouseId,
    Guid ProductId,
    string EventType,
    int Quantity,
    Guid ActorId,
    DateTimeOffset OccurredAt,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Reason);
