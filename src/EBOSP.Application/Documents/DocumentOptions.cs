namespace EBOSP.Application.Documents;

/// <summary>Bound from the "Documents" configuration section - restricts extensions/size per spec §18, not hardcoded (dev guide §47).</summary>
public sealed class DocumentOptions
{
    public const string SectionName = "Documents";

    public long MaxSizeBytes { get; init; } = 10 * 1024 * 1024;

    public IReadOnlyCollection<string> AllowedExtensions { get; init; } = [".pdf", ".png", ".jpg", ".jpeg", ".docx", ".xlsx", ".csv", ".txt"];
}
