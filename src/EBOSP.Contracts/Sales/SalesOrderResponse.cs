namespace EBOSP.Contracts.Sales;

public sealed record SalesOrderLineResponse(Guid ProductId, int Quantity, decimal UnitPrice);

public sealed record SalesOrderResponse(
    Guid Id,
    Guid QuotationId,
    Guid CustomerId,
    Guid BranchId,
    Guid WarehouseId,
    string Status,
    decimal Total,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<SalesOrderLineResponse> Lines);
