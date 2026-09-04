using EBOSP.Contracts.Common;
using EBOSP.Contracts.Notifications;

namespace EBOSP.Application.Notifications;

public interface INotificationService
{
    Task<PagedResult<NotificationResponse>> ListMineAsync(Guid tenantId, Guid recipientUserId, bool unreadOnly, PagedRequest request, CancellationToken cancellationToken);

    Task<NotificationResponse> MarkReadAsync(Guid tenantId, Guid recipientUserId, Guid id, CancellationToken cancellationToken);
}
