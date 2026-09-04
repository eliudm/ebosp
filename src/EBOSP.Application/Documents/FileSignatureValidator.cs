namespace EBOSP.Application.Documents;

/// <summary>
/// Magic-byte signature check against the claimed extension (spec §18: "Restrict extensions and
/// MIME types according to business need") - rejects e.g. an executable renamed to .pdf. Covers
/// only the extensions DocumentOptions allows by default; an extension with no known signature
/// (.csv, .txt - plain text has no reliable magic number) is accepted on extension/MIME checks
/// alone.
/// </summary>
public static class FileSignatureValidator
{
    private static readonly IReadOnlyDictionary<string, byte[][]> SignaturesByExtension = new Dictionary<string, byte[][]>
    {
        [".pdf"] = [[0x25, 0x50, 0x44, 0x46]], // %PDF
        [".png"] = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
        [".jpg"] = [[0xFF, 0xD8, 0xFF]],
        [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
        [".docx"] = [[0x50, 0x4B, 0x03, 0x04]], // PK.. (zip-based Office format)
        [".xlsx"] = [[0x50, 0x4B, 0x03, 0x04]],
    };

    public static bool Matches(string extension, ReadOnlySpan<byte> header)
    {
        if (!SignaturesByExtension.TryGetValue(extension.ToLowerInvariant(), out var candidates))
        {
            // No known signature for this extension (e.g. plain text) - nothing to check against.
            return true;
        }

        foreach (var signature in candidates)
        {
            if (header.Length >= signature.Length && header[..signature.Length].SequenceEqual(signature))
            {
                return true;
            }
        }

        return false;
    }
}
