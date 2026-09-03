using EBOSP.Application.Inventory;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Inventory;

public sealed class StockTransferRepository(AppDbContext context) : IStockTransferRepository
{
    public async Task AddAsync(StockTransfer transfer, CancellationToken cancellationToken) =>
        await context.StockTransfers.AddAsync(transfer, cancellationToken);

    public Task<bool> IdempotencyKeyExistsAsync(Guid tenantId, string idempotencyKey, CancellationToken cancellationToken) =>
        context.StockTransfers.AnyAsync(t => t.TenantId == tenantId && t.IdempotencyKey == idempotencyKey, cancellationToken);
}
