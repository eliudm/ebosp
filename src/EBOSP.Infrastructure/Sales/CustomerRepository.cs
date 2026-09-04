using EBOSP.Application.Sales;
using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Sales;

public sealed class CustomerRepository(AppDbContext context) : ICustomerRepository
{
    public async Task AddAsync(Customer customer, CancellationToken cancellationToken) =>
        await context.Customers.AddAsync(customer, cancellationToken);

    public Task<Customer?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        context.Customers.SingleOrDefaultAsync(c => c.TenantId == tenantId && c.Id == id, cancellationToken);

    public async Task<PagedResult<Customer>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var query = context.Customers.Where(c => c.TenantId == tenantId);
        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = request.SortDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name);
        var items = await ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);

        return new PagedResult<Customer> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
    }
}
