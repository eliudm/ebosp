using EBOSP.Application.Sales;
using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Sales;

public sealed class SalesOrderRepository(AppDbContext context) : ISalesOrderRepository
{
    public async Task AddAsync(SalesOrder order, CancellationToken cancellationToken) =>
        await context.SalesOrders.AddAsync(order, cancellationToken);

    public Task<SalesOrder?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        context.SalesOrders.Include(o => o.Lines).SingleOrDefaultAsync(o => o.TenantId == tenantId && o.Id == id, cancellationToken);

    public async Task<PagedResult<SalesOrder>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var query = context.SalesOrders.Include(o => o.Lines).Where(o => o.TenantId == tenantId);
        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = request.SortDescending ? query.OrderByDescending(o => o.CreatedAt) : query.OrderBy(o => o.CreatedAt);
        var items = await ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);

        return new PagedResult<SalesOrder> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
    }

    public Task<bool> ExistsForQuotationAsync(Guid tenantId, Guid quotationId, CancellationToken cancellationToken) =>
        context.SalesOrders.AnyAsync(o => o.TenantId == tenantId && o.QuotationId == quotationId, cancellationToken);

    public Task<decimal> SumOpenTotalsForCustomerAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken) =>
        context.SalesOrders
            .Where(o => o.TenantId == tenantId && o.CustomerId == customerId && o.Status != SalesOrderStatus.Cancelled)
            .SumAsync(o => o.Total, cancellationToken);
}
