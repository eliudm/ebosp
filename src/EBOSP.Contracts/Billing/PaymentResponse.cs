namespace EBOSP.Contracts.Billing;

public sealed record PaymentResponse(
    Guid Id,
    Guid InvoiceId,
    decimal Amount,
    string Method,
    string Status,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    Guid? ConfirmedByUserId,
    DateTimeOffset? ConfirmedAt);
