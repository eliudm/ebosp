namespace EBOSP.Application.Documents;

/// <summary>
/// File-bytes storage, separate from the metadata row (spec §18: "Store metadata in the relational
/// database and file bytes in object storage"). Local disk is a real, working implementation, not a
/// stub - a cloud provider (Azure Blob/S3) is a drop-in replacement whenever one is provisioned.
/// </summary>
public interface IObjectStorage
{
    Task UploadAsync(string storageKey, Stream content, CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
}
