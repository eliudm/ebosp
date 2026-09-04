using EBOSP.Application.Sales;
using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Sales;

public sealed class DeliveryRepository(AppDbContext context) : IDeliveryRepository
{
    public async Task AddAsync(Delivery delivery, CancellationToken cancellationToken) =>
        await context.Deliveries.AddAsync(delivery, cancellationToken);

    public Task<Delivery?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        context.Deliveries.Include(d => d.Lines).SingleOrDefaultAsync(d => d.TenantId == tenantId && d.Id == id, cancellationToken);

    public async Task<PagedResult<Delivery>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var query = context.Deliveries.Include(d => d.Lines).Where(d => d.TenantId == tenantId);
        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = request.SortDescending ? query.OrderByDescending(d => d.DeliveredAt) : query.OrderBy(d => d.DeliveredAt);
        var items = await ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);

        return new PagedResult<Delivery> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
    }
}
