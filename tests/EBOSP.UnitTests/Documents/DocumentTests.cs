using EBOSP.Domain.Identity;

namespace EBOSP.UnitTests.Documents;

public class DocumentTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ValidInput_Succeeds()
    {
        var document = Document.Create(Guid.NewGuid(), "PurchaseOrder", Guid.NewGuid().ToString(), "invoice.pdf", "application/pdf", 1024, "storage-key", Guid.NewGuid(), Now);

        Assert.Equal("invoice.pdf", document.FileName);
        Assert.Equal(1024, document.SizeBytes);
    }

    [Fact]
    public void Create_ZeroSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Document.Create(
            Guid.NewGuid(), "PurchaseOrder", Guid.NewGuid().ToString(), "invoice.pdf", "application/pdf", 0, "storage-key", Guid.NewGuid(), Now));
    }

    [Fact]
    public void Create_BlankFileName_Throws()
    {
        Assert.Throws<ArgumentException>(() => Document.Create(
            Guid.NewGuid(), "PurchaseOrder", Guid.NewGuid().ToString(), " ", "application/pdf", 1024, "storage-key", Guid.NewGuid(), Now));
    }
}
