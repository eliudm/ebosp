using EBOSP.Contracts.Common;
using EBOSP.Contracts.Inventory;

namespace EBOSP.Application.Inventory;

public interface IInventoryService
{
    Task<GoodsReceiptResponse> ReceiveAsync(Guid tenantId, Guid actingUserId, ReceiveStockRequest request, CancellationToken cancellationToken);

    Task<StockLedgerEntryResponse> IssueAsync(Guid tenantId, Guid actingUserId, IssueStockRequest request, CancellationToken cancellationToken);

    Task<StockAdjustmentResponse> AdjustAsync(Guid tenantId, Guid actingUserId, AdjustStockRequest request, CancellationToken cancellationToken);

    Task<StockTransferResponse> TransferAsync(Guid tenantId, Guid actingUserId, TransferStockRequest request, CancellationToken cancellationToken);

    Task<StockReservationResponse> ReserveAsync(Guid tenantId, Guid actingUserId, ReserveStockRequest request, CancellationToken cancellationToken);

    Task ReleaseReservationAsync(Guid tenantId, Guid reservationId, CancellationToken cancellationToken);

    Task<PagedResult<StockBalanceResponse>> ListBalancesAsync(Guid tenantId, Guid? warehouseId, Guid? productId, PagedRequest request, CancellationToken cancellationToken);

    Task<PagedResult<StockLedgerEntryResponse>> ListLedgerAsync(Guid tenantId, Guid? warehouseId, Guid? productId, PagedRequest request, CancellationToken cancellationToken);
}
