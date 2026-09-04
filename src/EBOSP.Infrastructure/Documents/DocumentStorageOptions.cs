namespace EBOSP.Infrastructure.Documents;

/// <summary>Infrastructure-only config (a filesystem path) - the Application layer only knows IObjectStorage, never where bytes physically live.</summary>
public sealed class DocumentStorageOptions
{
    public const string SectionName = "DocumentStorage";

    public string RootPath { get; init; } = "./data/documents";
}
