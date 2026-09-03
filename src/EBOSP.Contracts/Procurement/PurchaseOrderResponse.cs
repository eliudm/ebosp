namespace EBOSP.Contracts.Procurement;

public sealed record PurchaseOrderLineResponse(Guid ProductId, int Quantity, decimal UnitPrice);

public sealed record PurchaseOrderResponse(
    Guid Id,
    Guid PurchaseRequestId,
    Guid SupplierId,
    string PoNumber,
    string Status,
    decimal Total,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<PurchaseOrderLineResponse> Lines);
