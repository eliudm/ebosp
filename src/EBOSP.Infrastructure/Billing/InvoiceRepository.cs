using EBOSP.Application.Billing;
using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Billing;

public sealed class InvoiceRepository(AppDbContext context) : IInvoiceRepository
{
    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken) =>
        await context.Invoices.AddAsync(invoice, cancellationToken);

    public Task<Invoice?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        context.Invoices.SingleOrDefaultAsync(i => i.TenantId == tenantId && i.Id == id, cancellationToken);

    public async Task<PagedResult<Invoice>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var query = context.Invoices.Where(i => i.TenantId == tenantId);
        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = request.SortDescending ? query.OrderByDescending(i => i.CreatedAt) : query.OrderBy(i => i.CreatedAt);
        var items = await ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);

        return new PagedResult<Invoice> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
    }

    public Task<bool> ExistsForSalesOrderAsync(Guid tenantId, Guid salesOrderId, CancellationToken cancellationToken) =>
        context.Invoices.AnyAsync(i => i.TenantId == tenantId && i.SalesOrderId == salesOrderId, cancellationToken);
}
