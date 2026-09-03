namespace EBOSP.Contracts.Inventory;

public sealed record GoodsReceiptLineResponse(Guid ProductId, int Quantity);

public sealed record GoodsReceiptResponse(
    Guid Id,
    Guid WarehouseId,
    Guid ReceivedByUserId,
    DateTimeOffset ReceivedAt,
    Guid? PurchaseOrderId,
    string? Reference,
    IReadOnlyList<GoodsReceiptLineResponse> Lines);
