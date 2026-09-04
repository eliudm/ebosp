namespace EBOSP.Contracts.Reporting;

public sealed record SalesReportResponse(
    DateTimeOffset From,
    DateTimeOffset To,
    int TotalOrders,
    decimal TotalOrderValue,
    int TotalInvoices,
    decimal TotalInvoiced,
    decimal TotalCollected,
    decimal TotalOutstanding);
