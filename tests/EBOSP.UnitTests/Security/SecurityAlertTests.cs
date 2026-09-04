using EBOSP.Domain.Identity;

namespace EBOSP.UnitTests.Security;

public class SecurityAlertTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static SecurityAlert NewAlert() =>
        SecurityAlert.Raise(Guid.NewGuid(), "RepeatedLoginFailures", SecurityAlertSeverity.High, "10 failed login attempts.", Now);

    [Fact]
    public void Raise_StartsOpen()
    {
        var alert = NewAlert();

        Assert.Equal(SecurityAlertStatus.Open, alert.Status);
    }

    [Fact]
    public void Raise_BlankRule_Throws()
    {
        Assert.Throws<ArgumentException>(() => SecurityAlert.Raise(Guid.NewGuid(), " ", SecurityAlertSeverity.Low, "desc", Now));
    }

    [Fact]
    public void Acknowledge_OpenAlert_TransitionsToAcknowledged()
    {
        var alert = NewAlert();
        var officer = Guid.NewGuid();

        alert.Acknowledge(officer, Now);

        Assert.Equal(SecurityAlertStatus.Acknowledged, alert.Status);
        Assert.Equal(officer, alert.AcknowledgedByUserId);
    }

    [Fact]
    public void Acknowledge_AlreadyAcknowledged_Throws()
    {
        var alert = NewAlert();
        alert.Acknowledge(Guid.NewGuid(), Now);

        Assert.Throws<InvalidOperationException>(() => alert.Acknowledge(Guid.NewGuid(), Now));
    }

    [Fact]
    public void Investigate_AcknowledgedAlert_TransitionsToInvestigating()
    {
        var alert = NewAlert();
        alert.Acknowledge(Guid.NewGuid(), Now);

        alert.Investigate();

        Assert.Equal(SecurityAlertStatus.Investigating, alert.Status);
    }

    [Fact]
    public void Investigate_StillOpen_Throws()
    {
        var alert = NewAlert();

        Assert.Throws<InvalidOperationException>(() => alert.Investigate());
    }

    [Fact]
    public void Resolve_FromOpen_TransitionsToResolved()
    {
        var alert = NewAlert();

        alert.Resolve(Guid.NewGuid(), Now, "Handled directly.");

        Assert.Equal(SecurityAlertStatus.Resolved, alert.Status);
        Assert.Equal("Handled directly.", alert.ResolutionNotes);
    }

    [Fact]
    public void Resolve_FromInvestigating_TransitionsToResolved()
    {
        var alert = NewAlert();
        alert.Acknowledge(Guid.NewGuid(), Now);
        alert.Investigate();

        alert.Resolve(Guid.NewGuid(), Now, "Confirmed benign.");

        Assert.Equal(SecurityAlertStatus.Resolved, alert.Status);
    }

    [Fact]
    public void Resolve_AlreadyResolved_Throws()
    {
        var alert = NewAlert();
        alert.Resolve(Guid.NewGuid(), Now, "Done.");

        Assert.Throws<InvalidOperationException>(() => alert.Resolve(Guid.NewGuid(), Now, "Again."));
    }

    [Fact]
    public void Resolve_BlankNotes_Throws()
    {
        var alert = NewAlert();

        Assert.Throws<ArgumentException>(() => alert.Resolve(Guid.NewGuid(), Now, " "));
    }

    [Fact]
    public void MarkFalsePositive_FromOpen_TransitionsToFalsePositive()
    {
        var alert = NewAlert();

        alert.MarkFalsePositive(Guid.NewGuid(), Now, "Known automated test account.");

        Assert.Equal(SecurityAlertStatus.FalsePositive, alert.Status);
    }

    [Fact]
    public void MarkFalsePositive_AlreadyClosed_Throws()
    {
        var alert = NewAlert();
        alert.MarkFalsePositive(Guid.NewGuid(), Now, "Not real.");

        Assert.Throws<InvalidOperationException>(() => alert.MarkFalsePositive(Guid.NewGuid(), Now, "Again."));
    }
}
