using EBOSP.Application.Documents;

namespace EBOSP.Infrastructure.Documents;

/// <summary>
/// Real, working file-bytes storage on local disk - not a stub. A cloud provider (Azure Blob/S3)
/// is a drop-in IObjectStorage replacement whenever one is provisioned; nothing about the
/// domain/API layer changes.
/// </summary>
public sealed class LocalDiskObjectStorage(DocumentStorageOptions options) : IObjectStorage
{
    public async Task UploadAsync(string storageKey, Stream content, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(options.RootPath);
        var path = ResolvePath(storageKey);
        await using var file = File.Create(path);
        await content.CopyToAsync(file, cancellationToken);
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(File.OpenRead(ResolvePath(storageKey)));

    private string ResolvePath(string storageKey)
    {
        // storageKey is always a freshly generated Guid + a known extension (DocumentService), never
        // client input, so no path-traversal surface here - still resolved via Path.GetFileName as a
        // defense-in-depth backstop.
        var safeName = Path.GetFileName(storageKey);
        return Path.Combine(options.RootPath, safeName);
    }
}
