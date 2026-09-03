using EBOSP.Application.Inventory;
using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Inventory;

public sealed class StockLedgerEntryRepository(AppDbContext context) : IStockLedgerEntryRepository
{
    public async Task AddAsync(StockLedgerEntry entry, CancellationToken cancellationToken) =>
        await context.StockLedgerEntries.AddAsync(entry, cancellationToken);

    public Task<bool> IdempotencyKeyExistsAsync(Guid tenantId, string idempotencyKey, CancellationToken cancellationToken) =>
        context.StockLedgerEntries.AnyAsync(e => e.TenantId == tenantId && e.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<PagedResult<StockLedgerEntry>> ListAsync(Guid tenantId, Guid? warehouseId, Guid? productId, PagedRequest request, CancellationToken cancellationToken)
    {
        var query = context.StockLedgerEntries.Where(e => e.TenantId == tenantId);
        if (warehouseId is { } w)
        {
            query = query.Where(e => e.WarehouseId == w);
        }

        if (productId is { } p)
        {
            query = query.Where(e => e.ProductId == p);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var ordered = request.SortDescending ? query.OrderByDescending(e => e.OccurredAt) : query.OrderBy(e => e.OccurredAt);
        var items = await ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);

        return new PagedResult<StockLedgerEntry> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
    }
}
