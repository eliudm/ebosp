using EBOSP.Contracts.Common;
using EBOSP.Contracts.Reporting;

namespace EBOSP.Application.Reporting;

/// <summary>
/// Read-only aggregations over data every prior module already owns - no new tables. All methods
/// are tenant-scoped (spec §19: "All report queries must enforce tenant scope").
/// </summary>
public interface IReportingRepository
{
    Task<SalesReportResponse> GetSalesReportAsync(Guid tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);

    Task<InventoryReportResponse> GetInventoryReportAsync(Guid tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);

    Task<PagedResult<LowStockReportItem>> GetLowStockAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);

    Task<PagedResult<SlowMovingInventoryItem>> GetSlowMovingInventoryAsync(Guid tenantId, int daysInactive, PagedRequest request, CancellationToken cancellationToken);

    Task<ProcurementSpendReportResponse> GetProcurementSpendReportAsync(Guid tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);

    Task<PagedResult<OutstandingInvoiceItem>> GetOutstandingInvoicesAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken);
}
