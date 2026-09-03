using EBOSP.Application.Inventory;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Inventory;

public sealed class StockAdjustmentRepository(AppDbContext context) : IStockAdjustmentRepository
{
    public async Task AddAsync(StockAdjustment adjustment, CancellationToken cancellationToken) =>
        await context.StockAdjustments.AddAsync(adjustment, cancellationToken);

    public Task<bool> IdempotencyKeyExistsAsync(Guid tenantId, string idempotencyKey, CancellationToken cancellationToken) =>
        context.StockAdjustments.AnyAsync(a => a.TenantId == tenantId && a.IdempotencyKey == idempotencyKey, cancellationToken);
}
