using EBOSP.Contracts.Common;
using EBOSP.Contracts.Reporting;

namespace EBOSP.Application.Reporting;

/// <summary>Thin forwarding layer - kept for the same controller→service→repository consistency every other module follows, even though there's no domain logic to add here.</summary>
public sealed class ReportingService(IReportingRepository reports) : IReportingService
{
    public Task<SalesReportResponse> GetSalesReportAsync(Guid tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) =>
        reports.GetSalesReportAsync(tenantId, from, to, cancellationToken);

    public Task<InventoryReportResponse> GetInventoryReportAsync(Guid tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) =>
        reports.GetInventoryReportAsync(tenantId, from, to, cancellationToken);

    public Task<PagedResult<LowStockReportItem>> GetLowStockAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken) =>
        reports.GetLowStockAsync(tenantId, request, cancellationToken);

    public Task<PagedResult<SlowMovingInventoryItem>> GetSlowMovingInventoryAsync(Guid tenantId, int daysInactive, PagedRequest request, CancellationToken cancellationToken) =>
        reports.GetSlowMovingInventoryAsync(tenantId, daysInactive, request, cancellationToken);

    public Task<ProcurementSpendReportResponse> GetProcurementSpendReportAsync(Guid tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) =>
        reports.GetProcurementSpendReportAsync(tenantId, from, to, cancellationToken);

    public Task<PagedResult<OutstandingInvoiceItem>> GetOutstandingInvoicesAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken) =>
        reports.GetOutstandingInvoicesAsync(tenantId, request, cancellationToken);

    public Task<IReadOnlyList<TopProductItem>> GetTopProductsAsync(Guid tenantId, DateTimeOffset from, DateTimeOffset to, int top, CancellationToken cancellationToken) =>
        reports.GetTopProductsAsync(tenantId, from, to, top, cancellationToken);
}
