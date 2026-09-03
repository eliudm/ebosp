using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Inventory;

public interface IStockLedgerEntryRepository
{
    Task AddAsync(StockLedgerEntry entry, CancellationToken cancellationToken);

    Task<bool> IdempotencyKeyExistsAsync(Guid tenantId, string idempotencyKey, CancellationToken cancellationToken);

    Task<PagedResult<StockLedgerEntry>> ListAsync(Guid tenantId, Guid? warehouseId, Guid? productId, PagedRequest request, CancellationToken cancellationToken);
}
