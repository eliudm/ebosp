using EBOSP.Application.Inventory;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Inventory;

public sealed class GoodsReceiptRepository(AppDbContext context) : IGoodsReceiptRepository
{
    public async Task AddAsync(GoodsReceipt receipt, CancellationToken cancellationToken) =>
        await context.GoodsReceipts.AddAsync(receipt, cancellationToken);

    public Task<bool> IdempotencyKeyExistsAsync(Guid tenantId, string idempotencyKey, CancellationToken cancellationToken) =>
        context.GoodsReceipts.AnyAsync(r => r.TenantId == tenantId && r.IdempotencyKey == idempotencyKey, cancellationToken);
}
