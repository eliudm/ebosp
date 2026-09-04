namespace EBOSP.Contracts.Reporting;

public sealed record ProcurementSpendBySupplier(Guid SupplierId, string SupplierName, decimal Total);

public sealed record ProcurementSpendReportResponse(
    DateTimeOffset From,
    DateTimeOffset To,
    int TotalPurchaseOrders,
    decimal TotalSpend,
    IReadOnlyList<ProcurementSpendBySupplier> BySupplier);
