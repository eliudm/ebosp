namespace EBOSP.Contracts.Reporting;

public sealed record OutstandingInvoiceItem(Guid InvoiceId, Guid CustomerId, decimal Total, decimal PaidTotal, decimal Outstanding, DateTimeOffset CreatedAt);
