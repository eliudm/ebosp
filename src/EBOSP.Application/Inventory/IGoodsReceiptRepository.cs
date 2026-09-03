using EBOSP.Domain.Identity;

namespace EBOSP.Application.Inventory;

public interface IGoodsReceiptRepository
{
    Task AddAsync(GoodsReceipt receipt, CancellationToken cancellationToken);

    Task<bool> IdempotencyKeyExistsAsync(Guid tenantId, string idempotencyKey, CancellationToken cancellationToken);
}
