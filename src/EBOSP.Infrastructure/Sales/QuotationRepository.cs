using EBOSP.Application.Sales;
using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Sales;

public sealed class QuotationRepository(AppDbContext context) : IQuotationRepository
{
    public async Task AddAsync(Quotation quotation, CancellationToken cancellationToken) =>
        await context.Quotations.AddAsync(quotation, cancellationToken);

    public Task<Quotation?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        context.Quotations.Include(q => q.Lines).SingleOrDefaultAsync(q => q.TenantId == tenantId && q.Id == id, cancellationToken);

    public async Task<PagedResult<Quotation>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var query = context.Quotations.Include(q => q.Lines).Where(q => q.TenantId == tenantId);
        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = request.SortDescending ? query.OrderByDescending(q => q.Id) : query.OrderBy(q => q.Id);
        var items = await ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);

        return new PagedResult<Quotation> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
    }
}
