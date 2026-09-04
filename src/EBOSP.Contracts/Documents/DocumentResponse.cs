namespace EBOSP.Contracts.Documents;

public sealed record DocumentResponse(
    Guid Id,
    string EntityType,
    string EntityId,
    string FileName,
    string ContentType,
    long SizeBytes,
    Guid UploadedByUserId,
    DateTimeOffset UploadedAt);
