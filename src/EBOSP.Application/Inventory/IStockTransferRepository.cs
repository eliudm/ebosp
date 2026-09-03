using EBOSP.Domain.Identity;

namespace EBOSP.Application.Inventory;

public interface IStockTransferRepository
{
    Task AddAsync(StockTransfer transfer, CancellationToken cancellationToken);

    Task<bool> IdempotencyKeyExistsAsync(Guid tenantId, string idempotencyKey, CancellationToken cancellationToken);
}
