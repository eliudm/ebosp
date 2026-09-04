using EBOSP.Contracts.Common;
using EBOSP.Contracts.Documents;

namespace EBOSP.Application.Documents;

public interface IDocumentService
{
    Task<DocumentResponse> UploadAsync(
        Guid tenantId, Guid actingUserId, string entityType, string entityId, string fileName, string? contentType, Stream content, CancellationToken cancellationToken);

    Task<DocumentResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<(DocumentResponse Metadata, Stream Content)> GetContentAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<DocumentResponse>> ListForEntityAsync(Guid tenantId, string entityType, string entityId, PagedRequest request, CancellationToken cancellationToken);
}
