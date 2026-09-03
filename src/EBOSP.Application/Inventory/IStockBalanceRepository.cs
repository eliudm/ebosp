using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Inventory;

public interface IStockBalanceRepository
{
    Task<StockBalance?> GetAsync(Guid tenantId, Guid warehouseId, Guid productId, CancellationToken cancellationToken);

    Task AddAsync(StockBalance balance, CancellationToken cancellationToken);

    Task<PagedResult<StockBalance>> ListAsync(Guid tenantId, Guid? warehouseId, Guid? productId, PagedRequest request, CancellationToken cancellationToken);

    /// <summary>Refreshes a tracked balance to its current database values after a concurrency conflict, so the caller can safely reapply its mutation and retry.</summary>
    Task ReloadAsync(StockBalance balance, CancellationToken cancellationToken);
}
