namespace EBOSP.Contracts.Procurement;

public sealed record PurchaseRequestLineResponse(Guid ProductId, int Quantity, decimal EstimatedUnitPrice);

public sealed record PurchaseRequestResponse(
    Guid Id,
    Guid RequestedByUserId,
    Guid BranchId,
    string Justification,
    decimal EstimatedValue,
    DateTimeOffset RequiredDate,
    string Status,
    IReadOnlyList<PurchaseRequestLineResponse> Lines);
