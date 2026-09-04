using EBOSP.Application.Billing;
using EBOSP.Application.Common;
using EBOSP.Application.Procurement;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Documents;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Documents;

/// <summary>
/// Validates and stores an uploaded file (spec §18). Extension/MIME/size/magic-byte checks all run
/// synchronously before the row (or the object-storage write) exists - no async "scan" stage, since
/// no scanning engine exists anywhere in this stack.
/// </summary>
public sealed class DocumentService(
    IDocumentRepository documents,
    IObjectStorage storage,
    IPurchaseOrderRepository purchaseOrders,
    IInvoiceRepository invoices,
    IDomainEventRecorder events,
    IUnitOfWork unitOfWork,
    IClock clock,
    DocumentOptions options) : IDocumentService
{
    public async Task<DocumentResponse> UploadAsync(
        Guid tenantId, Guid actingUserId, string entityType, string entityId, string fileName, string? contentType, Stream content, CancellationToken cancellationToken)
    {
        if (!DocumentEntityTypes.WritePermissionByEntityType.ContainsKey(entityType))
        {
            throw new ArgumentException($"Unsupported entity type '{entityType}'.", nameof(entityType));
        }

        await EnsureEntityOwnedAsync(tenantId, entityType, entityId, cancellationToken);

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("This file extension is not allowed.", nameof(fileName));
        }

        await using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > options.MaxSizeBytes)
            {
                throw new ArgumentException("File size exceeds the allowed limit.", nameof(content));
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        if (buffer.Length == 0)
        {
            throw new ArgumentException("The uploaded file is empty.", nameof(content));
        }

        var header = buffer.GetBuffer().AsSpan(0, (int)Math.Min(buffer.Length, 16));
        if (!FileSignatureValidator.Matches(extension, header))
        {
            throw new ArgumentException("File content does not match its extension.", nameof(content));
        }

        buffer.Position = 0;
        var storageKey = Guid.NewGuid().ToString("N") + extension;
        await storage.UploadAsync(storageKey, buffer, cancellationToken);

        var document = Document.Create(
            tenantId, entityType, entityId, fileName, contentType ?? "application/octet-stream", buffer.Length, storageKey, actingUserId, clock.UtcNow);
        await documents.AddAsync(document, cancellationToken);

        events.Record("DocumentUploaded", tenantId, entityType, entityId, new { document.FileName, document.SizeBytes }, actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(document);
    }

    public async Task<DocumentResponse> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        ToResponse(await GetOwnedAsync(tenantId, id, cancellationToken));

    public async Task<(DocumentResponse Metadata, Stream Content)> GetContentAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        var document = await GetOwnedAsync(tenantId, id, cancellationToken);
        var stream = await storage.OpenReadAsync(document.StorageKey, cancellationToken);
        return (ToResponse(document), stream);
    }

    public async Task<PagedResult<DocumentResponse>> ListForEntityAsync(Guid tenantId, string entityType, string entityId, PagedRequest request, CancellationToken cancellationToken)
    {
        var page = await documents.ListForEntityAsync(tenantId, entityType, entityId, request, cancellationToken);
        return new PagedResult<DocumentResponse>
        {
            Items = page.Items.Select(ToResponse).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    private async Task EnsureEntityOwnedAsync(Guid tenantId, string entityType, string entityId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(entityId, out var id))
        {
            throw new NotFoundException($"{entityType} not found.");
        }

        var found = entityType switch
        {
            DocumentEntityTypes.PurchaseOrder => await purchaseOrders.GetByIdAsync(tenantId, id, cancellationToken) is not null,
            DocumentEntityTypes.Invoice => await invoices.GetByIdAsync(tenantId, id, cancellationToken) is not null,
            _ => false,
        };

        if (!found)
        {
            throw new NotFoundException($"{entityType} not found.");
        }
    }

    private async Task<Document> GetOwnedAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        await documents.GetByIdAsync(tenantId, id, cancellationToken) ?? throw new NotFoundException("Document not found.");

    private static DocumentResponse ToResponse(Document document) => new(
        document.Id, document.EntityType, document.EntityId, document.FileName, document.ContentType, document.SizeBytes, document.UploadedByUserId, document.UploadedAt);
}
