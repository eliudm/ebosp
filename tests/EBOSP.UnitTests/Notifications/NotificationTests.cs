using EBOSP.Domain.Identity;

namespace EBOSP.UnitTests.Notifications;

public class NotificationTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static Notification NewNotification() =>
        Notification.Create(Guid.NewGuid(), Guid.NewGuid(), "Title", "Body", null, null, Guid.NewGuid(), Now);

    [Fact]
    public void Create_StartsUnread()
    {
        var notification = NewNotification();

        Assert.Null(notification.ReadAt);
    }

    [Fact]
    public void Create_BlankTitle_Throws()
    {
        Assert.Throws<ArgumentException>(() => Notification.Create(Guid.NewGuid(), Guid.NewGuid(), " ", null, null, null, Guid.NewGuid(), Now));
    }

    [Fact]
    public void MarkRead_Unread_SetsReadAt()
    {
        var notification = NewNotification();

        notification.MarkRead(Now);

        Assert.Equal(Now, notification.ReadAt);
    }

    [Fact]
    public void MarkRead_AlreadyRead_Throws()
    {
        var notification = NewNotification();
        notification.MarkRead(Now);

        Assert.Throws<InvalidOperationException>(() => notification.MarkRead(Now));
    }
}
