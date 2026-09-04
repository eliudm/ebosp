using EBOSP.Domain.Common;

namespace EBOSP.Domain.Identity;

/// <summary>
/// Attached file metadata (ERD: DocumentId, EntityType, EntityId, StorageKey; spec §18: "Store
/// metadata in the relational database and file bytes in object storage"). No status/lifecycle -
/// extension/MIME/size/signature validation happens synchronously before this row is ever created
/// (DocumentService.UploadAsync), so there is no "pending" state to model.
/// </summary>
public sealed class Document : Entity, ITenantOwned
{
    private Document()
    {
    }

    public static Document Create(
        Guid tenantId,
        string entityType,
        string entityId,
        string fileName,
        string contentType,
        long sizeBytes,
        string storageKey,
        Guid uploadedByUserId,
        DateTimeOffset uploadedAt)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new ArgumentException("Entity type is required.", nameof(entityType));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        if (sizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Size must be positive.");
        }

        return new Document
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EntityType = entityType,
            EntityId = entityId,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            StorageKey = storageKey,
            UploadedByUserId = uploadedByUserId,
            UploadedAt = uploadedAt,
        };
    }

    public Guid TenantId { get; private init; }

    public string EntityType { get; private init; } = null!;

    public string EntityId { get; private init; } = null!;

    public string FileName { get; private init; } = null!;

    public string ContentType { get; private init; } = null!;

    public long SizeBytes { get; private init; }

    public string StorageKey { get; private init; } = null!;

    public Guid UploadedByUserId { get; private init; }

    public DateTimeOffset UploadedAt { get; private init; }
}
