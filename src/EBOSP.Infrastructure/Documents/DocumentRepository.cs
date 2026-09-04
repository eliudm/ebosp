using EBOSP.Application.Documents;
using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Documents;

public sealed class DocumentRepository(AppDbContext context) : IDocumentRepository
{
    public async Task AddAsync(Document document, CancellationToken cancellationToken) =>
        await context.Documents.AddAsync(document, cancellationToken);

    public Task<Document?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        context.Documents.SingleOrDefaultAsync(d => d.TenantId == tenantId && d.Id == id, cancellationToken);

    public async Task<PagedResult<Document>> ListForEntityAsync(Guid tenantId, string entityType, string entityId, PagedRequest request, CancellationToken cancellationToken)
    {
        var query = context.Documents.Where(d => d.TenantId == tenantId && d.EntityType == entityType && d.EntityId == entityId);
        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = request.SortDescending ? query.OrderByDescending(d => d.UploadedAt) : query.OrderBy(d => d.UploadedAt);
        var items = await ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);

        return new PagedResult<Document> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
    }
}
