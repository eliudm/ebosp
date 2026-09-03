using EBOSP.Domain.Identity;

namespace EBOSP.Application.Inventory;

public interface IStockAdjustmentRepository
{
    Task AddAsync(StockAdjustment adjustment, CancellationToken cancellationToken);

    Task<bool> IdempotencyKeyExistsAsync(Guid tenantId, string idempotencyKey, CancellationToken cancellationToken);
}
