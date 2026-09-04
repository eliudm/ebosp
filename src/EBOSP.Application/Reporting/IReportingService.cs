using EBOSP.Contracts.Common;
using EBOSP.Contracts.Reporting;

namespace EBOSP.Application.Reporting;

public interface IReportingService
{
    Task<SalesReportResponse> GetSalesReportAsync(Guid tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);

    Task<InventoryReportResponse> GetInventoryReportAsync(Guid tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);

    Task<PagedResult<LowStockReportItem>> GetLowStockAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);

    Task<PagedResult<SlowMovingInventoryItem>> GetSlowMovingInventoryAsync(Guid tenantId, int daysInactive, PagedRequest request, CancellationToken cancellationToken);

    Task<ProcurementSpendReportResponse> GetProcurementSpendReportAsync(Guid tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);

    Task<PagedResult<OutstandingInvoiceItem>> GetOutstandingInvoicesAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<TopProductItem>> GetTopProductsAsync(Guid tenantId, DateTimeOffset from, DateTimeOffset to, int top, CancellationToken cancellationToken);
}
