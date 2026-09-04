namespace EBOSP.Contracts.Billing;

public sealed record InvoiceResponse(
    Guid Id,
    Guid SalesOrderId,
    Guid CustomerId,
    decimal Total,
    string Status,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt);
