namespace EBOSP.Contracts.Sales;

public sealed record QuotationLineResponse(Guid ProductId, int Quantity, decimal UnitPrice);

public sealed record QuotationResponse(
    Guid Id,
    Guid CustomerId,
    Guid BranchId,
    decimal Total,
    string Status,
    IReadOnlyList<QuotationLineResponse> Lines);
