using EBOSP.Domain.Identity;

namespace EBOSP.UnitTests.MasterData;

public class BranchTests
{
    [Fact]
    public void Update_BlankName_Throws()
    {
        var branch = Branch.Create(Guid.NewGuid(), "HQ");

        Assert.Throws<ArgumentException>(() => branch.Update(" "));
    }

    [Fact]
    public void Deactivate_ThenActivate_RoundTrips()
    {
        var branch = Branch.Create(Guid.NewGuid(), "HQ");

        branch.Deactivate();
        Assert.Equal(BranchStatus.Inactive, branch.Status);

        branch.Activate();
        Assert.Equal(BranchStatus.Active, branch.Status);
    }
}
