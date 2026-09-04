using EBOSP.Application.Documents;

namespace EBOSP.UnitTests.Documents;

public class FileSignatureValidatorTests
{
    [Fact]
    public void Matches_PdfWithCorrectSignature_ReturnsTrue()
    {
        byte[] header = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34];

        Assert.True(FileSignatureValidator.Matches(".pdf", header));
    }

    [Fact]
    public void Matches_PdfExtensionWithExecutableSignature_ReturnsFalse()
    {
        // MZ - the DOS/Windows executable header, renamed to claim it's a PDF.
        byte[] header = [0x4D, 0x5A, 0x90, 0x00];

        Assert.False(FileSignatureValidator.Matches(".pdf", header));
    }

    [Fact]
    public void Matches_ExtensionWithNoKnownSignature_ReturnsTrue()
    {
        byte[] header = [0x01, 0x02, 0x03];

        Assert.True(FileSignatureValidator.Matches(".txt", header));
    }

    [Fact]
    public void Matches_PngWithCorrectSignature_ReturnsTrue()
    {
        byte[] header = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        Assert.True(FileSignatureValidator.Matches(".png", header));
    }
}
